
using BotDetect;
using BotDetect.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SharePlat.Web
{
    public class WebHelper
    {
        private static readonly string LOGINSTATE_USERID = "LoginState_UserId";

        /// <summary>
        /// 检查是否登录
        /// </summary>
        /// <returns></returns>
        public static bool CheckLogin()
        {
            int? userId = GetUserIdInSession();
            return (userId != null && userId.Value > 0);
        }

        /// <summary>
        /// 获取DotDetect验证码对象
        /// </summary>
        /// <param name="captChaId">验证码Id,用于标识一个验证码</param>
        /// <param name="inputId">验证码输入框的id</param>
        /// <param name="width">验证码图片宽度</param>
        /// <param name="height">验证码图片高度</param>
        /// <returns></returns>
        public static MvcCaptcha GetCaptcha(string captChaId, string inputId, int width = 250, int height = 50)
        {
            // create the control instance
            MvcCaptcha mvcCaptcha = new MvcCaptcha(captChaId);  //captChaId:用于标识一个验证码
            // set up client-side processing of the Captcha code input textbox
            mvcCaptcha.UserInputID = inputId;    //验证码输入框的id
            mvcCaptcha.HelpLinkEnabled = false;  //页面不显示dot detect帮助链接
            mvcCaptcha.ImageSize = new Size(width, height);
            return mvcCaptcha;
        }

        //用户id保存到session中
        public static void StoreUserIdInSession(int userId)
        {
            HttpContext context = HttpContext.Current;
            context.Session[LOGINSTATE_USERID] = userId;
        }

        //获得session中的用户id
        public static int? GetUserIdInSession()
        {
            HttpContext context = HttpContext.Current;
            int? userId = (int?)context.Session[LOGINSTATE_USERID];
            return userId;
        }

    }
}