using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kedo.Application.KdwFunctionCall.Models
{
    /// <summary>
    /// OpenAI配置
    /// </summary>
    public class LLMSettings
    {
        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>
        /// 温度
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// ChatUrl
        /// </summary>
        public string ChatUrl { get; set; }

    }

    /// <summary>
    /// OpenAI消息
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 角色
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>
        /// 函数调用
        /// </summary>
        [JsonPropertyName("function_call")]
        public FunctionCall FunctionCall { get; set; }

        public string Name { get; set; }
        
    }

    /// <summary>
    /// 函数定义
    /// </summary>
    public class FunctionDefinition
    {
        /// <summary>
        /// 函数名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// 函数描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        [JsonPropertyName("parameters")]
        public JsonElement Parameters { get; set; }
    }

    /// <summary>
    /// 函数调用
    /// </summary>
    public class FunctionCall
    {
        /// <summary>
        /// 函数名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }
    }

    /// <summary>
    /// OpenAI响应选择
    /// </summary>
    public class ResponseChoice
    {
        /// <summary>
        /// 消息
        /// </summary>
        [JsonPropertyName("message")]
        public ResponseMessage Message { get; set; }

        /// <summary>
        /// 结束原因
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    /// <summary>
    /// 响应消息
    /// </summary>
    public class ResponseMessage
    {
        /// <summary>
        /// 角色
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>
        /// 函数调用
        /// </summary>
        [JsonPropertyName("function_call")]
        public List<FunctionCall> FunctionCalls { get; set; }
    }

    /// <summary>
    /// OpenAI响应
    /// </summary>
    public class OpenAIResponse
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// 选择列表
        /// </summary>
        [JsonPropertyName("choices")]
        public List<ResponseChoice> Choices { get; set; }
    }

    /// <summary>
    /// OpenAI请求
    /// </summary>
    public class OpenAIRequest
    {
        /// <summary>
        /// 模型
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// 消息列表
        /// </summary>
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        ///// <summary>
        ///// 函数列表
        ///// </summary>
        //[JsonPropertyName("functions")]
        //public List<FunctionDefinition> Functions { get; set; }

        public List<tools> tools { set; get; }
        /// <summary>
        /// 函数调用模式
        /// </summary>
        [JsonPropertyName("function_call")]
        public string FunctionCall { get; set; } = "auto";

        /// <summary>
        /// 温度
        /// </summary>
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        /// <summary>
        /// 最大令牌数
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class tools
    {
        public string type { set; get; }

        public FunctionDefinition function { set; get; }
    }
}