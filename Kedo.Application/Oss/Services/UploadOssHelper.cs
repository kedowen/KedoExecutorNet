using Aliyun.OSS;

using Furion;

using System;
using System.Security.Cryptography;
using System.Text;

namespace Kedo.Application.Oss.Services
{
    public class UploadOssHelper
    {
        readonly string accessKeyId;
        readonly string accessKeySecret;
        readonly int timeout;
        readonly int maxSize;
        readonly string host;
        public UploadOssHelper()
        {
            accessKeyId = App.Configuration["OssInfo:AccessKeyId"];
            accessKeySecret = App.Configuration["OssInfo:AccessKeySecret"];
            host = App.Configuration["OssInfo:Host"];
            // 限制参数的生效时间，单位为小时，默认值为1。
            timeout = Convert.ToInt16(App.Configuration["OssInfo:Timeout"]);
            // 限制上传文件的大小，单位为MB，默认值为10。
            maxSize = Convert.ToInt16(App.Configuration["OssInfo:MaxSize"]);
        }

        public object CreateUploadParams(string imgAppType)
        {
            string dir = imgAppType.ToLower() + "/";
            OssClient client = new(host, accessKeyId, accessKeySecret);
            DateTime now = DateTime.Now;
            DateTime ex = now.AddHours(timeout);
            PolicyConditions policyConds = new();
            policyConds.AddConditionItem(PolicyConditions.CondContentLengthRange, 0L, maxSize * 1024 * 1024);
            policyConds.AddConditionItem(MatchMode.StartWith, PolicyConditions.CondKey, dir);
            string postPolicy = client.GeneratePostPolicy(ex, policyConds);
            byte[] binaryData = Encoding.Default.GetBytes(postPolicy);
            string encodedPolicy = Convert.ToBase64String(binaryData);
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPolicy));
            var Signature = Convert.ToBase64String(hashBytes);
            return new
            {
                accessid = accessKeyId,
                policy = encodedPolicy,
                signature = Signature,
                dir,
                host,
                expire = ex.ToString("O")
            };
        }
    }
}
