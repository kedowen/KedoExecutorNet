using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Kedo.Comm
{
    public static class XmlUtil
    {

        public static JObject XmlToJson(string xmlString)
        {
            var doc = XElement.Parse(xmlString);
            var node_cdata = doc.DescendantNodes().OfType<XCData>().ToList();

            foreach (var node in node_cdata)
            {
                node.Parent!.Add(node.Value);
                node.Remove();
            }

            return JObject.Parse(JsonConvert.SerializeXNode(doc, Newtonsoft.Json.Formatting.None, false)).Value<JObject>("xml");
        }
    }
}