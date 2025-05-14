using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.CodeExecutor
{
    // 代码执行器服务接口
    public interface ICodeExecutorService
    {
        Task<Dictionary<string, object>> ExecuteCodeAsync(string code, string language, Dictionary<string, object> parameters, bool ignoreErrors);
    }
}
