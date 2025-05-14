using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowExecutor.Dtos.input
{
    public class ProcessRequestInput
    {
        public string AgentFlowId { get; set; }
        public string GraphicAgentId { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
