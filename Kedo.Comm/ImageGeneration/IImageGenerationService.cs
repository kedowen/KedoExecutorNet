using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.ImageGeneration
{
    public interface IImageGenerationService
    {
        Task<(object imageData, string message)> GenerateImageAsync(string prompt, string negativePrompt, string style, int width, int height, List<string> referenceImages, double similarity);
    }
}
