using System.ComponentModel.DataAnnotations;

namespace Kedo.Application.KdwFunctionCall.Dtos.input
{
    /// <summary>
    /// 处理用户请求输入参数
    /// </summary>
    public class FunctionCallProcessRequestInput
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public string UserId { get; set; }
        
        /// <summary>
        /// 用户消息内容
        /// </summary>
        [Required(ErrorMessage = "消息内容不能为空")]
        public string Message { get; set; }
        
        /// <summary>
        /// 会话ID，可选，新对话不需要提供
        /// </summary>
        public string SessionId { get; set; }
    }
} 