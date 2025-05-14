using System.Collections;

namespace Kedo.Comm;

[Serializable]
public class EqueueDataModel
{
    public string businessTopic { set; get; }
    public string name { set; get; }

    public string key { set; get; }

    public List<string> sqls { set; get; }

}
