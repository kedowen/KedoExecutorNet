using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.CodeExecutor
{
    public class CodeExecutorService: ICodeExecutorService
    {
        public async Task<Dictionary<string, object>> ExecuteCodeAsync(string code, string language, Dictionary<string, object> parameters, bool ignoreErrors)
        {

            return  new Dictionary<string, object>();
        }
    }
}
