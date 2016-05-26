using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.Common
{
    /// <summary>
    /// Redis帮助类
    /// </summary>
    public class RedisHelper
    {
        public const string USERID_SESSIONID = "UserId_SessionId.";

        private readonly static string Prefix = "RuPengWang.";

        /// <summary>
        /// 往消息队列中放入数据
        /// </summary>
        /// <param name="listId">队列Id</param>
        /// <param name="value">数据</param>
        public static void Enqueue(string listId, string value)
        {
            listId = Prefix + listId;
            using (IRedisClient client = RedisManager.ClientManager.GetClient())
            {
                client.EnqueueItemOnList(listId, value);
            }
            return;
        }

        /// <summary>
        /// 从消息队列中取出数据
        /// </summary>
        /// <param name="listId">队列Id</param>
        /// <returns></returns>
        public static string Dequeue(string listId)
        {
            listId = Prefix + listId;
            using (IRedisClient client = RedisManager.ClientManager.GetClient())
            {
                return client.DequeueItemFromList(listId);
            }
        }

        /// <summary>
        /// 从消息队列中取出数据
        /// </summary>
        /// <param name="redisClient"></param>
        /// <param name="listId"></param>
        /// <returns></returns>
        public static string Dequeue(IRedisClient redisClient, string listId)
        {
            listId = Prefix + listId;
            return redisClient.DequeueItemFromList(listId);
        }

        /// <summary>
        /// Redis写
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="cacheTime">缓存时间(分钟)</param>
        /// <returns></returns>
        public static bool Set<T>(string key, T value, int cacheTime = 0)
        {
            key = Prefix + key;
            using (IRedisClient client = RedisManager.ClientManager.GetClient())
            {
                if (cacheTime <= 0)
                {
                    return client.Set<T>(key, value);
                }
                return client.Set<T>(key, value, DateTime.Now.AddMinutes(cacheTime));
            }
        }

        /// <summary>
        /// Redis读
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            key = Prefix + key;
            using (IRedisClient client = RedisManager.ClientManager.GetClient())
            {
                return client.Get<T>(key);
            }
        }
    }

    /// <summary>
    /// Redis管理类
    /// </summary>
    public class RedisManager
    {
        public static PooledRedisClientManager ClientManager { get; private set; }
        static RedisManager()
        {
            RedisClientManagerConfig redisConfig = new RedisClientManagerConfig();
            redisConfig.MaxWritePoolSize = 128;
            redisConfig.MaxReadPoolSize = 128;
            ClientManager = new PooledRedisClientManager(new string[] { "127.0.0.1" }, new string[] { "127.0.0.1" }, redisConfig);
        }
    }
}
