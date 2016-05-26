using SharePlat.DAL;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.BLL
{
    public class ChapterBLL
    {
        private static readonly ChapterDAL chapterDal = new ChapterDAL();

        //"踩"文章
        public int CaiChapter(int id, int count = 1)
        {
            return chapterDal.CaiChapter(id, count);
        }

        //"赞"文章
        public int ZanChapter(int id, int count = 1)
        {
            return chapterDal.ZanChapter(id, count);
        }

        //.../StaticPage/[UserId]/[ChapterId].shtml
        public void GenerateStaticChapterPage(string path ,int userId, int chapterId, string title, string content)
        {
            string directoryPath = Path.Combine(path,userId.ToString());
            string staticPagePath = Path.Combine(path, "ChapterTemplate.html");
            string fullPath = Path.Combine(path, userId.ToString(), chapterId + ".shtml");
            if (!Directory.Exists(directoryPath))  //创建目录
            {
                Directory.CreateDirectory(directoryPath);
            }
            string html = File.ReadAllText(staticPagePath, Encoding.UTF8);
            html = html.Replace("[ChapterTitle]", title).Replace("[ChapterBody]",content).Replace("[id]",
                chapterId.ToString()).Replace("[title]",title);
            File.WriteAllText(fullPath, html, Encoding.UTF8);
        }

        //编辑文章
        public int EditChapter(int id, int userId, string title, string content)
        {
            return chapterDal.EditChapter(id, userId, title, content);
        }

         //新增文章
        public int AddChapter(string title, string content, int userId)
        {
            Chapter chapter = new Chapter();
            chapter.Content = content;
            chapter.PostDate = DateTime.Now;
            chapter.Title = title;
            chapter.UserId = userId;
            return Convert.ToInt32( chapterDal.AddChapter(chapter));
        }

        //根据用户id查询用户的所有文章
        public List<Chapter> GetChaptersByUId(int userId)
        {
            return chapterDal.GetChaptersByUId(userId);
        }

        //根据用户id分页获取用户的文章
        public List<Chapter> GetPageChaptersByUId(int userId, int pageNum, int pageSize)
        {
            //pageNum=2, pageSize=5   limit 5,5
            int startIndex = (pageNum - 1) * pageSize;
            return chapterDal.GetPageChaptersByUId(userId, startIndex, pageSize);
        }

         //分页获取当天数据
        public List<Chapter> GetPageChaptersToday(DateTime today, int pageNum, int pageSize)
        {
            int startIndex = (pageNum - 1) * pageSize;
            return chapterDal.GetPageChaptersToday(today, startIndex, pageSize);
        }

        //获取当天文章的总数量
        public int GetChapterCountToday(DateTime today)
        {
            return Convert.ToInt32( chapterDal.GetChapterCountToday(today));
        }

         //根据文章id获取文章
        public Chapter GetByChapterId(int chapterId)
        {
            return chapterDal.GetByChapterId(chapterId);
        }
    }
}
