using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;

using MySqlConnector;
using Npgsql;
using Furion.FriendlyException;

namespace Kedo.Application.DataSource.Services
{
    public class DBHandler
    {


    }

    #region SqlServer
    public class DbDataAccess_SqlServer
    {
        public DataTable ExecDataTable(string MySql, string _DatabaseConnectionString)
        {
            DataTable table = new DataTable();
            using (SqlConnection conn = new SqlConnection(_DatabaseConnectionString))
            {
                try
                {
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(MySql, conn);
                    dataAdapter.Fill(table);
                    conn.Close();
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return table;
        }


        public bool ExecConnection(string _DatabaseConnectionString)
        {
            using (SqlConnection conn = new SqlConnection(_DatabaseConnectionString))
            {
                try
                {
                    conn.Open();
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                        return true;
                    }
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return false;
        }
    }
    #endregion
    #region MySql
    public class DbDataAccess_MySql
    {
        public DataTable ExecDataTable(string MySql, string _DatabaseConnectionString)
        {
            DataTable table = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(_DatabaseConnectionString))
            {
                try
                {

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(MySql, conn);
                    dataAdapter.Fill(table);
                    conn.Close();
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return table;
        }

        public bool ExecConnection(string _DatabaseConnectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(_DatabaseConnectionString))
            {
                try
                {
                    conn.Open();
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                        return true;
                    }
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return false;
        }
    }

    #endregion
    //#region Orcale  暂时不支持
    //public class DbDataAccess_Orcale
    //{
    //    public DataTable ExecDataTable(string MySql, string _DatabaseConnectionString)
    //    {
    //        DataTable table = new DataTable();

    //        using (OracleConnection conn = new OracleConnection(_DatabaseConnectionString))
    //        {
    //            try
    //            {
    //                OracleDataAdapter dataAdapter = new OracleDataAdapter(MySql, conn);
    //                dataAdapter.Fill(table);
    //                conn.Close();
    //            }
    //            catch (Exception ee)
    //            {
    //                if (conn.State != ConnectionState.Closed)
    //                {
    //                    conn.Close();
    //                }

    //            }
    //        }
    //        return table;
    //    }
    //}
    //#endregion
    #region PostgreSQL
    public class DbDataAccess_PostgreSQL
    {
        public DataTable ExecDataTable(string MySql, string _DatabaseConnectionString)
        {
            DataTable table = new DataTable();

            using (NpgsqlConnection conn = new NpgsqlConnection(_DatabaseConnectionString))
            {
                try
                {
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(MySql, conn);
                    dataAdapter.Fill(table);
                    conn.Close();
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return table;
        }

        public bool ExecConnection(string _DatabaseConnectionString)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(_DatabaseConnectionString))
            {
                try
                {
                    conn.Open();
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                        return true;
                    }
                }
                catch (Exception ee)
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                    throw Oops.Bah(ee.Message).StatusCode(201);
                }
            }
            return false;
        }
    }
    #endregion

}
