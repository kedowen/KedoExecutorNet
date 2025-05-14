using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowExecutor.Utils
{
    public class OnChatImageUtil
    {
        static string onUseModel = "gpt-4o";
        static string onUseUrl = "https://api.onechats.ai/v1/chat/completions";
        static string onUseKey = "sk-QenhOcuxO3ug3EMHzg5LD7eUUFECSaUTjKVE0dIWFin73pZJ";
        public static async Task<string> AIForFileData(string base64Image, string prompt)
        {

            var modelRequest = new ModelRequest
            {
                model = "gpt-4o",
                messages = new List<message>
            {
                new message
                {
                    role = "user",
                    content = new List<contentitem>
                    {
                        new textcontentitem
                        {
                            type = "text",
                            text = prompt
                        },
                        new imgcontentitem
                        {
                            type = "image_url",
                            image_url = new image_url
                            {
                    url = $"data:image/jpeg;base64,{base64Image}"
                }
                        }
                    }
                }
            }
            };

            // 序列化为 JSON（需要 Newtonsoft.Json 库）
            string chatString = Newtonsoft.Json.JsonConvert.SerializeObject(modelRequest, Newtonsoft.Json.Formatting.Indented);

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, onUseUrl);
            request.Headers.Add("Authorization", "Bearer " + onUseKey);

            request.Content = new StringContent(chatString, null, "application/json");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            JObject jsonObj = JObject.Parse(await response.Content.ReadAsStringAsync());



            string answerData = jsonObj["choices"][0]["message"]["content"].ToString();
            return answerData;
        }


    }


    public class image_url
    {
        public string url { get; set; }
    }

    public abstract class contentitem
    {
        public string type { get; set; }

    }

    public class textcontentitem : contentitem
    {
        public string text { get; set; }
    }

    public class imgcontentitem : contentitem
    {
        public image_url image_url { get; set; }
    }

    public class message
    {
        public string role { get; set; }
        public List<contentitem> content { get; set; }
    }

    public class ModelRequest
    {
        public string model { get; set; }
        public List<message> messages { get; set; }

    }
}
