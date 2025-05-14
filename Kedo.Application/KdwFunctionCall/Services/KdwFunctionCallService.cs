using Furion.DependencyInjection;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kedo.Application.KdwFunctionCall.Dtos.input;
using Kedo.Application.KdwFunctionCall.Dtos.output;
using Kedo.Application.KdwFunctionCall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kedo.Application.KdwFunctionCall.Services
{
    /// <summary>
    /// LLM Function Call服务实现
    /// </summary>
    public class KdwFunctionCallService : IKdwFunctionCallService, IScoped
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ISql _functionCallSql;
        private readonly ILogger<KdwFunctionCallService> _logger;
        private readonly HttpClient _httpClient;
        private readonly LLMSettings _openAISettings;

        static string onUseUrldeepseek = "https://api.deepseek.com/v1/chat/completions";
        /// <summary>
        /// 构造函数
        /// </summary>
        public KdwFunctionCallService(
            ILogger<KdwFunctionCallService> logger,
            IHttpContextAccessor contextAccessor,
            ISql functionCallSql,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _functionCallSql = functionCallSql;
            _httpClient = httpClientFactory.CreateClient("OpenAI");

            // 加载OpenAI配置
            _openAISettings = new LLMSettings();
            configuration.GetSection("OpenAISettings").Bind(_openAISettings);

            // 设置OpenAI API密钥
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAISettings.ApiKey}");
        }

        /// <summary>
        /// 处理用户请求，智能调用相关功能服务
        /// </summary>
        /// <param name="input">用户请求输入参数</param>
        /// <returns>处理结果</returns>
        public async Task<ProcessRequestOutput> ProcessUserRequestAsync(FunctionCallProcessRequestInput input)
        {
            try
            {
                _logger.LogInformation("开始处理用户请求: UserId={UserId}, Message={Message}", input.UserId, input.Message);

                // 获取所有注册的功能函数
                var functionDefinitions = _functionCallSql.GetAllRegisteredFunctions();
                _logger.LogInformation("获取到 {Count} 个注册的功能函数", functionDefinitions.Count);

                if (functionDefinitions.Count == 0)
                {
                    _logger.LogWarning("没有找到任何注册的功能函数");
                    return new ProcessRequestOutput
                    {
                        Message = "系统未配置任何功能函数，无法处理您的请求。",
                        FunctionCalled = false,
                        Success = false
                    };
                }

                // 转换为OpenAI函数定义格式
                var functions = functionDefinitions.Select(fn => new FunctionDefinition
                {
                    Name = fn.FunctionName,
                    Description = fn.Description,
                    Parameters = JsonDocument.Parse(fn.ParametersSchema).RootElement
                }).ToList();

                List<tools> toolsList = new List<tools>();
                foreach (var v in functions)
                {
                    tools tools = new tools();

                    tools.type = "function";
                    tools.function = v;

                    toolsList.Add(tools);
                }

                // 增强系统提示，强调使用函数
                string systemPrompt = "你是一个智能助手。当用户需要信息或执行操作时，你必须通过调用适当的函数来获取数据或执行操作，而不是直接回答。始终优先使用函数调用来满足用户需求。";
                // 构建OpenAI请求
                var openAIRequest = new OpenAIRequest
                {
                    Model = _openAISettings.Model,
                    Messages = new List<Kedo.Application.KdwFunctionCall.Models.Message>
                    {
                        new Kedo.Application.KdwFunctionCall.Models.Message
                        {
                            Role = "system",
                            Content = systemPrompt
                        },
                        new Kedo.Application.KdwFunctionCall.Models.Message
                        {
                            Role = "user",
                            Content = input.Message
                        }
                    },
                    tools = toolsList,
                    Temperature = _openAISettings.Temperature,
                    MaxTokens = _openAISettings.MaxTokens
                };

                // 强制模型考虑使用函数
                // 方法一: 使用 "auto" 让模型自行决定
                openAIRequest.FunctionCall = "auto";

                // 方法二(可选): 使用 { "name": "auto", "mode": "required" } 强制模型必须考虑使用函数
                // 如果API支持，可以尝试这种方式
                // openAIRequest.FunctionCall = new { name = "auto", mode = "required" };

                _logger.LogInformation("准备调用OpenAI API，使用模型: {Model}, 函数数量: {FunctionCount}",
                    _openAISettings.Model, functions.Count);

                // 调用OpenAI API
                var response = await CallOpenAIApiAsync(openAIRequest);

                // 处理API响应
                var result = await ProcessOpenAIResponseAsync(response, input.UserId, input.Message, functionDefinitions);

                // 如果没有调用函数，尝试再次调用，这次明确指定要使用函数
                if (!result.FunctionCalled && result.Success && functionDefinitions.Count > 0)
                {
                    _logger.LogInformation("首次调用未使用函数，尝试第二次调用并强制使用函数");

                    // 修改系统提示，更强烈地要求使用函数
                    systemPrompt = "你是一个智能助手。必须通过调用函数来响应用户请求。不要直接回答用户问题，而是找出最合适的函数并调用它。这是强制性要求。";

                    // 构建新的请求
                    var secondRequest = new OpenAIRequest
                    {
                        Model = _openAISettings.Model,
                        Messages = new List<Kedo.Application.KdwFunctionCall.Models.Message>
                        {
                            new Kedo.Application.KdwFunctionCall.Models.Message
                            {
                                Role = "system",
                                Content = systemPrompt
                            },
                            new Kedo.Application.KdwFunctionCall.Models.Message
                            {
                                Role = "user",
                                Content = "用户说: " + input.Message + "\n\n请务必使用函数来回应这个请求。"
                            }
                        },
                        //Functions = functions,
                        tools = toolsList,
                        Temperature = 0.1, // 降低温度，使回答更确定
                        MaxTokens = _openAISettings.MaxTokens
                    };

                    // 尝试强制使用函数 - 注意：这种方式在某些API版本可能不支持
                    try
                    {
                        // 方法一：使用JSON对象
                        secondRequest.FunctionCall = new { name = "auto", mode = "required" }.ToString();
                        var secondResponse = await CallOpenAIApiAsync(secondRequest);
                        var secondResult = await ProcessOpenAIResponseAsync(secondResponse, input.UserId, input.Message, functionDefinitions);

                        // 如果成功调用了函数，返回第二次的结果
                        if (secondResult.FunctionCalled)
                        {
                            _logger.LogInformation("第二次尝试成功调用了函数: {FunctionName}", secondResult.FunctionName);
                            return secondResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("使用mode=required尝试失败，可能API版本不支持: {Error}", ex.Message);

                        // 方法二：退回到字符串方式
                        secondRequest.FunctionCall = "auto";
                        var secondResponse = await CallOpenAIApiAsync(secondRequest);
                        var secondResult = await ProcessOpenAIResponseAsync(secondResponse, input.UserId, input.Message, functionDefinitions);

                        // 如果成功调用了函数，返回第二次的结果
                        if (secondResult.FunctionCalled)
                        {
                            _logger.LogInformation("第二次尝试成功调用了函数: {FunctionName}", secondResult.FunctionName);
                            return secondResult;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理用户请求时发生错误: {Message}", ex.Message);
                throw Oops.Oh(ex.Message);
            }
        }

        /// <summary>
        /// 获取所有已注册的功能函数列表
        /// </summary>
        /// <returns>功能函数列表</returns>
        public List<FunctionDefinitionOutput> GetRegisteredFunctions()
        {
            return _functionCallSql.GetAllRegisteredFunctions();
        }

        /// <summary>
        /// 根据名称获取指定功能函数
        /// </summary>
        /// <param name="functionName">功能函数名称</param>
        /// <returns>功能函数定义</returns>
        public FunctionDefinitionOutput GetFunctionByName(string functionName)
        {
            return _functionCallSql.GetFunctionByName(functionName).FirstOrDefault();
        }

        /// <summary>
        /// 获取用户的会话历史记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户会话历史</returns>
        public List<ConversationMessageOutput> GetUserConversationHistory(string userId)
        {
            return _functionCallSql.GetUserConversationHistory(userId);
        }

        /// <summary>
        /// 清除用户的会话历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>清除结果</returns>
        public async Task<bool> ClearUserConversationHistoryAsync(string userId)
        {
            return _functionCallSql.ClearUserConversationHistory(userId);
        }

        /// <summary>
        /// 调用OpenAI API
        /// </summary>
        /// <param name="request">OpenAI请求</param>
        /// <returns>OpenAI响应</returns>
        private async Task<OpenAIResponse> CallOpenAIApiAsync(OpenAIRequest request)
        {
            try
            {
                _logger.LogInformation("开始调用OpenAI API，URL: {Url}, Model: {Model}, MaxTokens: {MaxTokens}",
                    _openAISettings.ChatUrl, request.Model, request.MaxTokens);

                // 验证 max_tokens 的范围
                if (request.MaxTokens < 1 || request.MaxTokens > 8192)
                {
                    _logger.LogWarning("max_tokens 超出有效范围，重置为默认值 2000");
                    request.MaxTokens = 2000;
                }

                // 记录请求内容
                var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _logger.LogDebug("OpenAI API请求内容: {RequestJson}", requestJson);

                var response = await _httpClient.PostAsJsonAsync(_openAISettings.ChatUrl, request);

                // 记录响应状态
              //  _logger.LogInformation("OpenAI API响应状态码: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API调用失败: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                  //  throw new Exception($"OpenAI API调用失败: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("OpenAI API原始响应: {ResponseContent}", responseContent);

                var jsonResponse = JObject.Parse(responseContent);
                var message = jsonResponse["choices"]?[0]?["message"];

                if (message == null)
                {
                    _logger.LogError("API响应格式异常，无法获取消息内容: {ResponseContent}", responseContent);
                    throw new Exception("API响应格式异常，无法获取消息内容");
                }

                // 检查是否有 tool_calls
                if (message["tool_calls"] != null && message["tool_calls"].HasValues)
                {
                    _logger.LogInformation("检测到 tool_calls，准备处理工具调用");

                    // 创建工具调用列表
                    var functionCalls = new List<FunctionCall>();

                    // 遍历所有工具调用
                    foreach (var toolCall in message["tool_calls"])
                    {
                        // 确保是函数调用类型
                        if (toolCall["type"]?.ToString() == "function" && toolCall["function"] != null)
                        {
                            var functionCall = new FunctionCall
                            {
                                // Id = toolCall["id"]?.ToString(),
                                Name = toolCall["function"]["name"]?.ToString(),
                                Arguments = toolCall["function"]["arguments"]?.ToString()
                            };

                            _logger.LogInformation("函数调用详情: ID={Id}, 名称={Name}, 参数={Arguments}",
                                "", functionCall.Name, functionCall.Arguments);

                            functionCalls.Add(functionCall);
                        }
                        else
                        {
                            _logger.LogWarning("工具调用不是函数类型或缺少函数信息");
                        }
                    }

                    if (functionCalls.Count > 0)
                    {
                        // 创建新的响应对象，包含所有工具调用
                        // 注意：需要修改ResponseMessage和OpenAIResponse类以支持多个工具调用

                        // 假设我们修改了ResponseMessage类，添加了FunctionCalls属性
                        var newMessage = new ResponseMessage
                        {
                            Role = "assistant",
                            Content = message["content"]?.ToString(),
                            FunctionCalls = functionCalls  // 新增的属性，存储多个函数调用
                        };

                        // 创建新的响应对象
                        var openAIResponse = new OpenAIResponse
                        {
                            Id = jsonResponse["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                            Choices = new List<ResponseChoice>
            {
                new ResponseChoice
                {
                    Message = newMessage,
                    FinishReason = jsonResponse["choices"]?[0]?["finish_reason"]?.ToString() ?? "stop"
                }
            }
                        };

                        return openAIResponse;
                    }
                }
                else
                {
                    _logger.LogInformation("未检测到工具调用，按普通消息处理");
                    // 处理普通消息的代码
                }

                // 如果没有有效的工具调用或没有工具调用，返回null或处理普通消息
                //  return null;
                // 如果没有 function_call，使用普通的反序列化
                var openAIResponseNormal = JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);

                if (openAIResponseNormal == null)
                {
                    _logger.LogError("OpenAI API响应解析失败，响应内容: {ResponseContent}", responseContent);
                    throw new Exception("OpenAI API响应解析失败");
                }

                if (openAIResponseNormal.Choices == null || openAIResponseNormal.Choices.Count == 0)
                {
                    _logger.LogError("OpenAI API响应中没有选择项，响应内容: {ResponseContent}", responseContent);
                    throw new Exception("OpenAI API响应中没有选择项");
                }

                var choice = openAIResponseNormal.Choices[0];
                if (choice.Message == null)
                {
                    _logger.LogError("OpenAI API响应中的消息为空，响应内容: {ResponseContent}", responseContent);
                    throw new Exception("OpenAI API响应中的消息为空");
                }

                // 记录详细的响应信息
                _logger.LogInformation("OpenAI API响应详情: Role={Role}, Content={Content}, HasFunctionCall={HasFunctionCall}, FinishReason={FinishReason}",
                    choice.Message.Role,
                    choice.Message.Content,
                    choice.Message.FunctionCalls != null,
                    choice.FinishReason);

                return openAIResponseNormal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用OpenAI API时发生错误: {Message}", ex.Message);
                throw Oops.Oh($"调用OpenAI API失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理OpenAI响应，执行函数调用
        /// </summary>
        /// <param name="response">OpenAI响应</param>
        /// <param name="userId">用户ID</param>
        /// <param name="originalMessage">用户原始消息</param>
        /// <param name="availableFunctions">可用的函数定义列表</param>
        /// <returns>处理结果</returns>
        private async Task<ProcessRequestOutput> ProcessOpenAIResponseAsync(
            OpenAIResponse response,
            string userId,
            string originalMessage,
            List<FunctionDefinitionOutput> availableFunctions)
        {
            try
            {
                if (response?.Choices == null || response.Choices.Count == 0)
                {
                    _logger.LogWarning("OpenAI API返回的响应为空或没有选择项");
                    return new ProcessRequestOutput
                    {
                        Message = "AI服务返回的响应无效，请稍后重试。",
                        FunctionCalled = false,
                        Success = false
                    };
                }

                var responseMessage = response.Choices[0].Message;

                _logger.LogInformation("收到AI响应: Role={Role}, Content={Content}, HasFunctionCall={HasFunctionCall}",
                    responseMessage.Role,
                    responseMessage.Content,
                    responseMessage.FunctionCalls != null);

                // 检查是否需要函数调用
                if (responseMessage.FunctionCalls != null)
                {
                    var messages = new List<Kedo.Application.KdwFunctionCall.Models.Message>
                    {
                        new Kedo.Application.KdwFunctionCall.Models.Message
                        {
                            Role = "system",
                            Content = "你是一个智能助手。请根据函数返回的数据，生成一个友好且信息丰富的回复。"
                        },
                        new Kedo.Application.KdwFunctionCall.Models.Message
                        {
                            Role = "user",
                            Content = originalMessage
                        }

                    };
                    foreach (var functionCall in responseMessage.FunctionCalls)
                    {
                        var functionName = functionCall.Name;
                        var functionArgs = functionCall.Arguments;

                        _logger.LogInformation("AI决定调用函数: {FunctionName}, 参数: {Arguments}", functionName, functionArgs);

                        // 查找服务配置
                        var functionConfig = _functionCallSql.GetFunctionByName(functionName).FirstOrDefault();
                        if (functionConfig == null)
                        {
                            var errorMsg = $"未找到名为 {functionName} 的功能函数配置";
                            _logger.LogWarning(errorMsg);

                            return new ProcessRequestOutput
                            {
                                Message = errorMsg,
                                FunctionCalled = false,
                                FunctionName = functionName,
                                Success = false
                            };
                        }

                        // 调用外部服务
                        var functionResult = await CallServiceApiAsync(functionConfig, functionArgs);

                        _logger.LogInformation("函数调用完成: {FunctionName}, 结果: {Result}", functionName, functionResult);

                        messages.Add(
                            new Kedo.Application.KdwFunctionCall.Models.Message
                            {
                                Role = "user",
                                // Name = functionName,
                                Content = functionResult
                            }
                            );
                    }

                    // 构建新的请求，包含函数调用结果
                    var followUpRequest = new OpenAIRequest
                    {
                        Model = _openAISettings.Model,
                        Messages = messages,
                        Temperature = _openAISettings.Temperature,
                        MaxTokens = _openAISettings.MaxTokens
                    };

                    _logger.LogInformation("准备发送后续请求，包含函数调用结果");

                    // 再次调用 OpenAI 获取最终响应
                    var followUpResponse = await CallOpenAIApiAsync(followUpRequest);
                    var finalMessage = followUpResponse.Choices[0].Message;

                    _logger.LogInformation("收到最终响应: {Content}", finalMessage.Content);

                    // 返回处理结果
                    return new ProcessRequestOutput
                    {
                        Message = finalMessage.Content,
                        FunctionCalled = true,
                        //FunctionName = functionName,
                        // FunctionResult = functionResult,
                        Success = true
                    };
                }

                _logger.LogInformation("AI直接返回消息，没有调用函数");

                // 如果没有函数调用，直接返回AI的回复
                return new ProcessRequestOutput
                {
                    Message = responseMessage.Content,
                    FunctionCalled = false,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理OpenAI响应时发生错误: {Message}", ex.Message);
                return new ProcessRequestOutput
                {
                    Message = $"处理AI响应时发生错误: {ex.Message}",
                    FunctionCalled = false,
                    Success = false
                };
            }
        }

        /// <summary>
        /// 调用服务API
        /// </summary>
        /// <param name="functionConfig">功能函数配置</param>
        /// <param name="arguments">函数参数</param>
        /// <returns>API调用结果</returns>
        private async Task<string> CallServiceApiAsync(FunctionDefinitionOutput functionConfig, string arguments)
        {
            var logId = Guid.NewGuid().ToString();
            var userId = _contextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? "anonymous";

            try
            {
                HttpResponseMessage response;
                var content = new StringContent(arguments, Encoding.UTF8, "application/json");

                // 记录函数调用开始
                _logger.LogInformation("开始调用功能函数 {FunctionName}, 参数: {Arguments}",
                    functionConfig.FunctionName, arguments);

                // 根据HTTP方法调用API
                if (functionConfig.HttpMethod.ToUpper() == "POST")
                {
                    response = await _httpClient.PostAsync(functionConfig.EndpointUrl, content);
                }
                else if (functionConfig.HttpMethod.ToUpper() == "GET")
                {
                    // 对于GET请求，将参数转换为查询字符串
                    var argsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(arguments);
                    var queryString = string.Join("&", argsDict.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value.ToString())}"));
                    response = await _httpClient.GetAsync($"{functionConfig.EndpointUrl}?{queryString}");
                }
                else
                {
                    var errorMessage = $"不支持的HTTP方法: {functionConfig.HttpMethod}";
                    _logger.LogWarning(errorMessage);

                    // 记录调用日志
                    _functionCallSql.LogFunctionCall(
                        logId,
                        userId,
                        functionConfig.FunctionName,
                        arguments,
                        errorMessage,
                        false,
                        errorMessage,
                        DateTime.Now
                    );

                    return errorMessage;
                }

                // 检查响应状态
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                // 记录调用日志
                _functionCallSql.LogFunctionCall(
                    logId,
                    userId,
                    functionConfig.FunctionName,
                    arguments,
                    result,
                    true,
                    null,
                    DateTime.Now
                );

                return result;
            }
            catch (Exception ex)
            {
                var errorMessage = $"调用功能函数 {functionConfig.FunctionName} 时发生错误: {ex.Message}";
                _logger.LogError(ex, errorMessage);

                // 记录调用日志
                _functionCallSql.LogFunctionCall(
                    logId,
                    userId,
                    functionConfig.FunctionName,
                    arguments,
                    null,
                    false,
                    ex.Message,
                    DateTime.Now
                );

                return errorMessage;
            }
        }
    }
}