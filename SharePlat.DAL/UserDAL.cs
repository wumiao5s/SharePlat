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
    public class UserDAL
    {
        //激活用户,设置IsActive=1
        public int ActiveUser(int userId)
        {
            string sql = "update Users set IsActive=1 where id=" + userId;
            return MySqlHelper.ExecuteNonQuery(sql);
        }

        //根据用户id重置密码
        public int ResetPasswordByUId(int userId, string newPassword)
        {
            string sql = "update Users set Password=@password where Id=@id";
            return MySqlHelper.ExecuteNonQuery(sql, 
                new MySqlParameter("@password", newPassword), new MySqlParameter("@id",userId));
        }

        //根据用户名查询用户
        public User GetByUserName(string userName)
        {
            User user = new User();
            string sql = "select * from Users where UserName=@userName";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql, new MySqlParameter("@userName", userName));
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows.Count == 1)
                {
                    user = RowToUser(dt.Rows[0]);
                }
                else
                {
                    throw new Exception("查询到多个用户名相同的用户.");
                }
            }
            return user;
        }

        //根据用户Id查询用户
        public User GetById(int userId)
        {
            User user = new User();
            string sql = "select * from Users where Id=@userId";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql, new MySqlParameter("@userId", userId));
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows.Count == 1)
                {
                    user = RowToUser(dt.Rows[0]);
                }
                else
                {
                    throw new Exception("查询到多个用户Id相同的用户.");
                }
            }
            return user;
        }

        //根据邮箱查询用户
        public User GetByEmail(string email)
        {
            User user = new User();
            string sql = "select * from Users where Email=@email";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql, new MySqlParameter("@email", email));
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows.Count == 1)
                {
                    user = RowToUser(dt.Rows[0]);
                }
                else
                {
                    throw new Exception("查询到多个邮箱相同的用户.");
                }
            }
            return user;
        }

        //新增用户
        public int AddUser(User user)
        {
            string sql = "insert into Users(UserName,Password,Email,RegisterDate,IsActive) values(@UserName,@Password,@Email,@RegisterDate,@IsActive);select @@identity;";
            object obj = MySqlHelper.ExecuteScalar(sql, new MySqlParameter[] 
            {
                new MySqlParameter("@UserName",user.UserName),
                new MySqlParameter("@Password",user.Password),
                new MySqlParameter("@Email",user.Email),
                new MySqlParameter("@RegisterDate",user.RegisterDate),
                new MySqlParameter("@IsActive",user.IsActive ? 1 : 0 ),
            });
            return Convert.ToInt32(obj);
        }

        private User RowToUser(DataRow dataRow)
        {
            User user = new User();
            user.Id = (int)dataRow["Id"];
            user.Email = (string)dataRow["Email"];
            user.IsActive = Convert.ToBoolean(dataRow["IsActive"]);
            user.Password = (string)dataRow["Password"];
            user.RegisterDate = (DateTime)dataRow["RegisterDate"];
            user.UserName = (string)dataRow["UserName"];
            return user;
        }
    }
}
