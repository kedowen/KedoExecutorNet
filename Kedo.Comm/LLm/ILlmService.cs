using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.LLm
{
    public interface ILlmService
    {
        Task<string> GenerateTextAsync(string modelType, string modelValue, string systemPrompt, string userPrompt, double temperature);
    }
}
