using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.BaseIDatatem.Dtos.output
{
    public class AliOSSBaseData
    {
        public string EndPoint { set; get; } = "oss-cn-shanghai.aliyuncs.com";

        public string BucketName { set; get; } = "Kedo.oss-cn-shanghai.aliyuncs.com";

        public string AccessKeyId { set; get; } = "xxx";

        public string AccessSecret { set; get; } = "xxx";

        public string UrlPrefix { set; get; }

    }
}
