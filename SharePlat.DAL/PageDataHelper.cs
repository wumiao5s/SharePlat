using MySql.Data.MySqlClient;
using SharePlat.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharePlat.DAL
{
    public class PageDataHelper
    {
        // 分页获取列表数据
        public static List<T> GetDataListByPage<T>(string dataTableName, string strWhere, int startIndex, int dataNum)
            where T : new()
        {
            //select * from xxx where ... limit startIndex
            List<T> dataList = new List<T>();
            string sql = string.Format(@"select * from {0} {1} limit @startIndex,@dataNum",
                dataTableName, strWhere);
            DataTable dt = MySqlHelper.ExecuteDataTable(sql,
                new MySqlParameter("@startIndex", startIndex),
                new MySqlParameter("@dataNum", dataNum));
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    dataList.Add(RowToData<T>(row));
                }
            }
            return dataList;
        }

        /// <summary>
        /// DataRow转对象数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <returns></returns>
        private static T RowToData<T>(DataRow row) where T : new()
        {
            Type type = typeof(T);
            object obj = Activator.CreateInstance(type);
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                Attribute ingoreAttribute =
                    property.GetCustomAttribute(typeof(IngorePropertyAttribute));
                if (ingoreAttribute == null)
                {
                    property.SetValue(obj, row[property.Name]);
                }
            }
            return (T)obj;
        }
    }
}
