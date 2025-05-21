using Furion.DynamicApiController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kedo.Application.KdwFunctionCall.Dtos.input;
using Kedo.Application.KdwFunctionCall.Dtos.output;
using Kedo.Application.KdwFunctionCall.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.KdwFunctionCall
{
    /// <summary>
    /// OpenAI Function Call智能服务接口
    /// </summary>
    public class KdwFunctionCallServices : IDynamicApiController
    {
        private readonly IKdwFunctionCallService _functionCallService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="functionCallService">Function Call服务</param>
        public KdwFunctionCallServices(IKdwFunctionCallService functionCallService)
        {
            _functionCallService = functionCallService;
        }

        /// <summary>
        /// 处理用户请求，智能调用相关功能服务
        /// </summary>
        /// <param name="input">用户请求输入参数</param>
        /// <returns>处理结果</returns>
        [HttpPost]
        public async Task<ProcessRequestOutput> ProcessUserRequest([FromBody] FunctionCallProcessRequestInput input)
        {
            return await _functionCallService.ProcessUserRequestAsync(input);
        }

        /// <summary>
        /// 获取所有已注册的功能函数列表
        /// </summary>
        /// <returns>功能函数列表</returns>
        [HttpGet]
        public List<FunctionDefinitionOutput> GetRegisteredFunctions()
        {
            return _functionCallService.GetRegisteredFunctions();
        }

        /// <summary>
        /// 根据名称获取指定功能函数
        /// </summary>
        /// <param name="functionName">功能函数名称</param>
        /// <returns>功能函数定义</returns>
        [HttpGet]
        public FunctionDefinitionOutput GetFunctionByName([FromQuery] string functionName)
        {
            return _functionCallService.GetFunctionByName(functionName);
        }

        /// <summary>
        /// 获取用户的会话历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户会话历史</returns>
        [HttpGet]
        public List<ConversationMessageOutput> GetUserConversationHistory([FromQuery] string userId)
        {
            return _functionCallService.GetUserConversationHistory(userId);
        }

        /// <summary>
        /// 清除用户的会话历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>清除结果</returns>
        [HttpDelete]
        public async Task<bool> ClearUserConversationHistory([FromQuery] string userId)
        {
            return await _functionCallService.ClearUserConversationHistoryAsync(userId);
        }
    }
} 