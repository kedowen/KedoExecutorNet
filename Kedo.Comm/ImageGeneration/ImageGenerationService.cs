using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kedo.Comm.ImageGeneration
{
    public class ImageGenerationService : IImageGenerationService
    {
        public async Task<(object imageData, string message)> GenerateImageAsync(string prompt, string negativePrompt, string style, int width, int height, List<string> imagePaths, double similarity)
        {

            var parts = new List<string> { prompt };
            if (!string.IsNullOrEmpty(style))
            {
                parts.Add(style);
            }
            parts.Add($"{width}x{height}");
            string positivePart = string.Join(", ", parts);

            // 处理负面提示部分（存在时添加）
            string negativePart = string.IsNullOrEmpty(negativePrompt)
                ? ""
                : $" - {negativePrompt}";

            // 格式化相似度权重（保留两位小数）
            string similarityPart = $" (similarity:{similarity:F2})";
            // 组合所有部分
            var promptImg = positivePart + negativePart + similarityPart;
            // 创建StringBuilder来存储响应内容
            StringBuilder imageContent = new StringBuilder();

            // 准备messages内容
            List<object> messageContent = new List<object>
                            {
                                new
                                {
                                    type = "text",
                                    text = promptImg ?? "合并在一起" // 使用传入的promptImg或默认文本
                                }
                            };

            // 如果有图片路径，处理图片
            if (imagePaths != null && imagePaths.Count > 0)
            {
                foreach (var imagePath in imagePaths)
                {
                    // 从网络URL获取图片并转为base64
                    string imageUrl = imagePath.ToString();
                    string base64Image = await GetBase64FromUrl(imageUrl);
                    
                    // 添加图片到消息内容
                    messageContent.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:image/jpeg;base64,{base64Image}"
                        }
                    });
                }
            }

            var requestBody = new
            {
                model = "gpt-4o-image",
                messages = new[]
                {
            new
            {
                role = "user",
                content = messageContent.ToArray()
            }
        },
                stream = true // 启用流式输出
            };

            try
            {
                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); // 延长至 5 分钟
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sk-OfNDshoVWuPD8XbbkrRJvFscGl5Jk3B19SRJDaz0daRKNpFA");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string endpoint = "https://api.onechats.cn/v1/chat/completions";
                    using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content })
                    {
                        // 获取HTTP响应但不立即返回方法结果
                        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"错误: {response.StatusCode}");
                            string errorContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(errorContent);
                            return (null, promptImg);
                        }
                        // 手动读取流并处理完成所有内容
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                if (line.StartsWith("data: "))
                                {
                                    string data = line.Substring(6); // 去掉 "data: " 前缀

                                    if (data == "[DONE]")
                                        break;

                                    try
                                    {
                                        using (JsonDocument doc = JsonDocument.Parse(data))
                                        {
                                            if (doc.RootElement.TryGetProperty("choices", out JsonElement choices) &&
                                                choices.GetArrayLength() > 0)
                                            {
                                                var choice = choices[0];
                                                if (choice.TryGetProperty("delta", out JsonElement delta) &&
                                                    delta.TryGetProperty("content", out JsonElement content_delta))
                                                {
                                                    string contentPiece = content_delta.GetString();
                                                    if (!string.IsNullOrEmpty(contentPiece))
                                                    {
                                                        Console.Write(contentPiece);
                                                        imageContent.Append(contentPiece);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // 忽略无效的JSON
                                    }
                                }
                            }
                        }

                        // 流处理完毕，此处已确保所有数据都已处理完成
                        var urls = ExtractUrls(imageContent.ToString());
                        string result = urls.Count > 0 ? urls[0] : "";
                        // 方法在这里返回，确保所有处理都已完成
                        return (urls, promptImg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
                throw;
            }
        }

        private static async Task<string> GetBase64FromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] imageBytes = await client.GetByteArrayAsync(url);
                return Convert.ToBase64String(imageBytes);
            }
        }

        public static List<string> ExtractUrls(string text)
        {
            Regex regex = new Regex(
                                         @"https://file\.onechats\.ai/[-A-Za-z0-9+&@#/%?=~_|!:,.;]*[-A-Za-z0-9+&@#/%=~_|]",
                                         RegexOptions.IgnoreCase
                                     );

            MatchCollection matches = regex.Matches(text);

            List<string> results = new List<string>();
            foreach (Match match in matches)
            {
                if (!results.Contains(match.Value.Trim()))
                    results.Add(match.Value);
            }
            return results;
        }


    }

    public class ImgDataItem
    {
        public string ImageData { get; set; }
        public double Similarity { get; set; }
    }

}
