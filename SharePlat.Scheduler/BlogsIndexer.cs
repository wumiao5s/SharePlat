using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Index;
using Lucene.Net.Store;
using SharePlat.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharePlat.Scheduler
{
    class BlogsIndexer
    {
        public static string IndexPath { private get; set; }

        public static bool IsRunning { private get; set; }

        private static Dictionary<string, Action<string, IndexWriter>>
            _executeQueueDic = new Dictionary<string, Action<string, IndexWriter>>();
        public static Dictionary<string, Action<string, IndexWriter>> ExecuteQueueDic
        {
            get
            {
                return _executeQueueDic;
            }
            set
            {
                _executeQueueDic = value;
            }
        }

        public static void Start()
        {
            while (IsRunning)
            {
                ExecuteDequeue();
            }
        }

        // 执行消息队列出队操作
        private static void ExecuteDequeue()
        {
            FSDirectory directory = null;
            IndexWriter writer = null;
            try
            {
                string indexPath = IndexPath;//注意和磁盘上文件夹的大小写一致，否则会报错。
                directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());
                bool isExist = IndexReader.IndexExists(directory);
                if (isExist && IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);   //开锁
                }
                writer = new IndexWriter(directory, new PanGuAnalyzer(), !isExist,
                     Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED);

                #region  不断取出数据,写入到索引库中
                while (true)
                {
                    bool hasData = false;
                    foreach (string queueName in ExecuteQueueDic.Keys)
                    {
                        string data = RedisHelper.Dequeue(queueName);
                        if (data != null)
                        {
                            hasData = true;
                            var executeMethod = ExecuteQueueDic[queueName];
                            executeMethod(data, writer);
                        }
                    }

                    if (!hasData)    //如果所有队列中都没有数据,则暂停一会儿
                    {
                        Thread.Sleep(200);
                        return;
                    }
                }
                #endregion
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
                if (writer != null)
                {
                    directory.Close();//不要忘了Close，否则索引结果搜不到
                }
            }
        }
    }
}
