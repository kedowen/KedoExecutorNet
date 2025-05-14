using Furion.DatabaseAccessor;
using Furion.DependencyInjection;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kedo.Application.OnionFlowData.Service;
using Kedo.Comm;
using Kedo.Comm.EmailMessage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Kedo.Application.DataSource.Dtos.output;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Kedo.Comm.ImageGeneration;
using Kedo.Comm.Email;
using Kedo.Comm.WechatWork;
using Kedo.Comm.LLm;
using Kedo.Comm.SqlExecutor;
using Kedo.Comm.CodeExecutor;
using Microsoft.CSharp.RuntimeBinder;
using Furion.JsonSerialization;
using Kedo.Application.OnionFlowExecutor.Dtos.input;
using Kedo.Application.OnionFlowExecutor.Dtos;

namespace Kedo.Application.OnionFlowExecutor.Services
{
 
    public class OnionFlowExecutorService : IOnionFlowExecutorService, ITransient
    {
        private readonly ISqlRepository _sqlRepository;
        private readonly ILogger<OnionFlowDataService> _logger;
        private readonly IDistributedCache _redis;
        private readonly RabbitMQHelper _rabbitMQ;
        private readonly ISql _sql;
        private readonly string MessageQueueName;
        private readonly EmailMessageHelper _emailMessageHelper;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEmailService _emailService;
        private readonly IWechatWorkService _wechatWorkService;
        private readonly ILlmService _llmService;
        private readonly ISqlExecutorService _sqlExecutorService;
        private readonly ICodeExecutorService _codeExecutorService;
        private readonly IImageGenerationService _imageGenerationService;

        private FlowDefinition _flow;
        private Dictionary<string, object> _outputs;
        private List<NodeExecutionResult> _executionResults;
        private string _flowId;
        private HashSet<string> _executedNodes = new HashSet<string>();

        public OnionFlowExecutorService(
            ISqlRepository sqlRepository,
            ILogger<OnionFlowDataService> logger,
            ISql sql,
            EmailMessageHelper emailMessageHelper,
            IDistributedCache redis,
            RabbitMQHelper rabbitMQ,
            [FromServices] IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            EmailService emailService,
            WechatWorkService wechatWorkService,
            LlmService llmService,
            SqlExecutorService sqlExecutorService,
            CodeExecutorService codeExecutorService,
            ImageGenerationService imageGenerationService)
        {
            _sqlRepository = sqlRepository;
            _logger = logger;
            _sql = sql;
            _redis = redis;
            _cache = redis; // 使用相同的缓存实例
            _rabbitMQ = rabbitMQ;
            MessageQueueName = configuration["RabbitMQConfigurations:BIData"];
            _emailMessageHelper = emailMessageHelper;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
            _wechatWorkService = wechatWorkService;
            _llmService = llmService;
            _sqlExecutorService = sqlExecutorService;
            _codeExecutorService = codeExecutorService;
            _imageGenerationService = imageGenerationService;
        }

        #region 执行智能体流程

        /// <summary>
        /// 执行流程入口
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<NodeExecutionResult> OnionAgentFlowExecuteProcess(ProcessRequestInput request)
        {
            DataTable dataTable = _sql.QueryAgentGraphicData(request.GraphicAgentId);

            if (dataTable.Rows.Count == 0)
                throw Oops.Oh("无效智能体流程ID");

            string data = dataTable.Rows[0][0].ToString();

            byte[] dataGraphic = Convert.FromBase64String(data);

            string mAgentgraphic = Encoding.UTF8.GetString(dataGraphic);

            NodeExecutionResult nodeExecutionResult = await ExecuteFlowAsync(request, mAgentgraphic);

            return nodeExecutionResult;
        }

        /// <summary>
        /// 流程结果查询
        /// </summary>
        /// <param name="agentFlowId"></param>
        /// <returns></returns>
        public string GetProcessResult(string agentFlowId)
        {
            return _redis.GetString(agentFlowId);
        }

        #endregion

        #region 核心处理函数 - 统一值处理接口

        private object EvaluateInputValue(dynamic inputValue, Dictionary<string, object> outputs, List<dynamic> variableList)
        {
            if (inputValue == null) return null;

            // 处理基本类型
            if (inputValue is string || inputValue is int || inputValue is long ||
                inputValue is float || inputValue is double || inputValue is bool)
            {
                return inputValue;
            }

            // 处理JObject类型
            if (inputValue is JObject jObj)
            {
                // 处理有type/content结构的输入
                if (jObj["type"] != null)
                {
                    string type = jObj["type"].ToString();

                    // 检查是否有content字段，如果没有，则尝试使用value字段（新格式）
                    var content = jObj["content"] ?? jObj["value"];

                    if (type == "expression")
                    {
                        // 处理表达式类型
                        string expr = content?.ToString();
                        if (string.IsNullOrEmpty(expr))
                            return string.Empty;

                        // 1. 直接在outputs中查找
                        if (outputs.ContainsKey(expr))
                        {
                            return outputs[expr];
                        }

                        // 2. 全局变量查找 - variableList.type.title
                        if (expr.StartsWith("variableList."))
                        {
                            var match = Regex.Match(expr, @"variableList\.([^\.]+)\.([^\.]+)");
                            if (match.Success)
                            {
                                string varType = match.Groups[1].Value;
                                string varTitle = match.Groups[2].Value;

                                var variable = variableList.FirstOrDefault(v =>
                                    v.type?.ToString() == varType &&
                                    v.title?.ToString() == varTitle);

                                if (variable != null)
                                {
                                    return variable.value;
                                }
                            }
                        }

                        // 3. 尝试解析为节点引用 (node.outputs.property)
                        var refMatch = Regex.Match(expr, @"([^\.]+)\.outputs\.([^\.]+)");
                        if (refMatch.Success)
                        {
                            string nodeId = refMatch.Groups[1].Value;
                            string propName = refMatch.Groups[2].Value;
                            string fullKey = $"{nodeId}.outputs.{propName}";

                            if (outputs.ContainsKey(fullKey))
                            {
                                return outputs[fullKey];
                            }
                        }

                        // 如果无法解析引用，返回原始表达式字符串
                        return expr;
                    }
                    else if (type == "string")
                    {
                        return content?.ToString() ?? string.Empty;
                    }
                    else if (type == "number")
                    {
                        if (content == null) return 0;
                        if (double.TryParse(content.ToString(), out double number))
                            return number;
                        return 0;
                    }
                    else if (type == "boolean")
                    {
                        if (content == null) return false;
                        if (bool.TryParse(content.ToString(), out bool boolean))
                            return boolean;
                        return false;
                    }
                    else if (type == "object" || type == "array")
                    {
                        return content;
                    }
                    else if (type == "file")
                    {
                        // 处理文件类型
                        return content?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        // 未知类型，返回content
                        return content;
                    }
                }
                else
                {
                    // 没有type字段的JObject，直接返回
                    return jObj;
                }
            }

            // 如果输入是JArray类型
            else if (inputValue is JArray jArr)
            {
                // 处理新结构的数组，可能是参数数组或其他
                // 尝试解析数组内容
                return jArr;
            }
            else
            {
                string key = inputValue?.ToString() ?? string.Empty;
                // 尝试从outputs中查找这个键
                if (!string.IsNullOrEmpty(key) && outputs.ContainsKey(key))
                {
                    return outputs[key];
                }
                // 如果找不到，返回原始值
                return inputValue;
            }
        }

        #endregion

        /// <summary>
        /// 执行流程图 - 严格顺序执行版本
        /// </summary>
        /// <param name="flowData">流程图JSON数据</param>
        /// <param name="processRequestInput">初始查询或输入</param>
        /// <returns>异步任务</returns>
        public async Task<NodeExecutionResult> ExecuteFlowAsync(ProcessRequestInput processRequestInput, string flowData)
        {
            try
            {
                // 解析流程数据
                _flow = JsonConvert.DeserializeObject<FlowDefinition>(flowData);

                // 准备输入数据
                Dictionary<string, object> inputData = processRequestInput?.Data ?? new Dictionary<string, object>();

                // 存储执行结果的字典
                _outputs = new Dictionary<string, object>();
                _executionResults = new List<NodeExecutionResult>();
                _flowId = processRequestInput?.AgentFlowId ?? Guid.NewGuid().ToString(); // 使用传入的ID或生成新ID
                _executedNodes.Clear(); // 清空已执行节点集合

                // 记录流程开始状态
                await _redis.SetStringAsync($"flow_{_flowId}_status", "RUNNING");
                Console.WriteLine($"开始执行流程 (ID: {_flowId})...");

                // 构建节点依赖图 - 每个节点的前置节点
                Dictionary<string, HashSet<string>> nodeDependencies = new Dictionary<string, HashSet<string>>();
                Dictionary<string, HashSet<string>> nodeSuccessors = new Dictionary<string, HashSet<string>>();

                // 初始化节点依赖关系
                foreach (var node in _flow.nodes)
                {
                    nodeDependencies[node.id] = new HashSet<string>();
                    nodeSuccessors[node.id] = new HashSet<string>();
                }

                // 填充依赖和后继关系
                foreach (var edge in _flow.edges)
                {
                    string sourceId = edge.sourceNodeID;
                    string targetId = edge.targetNodeID;

                    if (nodeDependencies.ContainsKey(targetId) && nodeSuccessors.ContainsKey(sourceId))
                    {
                        nodeDependencies[targetId].Add(sourceId);
                        nodeSuccessors[sourceId].Add(targetId);
                    }
                }

                // 查找开始节点(没有入边的节点)
                var startNode = _flow.nodes.FirstOrDefault(n => n.type == "start");
                if (startNode == null)
                {
                    throw new Exception("流程中没有找到开始节点");
                }

                // 创建节点执行队列 - 初始只有开始节点
                Queue<Node> nodeQueue = new Queue<Node>();
                nodeQueue.Enqueue(startNode);

                // 记录最后一个执行的节点结果
                NodeExecutionResult lastNodeResult = null;

                // 循环处理队列中所有节点，严格按顺序执行
                while (nodeQueue.Count > 0)
                {
                    // 从队列中取出一个节点
                    Node currentNode = nodeQueue.Dequeue();

                    // 如果节点已经执行过，则跳过
                    if (_executedNodes.Contains(currentNode.id))
                    {
                        continue;
                    }

                    // 执行当前节点，并获取执行结果
                    var nodeResult = await ExecuteNodeAsync(currentNode, inputData, _flow.variableList);
                    lastNodeResult = nodeResult;

                    // 标记节点已执行
                    _executedNodes.Add(currentNode.id);

                    // 处理条件节点的特殊情况
                    if (currentNode.type == "condition")
                    {
                        // 获取匹配的分支ID
                        string matchedBranchId = nodeResult.output.ContainsKey("matched_branch")
                            ? nodeResult.output["matched_branch"]?.ToString()
                            : null;

                        // 查找与该条件匹配的分支边
                        var matchedEdges = _flow.edges.Where(e =>
                            e.sourceNodeID == currentNode.id &&
                            (e.sourcePortID == matchedBranchId || string.IsNullOrEmpty(e.sourcePortID)))
                            .ToList();

                        // 将匹配分支的目标节点添加到队列
                        foreach (var edge in matchedEdges)
                        {
                            var targetNode = _flow.nodes.FirstOrDefault(n => n.id == edge.targetNodeID);
                            if (targetNode != null && !_executedNodes.Contains(targetNode.id))
                            {
                                nodeQueue.Enqueue(targetNode);
                            }
                        }
                    }
                    else
                    {
                        // 对于非条件节点，将所有后继节点添加到队列
                        var outgoingEdges = _flow.edges.Where(e => e.sourceNodeID == currentNode.id).ToList();

                        foreach (var edge in outgoingEdges)
                        {
                            var targetNode = _flow.nodes.FirstOrDefault(n => n.id == edge.targetNodeID);
                            if (targetNode != null && !_executedNodes.Contains(targetNode.id))
                            {
                                nodeQueue.Enqueue(targetNode);
                            }
                        }
                    }
                }

                // 完成执行
                Console.WriteLine($"流程 {_flowId} 执行完成");
                await _redis.SetStringAsync($"flow_{_flowId}_status", "COMPLETED");

                // 存储最终的执行结果摘要
                await _redis.SetStringAsync($"flow_{_flowId}_summary",
                    JsonConvert.SerializeObject(new
                    {
                        totalNodes = _executionResults.Count,
                        completedAt = DateTime.Now,
                        status = "COMPLETED"
                    }));

                // 打印最终结果
                Console.WriteLine("\n执行结果:");
                var endNode = _flow.nodes.FirstOrDefault(n => n.type == "end");
                if (endNode != null && endNode.data.outputs?.properties != null)
                {
                    foreach (var output in endNode.data.outputs.properties)
                    {
                        string outputKey = $"{endNode.id}.outputs.{output.Key}";
                        if (_outputs.ContainsKey(outputKey))
                        {
                            Console.WriteLine($"{output.Key}: {_outputs[outputKey]}");
                        }
                        else
                        {
                            // 结束节点属性应该是表达式引用
                            var refValue = output.Value.defaultValue?.ToString();
                            if (!string.IsNullOrEmpty(refValue))
                            {
                                // 将引用解析为值
                                object resolvedValue = EvaluateInputValue(
                                    new JObject { ["type"] = "expression", ["content"] = refValue },
                                    _outputs,
                                    _flow.variableList
                                );

                                // 存储解析后的值
                                _outputs[outputKey] = resolvedValue;
                                Console.WriteLine($"{output.Key}: {resolvedValue}");
                            }
                            else
                            {
                                Console.WriteLine($"{output.Key}: [无值]");
                            }
                        }
                    }
                }

                return lastNodeResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"流程执行出错: {ex.Message}");
                throw;
            }
        }

        // 执行单个节点
        private async Task<NodeExecutionResult> ExecuteNodeAsync(Node node, Dictionary<string, object> inputData, List<dynamic> variableList)
        {
            Console.WriteLine($"正在执行节点: {node.data.title} (ID: {node.id})");

            // 创建节点执行结果对象
            var nodeResult = new NodeExecutionResult
            {
                id = node.id,
                title = node.data.title,
                type = node.type,
                input = new Dictionary<string, object>(),
                output = new Dictionary<string, object>(),
                startTime = DateTime.Now
            };

            try
            {
                // 根据节点类型执行不同操作
                switch (node.type)
                {
                    case "start":
                        // 处理开始节点
                        await ExecuteStartNodeAsync(node, inputData, _outputs, nodeResult, variableList);
                        break;

                    case "http_request":
                        // 处理HTTP请求节点
                        await ExecuteHttpRequestNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "llm":
                        // 处理LLM节点
                        await ExecuteLlmNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "email":
                        // 处理邮件节点
                        await ExecuteEmailNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "wechat_work":
                        // 处理企业微信节点
                        await ExecuteWechatWorkNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "loop":
                        // 处理循环节点
                        await ExecuteLoopNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "condition":
                        // 处理条件节点
                        await ExecuteConditionNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "sql_executor":
                        // 处理SQL执行器节点
                        await ExecuteSqlExecutorNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "code":
                        // 处理代码节点
                        await ExecuteCodeNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "image_generation":
                        // 处理图像生成节点
                        await ExecuteImageGenerationNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "global":
                        // 处理全局变量节点
                        ExecuteGlobalNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    case "end":
                        // 处理结束节点
                        ExecuteEndNodeAsync(node, _outputs, nodeResult, variableList);
                        break;

                    default:
                        Console.WriteLine($"未知的节点类型: {node.type}");
                        nodeResult.output["error"] = $"不支持的节点类型: {node.type}";
                        break;
                }

                // 记录节点执行完成时间
                nodeResult.endTime = DateTime.Now;
                nodeResult.duration = (nodeResult.endTime - nodeResult.startTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                // 捕获节点执行异常，记录错误但继续执行
                Console.WriteLine($"节点 {node.id} 执行出错: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["stackTrace"] = ex.StackTrace;
                nodeResult.success = false;
                nodeResult.endTime = DateTime.Now;
                nodeResult.duration = (nodeResult.endTime - nodeResult.startTime).TotalMilliseconds;
            }

            // 添加执行结果到结果列表
            _executionResults.Add(nodeResult);

            // 将当前执行结果保存到Redis
            await SaveNodeResultToRedisAsync(node.id, nodeResult);

            return nodeResult;
        }

        private async Task SaveNodeResultToRedisAsync(string nodeId, NodeExecutionResult nodeResult)
        {
            try
            {
                // 提取输入和输出中的JSON值
                var processedInput = ExtractDictionaryValues(nodeResult.input);
                var processedOutput = ExtractDictionaryValues(nodeResult.output);

                // 创建处理后的结果对象
                var processedResult = new NodeExecutionResult
                {
                    id = nodeResult.id,
                    title = nodeResult.title,
                    type = nodeResult.type,
                    input = processedInput,
                    output = processedOutput,
                    success = nodeResult.success,
                    startTime = nodeResult.startTime,
                    endTime = nodeResult.endTime,
                    duration = nodeResult.duration
                };

                // 更新原始对象的处理后的值
                nodeResult.input = processedInput;
                nodeResult.output = processedOutput;

                // 处理执行结果列表中所有项目的JSON值
                var processedExecutionResults = _executionResults.Select(result => new NodeExecutionResult
                {
                    id = result.id,
                    title = result.title,
                    type = result.type,
                    input = ExtractDictionaryValues(result.input),
                    output = ExtractDictionaryValues(result.output),
                    success = result.success,
                    startTime = result.startTime,
                    endTime = result.endTime,
                    duration = result.duration
                }).ToList();

                // 将当前执行结果保存到Redis
                await _cache.SetStringAsync(
                    $"flow_{_flowId}_results",
                    JsonConvert.SerializeObject(processedExecutionResults)
                );

                // 单独保存节点结果，便于按节点查询
                await _cache.SetStringAsync(
                    $"flow_{_flowId}_node_{nodeId}",
                    JsonConvert.SerializeObject(processedResult)
                );

                // 存储当前所有执行结果
                await _redis.SetStringAsync(_flowId, JsonConvert.SerializeObject(processedExecutionResults));

                // 单独存储节点执行结果
                await _redis.SetStringAsync($"{_flowId}_node_{nodeId}", JsonConvert.SerializeObject(processedResult));

                // 存储节点执行状态信息
                var nodeStatus = new
                {
                    id = nodeId,
                    type = nodeResult.type,
                    title = nodeResult.title,
                    success = nodeResult.success,
                    startTime = nodeResult.startTime,
                    endTime = nodeResult.endTime,
                    duration = nodeResult.duration,
                    hasError = nodeResult.output.ContainsKey("error")
                };
                await _redis.SetStringAsync($"{_flowId}_node_{nodeId}_status",
                    JsonConvert.SerializeObject(nodeStatus));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存节点结果到Redis失败: {ex.Message}");
            }
        }

        // 执行开始节点
        private async Task ExecuteStartNodeAsync(Node node, Dictionary<string, object> inputData, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            // 设置开始节点的输出
            if (node.data.outputs?.properties != null)
            {
                // 处理每个输出属性
                foreach (var property in node.data.outputs.properties)
                {
                    string propertyKey = property.Key;
                    string outputKey = $"{node.id}.outputs.{propertyKey}";

                    // 从输入数据中获取对应的值（如果存在）
                    object propertyValue = null;
                    bool hasValue = inputData != null && inputData.TryGetValue(propertyKey, out propertyValue);

                    // 如果输入数据中有对应的值，使用它
                    if (hasValue)
                    {
                        // 如果值是字符串，但属性类型是数组或对象，尝试解析JSON
                        string propertyType = property.Value?.type?.ToString().ToLower();
                        if (propertyValue is string strValue &&
                            (propertyType == "array" || propertyType == "object"))
                        {
                            try
                            {
                                // 尝试解析JSON字符串
                                propertyValue = JsonConvert.DeserializeObject(strValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"警告: 无法将字符串解析为{propertyType}: {ex.Message}");
                                // 保持原始字符串值
                            }
                        }
                    }
                    // 如果输入数据中没有对应的值，则使用默认值
                    else
                    {
                        // 获取属性的默认值
                        var defaultValue = property.Value?.defaultValue;

                        if (defaultValue != null)
                        {
                            propertyValue = defaultValue;
                        }
                        else
                        {
                            // 如果默认值不存在，则根据类型设置一个空值
                            string propertyType = property.Value?.type?.ToString().ToLower();
                            switch (propertyType)
                            {
                                case "array":
                                    propertyValue = new JArray();
                                    break;
                                case "object":
                                    propertyValue = new JObject();
                                    break;
                                case "number":
                                    propertyValue = 0;
                                    break;
                                case "boolean":
                                    propertyValue = false;
                                    break;
                                case "file":
                                    propertyValue = string.Empty; // 文件类型默认为空字符串
                                    break;
                                case "string":
                                default:
                                    propertyValue = string.Empty;
                                    break;
                            }
                        }
                    }

                    // 设置输出值
                    outputs[outputKey] = ExtractJsonValue(propertyValue);

                    // 记录到执行结果
                    nodeResult.input[propertyKey] = property.Value;
                    nodeResult.output[propertyKey] = ExtractJsonValue(propertyValue);

                    Console.WriteLine($"设置开始节点输出: {propertyKey} = {JsonConvert.SerializeObject(propertyValue)}");
                }
            }

            // 确保节点结果标记为成功
            nodeResult.success = true;
        }

        private async Task ExecuteHttpRequestNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 从注入的HttpClientFactory创建HttpClient
                HttpClient httpClient;

                if (_httpClientFactory != null)
                {
                    httpClient = _httpClientFactory.CreateClient("FlowExecution");
                }
                else
                {
                    // 如果没有注册HttpClientFactory，则创建一个临时的HttpClient
                    httpClient = new HttpClient();
                    Console.WriteLine("警告: 正在使用临时HttpClient，这在生产环境中不推荐");
                }

                // 收集所有自定义输入参数
                var customInputs = new Dictionary<string, object>();
                foreach (var prop in ((JObject)node.data.inputsValues).Properties())
                {
                    if (prop.Name != "method" && prop.Name != "url" && prop.Name != "headers" &&
                        prop.Name != "enableAuth" && prop.Name != "bodyType" && prop.Name != "bodyContent" &&
                        prop.Name != "timeout" && prop.Name != "maxRetries")
                    {
                        customInputs[prop.Name] = EvaluateInputValue(prop.Value, outputs, variableList);
                    }
                }

                // 获取HTTP请求参数
                var method = EvaluateInputValue(node.data.inputsValues.method, outputs, variableList)?.ToString() ?? "GET";
                var rawUrl = EvaluateInputValue(node.data.inputsValues.url, outputs, variableList)?.ToString() ?? "";

                // 解析URL中的模板变量
                var url = ResolveTemplateVariables(rawUrl, customInputs, outputs);

                if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentException("HTTP请求URL不能为空");
                }

                // 处理URL参数 - 使用索引器语法避免关键字问题
                var queryParams = new Dictionary<string, string>();
                var bodyParams = new Dictionary<string, string>();
                JArray paramsArray = null;

                // 尝试获取params参数数组
                if (node.data.inputsValues is JObject inputValues)
                {
                    if (inputValues["params"] != null)
                    {
                        paramsArray = inputValues["params"] as JArray;
                    }
                }

                // 判断请求方法类型
                bool isGetMethod = method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
                                   method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);

                // 判断是否已经指定了请求体
                string bodyType = EvaluateInputValue(node.data.inputsValues.bodyType, outputs, variableList)?.ToString() ?? "none";
                bool hasSpecifiedBody = bodyType != "none" &&
                                       node.data.inputsValues.bodyContent != null;

                // 处理参数
                if (paramsArray != null)
                {
                    foreach (var param in paramsArray)
                    {
                        string paramKey = param["key"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(paramKey))
                            continue;

                        // 获取参数值，使用统一的值评估函数
                        object paramValueObj = EvaluateInputValue(param["value"], outputs, variableList);
                        string paramValue = paramValueObj?.ToString() ?? "";

                        nodeResult.input[$"param_{paramKey}"] = paramValue;

                        // 对于GET请求，或者已经指定了请求体的情况，将参数添加到URL
                        if (isGetMethod || hasSpecifiedBody)
                        {
                            queryParams[paramKey] = paramValue;
                        }
                        else
                        {
                            // 对于POST/PUT/PATCH等请求，如果没有指定请求体，将参数添加到请求体
                            bodyParams[paramKey] = paramValue;
                        }
                    }
                }

                // 构建请求URL
                var requestUrl = url;
                if (queryParams.Count > 0)
                {
                    requestUrl += (url.Contains("?") ? "&" : "?") +
                                  string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                }

                nodeResult.input["url"] = requestUrl;
                nodeResult.input["method"] = method;

                // 设置超时
                if (node.data.inputsValues.timeout != null)
                {
                    var timeout = EvaluateInputValue(node.data.inputsValues.timeout, outputs, variableList);
                    int timeoutMs = Convert.ToInt32(timeout);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                    nodeResult.input["timeout"] = timeoutMs;
                }

                // 移除并收集所有原始请求头
                if (httpClient.DefaultRequestHeaders.Any())
                {
                    httpClient = new HttpClient();
                }

                Dictionary<string, string> headerCollection = new Dictionary<string, string>();

                // 设置请求头
                if (node.data.inputsValues.headers != null)
                {
                    foreach (var header in node.data.inputsValues.headers)
                    {
                        string headerKey = header.key?.ToString() ?? "";
                        if (string.IsNullOrEmpty(headerKey))
                            continue;

                        // 解析请求头值，使用统一的值评估函数
                        string headerValue = EvaluateInputValue(header.value, outputs, variableList)?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            headerCollection[headerKey] = headerValue;
                            nodeResult.input[$"header_{headerKey}"] = headerValue;
                        }
                    }
                }

                // 处理认证
                if (node.data.inputsValues.enableAuth == true)
                {
                    string authType = EvaluateInputValue(node.data.inputsValues.authType, outputs, variableList)?.ToString() ?? "";
                    string authValue = EvaluateInputValue(node.data.inputsValues.authValue, outputs, variableList)?.ToString() ?? "";
                    string authKey = EvaluateInputValue(node.data.inputsValues.authKey, outputs, variableList)?.ToString() ?? "";
                    string authAddTo = EvaluateInputValue(node.data.inputsValues.authAddTo, outputs, variableList)?.ToString() ?? "Header";

                    if (string.IsNullOrEmpty(authValue))
                    {
                        Console.WriteLine("警告: 启用了认证但认证值为空");
                    }
                    else
                    {
                        if (authType == "Bearer Token")
                        {
                            if (authAddTo == "Header")
                            {
                                headerCollection["Authorization"] = $"Bearer {authValue}";
                            }
                            else if (authAddTo == "Query")
                            {
                                requestUrl += (requestUrl.Contains("?") ? "&" : "?") +
                                             $"access_token={Uri.EscapeDataString(authValue)}";
                            }
                        }
                        else if (authType == "自定义" && !string.IsNullOrEmpty(authKey))
                        {
                            if (authAddTo == "Header")
                            {
                                headerCollection[authKey] = authValue;
                            }
                            else if (authAddTo == "Query")
                            {
                                requestUrl += (requestUrl.Contains("?") ? "&" : "?") +
                                             $"{authKey}={Uri.EscapeDataString(authValue)}";
                            }
                        }

                        nodeResult.input["auth_type"] = authType;
                        // 不记录敏感的认证值
                        nodeResult.input["auth_enabled"] = true;
                    }
                }

                // 执行请求
                HttpResponseMessage response = null;
                HttpContent content = null;

                // 设置重试次数
                int maxRetries = 0;
                if (node.data.inputsValues.maxRetries != null)
                {
                    var retries = EvaluateInputValue(node.data.inputsValues.maxRetries, outputs, variableList);
                    maxRetries = Convert.ToInt32(retries);
                    nodeResult.input["maxRetries"] = maxRetries;
                }

                // 重试逻辑
                int retryCount = 0;
                bool requestSuccess = false;
                Exception lastException = null;

                while (!requestSuccess && retryCount <= maxRetries)
                {
                    try
                    {
                        if (retryCount > 0)
                        {
                            Console.WriteLine($"HTTP请求重试 ({retryCount}/{maxRetries})...");
                            // 指数退避策略
                            await Task.Delay(Math.Min(1000 * (int)Math.Pow(2, retryCount - 1), 30000));
                        }

                        if (isGetMethod)
                        {
                            // 创建请求消息对象
                            var request = new HttpRequestMessage();

                            // GET或HEAD请求
                            if (method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                            {
                                request.Method = HttpMethod.Head;
                            }
                            else
                            {
                                request.Method = HttpMethod.Get;
                            }

                            request.RequestUri = new Uri(requestUrl);

                            // 添加所有收集的请求头
                            foreach (var header in headerCollection)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }

                            response = await httpClient.SendAsync(request);
                        }
                        else
                        {
                            // 创建请求消息对象
                            var request = new HttpRequestMessage();
                            request.RequestUri = new Uri(requestUrl);

                            // 根据方法设置HTTP方法
                            switch (method.ToUpper())
                            {
                                case "POST":
                                    request.Method = HttpMethod.Post;
                                    break;
                                case "PUT":
                                    request.Method = HttpMethod.Put;
                                    break;
                                case "DELETE":
                                    request.Method = HttpMethod.Delete;
                                    break;
                                case "PATCH":
                                    request.Method = new HttpMethod("PATCH");
                                    break;
                                default:
                                    throw new NotSupportedException($"不支持的HTTP方法: {method}");
                            }

                            // 创建请求内容
                            if (hasSpecifiedBody)
                            {
                                // 使用指定的请求体
                                content = CreateHttpContent(bodyType, node.data.inputsValues.bodyContent, outputs, variableList);
                                nodeResult.input["body_type"] = bodyType;
                                nodeResult.input["body_content"] = await content.ReadAsStringAsync();
                            }
                            else if (bodyParams.Count > 0)
                            {
                                // 使用从params收集的参数作为请求体
                                string jsonBody = JsonConvert.SerializeObject(bodyParams);
                                content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                                nodeResult.input["body_type"] = "JSON";
                                nodeResult.input["body_content"] = jsonBody;
                            }
                            else
                            {
                                // 没有参数，使用空请求体
                                content = new StringContent("", Encoding.UTF8, "application/json");
                                nodeResult.input["body_type"] = "none";
                            }

                            // 设置请求内容
                            request.Content = content;

                            // 添加所有收集的请求头
                            foreach (var header in headerCollection)
                            {
                                // 避免设置内容相关的头，因为那些会从content中设置
                                if (!header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                                {
                                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                }
                            }

                            Console.WriteLine($"正在发送 {method} 请求到 {requestUrl}");
                            Console.WriteLine($"请求体类型: {content.Headers.ContentType?.MediaType ?? "未设置"}");

                            // 发送请求
                            response = await httpClient.SendAsync(request);
                        }

                        requestSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        retryCount++;

                        if (retryCount > maxRetries)
                        {
                            Console.WriteLine($"HTTP请求失败，已达到最大重试次数: {ex.Message}");
                            throw new Exception($"HTTP请求失败，重试{maxRetries}次后仍然失败: {ex.Message}", ex);
                        }
                    }
                }

                if (!requestSuccess)
                {
                    throw lastException;
                }

                // 确保response不为null
                if (response == null)
                {
                    throw new InvalidOperationException("HTTP响应对象为null，这是一个意外情况");
                }

                // 处理响应
                var statusCode = (int)response.StatusCode;
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"响应状态码: {statusCode}");
                Console.WriteLine($"响应内容: {responseContent}");

                // 如果是错误状态码，记录详细信息
                if (statusCode >= 400)
                {
                    Console.WriteLine($"请求失败: {response.StatusCode} {response.ReasonPhrase}");
                    Console.WriteLine($"响应头: {string.Join(", ", response.Headers.Select(h => $"{h.Key}:{string.Join(",", h.Value)}"))}");
                }

                // 尝试解析JSON响应
                object parsedContent = responseContent;
                try
                {
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        if (responseContent.StartsWith("{") || responseContent.StartsWith("["))
                        {
                            parsedContent = JsonConvert.DeserializeObject(responseContent);
                        }
                    }
                }
                catch
                {
                    // 如果JSON解析失败，使用原始字符串
                    Console.WriteLine("警告: 响应内容不是有效的JSON，使用原始字符串");
                }

                // 存储输出
                outputs[$"{node.id}.outputs.body"] = parsedContent;
                outputs[$"{node.id}.outputs.statusCode"] = statusCode;

                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                // 添加内容头
                foreach (var header in response.Content.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                outputs[$"{node.id}.outputs.headers"] = responseHeaders;

                // 记录输出到执行结果
                nodeResult.output["body"] = parsedContent;
                nodeResult.output["statusCode"] = statusCode;
                nodeResult.output["headers"] = responseHeaders;

                // 检查状态码是否表示成功
                bool isSuccess = statusCode >= 200 && statusCode < 300;
                nodeResult.output["success"] = isSuccess;
                outputs[$"{node.id}.outputs.success"] = isSuccess;

                Console.WriteLine($"HTTP请求完成: {method} {url}, 状态码: {statusCode}");

                // 清理资源
                if (_httpClientFactory == null && httpClient != null)
                {
                    httpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP请求失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                outputs[$"{node.id}.outputs.success"] = false;
                outputs[$"{node.id}.outputs.error"] = ex.Message;
                throw;
            }
        }

        // 修改后的CreateHttpContent方法
        private HttpContent CreateHttpContent(string bodyType, object bodyContentObj, Dictionary<string, object> outputs, List<dynamic> variableList)
        {
            // 获取并处理请求体内容
            string bodyContent = EvaluateInputValue(bodyContentObj, outputs, variableList)?.ToString() ?? "";

            switch (bodyType?.ToLower())
            {
                case "json":
                    // 验证JSON格式
                    try
                    {
                        // 尝试解析JSON以确保格式正确
                        JsonConvert.DeserializeObject(bodyContent);
                        return new StringContent(bodyContent, Encoding.UTF8, "application/json");
                    }
                    catch (JsonException)
                    {
                        // 如果bodyContent不是有效JSON字符串，可能是需要序列化的对象
                        try
                        {
                            string jsonStr = JsonConvert.SerializeObject(bodyContent);
                            return new StringContent(jsonStr, Encoding.UTF8, "application/json");
                        }
                        catch
                        {
                            // 降级处理
                            Console.WriteLine("警告: 无效的JSON格式，将按原始文本发送");
                            return new StringContent(bodyContent, Encoding.UTF8, "application/json");
                        }
                    }

                case "form":
                case "form-data":
                    // 处理表单数据
                    var formContent = new MultipartFormDataContent();

                    try
                    {
                        // 尝试解析为JSON对象
                        Dictionary<string, string> formData = JsonConvert.DeserializeObject<Dictionary<string, string>>(bodyContent);
                        foreach (var item in formData)
                        {
                            formContent.Add(new StringContent(item.Value), item.Key);
                        }
                    }
                    catch
                    {
                        // 如果解析失败，尝试按照name=value&name2=value2格式解析
                        string[] pairs = bodyContent.Split('&');
                        foreach (var pair in pairs)
                        {
                            string[] keyValue = pair.Split('=');
                            if (keyValue.Length == 2)
                            {
                                formContent.Add(new StringContent(Uri.UnescapeDataString(keyValue[1])), keyValue[0]);
                            }
                        }
                    }

                    return formContent;

                case "x-www-form-urlencoded":
                    // 处理URL编码表单
                    var formUrlEncodedContent = new FormUrlEncodedContent(
                        bodyContent.Split('&')
                            .Select(pair => pair.Split('='))
                            .Where(pair => pair.Length == 2)
                            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]))
                    );
                    return formUrlEncodedContent;

                case "text":
                    // 纯文本
                    return new StringContent(bodyContent, Encoding.UTF8, "text/plain");

                case "xml":
                    // XML
                    return new StringContent(bodyContent, Encoding.UTF8, "application/xml");

                case "none":
                default:
                    // 默认或无内容类型
                    return new StringContent(bodyContent, Encoding.UTF8);
            }
        }
        // 执行LLM节点
        private async Task ExecuteLlmNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 使用注入的LLM服务
                if (_llmService == null)
                {
                    throw new InvalidOperationException("LLM服务未注册");
                }

                // 解析LLM参数，使用统一的值评估函数
                object modelData = EvaluateInputValue(node.data.inputsValues.modelType, outputs, variableList)?.ToString() ?? "gpt-4o";

                //提取模型信息
                dynamic obj = JsonConvert.DeserializeObject(modelData.ToString());
                string modelType = obj.value;
                string modelValue = obj.label;


                string temperatureStr = EvaluateInputValue(node.data.inputsValues.temperature, outputs, variableList)?.ToString() ?? "0.7";
                double temperature = double.Parse(temperatureStr);

                // 解析系统提示词和用户提示词，使用统一的值评估函数
                string systemPrompt = EvaluateInputValue(node.data.inputsValues.systemPrompt, outputs, variableList)?.ToString() ?? "";
                string userPrompt = EvaluateInputValue(node.data.inputsValues.prompt, outputs, variableList)?.ToString() ?? "";

                if (node.data.inputsValues.customParams != null && node.data.inputsValues.customParams is JArray)
                {
                    JArray customParamsArray = (JArray)node.data.inputsValues.customParams;
                    foreach (var param in customParamsArray)
                    {
                        // 处理逻辑
                        //    }
                        //}



                        //// 处理新格式的params参数数组
                        //if (node.data.inputsValues.@params != null && node.data.inputsValues.@params is JArray paramsArray)
                        //{
                        //    foreach (var param in paramsArray)
                        //    {
                        string paramTitle = param["title"]?.ToString();
                        string paramType = param["type"]?.ToString();

                        if (string.IsNullOrEmpty(paramTitle))
                            continue;

                        // 如果是表达式类型参数，需要评估表达式
                        if (paramType == "expression")
                        {
                            string paramValue = param["value"]?.ToString();
                            if (!string.IsNullOrEmpty(paramValue))
                            {
                                // 解析表达式引用的值
                                object resolvedValue = EvaluateInputValue(
                                    new JObject { ["type"] = "expression", ["content"] = paramValue },
                                    outputs,
                                    variableList
                                );

                                // 记录到节点输入中
                                nodeResult.input[$"param_{paramTitle}"] = resolvedValue;

                                // 替换提示词中的模板变量
                                if (resolvedValue != null)
                                {
                                    userPrompt = userPrompt.Replace($"{{{{{paramTitle}}}}}", resolvedValue.ToString());
                                }
                            }
                        }
                        else
                        {
                            // 直接使用值
                            object paramValue = param["value"];
                            nodeResult.input[$"param_{paramTitle}"] = paramValue;

                            if (paramValue != null)
                            {
                                userPrompt = userPrompt.Replace($"{{{{{paramTitle}}}}}", paramValue.ToString());
                            }
                        }
                    }
                }

                nodeResult.input["modelType"] = modelType;
                nodeResult.input["temperature"] = temperature;
                nodeResult.input["systemPrompt"] = systemPrompt;
                nodeResult.input["userPrompt"] = userPrompt;

                // 调用LLM服务
                string llmResponse = await _llmService.GenerateTextAsync(
                    modelType,modelValue,
                    systemPrompt,
                    userPrompt,
                    temperature
                );

                // 存储输出
                outputs[$"{node.id}.outputs.result"] = llmResponse;
                nodeResult.output["result"] = llmResponse;

                // 假设token计算为字符数/4（仅用于模拟）
                int tokens = llmResponse.Length / 4;
                outputs[$"{node.id}.outputs.tokens"] = tokens;
                nodeResult.output["tokens"] = tokens;
                nodeResult.success = true;

                Console.WriteLine($"LLM处理完成: 模型 {modelType}, 温度 {temperature}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM处理失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行邮件节点
        private async Task ExecuteEmailNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 解析邮件参数，使用统一的值评估函数
                string emailRecipients = EvaluateInputValue(node.data.inputsValues.email, outputs, variableList)?.ToString() ?? "";
                string subject = EvaluateInputValue(node.data.inputsValues.subject, outputs, variableList)?.ToString() ?? "";
                string content = EvaluateInputValue(node.data.inputsValues.content, outputs, variableList)?.ToString() ?? "";

                if (string.IsNullOrEmpty(emailRecipients))
                {
                    throw new ArgumentException("邮件收件人不能为空");
                }

                nodeResult.input["recipients"] = emailRecipients;
                nodeResult.input["subject"] = subject;
                nodeResult.input["content"] = content;

                // 使用注入的邮件服务发送邮件
                bool success = await _emailService.SendEmailAsync(emailRecipients, subject, content);

                // 存储输出
                outputs[$"{node.id}.outputs.success"] = success;
                outputs[$"{node.id}.outputs.result"] = success ? "邮件发送成功" : "邮件发送失败";

                nodeResult.output["success"] = success;
                nodeResult.output["result"] = success ? "邮件发送成功" : "邮件发送失败";
                nodeResult.success = success;

                Console.WriteLine($"邮件发送: 收件人 {emailRecipients}, 结果: {(success ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"邮件发送失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                outputs[$"{node.id}.outputs.success"] = false;
                outputs[$"{node.id}.outputs.result"] = $"邮件发送失败: {ex.Message}";
                throw;
            }
        }

        // 执行企业微信节点
        private async Task ExecuteWechatWorkNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 解析企业微信参数，使用统一的值评估函数
                string recipients = EvaluateInputValue(node.data.inputsValues.recipient, outputs, variableList)?.ToString() ?? "";
                string subject = EvaluateInputValue(node.data.inputsValues.subject, outputs, variableList)?.ToString() ?? "";
                string content = EvaluateInputValue(node.data.inputsValues.content, outputs, variableList)?.ToString() ?? "";

                if (string.IsNullOrEmpty(recipients))
                {
                    throw new ArgumentException("企业微信接收人不能为空");
                }

                nodeResult.input["recipients"] = recipients;
                nodeResult.input["subject"] = subject;
                nodeResult.input["content"] = content;

                // 使用注入的企业微信服务发送消息
                bool success = await _wechatWorkService.SendMessageAsync(recipients, subject, content);

                // 存储输出
                outputs[$"{node.id}.outputs.success"] = success;
                outputs[$"{node.id}.outputs.result"] = success ? "企业微信消息发送成功" : "企业微信消息发送失败";

                nodeResult.output["success"] = success;
                nodeResult.output["result"] = success ? "企业微信消息发送成功" : "企业微信消息发送失败";
                nodeResult.success = success;

                Console.WriteLine($"企业微信消息发送: 接收人 {recipients}, 结果: {(success ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"企业微信消息发送失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                outputs[$"{node.id}.outputs.success"] = false;
                outputs[$"{node.id}.outputs.result"] = $"企业微信消息发送失败: {ex.Message}";
                throw;
            }
        }

  
        // 构建内部节点依赖关系图
        private static Dictionary<string, HashSet<string>> BuildInternalDependencyGraph(List<Node> nodes, List<Edge> edges)
        {
            var dependencies = new Dictionary<string, HashSet<string>>();

            // 初始化每个节点的依赖集合
            foreach (var node in nodes)
            {
                dependencies[node.id] = new HashSet<string>();
            }

            // 填充依赖关系 - 对于每条边，目标节点依赖于源节点
            foreach (var edge in edges)
            {
                if (dependencies.ContainsKey(edge.targetNodeID))
                {
                    dependencies[edge.targetNodeID].Add(edge.sourceNodeID);
                }
            }

            return dependencies;
        }

        // 拓扑排序 - 返回节点的执行顺序
        private static List<string> TopologicalSort(List<string> nodeIds, Dictionary<string, HashSet<string>> dependencies)
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var temp = new HashSet<string>();

            // 检查循环依赖并排序
            foreach (var nodeId in nodeIds)
            {
                if (!DFSTopologicalSort(nodeId, dependencies, visited, temp, result))
                {
                    // 如果检测到循环依赖，返回null
                    return null;
                }
            }

            // 返回排序后的节点ID列表
            return result;
        }

        // 深度优先搜索辅助方法
        private static bool DFSTopologicalSort(string nodeId,
                                             Dictionary<string, HashSet<string>> dependencies,
                                             HashSet<string> visited,
                                             HashSet<string> temp,
                                             List<string> result)
        {
            // 如果节点已处理完毕，无需再次访问
            if (visited.Contains(nodeId))
                return true;

            // 如果节点在当前DFS路径中已被访问，说明有循环依赖
            if (temp.Contains(nodeId))
                return false;

            // 标记当前节点为临时访问
            temp.Add(nodeId);

            // 递归处理所有依赖
            if (dependencies.ContainsKey(nodeId))
            {
                foreach (var dependency in dependencies[nodeId])
                {
                    if (!DFSTopologicalSort(dependency, dependencies, visited, temp, result))
                    {
                        return false;
                    }
                }
            }

            // 移除临时标记，添加到已访问集合
            temp.Remove(nodeId);
            visited.Add(nodeId);

            // 将节点添加到结果列表
            result.Add(nodeId);

            return true;
        }

        private async Task ExecuteLoopNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 获取循环集合，使用统一的值评估函数
                object loopItems = EvaluateInputValue(node.data.inputsValues.loopTimes, outputs, variableList);
                IEnumerable<object> itemsToIterate = null;

                // 解析循环集合
                if (loopItems != null)
                {
                    // 先检查是否已经是IEnumerable<object>类型
                    if (loopItems is IEnumerable<object> enumerable && !(loopItems is string))
                    {
                        itemsToIterate = enumerable;
                    }
                    // 检查是否是JArray类型
                    else if (loopItems is JArray jArray)
                    {
                        itemsToIterate = jArray.ToObject<List<object>>();
                    }
                    // 检查是否是字符串形式的JSON数组
                    else if (loopItems is string str && str.StartsWith("[") && str.EndsWith("]"))
                    {
                        // 尝试解析JSON数组字符串
                        try
                        {
                            itemsToIterate = JsonConvert.DeserializeObject<List<object>>(str);
                        }
                        catch
                        {
                            // 如果解析失败，尝试将字符串按分隔符分割
                            itemsToIterate = str.Trim('[', ']').Split(',').Select(s => (object)s.Trim());
                        }
                    }
                    // 检查是否是数字（循环次数）
                    else if (int.TryParse(loopItems.ToString(), out int numTimes) && numTimes > 0)
                    {
                        // 如果是数字，创建一个包含数字0到numTimes-1的数组
                        itemsToIterate = Enumerable.Range(0, numTimes).Select(i => (object)i);
                    }
                    // 所有其他情况，包括普通字符串，都作为单个元素处理
                    else
                    {
                        // 重要修改：确保字符串被作为单个元素处理，而不是字符序列
                        itemsToIterate = new object[] { loopItems };
                    }
                }

                // 如果无法解析为集合，使用默认值
                if (itemsToIterate == null)
                {
                    Console.WriteLine("警告: 无法解析循环集合，使用空集合");
                    itemsToIterate = Array.Empty<object>();
                }

                // 记录实际的迭代项数量和类型，方便调试
                Console.WriteLine($"循环项数量: {itemsToIterate.Count()}, 第一项类型: {(itemsToIterate.Any() ? itemsToIterate.First()?.GetType().FullName ?? "null" : "无项目")}");

                nodeResult.input["loopItems"] = itemsToIterate;

                // 获取循环内部的节点和边
                var blocks = node.blocks ?? new List<Node>();

                // 如果没有内部节点，直接返回
                if (blocks.Count == 0)
                {
                    Console.WriteLine($"循环节点 {node.id} 没有内部节点，跳过执行");
                    nodeResult.output["result"] = "[]";
                    outputs[$"{node.id}.outputs.result"] = "[]";
                    return;
                }

                // 构建内部节点的依赖图用于拓扑排序
                var blockEdges = node.edges ?? new List<Edge>();
                var blockDependencies = BuildInternalDependencyGraph(blocks, blockEdges);

                // 执行拓扑排序确定内部节点执行顺序
                var sortedBlockNodes = TopologicalSort(blocks.Select(n => n.id).ToList(), blockDependencies);
                if (sortedBlockNodes == null)
                {
                    throw new Exception("循环内部节点检测到循环依赖，无法执行");
                }

                // 收集循环执行结果 - 修改：使用List<Dictionary<string, object>>存储每次迭代的完整结果
                var loopResults = new List<Dictionary<string, object>>();
                int itemIndex = 0;

                // 对集合中的每个项目执行循环
                foreach (var item in itemsToIterate)
                {
                    Console.WriteLine($"执行循环 {node.id} - 项目 {itemIndex + 1}/{itemsToIterate.Count()}, 项目类型: {item?.GetType().FullName ?? "null"}, 值: {item}");

                    // 创建循环迭代的局部输出字典
                    var iterationOutputs = new Dictionary<string, object>(outputs);

                    // 添加当前循环项到输出字典，以便内部节点引用
                    iterationOutputs["item"] = item;
                    iterationOutputs["index"] = itemIndex;

                    // 为每次迭代创建执行结果记录
                    var iterationResults = new List<NodeExecutionResult>();

                    // 创建存储本次迭代所有节点结果的字典
                    var iterationResultsMap = new Dictionary<string, object>();

                    // 执行内部节点
                    foreach (var blockNodeId in sortedBlockNodes)
                    {
                        var blockNode = blocks.First(n => n.id == blockNodeId);
                        Console.WriteLine($"循环内执行节点: {blockNode.data.title} (ID: {blockNode.id})");

                        // 创建节点执行结果对象
                        var blockNodeResult = new NodeExecutionResult
                        {
                            id = blockNode.id,
                            title = blockNode.data.title,
                            type = blockNode.type,
                            input = new Dictionary<string, object>(),
                            output = new Dictionary<string, object>(),
                            startTime = DateTime.Now
                        };

                        try
                        {
                            // 根据节点类型执行不同操作
                            switch (blockNode.type)
                            {
                                case "http_request":
                                    await ExecuteHttpRequestNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "llm":
                                    await ExecuteLlmNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "email":
                                    await ExecuteEmailNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "wechat_work":
                                    await ExecuteWechatWorkNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "condition":
                                    await ExecuteConditionNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "sql_executor":
                                    await ExecuteSqlExecutorNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "code":
                                    await ExecuteCodeNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "image_generation":
                                    await ExecuteImageGenerationNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                case "global":
                                    ExecuteGlobalNodeAsync(blockNode, iterationOutputs, blockNodeResult, variableList);
                                    break;

                                default:
                                    Console.WriteLine($"循环内未知的节点类型: {blockNode.type}");
                                    blockNodeResult.output["error"] = $"不支持的节点类型: {blockNode.type}";
                                    blockNodeResult.success = false;
                                    break;
                            }

                            // 记录节点执行完成时间
                            blockNodeResult.endTime = DateTime.Now;
                            blockNodeResult.duration = (blockNodeResult.endTime - blockNodeResult.startTime).TotalMilliseconds;
                        }
                        catch (Exception ex)
                        {
                            // 捕获节点执行异常，记录错误但继续执行
                            Console.WriteLine($"循环内节点 {blockNode.id} 执行出错: {ex.Message}");
                            blockNodeResult.output["error"] = ex.Message;
                            blockNodeResult.output["stackTrace"] = ex.StackTrace;
                            blockNodeResult.success = false;
                            blockNodeResult.endTime = DateTime.Now;
                            blockNodeResult.duration = (blockNodeResult.endTime - blockNodeResult.startTime).TotalMilliseconds;
                        }

                        // 添加执行结果到迭代结果列表
                        iterationResults.Add(blockNodeResult);

                        // **修改部分**：收集节点的输出到结果映射中
                        if (blockNode.data.outputs?.properties != null)
                        {
                            foreach (var outputProp in blockNode.data.outputs.properties)
                            {
                                string outputKey = $"{blockNodeId}.outputs.{outputProp.Key}";
                                if (iterationOutputs.ContainsKey(outputKey))
                                {
                                    // 以节点ID和属性名作为键存储结果，如 "llm_-zCXj.result"
                                    iterationResultsMap[$"{blockNode.id}.{outputProp.Key}"] = iterationOutputs[outputKey];
                                }
                            }
                        }
                    }

                    // **修改部分**：将完整的迭代结果添加到循环结果列表
                    loopResults.Add(iterationResultsMap);
                    itemIndex++;
                }

                // **修改部分**：将完整的循环结果存储到输出
                string loopResultJson = JsonConvert.SerializeObject(loopResults);
                outputs[$"{node.id}.outputs.result"] = loopResultJson;
                nodeResult.output["result"] = loopResultJson;

                Console.WriteLine($"循环节点 {node.id} 执行完成，共处理 {itemsToIterate.Count()} 个项目");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"循环节点执行失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行条件节点
        private async Task<string> ExecuteConditionNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 默认返回else分支ID
                string matchedBranchId = null;
                bool conditionMatched = false;

                // 解析条件配置
                if (node.data.inputsValues?.conditions != null)
                {
                    var conditions = node.data.inputsValues.conditions;

                    // 遍历所有条件分支
                    foreach (var condition in conditions)
                    {
                        string conditionType = condition.type?.ToString();
                        string branchId = condition.key?.ToString();

                        // 记录条件到节点结果
                        nodeResult.input[$"branch_{branchId}"] = conditionType;

                        // 如果是if或elseif分支，需要评估条件
                        if ((conditionType == "if" || conditionType == "elseif") && !conditionMatched)
                        {
                            // 获取条件值数组
                            var conditionValues = condition.value;
                            bool branchConditionMet = true;

                            // 如果有条件值数组，需要评估每个条件
                            if (conditionValues != null && conditionValues.Count > 0)
                            {
                                // 记录上一个条件的结果，用于处理逻辑关系
                                bool previousConditionResult = true;
                                bool skipRemainingConditions = false;

                                // 遍历所有条件
                                for (int i = 0; i < conditionValues.Count; i++)
                                {
                                    var condValue = conditionValues[i];

                                    // 获取与上一个条件的逻辑关系（第一个条件没有逻辑关系）
                                    string logicalRelation = i > 0 ? condValue.relation?.ToString() : null;

                                    // 根据逻辑关系和上一个条件结果决定是否需要评估当前条件
                                    if (i > 0)
                                    {
                                        // 如果上一个条件为false且是"且"关系，整个分支条件不满足，可以跳过后续评估
                                        if (!previousConditionResult && logicalRelation == "&&")
                                        {
                                            branchConditionMet = false;
                                            skipRemainingConditions = true;
                                            break;
                                        }

                                        // 如果上一个条件为true且是"或"关系，整个分支条件已满足，可以跳过后续评估
                                        if (previousConditionResult && logicalRelation == "||")
                                        {
                                            branchConditionMet = true;
                                            skipRemainingConditions = true;
                                            break;
                                        }
                                    }

                                    // 使用统一的值评估函数解析表达式
                                    object leftValue = EvaluateInputValue(condValue.conationLeft, outputs, variableList);
                                    string relation = condValue.conationRelation?.ToString() ?? "==";
                                    object rightValue = EvaluateInputValue(condValue.conationRight, outputs, variableList);

                                    // 比较值
                                    bool conditionMet = CompareValues(leftValue, relation, rightValue);

                                    // 记录条件评估结果
                                    nodeResult.input[$"condition_{condValue.key}_left"] = leftValue;
                                    nodeResult.input[$"condition_{condValue.key}_relation"] = relation;
                                    nodeResult.input[$"condition_{condValue.key}_right"] = rightValue;
                                    nodeResult.input[$"condition_{condValue.key}_result"] = conditionMet;

                                    // 根据逻辑关系更新分支条件结果
                                    if (i == 0)
                                    {
                                        // 第一个条件直接设置结果
                                        branchConditionMet = conditionMet;
                                    }
                                    else if (logicalRelation == "&&")
                                    {
                                        // "且"关系：两个条件都必须为true
                                        branchConditionMet = branchConditionMet && conditionMet;
                                    }
                                    else if (logicalRelation == "||")
                                    {
                                        // "或"关系：任一条件为true即可
                                        branchConditionMet = branchConditionMet || conditionMet;
                                    }

                                    // 更新上一个条件的结果
                                    previousConditionResult = conditionMet;
                                }

                                // 记录是否跳过了后续条件评估
                                if (skipRemainingConditions)
                                {
                                    nodeResult.input["skipped_conditions"] = true;
                                }
                            }

                            // 如果分支条件满足，记录匹配的分支ID
                            if (branchConditionMet)
                            {
                                matchedBranchId = branchId;
                                conditionMatched = true;
                                nodeResult.output["matched_branch"] = branchId;
                                nodeResult.output["matched_condition_type"] = conditionType;
                            }
                        }
                        // 如果是else分支且没有其他分支匹配，则使用else分支
                        else if (conditionType == "else" && !conditionMatched)
                        {
                            matchedBranchId = branchId;
                            conditionMatched = true;
                            nodeResult.output["matched_branch"] = branchId;
                            nodeResult.output["matched_condition_type"] = "else";
                        }
                    }
                }

                // 设置节点输出
                nodeResult.output["result"] = matchedBranchId;
                outputs[$"{node.id}.outputs.result"] = matchedBranchId;
                nodeResult.success = true;

                Console.WriteLine($"条件节点 {node.id} 执行完成，匹配分支: {matchedBranchId}");

                return matchedBranchId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"条件节点执行失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行SQL执行器节点
        private async Task ExecuteSqlExecutorNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 确认SQL执行器服务已经通过构造函数注入
                if (_sqlExecutorService == null)
                {
                    throw new InvalidOperationException("SQL执行器服务未注册");
                }

                // 解析SQL查询参数，使用统一的值评估函数
                string sqlQuery = EvaluateInputValue(node.data.inputsValues.sqlQuery, outputs, variableList)?.ToString() ?? "";
                string connectionStringId = EvaluateInputValue(node.data.inputsValues.dataTable, outputs, variableList)?.ToString() ?? "";

                // 收集自定义参数
                var sqlParams = new Dictionary<string, object>();
                foreach (var prop in ((JObject)node.data.inputsValues).Properties())
                {
                    if (prop.Name.StartsWith("param_") && prop.Value != null)
                    {
                        // 解析参数值，使用统一的值评估函数
                        object paramValue = EvaluateInputValue(prop.Value, outputs, variableList);
                        sqlParams[prop.Name] = paramValue;
                        nodeResult.input[prop.Name] = paramValue;
                    }
                }

                nodeResult.input["sqlQuery"] = sqlQuery;
                nodeResult.input["dataTable"] = connectionStringId;
                nodeResult.input["sqlParams"] = sqlParams;

                // 获取数据源信息 - 直接使用注入的_sql对象
                List<DataSourceInfoOutput> dataSourceInfoOutputs = _sql.QueryDataSourceInfoById(connectionStringId);
                DataSourceInfoOutput dataSourceInfoOutput = dataSourceInfoOutputs.FirstOrDefault();

                if (dataSourceInfoOutput == null)
                {
                    throw new InvalidOperationException($"未找到ID为{connectionStringId}的数据源信息");
                }

                string connectionString = dataSourceInfoOutput.F_ConnectionString;
                string sourceType = "";

                // 获取数据源类型
                DataTable dataTableSourceType = _sql.QueryDataSourceTypeById(dataSourceInfoOutput.F_DataSourceTypeId);

                if (dataTableSourceType.Rows.Count > 0)
                {
                    sourceType = dataTableSourceType.Rows[0]["F_ItemName"].ToString();
                }

                // 执行SQL查询
                var result = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery, sourceType, connectionString, sqlParams);
                List<object> outputList = result.outputList;
                int rowNum = result.rowNum;

                // 存储输出
                outputs[$"{node.id}.outputs.outputList"] = outputList;
                outputs[$"{node.id}.outputs.rowNum"] = rowNum;

                nodeResult.output["outputList"] = outputList;
                nodeResult.output["rowNum"] = rowNum;
                nodeResult.success = true;

                Console.WriteLine($"SQL查询执行完成: 影响行数={rowNum}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL查询执行失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行代码节点
        private async Task ExecuteCodeNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 使用注入的代码执行器服务
                if (_codeExecutorService == null)
                {
                    throw new InvalidOperationException("代码执行器服务未注册");
                }

                // 解析代码参数，使用统一的值评估函数
                string codeContent = EvaluateInputValue(node.data.inputsValues.codeContent, outputs, variableList)?.ToString() ?? "";
                string language = EvaluateInputValue(node.data.inputsValues.language, outputs, variableList)?.ToString() ?? "javascript";

                // 解析是否忽略错误
                object ignoreErrorsValue = EvaluateInputValue(node.data.inputsValues.ignoreErrors, outputs, variableList);
                bool ignoreErrors = false;

                if (ignoreErrorsValue is bool boolValue)
                    ignoreErrors = boolValue;
                else if (ignoreErrorsValue is string strValue)
                    ignoreErrors = strValue.ToLower() == "true";

                // 收集自定义参数
                var codeParams = new Dictionary<string, object>();
                foreach (var prop in ((JObject)node.data.inputsValues).Properties())
                {
                    if ((prop.Name.StartsWith("param_") || prop.Name == "input" || prop.Name == "global")
                         && prop.Value != null)
                    {
                        // 解析参数值，使用统一的值评估函数
                        object paramValue = EvaluateInputValue(prop.Value, outputs, variableList);
                        codeParams[prop.Name] = paramValue;
                        nodeResult.input[prop.Name] = paramValue;
                    }
                }

                nodeResult.input["codeContent"] = codeContent;
                nodeResult.input["language"] = language;
                nodeResult.input["ignoreErrors"] = ignoreErrors;
                nodeResult.input["params"] = codeParams;

                // 执行代码
                var result = await _codeExecutorService.ExecuteCodeAsync(codeContent, language, codeParams, ignoreErrors);

                // 存储输出
                if (node.data.outputs?.properties != null)
                {
                    foreach (var output in node.data.outputs.properties)
                    {
                        string outputKey = output.Key;
                        if (result.ContainsKey(outputKey))
                        {
                            outputs[$"{node.id}.outputs.{outputKey}"] = result[outputKey];
                            nodeResult.output[outputKey] = result[outputKey];
                        }
                        else
                        {
                            outputs[$"{node.id}.outputs.{outputKey}"] = null;
                            nodeResult.output[outputKey] = null;
                        }
                    }
                }

                nodeResult.success = true;
                Console.WriteLine($"代码执行完成: 语言={language}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"代码执行失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;

                // 如果设置了忽略错误，则继续执行
                if (node.data.inputsValues.ignoreErrors?.ToString() == "true" ||
                    (node.data.inputsValues.ignoreErrors is bool && (bool)node.data.inputsValues.ignoreErrors))
                {
                    nodeResult.success = true;
                }
                else
                {
                    throw;
                }
            }
        }

        // 执行图像生成节点
        private async Task ExecuteImageGenerationNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 使用注入的图像生成服务
                if (_imageGenerationService == null)
                {
                    throw new InvalidOperationException("图像生成服务未注册");
                }


                // 解析LLM参数，使用统一的值评估函数
                object modelData = EvaluateInputValue(node.data.inputsValues.modelType, outputs, variableList)?.ToString() ?? "gpt-4o";

                //提取模型信息
                dynamic obj = JsonConvert.DeserializeObject(modelData.ToString());
                string modelType = obj.value;
                string modelValue = obj.label;


                // 解析图像生成参数，使用统一的值评估函数
                string style = EvaluateInputValue(node.data.inputsValues.style, outputs, variableList)?.ToString() ?? "现实主义风格";

                // 解析宽度
                object widthValue = EvaluateInputValue(node.data.inputsValues.width, outputs, variableList);
                int width = widthValue != null ? Convert.ToInt32(widthValue) : 1024;

                // 解析高度
                object heightValue = EvaluateInputValue(node.data.inputsValues.height, outputs, variableList);
                int height = heightValue != null ? Convert.ToInt32(heightValue) : 1024;

                // 解析纵横比
                string aspectRatio = EvaluateInputValue(node.data.inputsValues.aspectRatio, outputs, variableList)?.ToString() ?? "1:1 (1024*1024)";

                // 处理提示词，使用统一的值评估函数
                string promptTemplate = EvaluateInputValue(node.data.inputsValues.prompt, outputs, variableList)?.ToString() ?? "";
                string negativePrompt = EvaluateInputValue(node.data.inputsValues.negativePrompt, outputs, variableList)?.ToString() ?? "";

                // 处理自定义参数 - 新格式下是customParams数组
                if (node.data.inputsValues.customParams != null && node.data.inputsValues.customParams is JArray)
                {
                    JArray customParamsArray = (JArray)node.data.inputsValues.customParams;
                    foreach (var param in customParamsArray)
                    {
                        string paramTitle = param["title"]?.ToString();
                        string paramType = param["type"]?.ToString();

                        if (string.IsNullOrEmpty(paramTitle))
                            continue;

                        // 如果是表达式类型参数，需要评估表达式
                        if (paramType == "expression")
                        {
                            string paramValue = param["value"]?.ToString();
                            if (!string.IsNullOrEmpty(paramValue))
                            {
                                // 解析表达式引用的值
                                object resolvedValue = EvaluateInputValue(
                                    new JObject { ["type"] = "expression", ["content"] = paramValue },
                                    outputs,
                                    variableList
                                );

                                // 记录到节点输入中
                                nodeResult.input[$"param_{paramTitle}"] = resolvedValue;

                                // 替换提示词中的模板变量
                                if (resolvedValue != null)
                                {
                                    promptTemplate = promptTemplate.Replace($"{{{{{paramTitle}}}}}", resolvedValue.ToString());
                                }
                            }
                        }
                        else
                        {
                            // 直接使用值
                            object paramValue = param["value"];
                            nodeResult.input[$"param_{paramTitle}"] = paramValue;

                            if (paramValue != null)
                            {
                                promptTemplate = promptTemplate.Replace($"{{{{{paramTitle}}}}}", paramValue.ToString());
                            }
                        }

                    }
                }

                // 处理参考图像 - 新格式
                var referenceImages = new List<string>(); // 修改为字符串列表
                if (node.data.inputsValues.referenceImages != null)
                {
                    // 获取referenceImages对象
                    var refImgObj = node.data.inputsValues.referenceImages;

                    // 修改后的代码，处理 refImgObj 为 JArray 的情况
                    if (refImgObj is JArray jarr)
                    {
                        foreach (var item in jarr)
                        {
                            if (item is JObject jObj && jObj["type"] != null)
                            {
                                string type = jObj["type"].ToString();

                                if (type == "file")
                                {
                                    // 直接读取文件内容
                                    string fileContent = jObj["content"]?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(fileContent))
                                    {
                                        // 分号分割多个文件
                                        string[] files = fileContent.Split(';');
                                        foreach (string file in files)
                                        {
                                            if (!string.IsNullOrEmpty(file))
                                            {
                                                referenceImages.Add(file); // 直接添加文件URL
                                            }
                                        }
                                    }
                                }
                                else if (type == "expression")
                                {
                                    // 从其他节点获取图像
                                    string expr = jObj["content"]?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(expr))
                                    {
                                        // 解析表达式引用的值
                                        object refImg = EvaluateInputValue(
                                            new JObject { ["type"] = "expression", ["content"] = expr },
                                            outputs,
                                            variableList
                                        );

                                        if (refImg != null)
                                        {
                                            // 根据引用值的类型添加到参考图像列表
                                            if (refImg is string strImg)
                                            {
                                                referenceImages.Add(strImg); // 直接添加图片URL
                                            }
                                            else if (refImg is JArray imgArray)
                                            {
                                                foreach (var img in imgArray)
                                                {
                                                    string imgData = img.ToString();
                                                    if (!string.IsNullOrEmpty(imgData))
                                                    {
                                                        referenceImages.Add(imgData); // 直接添加图片URL
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // 尝试直接添加对象
                                                referenceImages.Add(refImg.ToString()); // 转为字符串添加
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // The similarity is not an array now, it's a single value for all reference images
                // 设置整体相似度
                object similarityValue = EvaluateInputValue(node.data.inputsValues.similarity, outputs, variableList);
                double overallSimilarity = similarityValue != null ? Convert.ToDouble(similarityValue) : 0.7;

                nodeResult.input["style"] = style;
                nodeResult.input["width"] = width;
                nodeResult.input["height"] = height;
                nodeResult.input["aspectRatio"] = aspectRatio;
                nodeResult.input["prompt"] = promptTemplate;
                nodeResult.input["negativePrompt"] = negativePrompt;
                nodeResult.input["referenceImages"] = referenceImages;
                nodeResult.input["similarity"] = overallSimilarity;

                // 调用图像生成服务
                //var result = await _imageGenerationService.GenerateImageAsync(
                //    promptTemplate,
                //    negativePrompt,
                //    style,
                //    width,
                //    height,
                //    referenceImages,
                //    overallSimilarity
                //);
                object generatedImageData = new List<string> { "https://file.onechats.ai/tem/7b2bcfd5d57e13c338081530bc109000.png" };// result.imageData;
                string message = "success";//result.message;

                // 存储输出
                outputs[$"{node.id}.outputs.data"] = generatedImageData;
                outputs[$"{node.id}.outputs.msg"] = message;

                nodeResult.output["data"] = generatedImageData;
                nodeResult.output["msg"] = message;
                nodeResult.success = true;

                Console.WriteLine($"图像生成完成: 风格={style}, 尺寸={width}x{height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"图像生成失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.output["success"] = false;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行全局变量节点
        private void ExecuteGlobalNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {
                // 处理全局变量节点
                if (node.data.outputs?.properties != null)
                {
                    foreach (var property in node.data.outputs.properties)
                    {
                        string propertyKey = property.Key;
                        string outputKey = $"{node.id}.outputs.{propertyKey}";

                        // 获取变量的默认值
                        object rawValue = property.Value.defaultValue;

                        // 解析变量值，支持表达式
                        object propertyValue = EvaluateInputValue(
                            new JObject { ["type"] = "expression", ["content"] = rawValue?.ToString() },
                            outputs,
                            variableList
                        );

                        // 如果解析后的值仍为null，则根据类型设置一个默认值
                        if (propertyValue == null)
                        {
                            string propertyType = property.Value?.type?.ToString().ToLower();
                            switch (propertyType)
                            {
                                case "array":
                                    propertyValue = new JArray();
                                    break;
                                case "object":
                                    propertyValue = new JObject();
                                    break;
                                case "number":
                                    propertyValue = 0;
                                    break;
                                case "boolean":
                                    propertyValue = false;
                                    break;
                                case "string":
                                default:
                                    propertyValue = string.Empty;
                                    break;
                            }
                        }

                        // 设置输出值
                        outputs[outputKey] = ExtractJsonValue(propertyValue);

                        // 记录到执行结果
                        nodeResult.output[propertyKey] = ExtractJsonValue(propertyValue);

                        Console.WriteLine($"设置全局变量: {propertyKey} = {JsonConvert.SerializeObject(propertyValue)}");
                    }
                }

                nodeResult.success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"全局变量设置失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.success = false;
                throw;
            }
        }

        // 执行结束节点
        private void ExecuteEndNodeAsync(Node node, Dictionary<string, object> outputs, NodeExecutionResult nodeResult, List<dynamic> variableList)
        {
            try
            {

                // 处理结束节点 - 新结构：从inputsValues.output数组中获取参数
                if (node.data.inputsValues.customParams != null && node.data.inputsValues.customParams is JArray)
                {
                    JArray customParamsArray = (JArray)node.data.inputsValues.customParams;
                    foreach (var param in customParamsArray)
                    {

                        string title = param["title"]?.ToString();
                        string type = param["type"]?.ToString();

                        if (string.IsNullOrEmpty(title))
                            continue;
                        string outputKey = $"{node.id}.outputs.{title}";
                        // 如果是表达式类型参数，需要评估表达式
                        if (type == "expression")
                        {
                            string exprValue = param["value"]?.ToString() ?? "";
                            // string paramValue = param["value"]?.ToString();
                            if (!string.IsNullOrEmpty(exprValue))
                            {
                                // 解析表达式引用的值
                                object resolvedValue = EvaluateInputValue(
                                    new JObject { ["type"] = "expression", ["content"] = exprValue },
                                    outputs,
                                    variableList
                                );
                                // 存储解析后的值
                                outputs[outputKey] = resolvedValue;
                                nodeResult.output[title] = resolvedValue;
                                nodeResult.input[title] = exprValue; // 保存原始引用
                                //// 记录到节点输入中
                               
                            }
                        }
                        else
                        {
                            object value = param["value"];
                            outputs[outputKey] = value;
                            nodeResult.output[title] = value;
                            nodeResult.input[title] = value;
                        }

                    }
                }

                // 向后兼容老格式
                else if (node.data.outputs?.properties != null)
                {
                    foreach (var property in node.data.outputs.properties)
                    {
                        string outputName = property.Key;
                        string outputKey = $"{node.id}.outputs.{outputName}";

                        // 结束节点属性应该是表达式引用
                        object rawValue = property.Value.defaultValue;
                        string refValue = rawValue?.ToString() ?? "";

                        // 解析引用的表达式，获取最终值
                        object resolvedValue = EvaluateInputValue(
                            new JObject { ["type"] = "expression", ["content"] = refValue },
                            outputs,
                            variableList
                        );

                        // 存储解析后的值
                        outputs[outputKey] = resolvedValue;
                        nodeResult.output[outputName] = resolvedValue;
                        nodeResult.input[outputName] = refValue; // 保存原始引用

                        Console.WriteLine($"设置结束节点输出 {outputName} = {JsonConvert.SerializeObject(resolvedValue)}");
                    }
                }

                nodeResult.success = true;
                Console.WriteLine("流程执行完成，已到达结束节点");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"结束节点执行失败: {ex.Message}");
                nodeResult.output["error"] = ex.Message;
                nodeResult.success = false;
                throw;
            }
        }

        // 创建HTTP请求内容
        private HttpContent CreateHttpContent(dynamic inputsValues, Dictionary<string, object> outputs, Dictionary<string, object> customInputs, List<dynamic> variableList)
        {
            string bodyType = EvaluateInputValue(inputsValues.bodyType, outputs, variableList)?.ToString() ?? "none";

            switch (bodyType.ToLower())
            {
                case "json":
                    // 解析JSON内容，使用统一的值评估函数
                    object jsonContent = EvaluateInputValue(inputsValues.bodyContent, outputs, variableList);
                    string jsonStr = jsonContent?.ToString() ?? "{}";

                    if (string.IsNullOrEmpty(jsonStr)) jsonStr = "{}";

                    // 尝试格式化JSON以确保有效
                    try
                    {
                        // 如果是有效的JSON对象或数组，进行格式化
                        if ((jsonStr.StartsWith("{") && jsonStr.EndsWith("}")) ||
                            (jsonStr.StartsWith("[") && jsonStr.EndsWith("]")))
                        {
                            var parsedJson = JsonConvert.DeserializeObject(jsonStr);
                            jsonStr = JsonConvert.SerializeObject(parsedJson);
                        }
                    }
                    catch
                    {
                        // 如果不是有效的JSON，保持原样
                        Console.WriteLine("警告: 无效的JSON内容，使用原始字符串");
                    }

                    return new StringContent(jsonStr, Encoding.UTF8, "application/json");

                case "form-data":
                    var formData = new MultipartFormDataContent();
                    if (inputsValues.formData != null)
                    {
                        foreach (var item in inputsValues.formData)
                        {
                            string key = item.key?.ToString() ?? "";
                            if (string.IsNullOrEmpty(key))
                                continue;

                            // 解析表单数据值，使用统一的值评估函数
                            object value = EvaluateInputValue(item.value, outputs, variableList);
                            string valueStr = value?.ToString() ?? "";

                            formData.Add(new StringContent(valueStr), key);
                        }
                    }
                    return formData;

                case "x-www-form-urlencoded":
                    var formUrlEncoded = new List<KeyValuePair<string, string>>();
                    if (inputsValues.formData != null)
                    {
                        foreach (var item in inputsValues.formData)
                        {
                            string key = item.key?.ToString() ?? "";
                            if (string.IsNullOrEmpty(key))
                                continue;

                            // 解析表单数据值，使用统一的值评估函数
                            object value = EvaluateInputValue(item.value, outputs, variableList);
                            string valueStr = value?.ToString() ?? "";

                            formUrlEncoded.Add(new KeyValuePair<string, string>(key, valueStr));
                        }
                    }
                    return new FormUrlEncodedContent(formUrlEncoded);

                case "raw text":
                    // 解析文本内容，使用统一的值评估函数
                    object textContent = EvaluateInputValue(inputsValues.bodyContent, outputs, variableList);
                    string textStr = textContent?.ToString() ?? "";

                    if (string.IsNullOrEmpty(textStr)) textStr = "";
                    return new StringContent(textStr, Encoding.UTF8, "text/plain");

                case "binary":
                    // 暂不支持二进制内容，返回空内容
                    Console.WriteLine("警告: 二进制请求体类型暂不支持，使用空内容");
                    return new StringContent("", Encoding.UTF8);

                case "none":
                default:
                    return new StringContent("", Encoding.UTF8);
            }
        }

        #region 辅助方法

        // 比较值的辅助方法
        private static bool CompareValues(object leftValue, string relation, object rightValue)
        {
            // 如果左值为null，直接返回false
            if (leftValue == null)
                return false;

            // 获取右值的字符串表示
            string rightValueStr = rightValue?.ToString() ?? string.Empty;

            // 转换左值为字符串进行比较
            string leftValueStr = leftValue.ToString();

            // 根据关系类型进行比较
            switch (relation)
            {
                case "=":
                case "==":
                    // 如果两边都是数字，进行数值比较
                    if (double.TryParse(leftValueStr, out double leftNum) &&
                        double.TryParse(rightValueStr, out double rightNum))
                        return leftNum == rightNum;

                    // 否则进行字符串比较
                    return leftValueStr == rightValueStr;

                case "!=":
                    // 如果两边都是数字，进行数值比较
                    if (double.TryParse(leftValueStr, out leftNum) &&
                        double.TryParse(rightValueStr, out rightNum))
                        return leftNum != rightNum;

                    // 否则进行字符串比较
                    return leftValueStr != rightValueStr;

                case ">":
                    // 尝试转换为数值比较
                    if (double.TryParse(leftValueStr, out leftNum) &&
                        double.TryParse(rightValueStr, out rightNum))
                        return leftNum > rightNum;
                    return string.Compare(leftValueStr, rightValueStr) > 0;

                case "<":
                    if (double.TryParse(leftValueStr, out leftNum) &&
                        double.TryParse(rightValueStr, out rightNum))
                        return leftNum < rightNum;
                    return string.Compare(leftValueStr, rightValueStr) < 0;

                case ">=":
                    if (double.TryParse(leftValueStr, out leftNum) &&
                        double.TryParse(rightValueStr, out rightNum))
                        return leftNum >= rightNum;
                    return string.Compare(leftValueStr, rightValueStr) >= 0;

                case "<=":
                    if (double.TryParse(leftValueStr, out leftNum) &&
                        double.TryParse(rightValueStr, out rightNum))
                        return leftNum <= rightNum;
                    return string.Compare(leftValueStr, rightValueStr) <= 0;

                default:
                    return false;
            }
        }

        // 解析模板中的变量
        private static string ResolveTemplateVariables(string template, Dictionary<string, object> inputValues, Dictionary<string, object> outputs)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            // 使用正则表达式查找{{变量}}格式的模板变量
            var regex = new Regex(@"\{\{(.*?)\}\}");
            var matches = regex.Matches(template);

            if (matches.Count == 0)
                return template;

            string result = template;

            foreach (Match match in matches)
            {
                string variableName = match.Groups[1].Value.Trim();
                string placeholder = match.Value; // 完整的{{变量}}

                // 从inputValues中查找变量值
                if (inputValues.TryGetValue(variableName, out object variableValue))
                {
                    // 将解析后的值转换为字符串并替换到模板中
                    string valueStr = variableValue?.ToString() ?? string.Empty;
                    result = result.Replace(placeholder, valueStr);
                }
                else
                {
                    // 如果在inputValues中找不到，尝试直接从outputs中查找
                    string outputKey = variableName;
                    if (outputs.ContainsKey(outputKey))
                    {
                        string valueStr = outputs[outputKey]?.ToString() ?? string.Empty;
                        result = result.Replace(placeholder, valueStr);
                    }
                    else
                    {
                        // 找不到值，替换为空字符串
                        result = result.Replace(placeholder, string.Empty);
                        Console.WriteLine($"警告: 找不到模板变量 {variableName} 的值");
                    }
                }
            }

            return result;
        }

        private static object ExtractJsonValue(object value)
        {
            if (value == null)
                return null;

            // 处理System.Text.Json.JsonElement
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.String:
                        return jsonElement.GetString();
                    case System.Text.Json.JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out int intValue))
                            return intValue;
                        if (jsonElement.TryGetInt64(out long longValue))
                            return longValue;
                        if (jsonElement.TryGetDouble(out double doubleValue))
                            return doubleValue;
                        return 0;
                    case System.Text.Json.JsonValueKind.True:
                        return true;
                    case System.Text.Json.JsonValueKind.False:
                        return false;
                    case System.Text.Json.JsonValueKind.Array:
                        var array = new List<object>();
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            array.Add(ExtractJsonValue(item));
                        }
                        return array;
                    case System.Text.Json.JsonValueKind.Object:
                        var obj = new Dictionary<string, object>();
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            obj[property.Name] = ExtractJsonValue(property.Value);
                        }
                        return obj;
                    case System.Text.Json.JsonValueKind.Null:
                    default:
                        return null;
                }
            }

            // 处理Newtonsoft.Json.Linq.JToken
            if (value is Newtonsoft.Json.Linq.JToken jToken)
            {
                return jToken.ToObject<object>();
            }

            // Part字已经是普通类型，直接返回
            return value;
        }

        // 提取字典中所有值的辅助方法
        private static Dictionary<string, object> ExtractDictionaryValues(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in dict)
            {
                result[kvp.Key] = ExtractJsonValue(kvp.Value);
            }

            return result;
        }

        #endregion
    }
}