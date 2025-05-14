namespace Kedo.Application.KdwFunctionCall.Dtos.output
{
    /// <summary>
    /// 功能函数定义输出
    /// </summary>
    public class FunctionDefinitionOutput
    {
        /// <summary>
        /// 功能函数ID
        /// </summary>
        public string FunctionId { get; set; }
        
        /// <summary>
        /// 功能函数名称
        /// </summary>
        public string FunctionName { get; set; }
        
        /// <summary>
        /// 功能函数描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 端点URL
        /// </summary>
        public string EndpointUrl { get; set; }
        
        /// <summary>
        /// HTTP方法
        /// </summary>
        public string HttpMethod { get; set; }
        
        /// <summary>
        /// 参数架构
        /// </summary>
        public string ParametersSchema { get; set; }
    }
} 