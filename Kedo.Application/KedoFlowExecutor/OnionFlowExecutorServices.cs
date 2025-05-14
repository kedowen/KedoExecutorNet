using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using Kedo.Application.OnionFlowExecutor.Dtos;
using Kedo.Application.OnionFlowExecutor.Dtos.input;
using Kedo.Application.OnionFlowExecutor.Services;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowExecutor
{
    public class OnionFlowExecutorServices : IDynamicApiController
    {
        private readonly IOnionFlowExecutorService _onionFlowExecutorService;

        public OnionFlowExecutorServices(IOnionFlowExecutorService onionFlowExecutorService)
        {
            _onionFlowExecutorService = onionFlowExecutorService;
        }


        /// <summary>
        /// 智能体流程执行
        /// </summary>
        /// <param name="request"></param>
        [HttpPost]
        public async Task<NodeExecutionResult> OnionAgentFlowExecuteProcess([FromBody] ProcessRequestInput request)
        {
            return await _onionFlowExecutorService.OnionAgentFlowExecuteProcess(request);
        }

        /// <summary>
        /// 获取智能体执行过程中的结果
        /// </summary>
        /// <param name="agentFlowId"></param>
        /// <returns></returns>
        [HttpGet]
        public string GetAgentFlowExecuteProcessResult([FromQuery] string agentFlowId)
        {
            return _onionFlowExecutorService.GetProcessResult(agentFlowId);
        }

    }
}
