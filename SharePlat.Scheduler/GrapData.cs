using HtmlAgilityPack;
using Quartz;
using SharePlat.BLL;
using SharePlat.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharePlat.Scheduler
{
    public class GrapData : IJob
    {
        //抓取数据
        public void Execute(JobExecutionContext context)
        {
            GrapCnBlogs();
        }

        //抓取博客园的数据
        private void GrapCnBlogs()
        {
            string url = "http://www.cnblogs.com/candidate/";
            WebClient client = new WebClient();
            string html = Encoding.UTF8.GetString(client.DownloadData(url));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode;
            HtmlNodeCollection post_body_nodes = node.SelectNodes("//div[@class='post_item_body']");
            foreach (var item in post_body_nodes)
            {
                HtmlNode aNode = item.SelectSingleNode("h3/a");
                if (aNode == null)
                {
                    continue;
                }
                string blogUrl = aNode.GetAttributeValue("href", "");
                //http://www.cnblogs.com/gavinsp/p/5513536.html
                Regex regex = new Regex("http://www.cnblogs.com/.*/p/([0-9]+).html");
                Match match = regex.Match(blogUrl);
                int blogId;
                if (match != null && match.Groups != null)
                {
                    Group group = match.Groups[1];
                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string strBlogId = group.Value;
                    if (!int.TryParse(strBlogId, out blogId))
                    {
                        continue;
                    }
                    string title = aNode.InnerText;
                    string content = GetBlogContent(blogUrl);
                    string path = CommonHelper.GetAppSetting("StaticPageDirectory");
                    new ChapterBLL().GenerateStaticChapterPage(path, 0, blogId, title, content);
                    RedisHelper.Enqueue("BlogsIndex", 0 + "$" + strBlogId + "$" + title + "$" + content + "$" + now + "$" + blogUrl);
                    Console.WriteLine("title:" + title);
                }
                
                
            }

        }

        //获取博客正文内容
        private string GetBlogContent(string blogUrl)
        {
            //<div class="postBody">...</div>
            WebClient client = new WebClient();
            string html = Encoding.UTF8.GetString(client.DownloadData(blogUrl));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode;
            HtmlNode bodyNode = node.SelectSingleNode("//div[@class='postBody']");
            if (bodyNode != null)
            {
                string bodyContent = bodyNode.InnerText;
                return bodyContent;
            }
            return string.Empty;
        }
    }
}
