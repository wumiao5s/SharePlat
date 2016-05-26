using SharePlat.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnectTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string sql = "select * from testdb";
            DataTable dt = MySqlHelper.ExecuteDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine(row["id"] + ":" + row["name"]);
            }
            Console.ReadKey();
        }
    }
}
