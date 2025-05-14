using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Comm.LLm.OpenAI;
using Kedo.Comm.LLm.QianWen;
using Kedo.Comm.LLm.DeepSeek;
using Kedo.Comm.LLm.Claude;
using Kedo.Comm.LLm.Model;

namespace Kedo.Comm.LLm
{
    public class LlmService : ILlmService
    {
        /// <summary>
        ///大模型调用
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="modelValue"></param>
        /// <param name="systemPrompt"></param>
        /// <param name="userPrompt"></param>
        /// <param name="temperature"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> GenerateTextAsync(string modelType, string modelValue, string systemPrompt, string userPrompt, double temperature)
        {

            AgentFlowLLMInput agentFlowLLMInput = new AgentFlowLLMInput();
            agentFlowLLMInput.systemContent = systemPrompt;
            agentFlowLLMInput.chatContent = userPrompt;
            agentFlowLLMInput.model = modelValue;
            agentFlowLLMInput.temperature = temperature;

            switch (modelType.ToUpperInvariant())
            {
                case "GPT":
                    return await OpenAIHelper.ProcessGPTAsync(agentFlowLLMInput);

                case "QIANWEN":
                    return await QianWenHelper.ProcessQianwenAsync(agentFlowLLMInput);

                case "DEEPSEEK":
                    return await DeepSeekHelper.ProcessDeepSeekAsync(agentFlowLLMInput);

                case "CLAUDE":
                    return await ClaudeHelper.ProcessClaudeAsync(agentFlowLLMInput);
                default:
                    throw new ArgumentException($"Unsupported model type: {modelType}");
            }
        }
    }
}


