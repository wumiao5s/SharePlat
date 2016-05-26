using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using SharePlat.BLL;
using SharePlat.Common;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharePlat.Scheduler
{
    class Program
    {
        public static bool IsRunning;

        static void Main(string[] args)
        {
            IsRunning = true;
            Console.WriteLine("running...");
            GrapDataJob();
            Task task1 = Task.Factory.StartNew(() => SendActiveEmailMQ());
            Task task2 = Task.Factory.StartNew(() => SendResetPwdMQ());
            Task task3 = Task.Factory.StartNew(() => CaiZanClickMQ());
            Task task4 = Task.Factory.StartNew(() => WriteToChapterIndexer());
            Task task5 = Task.Factory.StartNew(() => WriteToBlogIndexer());
            Console.ReadKey();
            IsRunning = false;
            Console.WriteLine("end.");
        }

        //发送激活邮件消息队列(数据消费方)
        private static void SendActiveEmailMQ()
        {
            //数据消费者
            while (IsRunning)
            {
                string data = RedisHelper.Dequeue("sendActiveEmail");
                if (data != null)
                {
                    EmailModel model = JsonConvert.DeserializeObject<EmailModel>(data);
                    Console.WriteLine("发送激活邮件:" + model.MailTo);
                    CommonHelper.SendEmail(model.MailTo, model.Subject, model.Body, true);
                    Console.WriteLine("发送激活邮件完成");
                    Console.WriteLine();
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        //发送用户密码重置邮件消息队列(数据消费方)
        private static void SendResetPwdMQ()
        {
            //数据消费者
            while (IsRunning)
            {
                string data = RedisHelper.Dequeue("sendResetPwdEmail");
                if (data != null)
                {
                    EmailModel model = JsonConvert.DeserializeObject<EmailModel>(data);
                    Console.WriteLine("发送激活邮件:" + model.MailTo);
                    CommonHelper.SendEmail(model.MailTo, model.Subject, model.Body, true);
                    Console.WriteLine("发送激活邮件完成");
                    Console.WriteLine();
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        //踩/赞文章消息队列
        private static void CaiZanClickMQ()
        {
            ChapterBLL chapterBll = new ChapterBLL();
            while(IsRunning)
            {
                string data = RedisHelper.Dequeue("ZanCaiClick");  // 20|zan  21|cai
                if (data == null)
                {
                    Thread.Sleep(200);
                    continue;
                }
                string[] strs = data.Split('|');
                int chapterId = Convert.ToInt32(strs[0]);
                string action = strs[1];
                if (action == "zan")
                {
                    chapterBll.ZanChapter(chapterId, 1);
                    Console.WriteLine(action + ":" + chapterId);
                }
                else if (action == "cai")
                {
                    chapterBll.CaiChapter(chapterId, 1);
                    Console.WriteLine(action + ":" + chapterId);
                }
            }
        }

        //写入到文章索引库中
        private static void WriteToChapterIndexer()
        {
            Indexer.IndexPath = "d:/ChaptersIndexer";
            Indexer.IsRunning = true;
            Indexer.ExecuteQueueDic.Add("ChaptersIndex", WriteToIndex);
            Indexer.Start();
            return;
        }

        //写入到博客索引库中
        private static void WriteToBlogIndexer()
        {
            BlogsIndexer.IndexPath = "d:/BlogsIndexer";
            BlogsIndexer.IsRunning = true;
            BlogsIndexer.ExecuteQueueDic.Add("BlogsIndex", WriteToIndex);
            BlogsIndexer.Start();
            return;
        }

        //添加索引
        private static void WriteToIndex(string data, IndexWriter writer)
        {
            string[] strs = data.Split(new char[] { '$' });
            string userId = strs[0];
            string chapterId = strs[1];
            string title = strs[2];
            string content = strs[3];
            string time = strs[4];
            string url = strs[5];
            writer.DeleteDocuments(new Term("chapterId", chapterId));   //先删除旧有的索引,再添加(使得同时适用于新增/更新索引功能)
            Document document = new Document();
            document.Add(new Field("userId", userId, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("chapterId", chapterId, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("title", title, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("time", time, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("blogUrl", url, Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED,  //糗事内容索引
                Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS));
            writer.AddDocument(document);
            Console.WriteLine("writed:" + data);
        }

        //抓取数据任务Job
        private static void GrapDataJob()
        {
            ISchedulerFactory sf = new StdSchedulerFactory();
            IScheduler grapDataSched = sf.GetScheduler();
            JobDetail job = new JobDetail("job1", "group1", typeof(GrapData));
            DateTime ts = TriggerUtils.GetNextGivenSecondDate(null, 1);   //1秒后开始执行第一次Job
            TimeSpan interval = TimeSpan.FromSeconds(120);
            //注意:ts参数表示的是格林尼治时间
            Trigger trigger = new SimpleTrigger("trigger1", "group1", "job1", "group1", ts, null,
                SimpleTrigger.RepeatIndefinitely, interval);
            grapDataSched.AddJob(job, true);
            grapDataSched.ScheduleJob(trigger);
            grapDataSched.Start();
        }
    }
}
