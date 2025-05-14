using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.SqlExecutor
{
    //// SQL执行器服务接口
    public interface ISqlExecutorService
    {
        Task<(List<object> outputList, int rowNum)> ExecuteQueryAsync(string sqlQuery, string sqlType, string connectionString, Dictionary<string, object> parameters);
    }
}
