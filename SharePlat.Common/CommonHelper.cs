using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.Common
{
    public class CommonHelper
    {
        /// <summary>
        /// 生成页码的html
        /// </summary>
        /// <param name="urlFormat">超链接的格式。list.ashx?pagenum={pagenum}。地址中用{pagenum}做为当前页码的占位符</param>
        /// <param name="totalSize">总数据条数</param>
        /// <param name="pageSize">每页多少条数据 </param>
        /// <param name="currentPage">当前页的页码</param>
        /// <returns></returns>
        public static string Pager(string urlFormat, int totalSize,
            int pageSize, int currentPage)
        {
            StringBuilder sb = new StringBuilder();
            //currentPage当前页的页面。在当前页之前显示最多5个、之后显示最多5个。
            // 15,16,17,18,19,(20),21,22,23,24,25
            // 1,2,(3),4,5,6,7,8
            //一共50页, 43,44,45,46,47(48),49,50

            //总页数("天花板数 Ceiling")
            int totalPageCount = (int)Math.Ceiling((totalSize * 1.0f) / (pageSize * 1.0f));

            //在当前页面前后各最多显示5个页码
            //计算页码条中第一条的页码
            int firstPageNum = Math.Max(currentPage - 5, 1);
            //计算页码条中最后一条的页码
            int lastPageNum = Math.Min(currentPage + 5, totalPageCount);

            sb.AppendLine("<a href='" +
                urlFormat.Replace("{pagenum}", "1") + "'>首页</a>&nbsp;");
            for (int i = firstPageNum; i <= lastPageNum; i++)
            {
                string url = urlFormat.Replace("{pagenum}", i.ToString());
                if (i == currentPage)  //当前页
                {
                    sb.Append("" + i + "&nbsp;");
                }
                else
                {
                    sb.Append("<a href='" + url + "'>" + i + "</a>&nbsp;");
                }
            }
            sb.AppendLine("<a href='" +
                urlFormat.Replace("{pagenum}", totalPageCount.ToString()) + "'>末页</a>&nbsp;");
            return sb.ToString();
        }

        /// <summary>
        /// 计算给定字符串的md5植
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string CalcMD5(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return CalcMD5(bytes);
        }

        /// <summary>
        /// 计算给定字节数组的md5值
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CalcMD5(byte[] data)
        {
            using (MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider())
            {
                StringBuilder builder = new StringBuilder();
                data = provider.ComputeHash(data);
                foreach (byte b in data)
                    builder.Append(b.ToString("x2").ToLower());
                return builder.ToString();
            }
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailTo"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isBodyHtml"></param>
        /// <returns></returns>
        public static bool SendEmail(string mailTo, string subject, string body, bool isBodyHtml = false)
        {
            string host = GetAppSetting("host");
            int port = Convert.ToInt32(GetAppSetting("port"));
            string userName = GetAppSetting("userName");
            string password = GetAppSetting("password");  //qq使用授权码
            string displayName = GetAppSetting("displayName"); ;

            //设置发送邮件的信息
            using (MailMessage mailMsg = new MailMessage())
            {
                mailMsg.From = new MailAddress(userName, displayName, Encoding.UTF8);
                mailMsg.To.Add(mailTo);
                mailMsg.Subject = subject;
                mailMsg.SubjectEncoding = Encoding.UTF8;
                mailMsg.Body = body;
                mailMsg.BodyEncoding = Encoding.UTF8;
                mailMsg.IsBodyHtml = isBodyHtml;
                mailMsg.Priority = MailPriority.Normal;
                try
                {
                    using (SmtpClient client = new SmtpClient())
                    {
                        client.Host = host;
                        client.Port = port;
                        client.EnableSsl = true;   //使用安全套接字层加密连接
                        client.Credentials = new NetworkCredential(userName, password);//用户名和密码
                        client.Send(mailMsg);
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取appSettings配置项
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public static string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
