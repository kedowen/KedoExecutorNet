using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiktokenSharp;

namespace Kedo.Comm.LLm.Utils
{
    public static class OnionTokenUtil
    {
        public static int NumTokensFromMessages(List<Dictionary<string, string>> messages, string model = "gpt-3.5-turbo")
        {
            TikToken encoding;
            try
            {
                encoding = TikToken.EncodingForModel(model);
            }
            catch (KeyNotFoundException)
            {
                //Warning: model not found. Using cl100k_base encoding.
                encoding = TikToken.GetEncoding("cl100k_base");
            }
            int tokensPerName;
            int tokensPerMessage;
            if (model == "gpt-3.5" || model == "gpt-3.5-turbo")
            {
                //每条消息都在<|start|>{role/name}\n{content}<|end|>\n之后
                // every message follows <|start|>{role/name}\n{content}<|end|>\n
                tokensPerMessage = 4;
                //如果有名称，则省略该角色
                // if there's a name, the role is omitted
                tokensPerName = -1;
            }
            else if (model == "gpt-4")
            {
                tokensPerMessage = 3;
                tokensPerName = 1;
            }
            else if (model == "deepseek-chat")
            {
                tokensPerMessage = 3;
                tokensPerName = 1;
            }
            else throw new NotImplementedException($"num_tokens_from_messages() is not implemented for model {model}. See https://github.com/openai/openai-python/blob/main/chatml.md for information on how messages are converted to tokens.");
            var numTokens = 0;
            foreach (var message in messages)
            {
                numTokens += tokensPerMessage;
                foreach (var pair in message)
                {
                    numTokens += encoding.Encode(pair.Value).Count;
                    if (pair.Key == "name") numTokens += tokensPerName;
                }
            }
            //每个回复都带有<|start|>assistant<|message|>
            // every reply is primed with <|start|>assistant<|message|>
            numTokens += 3;

            return numTokens;
        }
    }
}
