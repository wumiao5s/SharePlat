using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json;
using PanGu;
using SharePlat.BLL;
using SharePlat.Common;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SharePlat.Web.Controllers
{
    public class ChaptersController : Controller
    {
        //显示文章列表
        public ActionResult ChapterList()
        {
            ChapterBLL chapterBll = new ChapterBLL();
            string strPageNum = Request["pagenum"];
            int pageNum = string.IsNullOrEmpty(strPageNum) ? 1 : Convert.ToInt32(strPageNum);
            int pageSize = 10;
            int totalSize = 0;

            //缓存总条数
            string cacheKey = "ChapterList.TotalSize." + DateTime.Today.Day;
            totalSize = RedisHelper.Get<int>(cacheKey);
            if (totalSize <= 0)
            {
                totalSize = chapterBll.GetChapterCountToday(DateTime.Today);
                RedisHelper.Set<int>(cacheKey, totalSize, 1);
            }

            //分页获取当天数据
            List<Chapter> chapterList = chapterBll.GetPageChaptersToday(DateTime.Today, pageNum, pageSize);
            ViewBag.TotalSize = totalSize;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = pageNum;
            ViewBag.ChapterList = chapterList;
            return View();
        }

        //获取文章的踩/赞数
        public JsonResult GetCaiZanCount()
        {
            int caiCount = 0;
            int zanCount = 0;
            int chapterId = Convert.ToInt32(Request["chapterId"]);
            ChapterBLL chapterBll = new ChapterBLL();
            Chapter chapter = chapterBll.GetByChapterId(chapterId);
            caiCount = chapter.CaiCount;
            zanCount = chapter.ZanCount;
            return Json(new { CaiCount = caiCount, ZanCount = zanCount }, JsonRequestBehavior.AllowGet);
        }

        //文章踩/赞点击记数
        public JsonResult ZanCaiClick()
        {
            string chapterId = Request["chapterId"];
            string action = Request["action"];
            //先判断是否登录
            if (!WebHelper.CheckLogin())
            {
                return Json(new
                {
                    Status = "error",
                    Msg = "请先<a href='javascript:void(0);' onclick='login()'>登录</a>",
                    JsonRequestBehavior.AllowGet
                });
            }
            //判断是否踩/赞过
            string key = "ZanCaiClick." + chapterId;
            if (RedisHelper.Get<string>(key) != null)
            {
                return Json(new
                {
                    Status = "error",
                    Msg = "您已经推荐过",
                    JsonRequestBehavior.AllowGet
                });
            }
            RedisHelper.Set<string>(key, "1", 10); //(10分钟之内只能允许踩/赞1次)

            //使用Redis消息队列对文章的踩/赞数进行更新   
            RedisHelper.Enqueue("ZanCaiClick", chapterId + "|" + action);  // 20|zan  21|cai
            return Json(new { Status = "ok", Msg = "", JsonRequestBehavior.AllowGet });
        }

        //获取文章的阅读数(Redis数据持久化)
        public JsonResult GetViewCount()
        {
            int chapterId = Convert.ToInt32(Request["id"]);
            string key = "GetViewCount." + chapterId;
            int count = RedisHelper.Get<int>(key);
            return Json(count, JsonRequestBehavior.AllowGet);
        }

        //增加文章的阅读数(Redis数据持久化)
        public JsonResult IncreaseViewCount()
        {
            string cookieName = "IncreaseViewCount";
            if (Request.Cookies[cookieName] != null)
            {
                return Json("0", JsonRequestBehavior.AllowGet);
            }
            Response.Cookies.Add(new HttpCookie(cookieName, "1"));  //标记

            int chapterId = Convert.ToInt32(Request["id"]);
            string key = "GetViewCount." + chapterId;
            int count = RedisHelper.Get<int>(key) + 1;
            RedisHelper.Set<int>(key, count);
            return Json("1", JsonRequestBehavior.AllowGet);
        }

        //异步提交评论
        public JsonResult SubComment()
        {
            //先判断登录
            int? userId = WebHelper.GetUserIdInSession();
            if (userId == null || userId.Value <= 0)
            {
                return Json(new { Status = "error", Data = "" }, JsonRequestBehavior.AllowGet);
            }
            //新增评论
            ChapterCommentBLL commentBll = new ChapterCommentBLL();
            DateTime now = DateTime.Now;
            ChapterComment comment = new ChapterComment();
            comment.ChapterId = Convert.ToInt32(Request["chapterId"]);
            comment.Comment = Request["comment"];
            comment.PostTime = now;
            comment.UserId = userId.Value;
            int id = commentBll.AddComment(comment);
            User user = new UserBLL().GetById(userId.Value);
            return Json(new { Status = "ok",
                              Id = id,
                              Time = now.ToString("yyyy-MM-ddTHH:mm:ss"),  //2016-05-16T22:34:28
                              UserName = user.UserName,
            }, JsonRequestBehavior.AllowGet);
        }

        //获取文章的评论
        public JsonResult GetComments()
        {
            int chapterId = Convert.ToInt32(Request["id"]);
            ChapterCommentBLL commentBll = new ChapterCommentBLL();
            List<ChapterComment> commentList = commentBll.GetCommentsByCId(chapterId);
            string jsonData = JsonConvert.SerializeObject(commentList);
            return Json(new { count = commentList.Count, comments = jsonData }, JsonRequestBehavior.AllowGet);
        }

        //文章分页搜索
        public ActionResult SearchPage()
        {
            string keywords = Request["keywords"];
            string strPageNum = Request["pagenum"];
            string searchType = Request["searchType"];  //c:chapters   b:blogs
            int pageNum = 1;
            if (!string.IsNullOrEmpty(strPageNum))
            {
                pageNum = Convert.ToInt32(strPageNum);
            }

            string indexerPath;
            if (searchType == "b" || searchType == "找知识")
            {
                indexerPath = "d:/BlogsIndexer";
            }
            else if (searchType == "c" || searchType == "找文章")
            {
                indexerPath = "d:/ChaptersIndexer";
            }
            else
            {
                throw new Exception("SearchType Error.");
            }
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexerPath), new NoLockFactory());
            IndexReader reader = IndexReader.Open(directory, true);
            IndexSearcher searcher = new IndexSearcher(reader);
            PhraseQuery query = new PhraseQuery();
            query.SetSlop(20);  //关键词之间的间隔
            //BooleanQuery booleanQuery = new BooleanQuery();

            //使用盘古分词算法进行分词
            Segment segment = new Segment();
            var wordInfos = segment.DoSegment(keywords);
            foreach (var wordInfo in wordInfos)
            {
                query.Add(new Term("content", wordInfo.Word));   //"content"索引中包含关键词的
            }
            TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
            searcher.Search(query, null, collector);

            //分页查询数据
            int pageSize = 3;
            ScoreDoc[] docs = collector.TopDocs((pageNum - 1) * pageSize, pageSize).scoreDocs;
            List<Chapter> chapters = new List<Chapter>();
            for (int i = 0; i < docs.Length; i++)
            {
                Chapter chapter = new Chapter();
                int docId = docs[i].doc;
                float score = docs[i].score;
                Document doc = searcher.Doc(docId);
                chapter.Title = doc.Get("title");
                chapter.Content = GetHightContent(doc.Get("content"), keywords);
                chapter.UserId = Convert.ToInt32(doc.Get("userId"));
                chapter.Id = Convert.ToInt32(doc.Get("chapterId"));
                string temp = doc.Get("time");
                chapter.PostDate = Convert.ToDateTime(doc.Get("time"));
                chapter.BlogUrl = doc.Get("blogUrl");
                chapters.Add(chapter);
            }
            ViewBag.Keywords = Server.UrlEncode(keywords);
            ViewBag.CurrentPage = pageNum;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalSize = collector.GetTotalHits();
            ViewBag.ChapterList = chapters;
            ViewBag.IsSearch = true;
            ViewBag.SearchType = searchType;
            return View("ChapterList");
        }

        /// <summary>
        /// 获取关键词高亮的摘要内容
        /// </summary>
        /// <param name="content">原始内容</param>
        /// <param name="keywords">关键词</param>
        /// <returns></returns>
        private string GetHightContent(string content, string keywords)
        {
            PanGu.HighLight.SimpleHTMLFormatter simpleHTMLFormatter =
                new PanGu.HighLight.SimpleHTMLFormatter("<span style='color:red'>", "</span>");
            PanGu.HighLight.Highlighter highlighter = new PanGu.HighLight.Highlighter(simpleHTMLFormatter, new Segment());
            highlighter.FragmentSize = 100;  //设置每个摘要段的字符数
            return highlighter.GetBestFragment(keywords, content);  //获取最匹配的摘要段
        }

    }

    public class SearchResult
    {
        public string UserId { get; set; }
        public string ChapterId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
