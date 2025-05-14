using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.Oss.Dtos
{
    public class ChatWithFlowAgentFileSourceInput
    {
        public string F_Id { get; set; }
        public string F_ConversationId { get; set; }
        public string F_AgentFlowId { get; set; }
        public string F_SourceType { get; set; }
        public string F_SourceUrl { get; set; }
        public DateTime? F_CreateDate { get; set; }
        public string F_DeleteMark { get; set; }
    }

}
