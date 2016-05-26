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
    public class ChapterDAL
    {
        //"踩"文章
        public int CaiChapter(int id, int count = 1)
        {
            string sql = "update Chapters set CaiCount=CaiCount+" + count;
            return MySqlHelper.ExecuteNonQuery(sql);
        }

        //"赞"文章
        public int ZanChapter(int id, int count = 1)
        {
            string sql = "update Chapters set ZanCount=ZanCount+" + count;
            return MySqlHelper.ExecuteNonQuery(sql);
        }

        //编辑文章
        public int EditChapter(int id, int userId, string title, string content)
        {
            string sql = "update Chapters set Title=@title,Content=@content where Id=@id and UserId=@userId";
            return MySqlHelper.ExecuteNonQuery(sql,new MySqlParameter[]
            {
                new MySqlParameter("@title",title),
                new MySqlParameter("@content",content),
                new MySqlParameter("@id",id),
                new MySqlParameter("@userId",userId),
            });
        }

        //新增文章
        public object AddChapter(Chapter chapter)
        {
            string sql = @"insert into Chapters(Title,Content,PostDate,CaiCount,ZanCount,UserId) 
                            values(@Title,@Content,@PostDate,@CaiCount,@ZanCount,@UserId);select @@identity;";
            return MySqlHelper.ExecuteScalar(sql, new MySqlParameter[] 
            {
                new MySqlParameter("@Title",chapter.Title),
                new MySqlParameter("@Content",chapter.Content),
                new MySqlParameter("@PostDate",chapter.PostDate),
                new MySqlParameter("@CaiCount",chapter.CaiCount),
                new MySqlParameter("@ZanCount",chapter.ZanCount),
                new MySqlParameter("@UserId",chapter.UserId),
            });
        }

        //根据用户id查询用户的所有文章
        public List<Chapter> GetChaptersByUId(int userId)
        {
            List<Chapter> chapters = new List<Chapter>();
            string sql = @"select u.UserName UserName,c.Id Id,c.Title Title,c.Content Content,c.PostDate PostDate,
                        c.CaiCount CaiCount,C.ZanCount ZanCount,c.UserId UserId
                        from Users u 
                        right join Chapters c on u.Id = c.UserId where u.Id=@UserId";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql, new MySqlParameter("@UserId",userId));
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    chapters.Add(RowToChapter(row));
                }
            }
            return chapters;
        }

        //根据用户id分页获取数据
        public List<Chapter> GetPageChaptersByUId(int userId, int startIndex, int dataNum)
        {
            return PageDataHelper.GetDataListByPage<Chapter>("Chapters", "where UserId=" + userId, startIndex, dataNum);
        }

        //分页获取当天数据
        public List<Chapter> GetPageChaptersToday(DateTime today, int startIndex, int dataNum)
        {
            return PageDataHelper.GetDataListByPage<Chapter>("Chapters", "where PostDate>'" + today.ToString() +"'", startIndex, dataNum);
        }

        //获取当天文章的总数量
        public object GetChapterCountToday(DateTime today)
        {
            string sql = "select count(Id) from Chapters where PostDate>@today";
            return MySqlHelper.ExecuteScalar(sql, new MySqlParameter("@today",today));
        }

        //根据文章id获取文章
        public Chapter GetByChapterId(int chapterId)
        {
            Chapter chapter = new Chapter();
            string sql = "select * from Chapters where Id=" + chapterId;
            DataTable dt = MySqlHelper.ExecuteDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows.Count > 1)
                {
                    throw new Exception("查询到多个文章.");
                }
                chapter = RowToChapter(dt.Rows[0]);
            }
            return chapter;
        }

        private Chapter RowToChapter(DataRow dataRow)
        {
            Chapter chapter = new Chapter();
            chapter.CaiCount = (int)dataRow["CaiCount"];
            chapter.Content = (string)dataRow["Content"];
            chapter.Id = (int)dataRow["Id"];
            chapter.PostDate = (DateTime)dataRow["PostDate"];
            chapter.Title = (string)dataRow["Title"];
            chapter.UserId =(int)dataRow["UserId"];
            chapter.ZanCount=(int)dataRow["ZanCount"];
            return chapter;
        }
    }
}
