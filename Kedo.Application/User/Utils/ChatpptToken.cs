using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Utils
{
    public class ChatpptToken
    {

        public static async Task<string> getToken(string user_id, string mobile, string area_code, string nickname, string accesskeyid, string channel, string signkey, string ver = "v1")
        {
            ChatPPTUserDataEntity jsonEntity = new ChatPPTUserDataEntity();
            jsonEntity.channel = channel;
            jsonEntity.user_id = user_id;
            jsonEntity.mobile = mobile;
            jsonEntity.area_code = area_code;
            jsonEntity.nickname = nickname =="" ? mobile : nickname;
            jsonEntity.timestamp = getTimetamp();
            jsonEntity.sign = Sign(channel, user_id, mobile, area_code, nickname, jsonEntity.timestamp, signkey);

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.yoojober.com/auth/channelToken");
            request.Headers.Add("accesskeyid", accesskeyid);
            request.Headers.Add("ver", ver);
            request.Headers.Add("channel", channel);

            var content = new StringContent(JsonConvert.SerializeObject(jsonEntity), null, "application/json");

            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string returnData = await response.Content.ReadAsStringAsync();
            JObject jsonObj = JObject.Parse(returnData);
            string token = jsonObj["data"]["token"].ToString();
            Console.WriteLine("Token: " + token);
            return token;

        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string getTimetamp()
        {
            TimeSpan mTimeSpan = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0);
            long timestamp = (long)mTimeSpan.TotalSeconds;
            return timestamp.ToString();
        }

        /// <summary>
        /// 签名
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user_id"></param>
        /// <param name="mobile"></param>
        /// <param name="area_code"></param>
        /// <param name="nickname"></param>
        /// <param name="timestamp"></param>
        /// <param name="signkey"></param>
        /// <returns></returns>
        public static string Sign(string channel, string user_id, string mobile, string area_code, string nickname, string timestamp, string signkey)
        {
            string mStringKeyValue = "area_code={0}&channel={1}&mobile={2}&nickname={3}&timestamp={4}&user_id={5}&{6}";
            mStringKeyValue = string.Format(mStringKeyValue, area_code, channel, mobile, nickname, timestamp, user_id, signkey);
            return GetMd5Hash(mStringKeyValue);
        }

        /// <summary>
        /// MD 32位小写加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }

    public class ChatPPTUserDataEntity
    {
        public string channel { get; set; }
        public string user_id { get; set; }
        public string mobile { get; set; }
        public string area_code { get; set; }
        public string nickname { get; set; }
        public string timestamp { get; set; }
        public string sign { get; set; }
    }

}
