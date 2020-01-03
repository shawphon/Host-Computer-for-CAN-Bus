using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANDriverLayer;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using DAL;
using System.Timers;

namespace COnHost
{
    class Program
    {
        static void Main(string[] args)
        {

            //string sql = "select * from Tab_BusLoad";
            //DataTable dt = null;
            //dt = SQLHelper.ExecuteDataTable(sql);
            //for (int i = 0; i < dt.Rows.Count; i++)
            //{
            //    Console.WriteLine("行{0}", dt.Rows[i][0]);
            //    Console.WriteLine("值{0}", dt.Rows[i][1]);

            //}
            VCI_CAN_OBJ[] recFrame = new VCI_CAN_OBJ[100];
            double timestamp = 1.1;
            int frameid = 0;
            int frametype = 0;
            int datalen = 8;
            byte a = Convert.ToByte(1);
            byte[] data = { a, a, a, a,a , a, a, a, a };
            string s = System.Text.Encoding.Default.GetString(data);
            byte[] b = System.Text.Encoding.Default.GetBytes(s);
            Console.WriteLine(System.Text.Encoding.Default.GetBytes(s));
            string sql = string.Format("INSERT INTO can0data " +
                "VALUES('{0}','{1}','{2}','{3}','{4}')", timestamp, frameid, frametype, datalen,
                System.Text.Encoding.Default.GetString(data));
            SQLHelper.ExecuteNonQuery(sql);

            sql = "select * from can0data";
            DataTable dt = SQLHelper.ExecuteDataTable(sql);

            Console.WriteLine(dt.Rows[dt.Rows.Count-1][5]);


            //ICANDriver intfCANDriver = new CANDriver(4, 0, 0);
            //Console.WriteLine(intfCANDriver.Open());
            //Console.WriteLine( intfCANDriver.Init());
            //intfCANDriver.Start();
            Console.WriteLine("success");
            Console.ReadKey();

        }
    }
}
