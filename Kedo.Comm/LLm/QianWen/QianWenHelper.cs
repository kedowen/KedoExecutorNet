using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Comm.LLm.Model;
using Microsoft.AspNetCore.Http;
using Kedo.Comm.LLm.Utils;

namespace Kedo.Comm.LLm.QianWen
{
    public class QianWenHelper
    {
        /// <summary>
        /// 通义千问
        /// </summary>
        /// <param name="agentFlowLLMInput"></param>
        /// <returns></returns>
        public static async Task<string> ProcessQianwenAsync(AgentFlowLLMInput agentFlowLLMInput)
        {
            string onUseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            string onUseKey = "**";
            ChatModelNomal chatModel = new ChatModelNomal();
            chatModel.model = agentFlowLLMInput.model;
            chatModel.temperature = agentFlowLLMInput.temperature;
            chatModel.stream = false;

            MessageModel messageModelSystem = new MessageModel();
            messageModelSystem.role = "system";
            messageModelSystem.content = agentFlowLLMInput.systemContent.Trim() != "" ? agentFlowLLMInput.systemContent : "您是数据领域专家";
            chatModel.messages.Add(messageModelSystem);


            MessageModel messageModelRole = new MessageModel();
            messageModelRole.role = "user";
            messageModelRole.content = agentFlowLLMInput.chatContent;
            chatModel.messages.Add(messageModelRole);

            string chatString = JsonConvert.SerializeObject(chatModel);
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, onUseUrl);
            request.Headers.Add("Authorization", "Bearer " + onUseKey);

            request.Content = new StringContent(chatString, null, "application/json");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var varResult = await response.Content.ReadAsStringAsync();
            JObject jsonObj = JObject.Parse(varResult);
            string answerData = jsonObj["choices"][0]["message"]["content"].ToString();
            return answerData;
        }




        static string onUseModel = "qwen-long";
        static string onUseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
        static string onUseKey = "sk-ff3bb432bf584ccca77443bcc1ab4a32";
        public static async Task KnowledgeBaseCompletionsStream(HttpContext httpContext, string systemContent, string kbchatContent)
        {
            httpContext.Response.Headers.Add("Content-Type", "text/event-stream");
            httpContext.Response.Headers.Add("Cache-Control", "no-cache");
            httpContext.Response.Headers.Add("Connection", "keep-alive");
            httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            try
            {
                string answerContent = string.Empty;
                ChatModelNomal chatModel = new ChatModelNomal();
                chatModel.model = onUseModel;

                MessageModel messageModelSystem = new MessageModel();
                messageModelSystem.role = "system";
                messageModelSystem.content = systemContent;
                chatModel.messages.Add(messageModelSystem);
                MessageModel messageModelRole = new MessageModel();
                messageModelRole.role = "user";
                messageModelRole.content = kbchatContent;
                chatModel.messages.Add(messageModelRole);

                string chatString = JsonConvert.SerializeObject(chatModel);
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, onUseUrl);
                request.Headers.Add("Authorization", "Bearer " + onUseKey);
                request.Content = new StringContent(chatString, null, "application/json");

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Set the correct content type for event stream
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers["Cache-Control"] = "no-cache";
                httpContext.Response.Headers["Connection"] = "keep-alive";

                using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (line != null && line.StartsWith("data:"))
                    {
                        var json = line.Substring(5).Trim(); // Remove "data:" prefix and trim whitespace
                        if (!string.IsNullOrEmpty(json))
                        {
                            if (json.Trim() == "[DONE]")
                            {
                                await httpContext.Response.WriteAsync($"data:[DONE]\n\n");
                                await httpContext.Response.Body.FlushAsync();
                            }
                            else
                            {
                                JObject parsedJson = JObject.Parse(json);
                                var choices = parsedJson["choices"] as JArray;
                                if (choices != null && choices.Count > 0)
                                {
                                    string content = choices[0]["delta"]["content"]?.ToString();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        // Write data with a newline character to separate events
                                        if (content.Contains('\n'))
                                        {
                                            content = content.Replace("\n", "onionnewline");
                                        }

                                        if (content.StartsWith(" "))
                                        {
                                            content = content.Replace(" ", "onionempty");
                                        }

                                        if (content.EndsWith(" "))
                                        {
                                            content = content.Replace(" ", "onionempty");
                                        }

                                        answerContent += content;

                                        await httpContext.Response.WriteAsync($"data:{content}\n\n");
                                        await httpContext.Response.Body.FlushAsync();
                                    }
                                }
                            }
                        }
                    }
                }

                var responseMessages = new List<Dictionary<string, string>>();

                responseMessages.Add(new Dictionary<string, string>()
                        {
                            { "role", "assistant" },
                            { "content", answerContent }
                        });

                //输入文本消耗token数
                var prompt_tokens = OnionTokenUtil.NumTokensFromMessages(new List<Dictionary<string, string>>()
                        {
                            new()
                            {
                                { "role", "user" },
                                { "content",kbchatContent}
                            }
                        });
                //返回内容消耗token数
                var completion_tokens = OnionTokenUtil.NumTokensFromMessages(responseMessages);
                //prompt_tokens 和 completion_tokens 的总和，表示该请求所使用的总令牌数
                var total_tokens = prompt_tokens + completion_tokens;

                await httpContext.Response.WriteAsync($"data:[Usage]:{total_tokens}\n\n");
                await httpContext.Response.Body.FlushAsync();

            }
            catch (Exception ex)
            {
                await httpContext.Response.WriteAsync($"data:{ex.Message.Replace("\n", "\\n")}\n\n");
                await httpContext.Response.Body.FlushAsync();
            }
        }



    }
}
