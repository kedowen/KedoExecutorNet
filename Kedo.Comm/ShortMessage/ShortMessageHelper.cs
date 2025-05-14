using Furion;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;


namespace Kedo.Comm
{
    public class ShortMessageHelper
    {
        private readonly ILogger<ShortMessageHelper> _logger;
        private string _accessKeyId;
        private string _accessKeySecret;
        private string _aliYunSmsSignName;
        private string _aliYunSmsTempateCode;
        private string _endpoint;
        private AlibabaCloud.SDK.Dysmsapi20170525.Client _client;

        /// <summary>
        /// 构造 ShortMessageHelper 
        /// </summary>
        /// <param name="AliYunAccessKey"></param>
        /// <param name="AliYunAccessSecret"></param>
        /// <param name="AliYunSmsSignName"></param>
        /// <param name="AliYunSmsTempateCode"></param>
        public ShortMessageHelper(ILogger<ShortMessageHelper> logger)
        {
            _logger = logger;

            _accessKeyId = App.Configuration.GetValue<string>("AliYunShortMessage:AliYunAccessKey"); 
            _accessKeySecret = App.Configuration.GetValue<string>("AliYunShortMessage:AliYunAccessSecret");
            _aliYunSmsSignName = "洋葱数字";//App.Configuration.GetValue<string>("AliYunShortMessage:AliYunSmsSignName"); 
            _aliYunSmsTempateCode = App.Configuration.GetValue<string>("AliYunShortMessage:AliYunSmsTempateCode");
            _endpoint = App.Configuration.GetValue<string>("AliYunShortMessage:Endpoint");

        
            AlibabaCloud.OpenApiClient.Models.Config config = new AlibabaCloud.OpenApiClient.Models.Config
            {
                AccessKeyId = _accessKeyId,
                AccessKeySecret = _accessKeySecret,
            };
            // 访问的域名
            config.Endpoint = _endpoint;
            _client = new AlibabaCloud.SDK.Dysmsapi20170525.Client(config);
        }
        
        /// <summary>
        /// 发送验证码短信
        /// </summary>
        /// <param name="cellphones"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public MessageSendModel SendValidSms(string cellphones, string code)
        {
            MessageSendModel  messageSendModel = new MessageSendModel();
            try
            {
                JObject mCotent = new JObject();
                mCotent.Add("code", code);
                AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest sendSmsRequest = new AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest
                {
                    PhoneNumbers = cellphones,
                    SignName = _aliYunSmsSignName,
                    TemplateCode = _aliYunSmsTempateCode,
                    TemplateParam = mCotent.ToString()
                };
                SendSmsResponse sendSmsResponse = _client.SendSms(sendSmsRequest);
              
                if (sendSmsResponse.Body.Code == "OK" && sendSmsResponse.Body.Message == "OK")
                {
                    messageSendModel.RetStatus = "1";
                    messageSendModel.Msg = "Success";
                   
                }
                else if ("isv.BUSINESS_LIMIT_CONTROL".Equals(sendSmsResponse.Body.Code))
                {
                    messageSendModel.RetStatus = "2";
                    messageSendModel.Msg = "获取验证码过于频繁";
                    throw new Exception("获取验证码过于频繁");
                }

                

            }
            catch (Exception ex)
            {
                messageSendModel.RetStatus = "3";
                messageSendModel.Msg = ex.ToString();
            }

            return messageSendModel;
          
        }

    }
}
