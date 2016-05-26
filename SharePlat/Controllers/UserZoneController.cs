using SharePlat.BLL;
using SharePlat.Common;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SharePlat.Web.Controllers
{
    public class UserZoneController : Controller
    {
        //个人主页
        public ActionResult Index(string userName)
        {
            if (!WebHelper.CheckLogin())  //判断登录
            {
                string url = string.Format("/UserOperation/Login?returnUrl=/{0}", userName);
                //return Redirect(url);
            }

            ChapterBLL chapterBll = new ChapterBLL();
            //查询用户发表的文章
            int userId = (int)WebHelper.GetUserIdInSession();
            int page = Convert.ToInt32(Request["pagenum"]);
            page = page <= 0 ? 1 : page;
            int pageSize = 3;
            int totalSize = chapterBll.GetChaptersByUId(userId).Count;
            List<Chapter> chapterList = chapterBll.GetPageChaptersByUId(userId, page, pageSize);
            ViewBag.UserName = userName;
            ViewBag.ChapterList = chapterList;
            ViewBag.TotalSize = totalSize;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            return View();
        }

        //新增/编辑文章页面
        public ActionResult AddOrEditChapter(string id)
        {
            string pageNum = Request["pagenum"];
            ViewBag.PageNum = pageNum == null ? 1 : Convert.ToInt32(pageNum);
            if (!WebHelper.CheckLogin())  //判断登录
            {
                string url = string.Format("/UserOperation/Login?returnUrl=/{0}", "AddOrEditChapter?id=" + id);
                //return Redirect(url);
            }

            ChapterBLL chapterBll = new ChapterBLL();
            Chapter chapter = new Chapter();
            if (string.IsNullOrEmpty(id))  //新增
            {
                return View(new Chapter());
            }
            else   //编辑
            {
                int chapterId = Convert.ToInt32(id);
                int userId = (int)WebHelper.GetUserIdInSession();
                chapter = chapterBll.GetByChapterId(chapterId);
                if (chapter != null && chapter.Id > 0 && chapter.UserId == userId)
                {
                    return View(chapter);
                }
                return View(new Chapter());
            }
        }

        //新增/编辑文章保存
        [ValidateInput(false)]
        public ActionResult SubmitAddOrEditChapter()
        {
            int chapterId = 0;
            string pageNum = Request["pageNum"];
            string id = Request["chapterId"];
            string title = Request["title"];
            string content = Request["editorValue"];
            if (!WebHelper.CheckLogin())  //判断登录
            {
                string url = string.Format("/UserOperation/Login?returnUrl=/{0}", "AddOrEditChapter?id=" + id);
                //return Redirect(url);
            }
            int userId = (int)WebHelper.GetUserIdInSession();
            DateTime now = DateTime.Now;
            ChapterBLL chapterBll = new ChapterBLL();
            if (!string.IsNullOrEmpty(id) && id != "0")  //编辑
            {
                chapterId = Convert.ToInt32(id);
                chapterBll.EditChapter(chapterId, userId, title, content);
            }
            else   //新增
            {
                chapterId = chapterBll.AddChapter(title, content, userId);
            }

            //对新增或者编辑的文章生成静态文件(.shtml)
            //.../StaticPage/[UserId]/[ChapterId].shtml
            string path = Server.MapPath("~/StaticPage");
            chapterBll.GenerateStaticChapterPage(path, userId, chapterId, title, content);

            //添加到队列,写入到文章索引库中
            RedisHelper.Enqueue("ChaptersIndex", userId + "$" + chapterId + "$" + title + "$" + content + "$" + now + "$" + "url");

            string userName = new UserBLL().GetById(userId).UserName;
            return Redirect("/" + userName + "?pagenum=" + pageNum);
        }

        public ActionResult UEditorTest()
        {
            return View();
        }
    }
}
