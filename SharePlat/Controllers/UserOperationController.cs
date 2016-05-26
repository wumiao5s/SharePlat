using BotDetect.Web;
using Newtonsoft.Json;
using SharePlat.BLL;
using SharePlat.Common;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace SharePlat.Web.Controllers
{
    public class UserOperationController : Controller
    {
        //用户注册页面
        public ActionResult Register()
        {
            return View();
        }

        //用户登录页面
        public ActionResult Login()
        {
            ViewBag.ReturnUrl = Request["ReturnUrl"];
            return View();
        }

        //激活用户请求
        public string ActiveUser()
        {
            string key  = Request["id"];
            string strUserId = Request["uid"];
            string value = RedisHelper.Get<string>(key);
            if (string.IsNullOrEmpty(value))
            {
                return "激活用户请求已过期.";
            }
            if (strUserId != value)
            {
                return "激活用户失败.";
            }
            int userId = Convert.ToInt32(strUserId);
            //激活用户操作,IsActive=1
            UserBLL userBll = new UserBLL();
            if (!userBll.ActiveUser(userId))
            {
                return "激活用户失败.";
            }
            return "激活用户成功!<a href='/UserOperation/Login'>点击登录</a>";
        }

        //请求重置重置密码页面
        public ActionResult RequireResetPassword()
        {
            return View();
        }

        //重置密码页面
        public ActionResult ResetPassword()
        {
            string key = Request["id"];
            ViewBag.Key = key;
            return View();
        }

        //用户注册提交
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult SubRegister()
        {
            string userName = Request["UserName"];
            string password = Request["passwordOne"];
            string email = Request["Email"];
            string captchaId = Request["CaptchaId"];
            string userInput = Request["UserInput"];
            string instanceId = Request["InstanceId"];

            //检查用户名,密码,邮箱   102020@qq.com.cn
            Regex regex = new Regex("^[0-9a-zA-Z._-]+[@][0-9a-zA-Z_-]+([.][a-zA-Z]+){1,2}$");
            bool result = regex.IsMatch(email);
            if (string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(email) ||
                !regex.IsMatch(email))
            {
                return Json(new { Status = "error", Msg = "data error" }, JsonRequestBehavior.AllowGet);
            }
            //检查验证码
            if (!Captcha.AjaxValidate(captchaId, userInput, instanceId))
            {
                return Json(new { Status = "error", Msg = "验证码错误" }, JsonRequestBehavior.AllowGet);
            }

            UserBLL userBll = new UserBLL();
            //判断用户名是否存在
            bool exist = userBll.IsUserNameExist(userName);
            if (exist)
            {
                return Json(new { Status = "error", Msg = "用户名已存在" }, JsonRequestBehavior.AllowGet);
            }

            //新增用户,发送激活邮件
            int userId = userBll.AddUser(userName, password, email);
            string guid = Guid.NewGuid().ToString();
            string url = string.Format("http://localhost:5555/UserOperation/ActiveUser?id={0}&uid={1}",guid,userId);
            string body = string.Format("请点击以下链接进行用户激活:<br/><a href='{0}'>{0}</a>", url);
            EmailModel model = new EmailModel()
            {
                MailTo = email,
                Subject = "SharePlat用户激活邮件",
                Body = body
            };
            string data = JsonConvert.SerializeObject(model);
            RedisHelper.Enqueue("sendActiveEmail", data);  //使用消息队列发送激活邮件
            RedisHelper.Set<string>(guid, userId.ToString(), 10);     //设置激活有效期
            return Json(new { Status = "ok", Msg = "" }, JsonRequestBehavior.AllowGet);
        }

        //用户登录提交
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult SubLogin()
        {
            string userName = Request["input1"];
            string password = Request["input2"];
            string captchaId = Request["captchaId"];
            string captchaInstanceId = Request["captchaInstanceId"];
            string captchaUserInput = Request["captchaUserInput"];

            //数据验证
            if (string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(password))
            {
                return Json(new { Status = "error", Msg = "data error" }, JsonRequestBehavior.AllowGet);
            }

            //检查验证码
            if (!Captcha.AjaxValidate(captchaId, captchaUserInput, captchaInstanceId))
            {
                return Json(new { Status = "error", Msg = "验证码错误" }, JsonRequestBehavior.AllowGet);
            }

            //登录校验
            UserBLL userBll = new UserBLL();
            int userId;
            if (!userBll.CheckUserLogin(userName, password, out userId))
            {
                return Json(new { Status = "error", Msg = "用户名或者密码错误" }, JsonRequestBehavior.AllowGet);
            }

            //登录成功,保存登录状态
            WebHelper.StoreUserIdInSession(userId);
            return Json(new { Status = "ok", Msg = "" }, JsonRequestBehavior.AllowGet);
        }

        //提交重置密码请求
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult SubmitRequireResetPassword()
        {
            /*userName: userName, email: email, captchaCode: captchaCode, captchaId: captchaId, instanceId: instanceId*/
            string userName = Request["userName"];
            string email = Request["email"];
            string captchaCode = Request["captchaCode"];
            string captchaId = Request["captchaId"];
            string instanceId = Request["instanceId"];

            //数据验证
            Regex regex = new Regex("^[0-9a-zA-Z._-]+[@][0-9a-zA-Z_-]+([.][a-zA-Z]+){1,2}$");
            if (string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(email) ||
                !regex.IsMatch(email))
            {
                return Json(new { Status = "error", Msg = "data error" }, JsonRequestBehavior.AllowGet);
            }

            //校验验证码
            if (!Captcha.AjaxValidate(captchaId, captchaCode, instanceId))
            {
                return Json(new { Status = "error", Msg = "验证码错误" }, JsonRequestBehavior.AllowGet);
            }

            //校验数据正确性
            User user = new UserBLL().GetByUserName(userName);
            if (user == null || user.Id <= 0 || user.Email != email)
            {
                return Json(new { Status = "error", Msg = "用户名与邮箱不匹配" }, JsonRequestBehavior.AllowGet);
            }

            //发送重置密码邮件
            string guid = Guid.NewGuid().ToString();
            string url = "http://localhost:5555/UserOperation/ResetPassword?id=" + guid;
            string body = string.Format("请点击以下链接进行密码重置:<br/><a href='{0}'>{0}</a>", url);
            EmailModel model = new EmailModel()
            {
                MailTo = email,
                Subject = "SharePlat用户密码重置邮件",
                Body = body
            };
            string data = JsonConvert.SerializeObject(model);
            RedisHelper.Enqueue("sendResetPwdEmail", data);        //使用Redis消息队列发送密码重置邮件
            RedisHelper.Set<string>(guid, user.Id.ToString(),20);   //设置重置邮件有效期
            return Json(new { Status = "ok", Msg = "" }, JsonRequestBehavior.AllowGet);
        }

        //提交重置密码
        public JsonResult SubmitResetPassword()
        {
            string key  = Request["key"];
            string newPassword = Request["password"];
            string strUserId = RedisHelper.Get<string>(key);
            if (string.IsNullOrEmpty(strUserId))
            {
                return Json(new { Status = "error", Msg = "重置密码请求已过期,请重新请求重置密码" }, JsonRequestBehavior.AllowGet);
            }
            int userId = Convert.ToInt32(strUserId);
            //更新userId的密码
            UserBLL userBll = new UserBLL();
            if (!userBll.ResetPasswordByUId(userId, newPassword))
            {
                return Json(new { Status = "error", Msg = "重置密码失败" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Status = "ok", Msg = "" }, JsonRequestBehavior.AllowGet);
        }

        //检查用户名是否可用
        public ActionResult CheckUserNameAvaliable()
        {
            string userName = Request["UserName"];  //UserName
            UserBLL userBll = new UserBLL();
            bool exist = userBll.IsUserNameExist(userName);
            string ret = exist ? "exist" : "";
            return Json(new { d = ret }, JsonRequestBehavior.AllowGet);
        }

        //检查邮箱是否可用
        public ActionResult CheckEmailAvaliable()
        {
            string email = Request["Email"];
            UserBLL userBll = new UserBLL();
            bool exist = userBll.IsEmailExist(email);
            string ret = exist ? "exist" : "";
            return Json(new { d = ret }, JsonRequestBehavior.AllowGet);
        }

        //检查是否登录
        public JsonResult CheckLogin()
        {
            bool result = WebHelper.CheckLogin();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
