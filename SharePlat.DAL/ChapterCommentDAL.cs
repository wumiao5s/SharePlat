using MySql.Data.MySqlClient;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.DAL
{
    public class ChapterCommentDAL
    {
        //新增一条评论
        public object AddComment(ChapterComment comment)
        {
            string sql = "insert ChapterComments(Comment,PostTime,ChapterId,UserId) values(@Comment,@PostTime,@ChapterId,@UserId);select  @@Identity;";
            return MySqlHelper.ExecuteScalar(sql, new MySqlParameter[]
            {
                 new MySqlParameter("@Comment",comment.Comment),
                 new MySqlParameter("@PostTime",comment.PostTime),
                 new MySqlParameter("@ChapterId",comment.ChapterId),
                 new MySqlParameter("@UserId",comment.UserId),
            });
        }

        //获取某个文章的所有评论
        public List<ChapterComment> GetCommentsByCId(int chapterId)
        {
            List<ChapterComment> comments = new List<ChapterComment>();
            string sql = "select * from ChapterComments A left join Users B on A.UserId = B.Id where ChapterId=@chapterId";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql, new MySqlParameter("@chapterId", chapterId));
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    comments.Add(RowToComment(row));
                }
            }
            return comments;
        }

        private ChapterComment RowToComment(DataRow row)
        {
            ChapterComment comment = new ChapterComment();
            comment.ChapterId = (int)row["ChapterId"];
            comment.Comment = (string)row["Comment"];
            comment.Id = (int)row["Id"];
            comment.PostTime = (DateTime)row["PostTime"];
            comment.UserId = (int)row["UserId"];
            comment.UserName = (string)row["UserName"];
            return comment;
        }
    }
}
