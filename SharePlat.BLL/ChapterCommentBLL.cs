using SharePlat.DAL;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.BLL
{
    public class ChapterCommentBLL
    {
        private readonly static ChapterCommentDAL commentDal = new ChapterCommentDAL();

        //新增一条评论
        public int AddComment(ChapterComment comment)
        {
            return Convert.ToInt32(commentDal.AddComment(comment));
        }

         //获取某个文章的所有评论
        public List<ChapterComment> GetCommentsByCId(int chapterId)
        {
            return commentDal.GetCommentsByCId(chapterId);
        }
    }
}
