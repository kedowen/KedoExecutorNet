namespace Kedo.Application.KdwFunctionCall.Dtos.output
{
    /// <summary>
    /// 处理用户请求输出结果
    /// </summary>
    public class ProcessRequestOutput
    {
        /// <summary>
        /// 处理结果消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 是否调用了功能函数
        /// </summary>
        public bool FunctionCalled { get; set; }
        
        /// <summary>
        /// 调用的功能函数名称
        /// </summary>
        public string FunctionName { get; set; }
        
        /// <summary>
        /// 功能函数调用结果
        /// </summary>
        public string FunctionResult { get; set; }
        
        /// <summary>
        /// 是否处理成功
        /// </summary>
        public bool Success { get; set; }
    }
} 