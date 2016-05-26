using SharePlat.Common;
using SharePlat.DAL;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.BLL
{
    public class UserBLL
    {
        private readonly UserDAL userDal = new UserDAL();

        //激活用户,设置IsActive=1
        public bool ActiveUser(int userId)
        {
            return userDal.ActiveUser(userId) == 1;
        }

        //根据用户id重置密码
        public bool ResetPasswordByUId(int userId, string newPassword)
        {
            string md5Password = CommonHelper.CalcMD5(newPassword);
            return userDal.ResetPasswordByUId(userId, md5Password) == 1;
        }

        //根据用户名查询用户
        public User GetByUserName(string userName)
        {
            return userDal.GetByUserName(userName);
        }

        //根据用户Id查询用户
        public User GetById(int userId)
        {
            return userDal.GetById(userId);
        }

        //判断用户名是否已经存在 true:存在  false:不存在
        public bool IsUserNameExist(string userName)
        {
            User user = userDal.GetByUserName(userName);
            if (user != null && user.Id > 0)
            {
                return true;
            }
            return false;
        }

        //检查用户登录
        public bool CheckUserLogin(string userName, string password, out int userId)
        {
            userId = 0;
            User user = userDal.GetByUserName(userName);
            if (user != null && user.Id > 0 && user.IsActive)
            {
                userId = user.Id;
                string md5Password = CommonHelper.CalcMD5(password);
                return md5Password == user.Password;
            }
            return false;
        }

        //根据邮箱查询用户
        public User GetByEmail(string email)
        {
            return userDal.GetByEmail(email);
        }

        //判断邮箱是否已经存在 true:存在  false:不存在
        public bool IsEmailExist(string email)
        {
            User user = userDal.GetByEmail(email);
            if (user != null && user.Id > 0)
            {
                return true;
            }
            return false;
        }

        //新增用户
        public int AddUser(string userName, string password, string email)
        {
            User user = new User();
            user.Email = email;
            user.IsActive = false;
            user.Password = CommonHelper.CalcMD5(password);   //MD5加密
            user.RegisterDate = DateTime.Now;
            user.UserName = userName;
            return userDal.AddUser(user);
        }
    }
}
