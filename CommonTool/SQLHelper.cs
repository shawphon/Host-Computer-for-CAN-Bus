/**
 * 数据库的操作:
 * Created by Leslie @ changzhou
 * 
 * 两种方式读取数据库数据，一是sqlAdapter 联合DataSet读取数据库数据；
 * 二是sqlReader一行一行的读取数据库数据
 * **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace DAL
{
    public static class SQLHelper
    {
        //读取配置文件中的连接字符串
        static string connstr = ConfigurationManager.ConnectionStrings["sql"].ConnectionString;

        public static string GetConnString()
        {
            return connstr;
        }

        /// <summary>
        /// 适合增删改操作，返回影响条数
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                using (SqlCommand comm = conn.CreateCommand())
                {
                    conn.Open();
                    comm.CommandText = sql;
                    comm.Parameters.AddRange(parameters);
                    return comm.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 查询操作，返回查询结果中的第一行第一列的值
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                using (SqlCommand comm = conn.CreateCommand())
                {
                    conn.Open();
                    comm.CommandText = sql;
                    comm.Parameters.AddRange(parameters);
                    return comm.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Adapter调整，查询操作，返回DataTable
        ///  SqlDataAdapter的方式，数据源在内存中，用一个数据集DataSet类的实例进行存储。
        ///  SqlDataAdapter相当于是一个桥梁，将数据库服务器中的数据读取到内存中，它的Fill( )方法完成了这个过程。
        ///  因此，对于小量的数据，它的一个优点还在于，即使当服务器连接断开时，也能继续读取数据。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connstr))
            {
                DataTable dt = new DataTable();
                adapter.SelectCommand.Parameters.AddRange(parameters);
                adapter.Fill(dt);
                return dt;
            }
        }

        /// <summary>
        /// SqlDataReader的数据源在数据库服务器上，对于程序而言，它在数据库服务器上设置了一个游标，
        /// 指向一行数据，用Read()方法来对游标进行判断，当它返回false时，表示查询的数据已取完。
        /// 因此，它适合数据量比较大的时候的读取，因为它不占内存，数据在数据库服务器中。它的缺点在于，
        /// 当数据库服务连接断开时，不能再进行数据的读取了。
        /// </summary>
        /// <param name="sqlText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static SqlDataReader ExecuteReader(string sql, params SqlParameter[] parameters)
        {
            //SqlDataReader要求，它读取数据的时候有，它独占它的SqlConnection对象，而且SqlConnection必须是Open状态
            SqlConnection conn = new SqlConnection(connstr);//不要释放连接，因为后面还需要连接打开状态
            SqlCommand cmd = conn.CreateCommand();
            conn.Open();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            //CommandBehavior.CloseConnection当SqlDataReader释放的时候，顺便把SqlConnection对象也释放掉
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
    }

    #region
    //    /// <summary>
    //    /// 执行非查询sql语句，返回受影响行数，如果执行非增删改则返回-1
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">参数数组</param>
    //    /// <returns>影响行数res</returns>
    //    public static int ExecuteNonQuery(string sql, params SqlParameter[] paras)
    //    {
    //        int res = -1;
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                cmd.CommandType = CommandType.StoredProcedure;
    //                if (paras != null || paras.Length > 0)
    //                {
    //                    cmd.Parameters.AddRange(paras);
    //                }
    //                conn.Open();
    //                res = cmd.ExecuteNonQuery();
    //            }
    //        }
    //        return res;
    //    }
    //    /// <summary>
    //    /// 执行非查询sql语句，返回受影响行数，如果执行非增删改则返回-1
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">参数数组</param>
    //    /// <returns>影响行数res</returns>
    //    public static int ExecuteNonParaQuery(string sql)
    //    {
    //        int res = -1;
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                conn.Open();
    //                res = cmd.ExecuteNonQuery();
    //            }
    //        }
    //        return res;
    //    }

    //    /// <summary>
    //    /// 执行读取数据，返回一个对象
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">参数数组</param>
    //    /// <returns>返回一个对象o</returns>
    //    public static object ExecuteScalar(string sql, params SqlParameter[] paras)
    //    {
    //        object o = null;
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                cmd.CommandType = CommandType.StoredProcedure;
    //                if (paras != null)
    //                {
    //                    cmd.Parameters.AddRange(paras);
    //                }
    //                conn.Open();
    //                o = cmd.ExecuteScalar();
    //            }
    //        }
    //        return o;
    //    }
    //    /// <summary>
    //    /// 执行查询sql语句，返回一个对象
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">查询参数</param>
    //    /// <returns>返回DataReader对象</returns>
    //    public static SqlDataReader ExecuteReader(string sql, params SqlParameter[] paras)
    //    {
    //        SqlConnection conn = new SqlConnection(connstr);
    //        using (SqlCommand cmd = new SqlCommand(sql, conn))
    //        {
    //            cmd.CommandType = CommandType.StoredProcedure;
    //            if (paras != null)
    //            {
    //                cmd.Parameters.AddRange(paras);
    //            }
    //            conn.Open();
    //            try
    //            {
    //                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
    //            }
    //            catch (Exception ex)
    //            {
    //                cmd.Dispose();
    //                throw ex;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 执行查询sql语句，返回一个无参数dataset对象
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras"></param>
    //    /// <returns>返回dataset 对象</returns>
    //    public static DataSet GetDataSetNotPara(string sql)
    //    {
    //        DataSet ds = new DataSet();
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                //根据传来的参数。决定是sql语句还是存储过程
    //                cmd.CommandType = CommandType.Text;
    //                cmd.CommandText = sql;
    //                conn.Open();
    //                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
    //                {
    //                    sda.Fill(ds);
    //                }
    //            }
    //        }
    //        return ds;
    //    }

    //    /// <summary>
    //    /// 执行查询sql语句，返回一个无参数dataTable对象
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras"></param>
    //    /// <returns>返回dataset 对象</returns>
    //    public static DataTable GetDataTableNotPara(string sql)
    //    {
    //        DataTable dt = new DataTable();
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                //根据传来的参数。决定是sql语句还是存储过程

    //                conn.Open();
    //                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
    //                {
    //                    sda.Fill(dt);
    //                }
    //            }
    //        }
    //        return dt;
    //    }

    //    /// <summary>
    //    /// 执行查询sql语句，返回一个dataset对象
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">查询参数</param>
    //    /// <returns>返回dataset 对象</returns>
    //    public static DataSet GetDataSet(string sql, params SqlParameter[] paras)
    //    {
    //        DataSet ds = new DataSet();
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                //根据传来的参数。决定是sql语句还是存储过程
    //                cmd.CommandType = CommandType.StoredProcedure;
    //                //添加参数
    //                cmd.Parameters.AddRange(paras);
    //                conn.Open();
    //                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
    //                {
    //                    sda.Fill(ds);
    //                }
    //            }
    //        }
    //        return ds;
    //    }
    //    /// <summary>
    //    /// 可以执行sql语句或存储过程
    //    /// </summary>
    //    /// <param name="text"></param>
    //    /// <param name="ct"></param>
    //    /// <param name="param"></param>
    //    /// <returns></returns>
    //    public static DataTable ProcGetTable(string sql, params SqlParameter[] param)
    //    {
    //        DataTable dt = new DataTable();

    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlCommand cmd = new SqlCommand(sql, conn))
    //            {
    //                //根据传来的参数。决定是sql语句还是存储过程

    //                cmd.CommandType = CommandType.StoredProcedure;
    //                //添加参数
    //                cmd.Parameters.AddRange(param);
    //                //cmd.Parameters.Add("@name", SqlDbType.NVarChar, 20).Value = param[0];
    //                //cmd.Parameters.Add("@pwd", SqlDbType.NVarChar, 20).Value = param[1];
    //                conn.Open();
    //                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
    //                {
    //                    sda.Fill(dt);
    //                }
    //            }
    //        }
    //        return dt;
    //    }

    //    /// <summary>
    //    /// 实现分页功能
    //    /// </summary>
    //    /// <param name="sql">sql语句</param>
    //    /// <param name="paras">参数数组（显示index页和每页显示条数size）</param>
    //    /// <returns>查询结果</returns>
    //    public static DataTable GetParaTable(string sql, params SqlParameter[] paras)
    //    {
    //        DataSet ds = new DataSet();
    //        using (SqlConnection conn = new SqlConnection(connstr))
    //        {
    //            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
    //            {
    //                if (paras != null)
    //                {
    //                    da.SelectCommand.Parameters.AddRange(paras);
    //                }
    //                da.SelectCommand.CommandType = CommandType.StoredProcedure;
    //                da.Fill(ds);
    //            }
    //        }
    //        return ds.Tables[0];
    //    }
    //}
    #endregion
}