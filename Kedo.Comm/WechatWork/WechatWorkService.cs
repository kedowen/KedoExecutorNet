using Furion.JsonSerialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.WechatWork
{
    public class WechatWorkService : IWechatWorkService
    {
        // 企业微信配置
        private const string CorpId = "ww846b855ea382b788";          // 企业ID
        private const string CorpSecret = "GNuSOpCqUkvMwX06liERw2m3_UZrsIDRMY-KuuKS0oc";  // 应用secret
        private const int AgentId = 1000002;                  // 应用ID

        public async Task<bool> SendMessageAsync(string toUser, string subject, string content)
        {
            try
            {
                // 获取访问令牌
                string accessToken = GetAccessToken();
                // 构造消息体
                var message = new
                {
                    touser = toUser,
                    msgtype = "text",
                    agentid = AgentId,
                    text = new { content = content },
                    safe = 0
                };

                // 发送消息
                string response = PostRequest(
                    $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={accessToken}",
                    JsonConvert.SerializeObject(message)
                );

                ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(response);
                if (responseData.ErrCode == 0 && responseData.ErrMsg.ToLower().Trim() == "ok")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static string GetAccessToken()
        {
            string url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={CorpId}&corpsecret={CorpSecret}";
            string response = GetRequest(url);

            dynamic result = JsonConvert.DeserializeObject(response);
            if (result.errcode != 0)
            {
                throw new Exception($"获取token失败: errcode={result.errcode}, errmsg={result.errmsg}");
            }
            return result.access_token;
        }

        private static string GetRequest(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                return client.GetStringAsync(url).Result;
            }
        }


        private static string PostRequest(string url, string json)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // 设置 Content-Type 头
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                return client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            }
        }
    }
    public class ResponseData
    {
        [JsonProperty("errcode")] // 确保JSON键与属性正确映射
        public int ErrCode { get; set; }

        [JsonProperty("errmsg")]
        public string ErrMsg { get; set; }

        [JsonProperty("msgid")]
        public string MsgId { get; set; }
    }


}

