using Furion.DatabaseAccessor;
using Kedo.Application.KdwFunctionCall.Dtos.output;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.KdwFunctionCall.Services
{
    /// <summary>
    /// Function Call SQL操作接口
    /// </summary>
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        /// 获取所有注册的功能函数
        /// </summary>
        /// <returns>功能函数列表</returns>
        [SqlExecute("SELECT function_id, function_name, description, endpoint_url, http_method, parameters_schema FROM ai_function_definitions WHERE enabled = 1")]
        List<FunctionDefinitionOutput> GetAllRegisteredFunctions();
        
        /// <summary>
        /// 根据名称获取功能函数
        /// </summary>
        /// <param name="functionName">功能函数名称</param>
        /// <returns>功能函数</returns>
        [SqlExecute("SELECT function_id, function_name, description, endpoint_url, http_method, parameters_schema FROM ai_function_definitions WHERE function_name = @functionName AND enabled = 1")]
       List<FunctionDefinitionOutput> GetFunctionByName(string functionName);
        
        /// <summary>
        /// 保存会话消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="role">角色</param>
        /// <param name="content">内容</param>
        /// <param name="functionName">功能函数名称</param>
        /// <param name="functionArgs">功能函数参数</param>
        /// <param name="createTime">创建时间</param>
        /// <returns>操作是否成功</returns>
        [SqlExecute("INSERT INTO ai_conversation_messages (message_id, user_id, role, content, function_name, function_args, create_time) VALUES (@messageId, @userId, @role, @content, @functionName, @functionArgs, @createTime)")]
        bool SaveConversationMessage(string messageId, string userId, string role, string content, string functionName, string functionArgs, DateTime createTime);
        
        /// <summary>
        /// 获取用户会话历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>会话历史记录</returns>
        [SqlExecute("SELECT message_id, user_id, role, content, function_name, function_args, create_time FROM ai_conversation_messages WHERE user_id = @userId ORDER BY create_time ASC")]
        List<ConversationMessageOutput> GetUserConversationHistory(string userId);
        
        /// <summary>
        /// 清除用户会话历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作是否成功</returns>
        [SqlExecute("DELETE FROM ai_conversation_messages WHERE user_id = @userId")]
        bool ClearUserConversationHistory(string userId);
        
        /// <summary>
        /// 记录函数调用日志
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="functionName">功能函数名称</param>
        /// <param name="requestData">请求数据</param>
        /// <param name="responseData">响应数据</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="createTime">创建时间</param>
        /// <returns>操作是否成功</returns>
        [SqlExecute("INSERT INTO ai_function_call_logs (log_id, user_id, function_name, request_data, response_data, is_success, error_message, create_time) VALUES (@logId, @userId, @functionName, @requestData, @responseData, @isSuccess, @errorMessage, @createTime)")]
        bool LogFunctionCall(string logId, string userId, string functionName, string requestData, string responseData, bool isSuccess, string errorMessage, DateTime createTime);
    }
} 