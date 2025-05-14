using Kedo.Application.OnionFlowExecutor.Dtos;
using Kedo.Application.OnionFlowExecutor.Dtos.input;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowExecutor.Services
{
    public interface IOnionFlowExecutorService
    {
        #region   智能体Flow 执行

        // void OnionAgentFlowExecuteProcess(ProcessRequestInput request);

        Task<NodeExecutionResult> OnionAgentFlowExecuteProcess(ProcessRequestInput request);

        string GetProcessResult(string agentFlowId);

        #endregion
    }
}
