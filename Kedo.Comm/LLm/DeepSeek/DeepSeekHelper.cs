using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Comm.LLm.Model;

namespace Kedo.Comm.LLm.DeepSeek
{
    public class DeepSeekHelper
    {
        /// <summary>
        /// DeepSeek
        /// </summary>
        /// <param name="agentFlowLLMInput"></param>
        /// <returns></returns>
        public static async Task<string> ProcessDeepSeekAsync(AgentFlowLLMInput agentFlowLLMInput)
        {
            string onUseUrl = "https://api.deepseek.com/chat/completions";
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

    }
}
