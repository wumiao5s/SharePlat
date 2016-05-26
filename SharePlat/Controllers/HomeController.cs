using BotDetect.Web;
using BotDetect.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace SharePlat.Web.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Index/

        public ActionResult Index()
        {
            int? userId = WebHelper.GetUserIdInSession();
            if (userId == null || userId.Value <= 0)  //判断是否登录
            {
                return Redirect("/UserOperation/Login");
            }

            return View();
        }

        public ActionResult ValidCodeTest()
        {
            return View();
        }

        //
        // GET: /Example/CheckCaptcha/
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public JsonResult CheckCaptcha(string captchaId, string instanceId, string userInput)
        {
            bool ajaxValidationResult = Captcha.AjaxValidate(captchaId, userInput, instanceId);
            return Json(ajaxValidationResult, JsonRequestBehavior.AllowGet);
        }

    }
}
