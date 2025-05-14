using System;

namespace Kedo.Application.OnionFlowData.Dtos.output
{
    public class OnionFlowHisVersionOutput
    {
        public string F_Id { get; set; }
        public string F_OnionFlowId { get; set; }
        public string F_Caption { get; set; }
        public string F_OnionFlowSchemeData { get; set; }
        public string F_IsMasterVersion { get; set; }
        public int F_DeleteMark { get; set; }
        public int F_EnabledMark { get; set; }
        public DateTime F_CreateDate { get; set; }
        public string F_CreateUserId { get; set; }
    }
} 