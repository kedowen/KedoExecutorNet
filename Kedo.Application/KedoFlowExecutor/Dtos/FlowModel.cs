using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Comm.LLm;

namespace Kedo.Application.OnionFlowExecutor.Dtos
{
    #region 数据模型

    public class FlowDefinition
    {
        public List<Node> nodes { get; set; }
        public List<Edge> edges { get; set; }
        public List<dynamic> variableList { get; set; } = new List<dynamic>();

        // 自定义JSON转换器，确保正确解析循环节点
        public static FlowDefinition FromJson(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new LoopNodeConverter() }
            };

            return JsonConvert.DeserializeObject<FlowDefinition>(json, settings);
        }

        // 循环节点JSON转换器
        private class LoopNodeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Node);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jObject = JObject.Load(reader);
                Node node = new Node
                {
                    id = jObject["id"]?.ToString(),
                    type = jObject["type"]?.ToString(),
                    meta = jObject["meta"]?.ToObject<Meta>(),
                    data = jObject["data"]?.ToObject<Data>()
                };

                // 如果是循环节点，解析blocks和edges
                if (node.type == "loop" && jObject["blocks"] != null)
                {
                    node.blocks = jObject["blocks"]?.ToObject<List<Node>>();
                    node.edges = jObject["edges"]?.ToObject<List<Edge>>();
                }

                return node;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class Node
    {
        public string id { get; set; }
        public string type { get; set; }
        public Meta meta { get; set; }
        public Data data { get; set; }
        public List<Node> blocks { get; set; }  // 循环节点内部的节点
        public List<Edge> edges { get; set; }   // 循环节点内部的边
    }

    public class Meta
    {
        public Position position { get; set; }
    }

    public class Position
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Data
    {
        public string title { get; set; }
        public dynamic inputsValues { get; set; }
        public Schema inputs { get; set; }
        public Schema outputs { get; set; }
    }

    public class Schema
    {
        public string type { get; set; }
        public List<string> required { get; set; }
        public Dictionary<string, Property> properties { get; set; }
    }

    public class Property
    {
        public string type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public object defaultValue { get; set; }
        public List<string> enumValue { get; set; }
    }

    public class Edge
    {
        public string sourceNodeID { get; set; }
        public string targetNodeID { get; set; }
        public string sourcePortID { get; set; }
    }

    public class NodeExecutionResult
    {
        public string id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public Dictionary<string, object> input { get; set; }
        public Dictionary<string, object> output { get; set; }
        public bool success { get; set; } = true; // 默认为成功
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public double duration { get; set; } // 执行时间(毫秒)
    }
    public class ChatModelNomal
    {

        public string id = Guid.NewGuid().ToString();
        // public int max_tokens = 8000;

        public double temperature { set; get; } = 0.7;
        public bool stream { get; set; } = true;
        public string model { get; set; }
        public List<MessageModel> messages { get; set; } = new List<MessageModel> { };


    }

    public class MessageModel
    {
        public string role { get; set; }
        public string content { get; set; }

        //  public string name { get; set; }
    }
    #endregion

}
