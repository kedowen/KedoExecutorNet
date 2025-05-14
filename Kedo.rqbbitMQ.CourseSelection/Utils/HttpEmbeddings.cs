using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.rabbitMQ.BIData.Utils
{
    public class HttpEmbeddings
    {
        //   public async Task<string> EmbeddingsContent(EmbeddingInput embeddingInput)

        public async Task<string> EmbeddingsContent(string txtContent, string mDocId)
        {
            var segmenter = new SmartTextSegmenter(enableDetailedLogging: false);
            List<TextSegment> textSegments = await segmenter.SegmentTextWithMinLength(txtContent);
            foreach (var item in textSegments)
            {
                EmbeddingInput embeddingInput = new EmbeddingInput();
                embeddingInput.DocId = mDocId;
                embeddingInput.Texts = item.Text;
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://www.Kedo.cn:8876/api/KnowledgeBaseHandle/EmbeddingsFileContent");
                var content = new StringContent(JsonConvert.SerializeObject(embeddingInput), null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            // Console.WriteLine(await response.Content.ReadAsStringAsync());
            return "";
        }


    }
    public class EmbeddingInput
    {
        /// <summary>
        /// 要生成嵌入向量的文本列表
        /// </summary>
        public string Texts { get; set; }

        /// <summary>
        /// 嵌入模型名称, 默认为 "text-embedding-ada-002"
        /// </summary>
        public string Model { get; set; } = "text-embedding-ada-002";


        public string DocId { get; set; }
    }
}
