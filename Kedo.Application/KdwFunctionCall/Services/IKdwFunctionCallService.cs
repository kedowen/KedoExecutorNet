using Kedo.Application.KdwFunctionCall.Dtos.input;
using Kedo.Application.KdwFunctionCall.Dtos.output;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.KdwFunctionCall.Services
{
    /// <summary>
    /// OpenAI Function Calling服务接口
    /// </summary>
    public interface IKdwFunctionCallService
    {
        /// <summary>
        /// 处理用户请求，智能调用相关功能服务
        /// </summary>
        /// <param name="input">用户请求输入参数</param>
        /// <returns>处理结果</returns>
        Task<ProcessRequestOutput> ProcessUserRequestAsync(FunctionCallProcessRequestInput input);
        
        /// <summary>
        /// 获取所有已注册的功能函数列表
        /// </summary>
        /// <returns>功能函数列表</returns>
        List<FunctionDefinitionOutput> GetRegisteredFunctions();
        
        /// <summary>
        /// 根据名称获取指定功能函数
        /// </summary>
        /// <param name="functionName">功能函数名称</param>
        /// <returns>功能函数定义</returns>
        FunctionDefinitionOutput GetFunctionByName(string functionName);
        
        /// <summary>
        /// 获取用户的会话历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户会话历史</returns>
        List<ConversationMessageOutput> GetUserConversationHistory(string userId);
        
        /// <summary>
        /// 清除用户的会话历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>清除结果</returns>
        Task<bool> ClearUserConversationHistoryAsync(string userId);
    }
} 