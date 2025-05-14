using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.SqlExecutor
{
    public class SqlExecutorService: ISqlExecutorService
    {
        public async Task<(List<object> outputList, int rowNum)> ExecuteQueryAsync(string sqlQuery, string sqlType, string connectionString, Dictionary<string, object> parameters)
        {
            // 这里应该连接实际的数据库并执行SQL查询
            // 为了示例，我们返回模拟数据
            Console.WriteLine($"执行SQL查询: {sqlQuery}");
            Console.WriteLine($"数据库: {connectionString}");
            foreach (var param in parameters)
            {
                Console.WriteLine($"参数 {param.Key}: {param.Value}");

                // 将参数替换到SQL查询中
                sqlQuery = sqlQuery.Replace("@" + param.Key, param.Value?.ToString() ?? "NULL");
                sqlQuery = sqlQuery.Replace("{{" + param.Key + "}}", param.Value?.ToString() ?? "NULL");
            }

            // await Task.Delay(1000); // 模拟查询延迟
            DataTable tableResult = QueryDBDataBySQL(sqlQuery, sqlType, connectionString);
            // 创建结果列表
            var outputList = new List<object>();
            // 遍历 DataTable 的每一行
            foreach (DataRow row in tableResult.Rows)
            {
                // 创建字典存储行数据
                var item = new Dictionary<string, object>();
                // 遍历列名获取数据
                foreach (DataColumn column in tableResult.Columns)
                {
                    // 处理 DBNull 值并填充字典
                    item[column.ColumnName] = row[column] ?? null;
                }
                // 添加到结果列表
                outputList.Add(item);
            }
            return (outputList, outputList.Count);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="SqlString"></param>
        /// <param name="sourceType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public DataTable QueryDBDataBySQL(string SqlString, string sourceType, string connectionString)
        {
            string sql = SqlString;
  
            DataTable dataTable = new DataTable();

            switch (sourceType.ToLower())
            {
                case "sqlserver":
                    DbDataAccess_SqlServer dbDataAccess_SqlServer = new DbDataAccess_SqlServer();
                    dataTable = dbDataAccess_SqlServer.ExecDataTable(sql, connectionString);
                    break;

                case "mysql":
                    DbDataAccess_MySql dbDataAccess_MySql = new DbDataAccess_MySql();
                    dataTable = dbDataAccess_MySql.ExecDataTable(sql, connectionString);
                    break;

                case "postgresql":
                    DbDataAccess_PostgreSQL dbDataAccess_PostgreSQL = new DbDataAccess_PostgreSQL();
                    dataTable = dbDataAccess_PostgreSQL.ExecDataTable(sql, connectionString);
                    break;
            }
            return dataTable;
        }
    }
}


