using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SendMailTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (SendEmail("15950060547@163.com", "subject", "test"))
            {
                Console.WriteLine("ok");
            }
           
            Console.ReadKey();
        }

        public static bool SendEmail(string mailTo, string subject, string body, bool isBodyHtml = false)
        {
            //走配置
            string host = "smtp.qq.com";
            int port = 587;
            string userName = "1032131483@qq.com";
            string password = "waceygarcyusbfaj";  //qq使用授权码

            //设置发送邮件的信息
            using (MailMessage mailMsg = new MailMessage())
            {
                mailMsg.From = new MailAddress(userName, "miaosha5s", Encoding.UTF8);
                mailMsg.To.Add(mailTo);
                mailMsg.Subject = subject;
                mailMsg.SubjectEncoding = Encoding.UTF8;
                mailMsg.Body = body;
                mailMsg.BodyEncoding = Encoding.UTF8;
                mailMsg.IsBodyHtml = isBodyHtml;
                mailMsg.Priority = MailPriority.Normal;
                using (SmtpClient client = new SmtpClient())
                {
                    client.Host = host;
                    client.Port = port;
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(userName, password);//用户名和密码
                    client.Send(mailMsg);
                }
            }
            return true;
        }
    }
}
