using System;

namespace Kedo.Application.KdwFunctionCall.Dtos.output
{
    /// <summary>
    /// 会话消息输出
    /// </summary>
    public class ConversationMessageOutput
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; set; }
        
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// 角色
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 功能函数名称
        /// </summary>
        public string FunctionName { get; set; }
        
        /// <summary>
        /// 功能函数参数
        /// </summary>
        public string FunctionArgs { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
} 