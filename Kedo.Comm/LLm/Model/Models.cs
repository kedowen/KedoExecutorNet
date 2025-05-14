using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.LLm.Model
{

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

    public class AgentFlowLLMInput
    {
        //  public string conversationId { get; set; }

        public string model { get; set; }

        public double temperature { get; set; } = 0.7;


        public string systemContent { set; get; }

        public string chatContent { set; get; }
    }
}
