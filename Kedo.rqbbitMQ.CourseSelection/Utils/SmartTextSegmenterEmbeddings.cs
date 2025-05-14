//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Kedo.rabbitMQ.BIData.Utils
//{
//    internal class SmartTextSegmenterEmbeddings
//    {
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.IO;

/// <summary>
/// 表示文本段落的类
/// </summary>
public class TextSegment
{
    public string Text { get; set; }
    public string SegmentId { get; set; }
    public string DocType { get; set; } = "unknown";
    public int OriginalPosition { get; set; } = 0;
    public int SubPosition { get; set; } = 0;
    public List<string> MergedSegments { get; set; }
    public int MergedCount { get; set; } = 1;
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// 智能文本分段工具类
/// </summary>
public class SmartTextSegmenter
{
    private readonly HttpClient _httpClient;
    private readonly string _aiModelEndpoint;
    private readonly bool _enableDetailedLogging;

    /// <summary>
    /// 初始化智能分段工具实例
    /// </summary>
    /// <param name="aiModelEndpoint">AI模型API端点</param>
    /// <param name="enableDetailedLogging">是否启用详细日志</param>
    public SmartTextSegmenter(string aiModelEndpoint = "http://119.3.206.67:11434/api/generate", bool enableDetailedLogging = true)
    {
        _httpClient = new HttpClient();
        _aiModelEndpoint = aiModelEndpoint;
        _enableDetailedLogging = enableDetailedLogging;

        // 设置超时
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    #region 主要分段方法

    /// <summary>
    /// 主要分段方法 - 根据文本内容和最小长度要求智能分段
    /// </summary>
    /// <param name="text">要分段的文本</param>
    /// <param name="minLength">最小段落长度</param>
    /// <param name="maxLength">最大段落长度</param>
    /// <returns>分段后的文本段落列表</returns>
    public async Task<List<TextSegment>> SegmentTextWithMinLength(string text, int minLength = 50, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
        {
            LogWarning("警告: 输入文本为空");
            return new List<TextSegment>();
        }

        // 1. 对文本进行分类，确定分段策略
        string docType = await ClassifyTextAI(text);
        LogInfo($"文本已分类为: {docType}");

        // 2. 根据文档类型调整参数
        AdjustParametersByDocType(docType, ref minLength, ref maxLength);

        // 3. 根据空行和换行符预分段
        List<string> rawParagraphs = PreSegmentText(text);
        LogInfo($"初步分段: {rawParagraphs.Count}个原始段落");

        // 4. 处理长段落和短段落
        List<TextSegment> segments = ProcessAllParagraphs(rawParagraphs, minLength, maxLength, docType);
        LogInfo($"处理后段落数: {segments.Count}");

        // 5. 最终合并相邻段落
        segments = MergeParagraphs(segments, minLength, maxLength, docType);

        // 6. 最终检查与优化
        return OptimizeSegments(segments, minLength, maxLength, docType);
    }

    /// <summary>
    /// 基础分段方法 - 按最大长度分段，尽量在标点处断句
    /// </summary>
    /// <param name="text">要分段的文本</param>
    /// <param name="maxLength">最大段落长度</param>
    /// <param name="metadata">元数据</param>
    /// <returns>分段后的文本段落列表</returns>
    public List<TextSegment> SegmentText(string text, int maxLength, Dictionary<string, object> metadata = null)
    {
        // 移除多余空白，规范化换行符
        text = Regex.Replace(text, @"\r?\n", " ").Replace("\t", " ");
        text = Regex.Replace(text, @"\s+", " ");

        List<TextSegment> paragraphs = new List<TextSegment>();
        int start = 0;

        LogInfo($"开始分段文本，长度={text.Length}，最大段长={maxLength}");

        // 如果文本总长度小于最大长度，直接返回整个文本
        if (text.Length <= maxLength)
        {
            LogInfo($"文本长度({text.Length})小于最大段长({maxLength})，不进行分段");
            string segmentId = metadata != null && metadata.ContainsKey("segmentId") ?
                $"{metadata["segmentId"]}-0" : $"segment-{DateTime.Now.Ticks}-0";

            var segment = new TextSegment
            {
                Text = text,
                SegmentId = segmentId
            };

            // 添加其他元数据
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    if (kvp.Key != "text" && kvp.Key != "segmentId")
                    {
                        segment.Metadata[kvp.Key] = kvp.Value;
                    }
                }
            }

            paragraphs.Add(segment);
            return paragraphs;
        }

        // 段落计数器
        int segmentCounter = 0;

        while (start < text.Length)
        {
            // 如果剩余文本不足maxLength，直接添加并结束
            if (start + maxLength >= text.Length)
            {
                string segmentText = text.Substring(start);
                string segmentId = metadata != null && metadata.ContainsKey("segmentId") ?
                    $"{metadata["segmentId"]}-{segmentCounter++}" : $"segment-{DateTime.Now.Ticks}-{segmentCounter++}";

                var segment = new TextSegment
                {
                    Text = segmentText,
                    SegmentId = segmentId,
                    OriginalPosition = metadata != null && metadata.ContainsKey("originalPosition") ?
                        Convert.ToInt32(metadata["originalPosition"]) + paragraphs.Count : paragraphs.Count,
                    DocType = metadata != null && metadata.ContainsKey("docType") ?
                        metadata["docType"].ToString() : "unknown"
                };

                // 添加其他元数据
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        if (kvp.Key != "text" && kvp.Key != "segmentId" &&
                            kvp.Key != "originalPosition" && kvp.Key != "docType")
                        {
                            segment.Metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }

                paragraphs.Add(segment);
                break;
            }

            // 尝试找到接近maxLength的断句点
            int breakPoint = FindBreakPoint(text, start, start + maxLength, maxLength / 4);

            // 添加段落
            string paragraphText = text.Substring(start, breakPoint - start);
            string paragraphId = metadata != null && metadata.ContainsKey("segmentId") ?
                $"{metadata["segmentId"]}-{segmentCounter++}" : $"segment-{DateTime.Now.Ticks}-{segmentCounter++}";

            var paragraphSegment = new TextSegment
            {
                Text = paragraphText,
                SegmentId = paragraphId,
                OriginalPosition = metadata != null && metadata.ContainsKey("originalPosition") ?
                    Convert.ToInt32(metadata["originalPosition"]) + paragraphs.Count : paragraphs.Count,
                DocType = metadata != null && metadata.ContainsKey("docType") ?
                    metadata["docType"].ToString() : "unknown"
            };

            // 添加其他元数据
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    if (kvp.Key != "text" && kvp.Key != "segmentId" &&
                        kvp.Key != "originalPosition" && kvp.Key != "docType")
                    {
                        paragraphSegment.Metadata[kvp.Key] = kvp.Value;
                    }
                }
            }

            paragraphs.Add(paragraphSegment);

            // 更新起始位置
            start = breakPoint;
        }

        LogInfo($"分段完成，共生成{paragraphs.Count}个段落，平均长度={Math.Round(paragraphs.Sum(p => p.Text.Length) / (double)paragraphs.Count)}");
        return paragraphs;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 根据文档类型调整分段参数
    /// </summary>
    private void AdjustParametersByDocType(string docType, ref int minLength, ref int maxLength)
    {
        switch (docType)
        {
            case "FAQ":
                // FAQ通常短小精悍，保持完整问答
                minLength = Math.Max(50, minLength);
                maxLength = Math.Min(800, maxLength);
                break;

            case "长文档":
                // 长文档需要更长的上下文
                minLength = Math.Max(150, minLength);
                maxLength = Math.Min(1000, maxLength);
                break;

            case "一般知识存储":
            default:
                // 使用默认参数
                break;
        }

        LogInfo($"根据文档类型({docType})调整参数: 最小长度={minLength}, 最大长度={maxLength}");
    }

    /// <summary>
    /// 预分段 - 根据空行和换行符初步分段
    /// </summary>
    private List<string> PreSegmentText(string text)
    {
        // 将文本按空行分割为段落
        string[] paragraphs = Regex.Split(text, @"\r?\n\s*\r?\n");

        List<string> result = new List<string>();
        foreach (string paragraph in paragraphs)
        {
            string trimmed = paragraph.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                result.Add(trimmed);
            }
        }

        // 如果没有找到段落（可能是因为没有空行），尝试按换行符分割
        if (result.Count <= 1 && text.Length > 1000)
        {
            LogInfo("未检测到空行分隔的段落，尝试按换行符分割");
            paragraphs = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            result.Clear();
            foreach (string paragraph in paragraphs)
            {
                string trimmed = paragraph.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 处理所有段落 - 分割长段落，合并短段落
    /// </summary>
    private List<TextSegment> ProcessAllParagraphs(List<string> rawParagraphs, int minLength, int maxLength, string docType)
    {
        List<TextSegment> segments = new List<TextSegment>();

        for (int i = 0; i < rawParagraphs.Count; i++)
        {
            string paragraph = rawParagraphs[i];

            // 创建基本元数据
            var metadata = new Dictionary<string, object>
            {
                ["segmentId"] = $"segment-{DateTime.Now.Ticks}-{i}",
                ["docType"] = docType,
                ["originalPosition"] = i
            };

            if (paragraph.Length > maxLength)
            {
                // 分割长段落
                var metadataObj = new TextSegment
                {
                    SegmentId = metadata["segmentId"].ToString(),
                    DocType = docType,
                    OriginalPosition = i
                };

                var subSegments = SegmentLongParagraph(paragraph, maxLength, metadataObj);
                segments.AddRange(subSegments);
                LogInfo($"长段落[{i}]已分割为{subSegments.Count}个子段落");
            }
            else
            {
                // 直接添加合适长度的段落
                segments.Add(new TextSegment
                {
                    Text = paragraph,
                    SegmentId = metadata["segmentId"].ToString(),
                    DocType = docType,
                    OriginalPosition = i
                });
            }
        }

        // 合并短段落
        if (docType != "FAQ") // FAQ通常保持原始结构
        {
            segments = MergeShortSegments(segments, minLength, maxLength);
        }

        return segments;
    }

    /// <summary>
    /// 分割长段落
    /// </summary>
    private List<TextSegment> SegmentLongParagraph(string paragraph, int maxLength, TextSegment metadata)
    {
        List<TextSegment> segments = new List<TextSegment>();
        int start = 0;
        int subIndex = 0;

        while (start < paragraph.Length)
        {
            int end = Math.Min(start + maxLength, paragraph.Length);

            // 如果不是结尾，尝试找到更好的断句点
            if (end < paragraph.Length)
            {
                // 确定搜索范围，从maxLength/2开始到maxLength
                int searchStart = Math.Max(start + maxLength / 2, start);
                int searchEnd = end;
                string searchText = paragraph.Substring(searchStart, searchEnd - searchStart);
                int breakPoint = -1;

                // 按优先级查找标点
                string[] punctuations = { "。", ".", "!", "！", "?", "？", ";", "；", ":", "：", ",", "，" };
                foreach (string punct in punctuations)
                {
                    int lastIndex = searchText.LastIndexOf(punct);
                    if (lastIndex != -1)
                    {
                        breakPoint = searchStart + lastIndex + 1;
                        LogInfo($"在长段落中找到断句点: '{punct}' 位置={breakPoint - start}/{paragraph.Length}");
                        break;
                    }
                }

                // 如果找到了断句点，使用它
                if (breakPoint != -1)
                {
                    end = breakPoint;
                }
                else
                {
                    LogInfo($"未找到理想断句点，使用最大长度({maxLength})截断");
                }
            }

            // 创建分段
            string segmentText = paragraph.Substring(start, end - start);
            string segmentId = !string.IsNullOrEmpty(metadata.SegmentId) ?
                $"{metadata.SegmentId}-sub{subIndex}" :
                $"segment-{DateTime.Now.Ticks}-{subIndex}";

            segments.Add(new TextSegment
            {
                Text = segmentText,
                SegmentId = segmentId,
                DocType = metadata.DocType,
                OriginalPosition = metadata.OriginalPosition,
                SubPosition = subIndex,
                Metadata = new Dictionary<string, object>(metadata.Metadata)
            });

            // 更新起始位置和子索引
            start = end;
            subIndex++;
        }

        LogInfo($"长段落分割: 原长度={paragraph.Length} -> {segments.Count}个子段落");
        return segments;
    }

    /// <summary>
    /// 合并短段落
    /// </summary>
    private List<TextSegment> MergeShortSegments(List<TextSegment> segments, int minLength, int maxLength)
    {
        if (segments == null || segments.Count <= 1) return segments;

        List<TextSegment> result = new List<TextSegment>();
        TextSegment current = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // 如果当前没有累积段落，或当前段落已达到最小长度
            if (current == null || current.Text.Length >= minLength)
            {
                // 如果当前段落长度足够，直接添加到结果
                if (current == null)
                {
                    if (segment.Text.Length >= minLength)
                    {
                        result.Add(segment);
                        continue;
                    }
                    else
                    {
                        // 开始一个新的累积段落
                        current = CloneSegment(segment);
                    }
                }
                else
                {
                    // 保存当前累积段落
                    result.Add(current);
                    // 开始新段落
                    current = segment.Text.Length < minLength ? CloneSegment(segment) : null;
                    if (current == null)
                    {
                        result.Add(segment);
                    }
                }
            }
            else
            {
                // 当前段落长度不足，尝试与下一段合并
                int combinedLength = current.Text.Length + segment.Text.Length;

                // 如果合并后不超过最大长度的120%，则合并
                if (combinedLength <= maxLength * 1.2)
                {
                    current.Text += "\n\n" + segment.Text;
                    LogInfo($"合并短段落: ID={current.SegmentId} <- ID={segment.SegmentId}, 合并后长度={current.Text.Length}");

                    // 更新元数据
                    if (current.MergedSegments == null)
                    {
                        current.MergedSegments = new List<string> { current.SegmentId };
                    }
                    current.MergedSegments.Add(segment.SegmentId);
                    current.MergedCount++;

                    // 如果合并后达到最小长度，保存并重置
                    if (current.Text.Length >= minLength)
                    {
                        result.Add(current);
                        current = null;
                    }
                }
                else
                {
                    // 合并后会超过最大长度，保存当前段落并开始新段落
                    result.Add(current);
                    current = segment.Text.Length < minLength ? CloneSegment(segment) : null;
                    if (current == null)
                    {
                        result.Add(segment);
                    }
                }
            }
        }

        // 添加最后一个累积段落
        if (current != null)
        {
            result.Add(current);
        }

        LogInfo($"短段落合并: {segments.Count}个原始段落 -> {result.Count}个合并后段落");
        return result;
    }

    /// <summary>
    /// 合并相邻段落
    /// </summary>
    /// <param name="paragraphs">段落对象数组</param>
    /// <param name="minLength">最小段落长度</param>
    /// <param name="maxLength">最大段落长度</param>
    /// <param name="docType">文档类型</param>
    /// <returns>合并后的段落对象数组</returns>
    private List<TextSegment> MergeParagraphs(List<TextSegment> paragraphs, int minLength, int maxLength, string docType)
    {
        if (paragraphs == null || paragraphs.Count <= 1) return paragraphs;

        List<TextSegment> result = new List<TextSegment>();
        TextSegment current = CloneSegment(paragraphs[0]);

        // 根据文档类型调整合并策略
        double minLengthMultiplier = 1.0;
        double maxLengthMultiplier = 1.0;

        switch (docType)
        {
            case "长文档":
                // 长文档使用更积极的合并策略
                minLengthMultiplier = 1.5;
                maxLengthMultiplier = 1.2;
                break;

            case "FAQ":
                // FAQ尽量保持问答完整
                minLengthMultiplier = 0.8;
                maxLengthMultiplier = 1.0;
                break;

            case "一般知识存储":
            default:
                // 默认策略
                minLengthMultiplier = 1.0;
                maxLengthMultiplier = 1.0;
                break;
        }

        for (int i = 1; i < paragraphs.Count; i++)
        {
            var next = paragraphs[i];

            // 确定是否应该合并
            bool shouldMerge = false;

            if (docType == "长文档")
            {
                // 长文档使用更积极的合并策略
                shouldMerge = current.Text.Length < minLength * minLengthMultiplier &&
                             (current.Text.Length + next.Text.Length) <= maxLength * maxLengthMultiplier;
            }
            else
            {
                // 其他文档类型使用保守策略
                shouldMerge = current.Text.Length < minLength &&
                             (current.Text.Length + next.Text.Length) <= maxLength;
            }

            if (shouldMerge)
            {
                // 合并文本，保留换行符以保持段落结构
                current.Text += "\n\n" + next.Text;
                LogInfo($"合并相邻段落: ID={current.SegmentId} <- ID={next.SegmentId}, 合并后长度={current.Text.Length}");

                // 更新元数据
                if (current.MergedSegments == null)
                {
                    current.MergedSegments = new List<string> { current.SegmentId };
                }
                current.MergedSegments.Add(next.SegmentId);
                current.MergedCount++;
            }
            else
            {
                // 保存当前段落，开始新段落
                result.Add(current);
                current = CloneSegment(next);
            }
        }

        // 添加最后一个段落
        if (current != null)
        {
            result.Add(current);
        }

        LogInfo($"相邻段落合并: {paragraphs.Count}个原始段落 -> {result.Count}个合并后段落");
        return result;
    }

    /// <summary>
    /// 查找最佳断句点
    /// </summary>
    private int FindBreakPoint(string text, int start, int end, int minLength)
    {
        // 向前查找的范围，确保最小长度要求
        int searchStart = Math.Min(start + minLength, end);
        int searchEnd = end;

        // 如果搜索范围无效，直接返回end
        if (searchStart >= searchEnd)
        {
            return end;
        }

        string textToSearch = text.Substring(searchStart, searchEnd - searchStart);

        // 按优先级查找标点
        string[][] punctuationGroups = new string[][]
        {
            new string[] { "。", ".", "!", "！", "?", "？" },  // 句末标点
            new string[] { ";", "；", ":", "：" },             // 次级标点
            new string[] { ",", "，" }                         // 三级标点
        };

        foreach (string[] group in punctuationGroups)
        {
            foreach (string punct in group)
            {
                int lastIndex = textToSearch.LastIndexOf(punct);
                if (lastIndex != -1)
                {
                    int breakPoint = searchStart + lastIndex + 1; // +1 包含标点
                    LogInfo($"找到断句点: '{punct}' 位置={breakPoint - start}/{end - start}");
                    return breakPoint;
                }
            }
        }

        // 查找最后一个空格
        int lastSpace = textToSearch.LastIndexOf(' ');
        if (lastSpace != -1)
        {
            int breakPoint = searchStart + lastSpace + 1;
            LogInfo($"找到空格作为断句点: 位置={breakPoint - start}/{end - start}");
            return breakPoint;
        }

        // 如果找不到任何标点，返回end
        LogInfo($"未找到断句点，使用最大长度({end - start})截断");
        return end;
    }

    /// <summary>
    /// 使用AI对文本进行分类
    /// </summary>
    public async Task<string> ClassifyTextAI(string text)
    {
        // 文本太短时直接返回默认分类
        if (string.IsNullOrEmpty(text))
        {
            LogError("分类失败: 无效的文本输入");
            return "一般知识存储"; // 默认分类
        }

        // 通过文本特征进行快速初步分类
        if (text.Length < 300)
        {
            LogInfo($"文本长度({text.Length})较短，初步判断为FAQ");
            return "FAQ";
        }
        else if (text.Length > 10000)
        {
            LogInfo($"文本长度({text.Length})较长，初步判断为长文档");
            return "长文档";
        }

        // 检查是否含有问答特征
        bool hasFAQFeatures = text.Contains("?") || text.Contains("？") ||
                             Regex.IsMatch(text, @"^\s*问[:：]");

        if (hasFAQFeatures && text.Length < 1000)
        {
            LogInfo("文本包含问答特征，初步判断为FAQ");
            return "FAQ";
        }

        // 截取前300个字符用于分类
        string sampleText = text.Length > 300 ? text.Substring(0, 300) : text;
        LogInfo($"使用文本前300字符进行分类: \"{TruncateForLogging(sampleText, 100)}\"");

        try
        {
            // 构建分类请求
            var prompt = $@"根据以下文本内容的开头部分判断它属于哪种类型：
                            - ""FAQ""（问答形式内容）
                            - ""一般知识存储""（百科知识类内容）
                            - ""长文档""（长篇内容如新闻/论文/书籍/小说）
                            文本开头：{sampleText}
                            请只返回一个分类结果：""FAQ""、""一般知识存储""或""长文档""，不要有任何其他说明。";

            // 设置超时保护
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

            // 创建分类任务
            var classifyTask = Task.Run(async () => {
                try
                {
                    // 构建请求体
                    var requestBody = new
                    {
                        model = "gemma3",
                        prompt = prompt,
                        stream = false
                    };

                    // 序列化请求体
                    string jsonRequest = JsonSerializer.Serialize(requestBody);

                    // 发送请求
                    HttpResponseMessage response = await _httpClient.PostAsync(
                        _aiModelEndpoint,
                        new StringContent(jsonRequest, Encoding.UTF8, "application/json")
                    );

                    // 确保请求成功
                    response.EnsureSuccessStatusCode();

                    // 读取响应
                    string responseJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                    {
                        // 提取响应文本
                        string classification = "";
                        if (doc.RootElement.TryGetProperty("response", out JsonElement responseElement))
                        {
                            classification = responseElement.GetString().Trim();
                            LogInfo($"AI分类结果: \"{classification}\"");
                        }
                        else
                        {
                            LogWarning("AI响应格式异常，未找到response字段");
                            return FallbackClassification(text);
                        }

                        // 验证分类结果
                        string[] validCategories = { "FAQ", "一般知识存储", "长文档" };

                        if (validCategories.Contains(classification))
                        {
                            return classification;
                        }

                        // 尝试从结果中提取分类
                        foreach (string category in validCategories)
                        {
                            if (classification.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                LogInfo($"从结果中提取分类: {category}");
                                return category;
                            }
                        }

                        // 如果无法提取有效分类，使用备用策略
                        return FallbackClassification(text);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"AI请求失败: {ex.Message}");
                    return FallbackClassification(text);
                }
            });

            // 等待任务完成或超时
            Task completedTask = await Task.WhenAny(classifyTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                LogWarning("AI分类请求超时，使用备用分类策略");
                return FallbackClassification(text);
            }

            // 返回分类结果
            return await classifyTask;
        }
        catch (Exception error)
        {
            LogError($"AI分类异常: {error.Message}");
            return FallbackClassification(text);
        }
    }

    /// <summary>
    /// 最终优化段落
    /// </summary>
    private List<TextSegment> OptimizeSegments(List<TextSegment> segments, int minLength, int maxLength, string docType)
    {
        if (segments == null || segments.Count == 0)
        {
            return segments;
        }

        LogInfo($"开始最终优化: {segments.Count}个段落");

        // 重新编号段落
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].OriginalPosition = i;
        }

        // 处理特别短的段落
        List<TextSegment> optimizedSegments = new List<TextSegment>(segments);
        bool madeChanges = true;
        int iterations = 0;
        int maxIterations = 3; // 防止无限循环

        while (madeChanges && iterations < maxIterations)
        {
            madeChanges = false;
            iterations++;

            for (int i = 0; i < optimizedSegments.Count - 1; i++)
            {
                // 对于较短的段落，考虑与下一个合并
                if (optimizedSegments[i].Text.Length < minLength / 2)
                {
                    int nextLength = optimizedSegments[i + 1].Text.Length;
                    int combinedLength = optimizedSegments[i].Text.Length + nextLength;

                    if (combinedLength <= maxLength)
                    {
                        LogInfo($"优化: 合并短段落 {i} (长度={optimizedSegments[i].Text.Length}) 与 {i + 1} (长度={nextLength})");

                        // 合并段落
                        optimizedSegments[i].Text += "\n\n" + optimizedSegments[i + 1].Text;

                        // 更新元数据
                        if (optimizedSegments[i].MergedSegments == null)
                        {
                            optimizedSegments[i].MergedSegments = new List<string> { optimizedSegments[i].SegmentId };
                        }

                        if (optimizedSegments[i + 1].MergedSegments != null)
                        {
                            optimizedSegments[i].MergedSegments.AddRange(optimizedSegments[i + 1].MergedSegments);
                        }
                        else
                        {
                            optimizedSegments[i].MergedSegments.Add(optimizedSegments[i + 1].SegmentId);
                        }

                        optimizedSegments[i].MergedCount += optimizedSegments[i + 1].MergedCount;

                        // 移除已合并的段落
                        optimizedSegments.RemoveAt(i + 1);
                        madeChanges = true;
                        i--; // 重新检查当前索引
                    }
                }
            }
        }

        // 检查段落长度是否超过最大长度
        for (int i = 0; i < optimizedSegments.Count; i++)
        {
            if (optimizedSegments[i].Text.Length > maxLength * 1.5)
            {
                LogWarning($"段落 {i} 超过最大长度的150%: {optimizedSegments[i].Text.Length} > {maxLength * 1.5}");

                // 对于超长段落，尝试重新分割
                var metadataObj = new TextSegment
                {
                    SegmentId = optimizedSegments[i].SegmentId + "-resplit",
                    DocType = optimizedSegments[i].DocType,
                    OriginalPosition = i
                };

                List<TextSegment> resplitSegments = SegmentLongParagraph(
                    optimizedSegments[i].Text,
                    maxLength,
                    metadataObj
                );

                if (resplitSegments.Count > 1)
                {
                    LogInfo($"重新分割超长段落: 1个段落 -> {resplitSegments.Count}个子段落");

                    // 替换原段落
                    optimizedSegments.RemoveAt(i);
                    optimizedSegments.InsertRange(i, resplitSegments);

                    // 更新索引以跳过新插入的段落
                    i += resplitSegments.Count - 1;
                }
            }
        }

        // 添加段落索引和元数据
        for (int i = 0; i < optimizedSegments.Count; i++)
        {
            optimizedSegments[i].OriginalPosition = i;
            optimizedSegments[i].Metadata["finalIndex"] = i;
            optimizedSegments[i].Metadata["totalSegments"] = optimizedSegments.Count;
        }

        LogInfo($"优化完成: {segments.Count}个原始段落 -> {optimizedSegments.Count}个优化段落");
        return optimizedSegments;
    }

    /// <summary>
    /// 备用分类策略 - 当AI分类失败时使用
    /// </summary>
    private string FallbackClassification(string text)
    {
        // 基于文本长度和特征的启发式分类
        if (text.Length > 3000)
        {
            LogInfo($"启发式分类: 基于文本长度({text.Length})判断为长文档");
            return "长文档";
        }
        else if (text.Contains("?") || text.Contains("？") ||
                Regex.IsMatch(text, @"\b[问答][：:]\s*\w+") ||
                text.Length < 500)
        {
            LogInfo("启发式分类: 基于问答特征判断为FAQ");
            return "FAQ";
        }
        else
        {
            LogInfo("启发式分类: 无明显特征，判断为一般知识存储");
            return "一般知识存储";
        }
    }

    /// <summary>
    /// 克隆段落对象
    /// </summary>
    private TextSegment CloneSegment(TextSegment segment)
    {
        if (segment == null) return null;

        TextSegment clone = new TextSegment
        {
            Text = segment.Text,
            SegmentId = segment.SegmentId,
            DocType = segment.DocType,
            OriginalPosition = segment.OriginalPosition,
            SubPosition = segment.SubPosition,
            MergedCount = segment.MergedCount
        };

        // 复制合并段落列表
        if (segment.MergedSegments != null)
        {
            clone.MergedSegments = new List<string>(segment.MergedSegments);
        }

        // 复制元数据
        if (segment.Metadata != null)
        {
            clone.Metadata = new Dictionary<string, object>(segment.Metadata);
        }
        else
        {
            clone.Metadata = new Dictionary<string, object>();
        }

        return clone;
    }

    #endregion

    #region 日志方法

    /// <summary>
    /// 记录信息日志
    /// </summary>
    private void LogInfo(string message)
    {
        if (_enableDetailedLogging)
        {
            Console.WriteLine($"📌 {message}");
        }
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    private void LogWarning(string message)
    {
        Console.WriteLine($"⚠️ {message}");
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    private void LogError(string message)
    {
        Console.Error.WriteLine($"❌ {message}");
    }

    /// <summary>
    /// 截断长文本用于日志显示
    /// </summary>
    private string TruncateForLogging(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength) + "...";
    }

    #endregion
}



