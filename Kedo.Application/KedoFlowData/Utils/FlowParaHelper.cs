using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowData.Utils
{
    public static class FlowParaHelper
    {

        public static string ExtractStartNodeOutputs(string jsonDatas)
        {
            try
            {
                byte[] dataGraphic = Convert.FromBase64String(jsonDatas);

                string mAgentgraphic = Encoding.UTF8.GetString(dataGraphic);

                // 如果输入是字符串，则先解析为JObject
                JObject data = mAgentgraphic is string jsonStr ? JObject.Parse(jsonStr) : (JObject)mAgentgraphic;

                // 查找type为"start"的节点
                var startNode = data["nodes"]?
                    .Children()
                    .FirstOrDefault(node => node["type"]?.Value<string>() == "start");

                if (startNode == null)
                {
                    throw new Exception("未找到开始节点");
                }

                // 提取data.outputs参数
                var dataoutputs = (JObject)startNode["data"]?["outputs"];

                return ConvertJsonToBase64(dataoutputs.ToString());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"解析JSON出错: {ex.Message}");
                return null;
            }
        }

        public static string ConvertJsonToBase64(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("JSON字符串不能为空", nameof(json));
            }

            // 将JSON字符串转换为字节数组
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // 将字节数组转换为Base64字符串
            return Convert.ToBase64String(bytes);
        }
    }
}
