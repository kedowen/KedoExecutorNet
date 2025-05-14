using Aliyun.OSS;
using Furion;
using Furion.DataEncryption;
using Furion.DependencyInjection;
using Furion.FriendlyException;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Kedo.Application.User.Dtos.input;
using Kedo.Application.User.Dtos.output;
using Kedo.Application.User.Dtos.ssouser;
using Kedo.Application.User.Utils;
using Kedo.Comm;
using Kedo.Comm.EmailMessage;
using Kedo.Core.Method;
using Kedo.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kedo.Application.User.Services
{
    public class UserService : IUserService, IScoped
    {
        private readonly ILogger<UserService> _logger;
        private readonly IDistributedCache _redis;
        private readonly RabbitMQHelper _rabbitMQ;
        private readonly ShortMessageHelper _shortMessageHelper;
        private static IHttpContextAccessor _contextAccessor;
        private readonly EmailMessageHelper _emailMessageHelper;
        private readonly string MessageQueueName;
        private readonly ISql _sql;

        string publishUrl;
        string publishFilePath;
        readonly string bucketName;
        readonly string endpoint;
        readonly string accessKeyId;
        readonly string accessKeySecret;
        OssClient client;
        int mInitOnionCoin;

        string mqttAddr;
        string mqttUser;
        string mqttPwd;

        private readonly string _FileSavePath;
        private readonly string _FileUrl;

        private readonly string accesskeyid;
        private readonly string channel;
        private readonly string signkey;


        private static Dictionary<string, SSOUserCache> kvSSOUser = new Dictionary<string, SSOUserCache>();
        public UserService(ILogger<UserService> logger, ISql sql, IDistributedCache redis, RabbitMQHelper rabbitMQ, ShortMessageHelper shortMessageHelper, EmailMessageHelper emailMessageHelper, [FromServices] IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            _sql = sql;
            _logger = logger;
            _redis = redis;
            _rabbitMQ = rabbitMQ;
            _shortMessageHelper = shortMessageHelper;
            _emailMessageHelper = emailMessageHelper;
            MessageQueueName = configuration["RabbitMQConfigurations:BIData"];
            accessKeyId = App.Configuration["OssInfo:AccessKeyId"];
            accessKeySecret = App.Configuration["OssInfo:AccessKeySecret"];
            bucketName = App.Configuration["OssInfo:BucketName"];
            endpoint = App.Configuration["OssInfo:Endpoint"];
            client = new OssClient(endpoint, accessKeyId, accessKeySecret);

            publishUrl = configuration["Avatar:PublishUrl"];
            publishFilePath = configuration["Avatar:FilePath"];

            mInitOnionCoin = Convert.ToInt32(App.Configuration["OnionChat:OnionCoin"]);//初始化洋葱币  用户第一登录的时候 送洋葱币

            mqttAddr = configuration["MQTT:Addr"];
            mqttUser = configuration["MQTT:User"];
            mqttPwd = configuration["MQTT:Pwd"];


            accesskeyid = configuration["ChatPPT:accesskeyid"];
            channel = configuration["ChatPPT:channel"];
            signkey = configuration["ChatPPT:signkey"];
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="userRegistInput"></param>
        /// <returns></returns>
        public string UserRegist(UserRegistInput userRegistInput)
        {
            //验证用户的验证码的合法性 
            if (!CheckSmsCode(userRegistInput.F_Mobile, userRegistInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);

            string mSecretkey = Md5Helper.Encrypt(CreateRandomCode.CreateNo(), 16).ToLower();
            string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(userRegistInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();

            var sqls = new List<string>();
            string mInsert = @"insert into bas_user(F_UserId,F_Account,F_Password,F_Secretkey,F_UserName,F_Mobile,F_IndustryCategory,
                               F_Job,F_TenantId,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate,F_IsPwdSetted,F_OnionCoin)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}','{10}','{11}','{12}','{13}','{14}')";
            mInsert = string.Format(mInsert, Guid.NewGuid().ToString(), userRegistInput.F_Mobile, dbPassword,
                mSecretkey, userRegistInput.F_UserName, userRegistInput.F_Mobile,
                userRegistInput.F_IndustryCategory,
                userRegistInput.F_Job,
               "F_TenantId",
                "0", "1", "UserRegist", DateTime.Now, 1, mInitOnionCoin);

            sqls.Add(mInsert);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("注册失败").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");
            return "";
        }

        /// <summary>
        /// 获取验证啊
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        public string GetVerificationCode(string phoneNum)
        {
            string code;
            DataTable dataTable = _sql.QuerySmsCodeMins(phoneNum, DateTime.Now.AddMinutes(-5).ToString(), DateTime.Now.ToString());
            if (dataTable.Rows.Count > 0)
            {
                code = dataTable.Rows[0][0].ToString();
            }
            else
            {
                code = DateTime.Now.ToString("mssfff");
                if (code.Length == 7)
                    code = code.Substring(1, 6);
            }

            MessageSendModel messageSendModel = _shortMessageHelper.SendValidSms(phoneNum, code);
            if (messageSendModel.RetStatus != "1")
                throw Oops.Bah("验证码发送失败").StatusCode(201);

            //若返回成功  将Code 存入 数据库
            string mId = Guid.NewGuid().ToString();
            var sqls = new List<string>();
            string mInsert = @"insert into his_nodifycode(F_Id,F_FPhoneNum,F_SmsCode,F_FSmsType,F_TerminalType,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}')";
            mInsert = string.Format(mInsert, mId, phoneNum, code, "验证码", "PC", "0", "1", "验证码", DateTime.Now);
            sqls.Add(mInsert);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("验证码入列异常").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");
            return "验证码发送成功";
        }



        /// <summary>
        /// 获取验证啊
        /// </summary>
        /// <param name="EmailNum"></param>
        /// <returns></returns>
        public string GetEmailVerificationCode(string EmailNum)
        {
            string code;
            DataTable dataTable = _sql.QuerySmsCodeMins(EmailNum, DateTime.Now.AddMinutes(-5).ToString(), DateTime.Now.ToString());
            if (dataTable.Rows.Count > 0)
            {
                code = dataTable.Rows[0][0].ToString();
            }
            else
            {
                code = DateTime.Now.ToString("mssfff");
                if (code.Length == 7)
                    code = code.Substring(1, 6);
            }

            MessageSendModel messageSendModel = _emailMessageHelper.SendEmailCode(EmailNum, "洋葱数字邮箱验证码", code);

            if (messageSendModel.RetStatus != "1")
                throw Oops.Bah("验证码发送失败").StatusCode(201);

            //若返回成功  将Code 存入 数据库
            string mId = Guid.NewGuid().ToString();
            var sqls = new List<string>();
            string mInsert = @"insert into his_nodifycode(F_Id,F_FPhoneNum,F_SmsCode,F_FSmsType,F_TerminalType,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}')";
            mInsert = string.Format(mInsert, mId, EmailNum, code, "验证码", "PC", "0", "1", "验证码", DateTime.Now);
            sqls.Add(mInsert);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("验证码入列异常").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");


            return "验证码发送成功";
        }

        /// <summary>
        /// 验证短信验证码
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <param name="UserCode"></param>
        /// <returns></returns>
        private bool CheckSmsCode(string phoneNum, string UserCode)
        {
            string code = "";
            bool result = false;
            DataTable dataTable = _sql.QuerySmsCodeMins(phoneNum, DateTime.Now.AddMinutes(-5).ToString(), DateTime.Now.ToString());
            if (dataTable.Rows.Count > 0)
                code = dataTable.Rows[0][0].ToString();

            if (UserCode.Trim() == code)
                result = true;

            return result;
        }


        /// <summary>
        /// 验证用户手机号码是否存在
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        public string CheckPhoneNoIsUsed(string phoneNum)
        {
            DataTable dataTable = _sql.QueryPhoneNo(phoneNum);
            if (dataTable.Rows.Count > 0)
                throw Oops.Bah("手机号码已被其他用户使用").StatusCode(201);
            return "success";
        }

        /// <summary>
        /// 用户登录--账号 密码
        /// </summary>
        /// <param name="userLoginByAccountInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByAccount(UserLoginByAccountInput userLoginByAccountInput)
        {
            UserInfoOutput userInfoOutput = new UserInfoOutput();

            DataTable dataTable = _sql.QueryUserPassword(userLoginByAccountInput.F_Account);
            if (dataTable.Rows.Count == 0)
            {
                throw Oops.Bah("当前用户不存在").StatusCode(201);
            }
            else
            {
                string mSecretkey = dataTable.Rows[0]["F_Secretkey"].ToString();
                string mPassword = dataTable.Rows[0]["F_Password"].ToString();
                string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(userLoginByAccountInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();

                if (dbPassword.Trim() == mPassword.Trim())
                {
                    List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfo(userLoginByAccountInput.F_Account);
                    if (dtUserInfo.Count > 0)
                        userInfoOutput = dtUserInfo[0];
                    userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
                }
                else
                {
                    throw Oops.Bah("账号密码不匹配").StatusCode(201);
                }
            }
            SetSSOUserInfo(ref userInfoOutput);
            return userInfoOutput;
        }

        /// <summary>
        /// 企业登录 获取token
        /// </summary>
        /// <param name="enterpriseUserLoginInput"></param>
        /// <returns></returns>
        public UserInfoOutput EnterpriseUserLoginByAppIdAndSecret(EnterpriseUserLoginInput enterpriseUserLoginInput)
        {
            //检查APPID 是否存在 以及 Secret 是否合法
            UserInfoOutput userInfoOutput = new UserInfoOutput();
            DataTable dataTable = _sql.QueryEnterpriseUserId(enterpriseUserLoginInput.AppID, enterpriseUserLoginInput.SecretKey);

            if (dataTable.Rows.Count == 0)
            {
                throw Oops.Bah("当前账户不存在").StatusCode(201);
            }
            else
            {
                //判断传递进来的用户是否存在 如果不存在 则创建用户  如果存在 则读取  传递进来的 UserId 在本系统重作为 F_ThirdPartyUserId
                List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByThirdPartyUserId(enterpriseUserLoginInput.UserId);
                if (dtUserInfo.Count > 0)
                {
                    userInfoOutput = dtUserInfo[0];
                    userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
                }
                else  //创建用户存在 当前使用用户是新用户 需要在系统重创建用户信息 
                {
                    //此处的UserId 是创建APPID的UserId ,一个人创建 多个人使用  
                    string mUserIdtemp = dataTable.Rows[0]["UserId"].ToString();
                    userInfoOutput = UserLoginByUserId(mUserIdtemp);//获取此账号下 创建者的信息  以及相关的企业信息

                    UserRegistInput userRegistInput = new UserRegistInput();
                    userRegistInput.F_UserName = enterpriseUserLoginInput.UserName;
                    userRegistInput.F_Mobile = "";

                    string mUserId = Guid.NewGuid().ToString();
                    var sqls = new List<string>();
                    string mInsert = @"insert into bas_user(F_UserId,F_Account,F_Password,F_Secretkey,F_UserName,F_Mobile,F_IndustryCategory,
                               F_Job,F_TenantId,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate,F_IsPwdSetted,F_OnionCoin,F_ThirdPartyUserId)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}','{10}','{11}','{12}','{13}','{14}','{15}')";

                    mInsert = string.Format(mInsert, mUserId, userRegistInput.F_Mobile, "",
                        "", userRegistInput.F_UserName, userRegistInput.F_Mobile,
                        userRegistInput.F_IndustryCategory,
                        userRegistInput.F_Job,
                       "F_TenantId",
                        "0", "1", "UserRegist", DateTime.Now, 1, mInitOnionCoin, enterpriseUserLoginInput.UserId);
                    sqls.Add(mInsert);

                    //---------------- 添加到用户群组中-----------------

                    //   CREATE TABLE `bas_user_gptusergroup` (
                    //  `F_Id` varchar(45) NOT NULL,
                    //  `F_UserId` varchar(500) DEFAULT NULL,
                    //  `F_MemberId` varchar(500) DEFAULT NULL,
                    //  `F_MemberName` longtext,
                    //  `F_MemberAccount` varchar(45) DEFAULT NULL,
                    //  `F_CreateUserId` varchar(45) DEFAULT NULL,
                    //  `F_CreateDate` datetime DEFAULT NULL,
                    //  `F_DeleteMark` int DEFAULT NULL,
                    //  `F_EnabledMark` int DEFAULT NULL,
                    //  PRIMARY KEY(`F_Id`)
                    //) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

                    string mId = Guid.NewGuid().ToString();
                    string insertIntoUserGroup = "insert into bas_user_gptusergroup (F_Id,F_MasterUserId,F_MemberId,F_MemberName,F_MemberAccount,F_CreateUserId,F_CreateDate,F_DeleteMark,F_EnabledMark)" +
                        "values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')";
                    insertIntoUserGroup = string.Format(insertIntoUserGroup, mId, mUserIdtemp, mUserId, enterpriseUserLoginInput.UserName,"", mUserId,DateTime.Now,"0","1");
                    sqls.Add(insertIntoUserGroup);
                    //---------------------------------------------------

                    userInfoOutput = new UserInfoOutput();
                    userInfoOutput.F_UserId = mUserId;
                    userInfoOutput.F_UserName = enterpriseUserLoginInput.UserName;
                    userInfoOutput.F_Account = "";

                    if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                    {
                        host = App.HttpContext?.Request.Host.Value,
                        data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                    })) throw Oops.Bah("注册失败").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");

                }
                SetSSOUserInfo(ref userInfoOutput);
            }
           
            return userInfoOutput;
        }



        /// <summary>
        /// 用户登录--手机号码
        /// </summary>
        /// <param name="userLoginByPhoneNumInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByMobile(UserLoginByPhoneNumInput userLoginByPhoneNumInput)
        {
            //验证码验证用户的合法性
            if (!CheckSmsCode(userLoginByPhoneNumInput.F_Mobile, userLoginByPhoneNumInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfo(userLoginByPhoneNumInput.F_Mobile);
            //验证手机号码是否存在 
            if (dtUserInfo.Count > 0)
                userInfoOutput = dtUserInfo[0];

            //不存在则注册 用户信息
            if (dtUserInfo.Count == 0)
            {
                UserRegistInput userRegistInput = new UserRegistInput();

                userRegistInput.F_UserName = userLoginByPhoneNumInput.F_Mobile;
                userRegistInput.F_Mobile = userLoginByPhoneNumInput.F_Mobile;

                string mUserId = Guid.NewGuid().ToString();
                var sqls = new List<string>();
                string mInsert = @"insert into bas_user(F_UserId,F_Account,F_Password,F_Secretkey,F_UserName,F_Mobile,F_IndustryCategory,
                               F_Job,F_TenantId,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate,F_IsPwdSetted,F_OnionCoin)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}','{10}','{11}','{12}','{13}','{14}')";
                mInsert = string.Format(mInsert, mUserId, userRegistInput.F_Mobile, "",
                    "", userRegistInput.F_UserName, userRegistInput.F_Mobile,
                    userRegistInput.F_IndustryCategory,
                    userRegistInput.F_Job,
                   "F_TenantId",
                    "0", "1", "UserRegist", DateTime.Now, 1, mInitOnionCoin);

                sqls.Add(mInsert);

                if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                {
                    host = App.HttpContext?.Request.Host.Value,
                    data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                })) throw Oops.Bah("注册失败").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");


                userInfoOutput.F_Mobile = userLoginByPhoneNumInput.F_Mobile;
                userInfoOutput.F_UserId = mUserId;
                userInfoOutput.F_UserName = userLoginByPhoneNumInput.F_Mobile;
                userInfoOutput.F_Nickname = userLoginByPhoneNumInput.F_Mobile;
                // throw Oops.Bah("当前用户不存在").StatusCode(201);
            }
            else
            {
                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            SetSSOUserInfo(ref userInfoOutput);
            return userInfoOutput;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByUserId(string userId)
        {
            UserInfoOutput userInfoOutput = new UserInfoOutput();
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByUserId(userId);

            if (dtUserInfo.Count > 0)
                userInfoOutput = dtUserInfo[0];

            if (string.IsNullOrEmpty(userInfoOutput.F_Nickname))
                userInfoOutput.F_Nickname = userInfoOutput.F_Account;

            if (string.IsNullOrEmpty(userInfoOutput.F_UserName))
                userInfoOutput.F_UserName = userInfoOutput.F_Account;

            return userInfoOutput;
        }


        /// <summary>
        /// 通过OpenId 获取用户信息
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByOpenId(string openId)
        {

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByOpenId(openId);

            if (dtUserInfo.Count > 0)
                userInfoOutput = dtUserInfo[0];

            if (string.IsNullOrEmpty(userInfoOutput.F_Nickname))
                userInfoOutput.F_Nickname = userInfoOutput.F_Account;

            if (string.IsNullOrEmpty(userInfoOutput.F_UserName))
                userInfoOutput.F_UserName = userInfoOutput.F_Account;

            return userInfoOutput;
        }


        /// <summary>
        /// 用户邮箱绑定
        /// </summary>
        /// <param name="userEmailBindingInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserEmailBinding(UserEmailBindingInput userEmailBindingInput)
        {
            //验证码验证用户的合法性
            if (!CheckSmsCode(userEmailBindingInput.F_Email, userEmailBindingInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);

            var sqls = new List<string>();
            string mUpdate = @"update bas_user set F_Email='{0}' where F_UserId='{1}'";
            mUpdate = string.Format(mUpdate, userEmailBindingInput.F_Email, userEmailBindingInput.F_UserId);

            sqls.Add(mUpdate);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);

            Thread.Sleep(1000);

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByUserId(userEmailBindingInput.F_UserId);

            if (dtUserInfo.Count > 0)
                userInfoOutput = dtUserInfo[0];

            if (dtUserInfo.Count == 0)
            {
                throw Oops.Bah("当前用户不存在").StatusCode(201);
            }
            else
            {
                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            //SetSSOUserInfo(userInfoOutput);
            return userInfoOutput;

        }

        /// <summary>
        /// 用户手机号码绑定
        /// </summary>
        /// <param name="userPhoneNumBindingInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserPhoneNumBinding(UserPhoneNumBindingInput userPhoneNumBindingInput)
        {
            //验证码验证用户的合法性
            if (!CheckSmsCode(userPhoneNumBindingInput.F_Mobile, userPhoneNumBindingInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);


            var sqls = new List<string>();
            string mUpdate = @"update bas_user set F_Mobile='{0}' where F_UserId='{1}'";
            mUpdate = string.Format(mUpdate, userPhoneNumBindingInput.F_Mobile, userPhoneNumBindingInput.F_UserId);

            sqls.Add(mUpdate);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);

            Thread.Sleep(1000);

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByUserId(userPhoneNumBindingInput.F_UserId);

            if (dtUserInfo.Count > 0)
                userInfoOutput = dtUserInfo[0];

            if (dtUserInfo.Count == 0)
            {
                throw Oops.Bah("当前用户不存在").StatusCode(201);
            }
            else
            {
                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            //SetSSOUserInfo(userInfoOutput);
            return userInfoOutput;

        }


        /// <summary>
        /// 用户登录--通过微信
        /// </summary>
        /// <param name="userLoginByWechatInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByWechat(UserLoginByWechatInput userLoginByWechatInput)
        {
            //判断当前用户是否存在
            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByOpenId(userLoginByWechatInput.F_OpenId);
            UserInfoOutput userInfoOutput = new UserInfoOutput();
            if (dtUserInfo.Count > 0)//存在则通过OpenId 获取用户数据
            {
                userInfoOutput = dtUserInfo[0];
                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            else
            {
                string mUserId = Guid.NewGuid().ToString();
                //消息队列存入数据库  F_AllowEndTime
                userInfoOutput.F_UserId = mUserId;
                userInfoOutput.F_UserName = userLoginByWechatInput.F_UserName;
                userInfoOutput.F_HeadImgurl = userLoginByWechatInput.F_HeadImgurl;
                userInfoOutput.F_Gender = userLoginByWechatInput.F_Sex;

                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userLoginByWechatInput.F_HeadIconKey);

                var sqls = new List<string>();
                string mInsert = @"insert into bas_user(F_UserId,F_UserName,F_Nickname,F_OpenId,F_UnionId,F_TenantId,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate,F_HeadImgurl,F_OnionCoin,F_HeadIcon)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}','{9}','{10}','{11}','{12}')";
                mInsert = string.Format(mInsert, mUserId, userLoginByWechatInput.F_UserName, userLoginByWechatInput.F_UserName, userLoginByWechatInput.F_OpenId, userLoginByWechatInput.F_UnionId, "mF_TenantId", "0", "1", "WechatLogin", DateTime.Now, userLoginByWechatInput.F_HeadImgurl, mInitOnionCoin, userLoginByWechatInput.F_HeadIconKey);
                sqls.Add(mInsert);
                if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                {
                    host = App.HttpContext?.Request.Host.Value,
                    data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                })) throw Oops.Bah("微信登录失败").StatusCode(201);
            }
            SetSSOUserInfo(ref userInfoOutput);
            return userInfoOutput;
        }



        /// <summary>
        /// 用户登录--单点登录
        /// </summary>
        /// <param name="userLoginByWechatInput"></param>
        /// <returns></returns>
        //public string UserLoginSSO(UserLoginByWechatInput userLoginByWechatInput)
        //{
        //    //判断当前用户是否存在
        //    List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByOpenId(userLoginByWechatInput.F_OpenId);
        //    UserInfoOutput userInfoOutput = new UserInfoOutput();
        //    if (dtUserInfo.Count > 0)//存在则通过OpenId 获取用户数据
        //    {
        //        userInfoOutput = dtUserInfo[0];
        //        userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
        //    }
        //    else
        //    {
        //        string mUserId = Guid.NewGuid().ToString();
        //        //消息队列存入数据库  F_AllowEndTime
        //        userInfoOutput.F_UserId = mUserId;
        //        userInfoOutput.F_UserName = userLoginByWechatInput.F_UserName;
        //        userInfoOutput.F_HeadImgurl = userLoginByWechatInput.F_HeadImgurl;
        //        userInfoOutput.F_Gender = userLoginByWechatInput.F_Sex;

        //        userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userLoginByWechatInput.F_HeadIconKey);

        //        var sqls = new List<string>();
        //        string mInsert = @"insert into bas_user(F_UserId,F_UserName,F_Nickname,F_OpenId,F_UnionId,F_TenantId,F_DeleteMark,F_EnabledMark,F_Description,F_CreateDate,F_HeadImgurl,F_OnionCoin,F_HeadIcon)
        //                       VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}','{9}','{10}','{11}','{12}')";
        //        mInsert = string.Format(mInsert, mUserId, userLoginByWechatInput.F_UserName, userLoginByWechatInput.F_UserName, userLoginByWechatInput.F_OpenId, userLoginByWechatInput.F_UnionId, "mF_TenantId", "0", "1", "WechatLogin", DateTime.Now, userLoginByWechatInput.F_HeadImgurl, mInitOnionCoin, userLoginByWechatInput.F_HeadIconKey);
        //        sqls.Add(mInsert);
        //        if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
        //        {
        //            host = App.HttpContext?.Request.Host.Value,
        //            data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
        //        })) throw Oops.Bah("微信登录失败").StatusCode(201);
        //    }
        //    SetSSOUserInfo(ref userInfoOutput);
        //    return "";
        //}


        /// <summary>
        /// 添加用户密码
        /// </summary>
        /// <param name="addUserPwdInput"></param>
        /// <returns></returns>
        public string AddUserPwd(AddUserPwdInput addUserPwdInput)
        {
            string mSecretkey = Md5Helper.Encrypt(CreateRandomCode.CreateNo(), 16).ToLower();
            string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(addUserPwdInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();

            var sqls = new List<string>();

            string userAddPwd = "update bas_user set F_Password='{0}',F_Secretkey='{1}',F_IsPwdSetted='1' where F_Account='{2}'";
            userAddPwd = string.Format(userAddPwd, dbPassword, mSecretkey, addUserPwdInput.F_Account);
            sqls.Add(userAddPwd);

            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("注册失败").StatusCode(201); //throw Oops.Oh("rabbitMQ 入列异常");

            return "";
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="userWechatBindingInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserWechatBinding(UserWechatBindingInput userWechatBindingInput)
        {
            var sqls = new List<string>();
            List<UserInfoOutput> mIsExistUserInfo = _sql.QueryUserInfoByOpenId(userWechatBindingInput.F_OpenId);
            if (mIsExistUserInfo.Count == 0)
            {
                userWechatBindingInput.F_UserId = Guid.NewGuid().ToString();
                string mSave = @"insert into bas_user (F_OpenId,F_UnionId,F_HeadImgurl,F_Nickname,F_Gender,F_UserId,F_EnabledMark,F_DeleteMark,F_CreateDate,F_UserName) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')";
                mSave = string.Format(mSave, userWechatBindingInput.F_OpenId, userWechatBindingInput.F_UnionId, userWechatBindingInput.F_HeadImgurl, userWechatBindingInput.F_UserName, userWechatBindingInput.F_Sex, userWechatBindingInput.F_UserId, '1', '0', DateTime.Now, userWechatBindingInput.F_UserName);
                sqls.Add(mSave);

                if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                {
                    host = App.HttpContext?.Request.Host.Value,
                    data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            }
            else
            {
                //string mUpdate = @"update bas_user set F_HeadImgurl='{2}',F_Nickname='{3}',F_Gender='{4}' where F_UserId='{5}'";
                //mUpdate = string.Format(mUpdate, userWechatBindingInput.F_OpenId, userWechatBindingInput.F_UnionId, userWechatBindingInput.F_HeadImgurl, userWechatBindingInput.F_Nickname, userWechatBindingInput.F_Sex, userWechatBindingInput.F_UserId);
                //sqls.Add(mUpdate);
            }



            Thread.Sleep(1000);

            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByOpenId(userWechatBindingInput.F_OpenId);

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            if (dtUserInfo.Count > 0)//存在则通过OpenId 获取用户数据
            {
                userInfoOutput = dtUserInfo[0];
                //  userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            return userInfoOutput;

        }

        /// <summary>
        /// 微信解绑
        /// </summary>
        /// <param name="userWechatUnbindingInput"></param>
        /// <returns></returns>
        public UserInfoOutput UserWechatUnbinding(UserWechatUnbindingInput userWechatUnbindingInput)
        {
            var sqls = new List<string>();
            string mUpdate = @"update bas_user set F_OpenId='',F_UnionId='',F_HeadImgurl='' where F_UserId='{0}'";
            mUpdate = string.Format(mUpdate, userWechatUnbindingInput.F_UserId);

            sqls.Add(mUpdate);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);

            Thread.Sleep(1000);

            List<UserInfoOutput> dtUserInfo = _sql.QueryUserInfoByUserId(userWechatUnbindingInput.F_UserId);

            UserInfoOutput userInfoOutput = new UserInfoOutput();
            if (dtUserInfo.Count > 0)//存在则通过OpenId 获取用户数据
            {
                userInfoOutput = dtUserInfo[0];
                userInfoOutput.F_HeadIcon = QueryUserAvatarByKey(userInfoOutput.F_HeadIcon);
            }
            return userInfoOutput;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public UserInfoOutput UserLoginByToken(string token)
        {
            if (kvSSOUser.ContainsKey(token))
            {
                if (kvSSOUser[token].AuthTime.AddMinutes(120) > DateTime.Now)
                {
                    kvSSOUser[token].AuthTime = DateTime.Now;
                    return kvSSOUser[token].UserInfoOutput;
                }
                else
                {
                    throw Oops.Bah("Token过期").StatusCode(201);
                }
            }
            throw Oops.Bah("Token验证失败").StatusCode(201);
        }





        #region 解密

        //public static string Decrypt(string encryptedData, string key)
        //{
        //    using (Aes aes = Aes.Create())
        //    {
        //        aes.Key = Encoding.UTF8.GetBytes(key);
        //        aes.Mode = CipherMode.CBC;
        //        aes.Padding = PaddingMode.PKCS7;

        //        byte[] iv = new byte[16];
        //        Array.Copy(Convert.FromBase64String(encryptedData), iv, iv.Length);
        //        aes.IV = iv;

        //        byte[] cipherTextBytes = Convert.FromBase64String(encryptedData);
        //        byte[] decryptedBytes = null;

        //        using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
        //        {
        //            decryptedBytes = decryptor.TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length);
        //        }

        //        return Encoding.UTF8.GetString(decryptedBytes);
        //    }
        //}

        //public static Dictionary<string, object> DecryptToDictionary(string encryptedData, string key)
        //{
        //    string decryptedData = Decrypt(encryptedData, key);
        //    return JsonConvert.DeserializeObject<Dictionary<string, object>>(decryptedData);
        //}

        #endregion


        public UserInfoOutput VerifyUserIdentityByToken(string token)
        {
            if (kvSSOUser.ContainsKey(token))
            {
                if (kvSSOUser[token].AuthTime.AddMinutes(120) > DateTime.Now)
                {
                    kvSSOUser[token].AuthTime = DateTime.Now;
                    return kvSSOUser[token].UserInfoOutput;
                }
                else
                {
                    throw Oops.Bah("Token过期").StatusCode(201);
                }
            }
            throw Oops.Bah("Token验证失败").StatusCode(201);
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="userInfoOutput"></param>
        void SetSSOUserInfo(ref UserInfoOutput userInfoOutput)
        {
            SSOUserCache ssoUserCache = new SSOUserCache();
            ssoUserCache.AuthTime = DateTime.Now;
            ssoUserCache.UserInfoOutput = userInfoOutput;
            if (!kvSSOUser.ContainsKey(userInfoOutput.token))
            {
                kvSSOUser.Add(userInfoOutput.token, ssoUserCache);
            }
            //--------------------2025/2/23 -----注释---------------------------
            //token值 写入缓存  
            //string timeStamp = ChatpptToken.getTimetamp();
            //string mSign = Sign(userInfoOutput.F_UserId, userInfoOutput.F_Account, userInfoOutput.F_UserName, timeStamp, "onionbit123");
            //// 生成 token
            //var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>()
            //{
            //    { "UserId", userInfoOutput.F_UserId },  // 存储Id
            //    { "Account",userInfoOutput.F_Account }, // 存储用户名
            //    { "UserName",userInfoOutput.F_UserName },
            //    { "Timestamp",timeStamp },
            //    { "Sign",mSign}
            //});
            //----------------------------------------------------

            //JwtDecoder.
            //  _redis.SetStringAsync("access_token", accessToken);

            //   _contextAccessor.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type,Access-Token,Accept,X-Custom-Header");

            //  _contextAccessor.HttpContext.Response.Headers["Access-Token"] = accessToken;

            var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>
            {
                 {"UserId",userInfoOutput.F_UserId},
                 {"Account",userInfoOutput.F_Account }
            }, 1);


            // 生成刷新Token令牌
            var refreshToken = JWTEncryption.GenerateRefreshToken(accessToken,100);

            // 设置响应报文头
            //   _contextAccessor.HttpContext.SetTokensOfResponseHeaders(accessToken, refreshToken);

            //  _contextAccessor.HttpContext.Response.Headers.Add("Authorization", $"Bearer {accessToken}");
            // _contextAccessor.HttpContext.Response.Headers.Add("RefreshToken", refreshToken);

            _contextAccessor.HttpContext.Response.Headers.Add("Authorization", $"Bearer {accessToken}");
            _contextAccessor.HttpContext.Response.Headers.Add("X-Authorization", $"Bearer {refreshToken}" );


            _contextAccessor.HttpContext.Response.Headers["access-token"] = accessToken;
            _contextAccessor.HttpContext.Response.Headers["x-access-token"] = refreshToken;


            userInfoOutput.token = accessToken;

            userInfoOutput.refreshtoken = refreshToken;

        }


        //public string RefreshToken(string userId)
        //{ 
        
        
        //}


        /// <summary>
        /// 企业登录
        /// </summary>
        /// <param name="userInfoOutput"></param>
        /// <returns></returns>
        //string GetTokens(UserInfoOutput userInfoOutput)
        //{
        //    //token值 写入缓存  
        //    string timeStamp = ChatpptToken.getTimetamp();
        //    string mSign = Sign(userInfoOutput.F_UserId, userInfoOutput.F_Account, userInfoOutput.F_UserName, timeStamp, "onionbit123");
        //    // 生成 token
        //    var accessToken = JWTEncryption.Encrypt(new Dictionary<string, object>()
        //    {
        //        { "UserId", userInfoOutput.F_UserId },  // 存储Id
        //        { "Account",userInfoOutput.F_Account }, // 存储用户名
        //        { "UserName",userInfoOutput.F_UserName },
        //        { "Timestamp",timeStamp },
        //        { "Sign",mSign}
        //    });

        //    return accessToken;
        //}





        /// <summary>
        /// 获取用户简略信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public UserSimpleInfoOutput QueryUserSimpleInfo(string account)
        {
            //QueryUserSimpleInfo
            UserSimpleInfoOutput userInfoOutput = new UserSimpleInfoOutput();
            List<UserSimpleInfoOutput> dtUserInfo = _sql.QueryUserSimpleInfo(account);
            if (dtUserInfo.Count == 0)
            {
                throw Oops.Bah("当前用户不存在").StatusCode(201);
            }
            else
            {
                userInfoOutput = dtUserInfo[0];
            }
            return userInfoOutput;
        }

        /// <summary>
        /// 修改找回 用户密码
        /// </summary>
        /// <param name="getBackPwdUserInput"></param>
        /// <returns></returns>
        public string GetBackPassword(GetBackPwdUserInput getBackPwdUserInput)
        {
            //短信验证码 
            if (!CheckSmsCode(getBackPwdUserInput.F_Mobile, getBackPwdUserInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);

            var sqls = new List<string>();

            string mSecretkey = Md5Helper.Encrypt(CreateRandomCode.CreateNo(), 16).ToLower();
            string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(getBackPwdUserInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();
            string mResetPwd = @"update bas_user set F_Password='{0}' ,F_Secretkey='{1}' where F_Account='{2}'";
            mResetPwd = string.Format(mResetPwd, dbPassword, mSecretkey, getBackPwdUserInput.F_Mobile);

            sqls.Add(mResetPwd);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            return "";
        }


        public string ModifyUserPwd(ModifyUserPwdInput modifyUserPwdInput)
        {
            DataTable dataTable = _sql.QueryUserPassword(modifyUserPwdInput.F_Account);
            string mSecretkey = dataTable.Rows[0]["F_Secretkey"].ToString();
            string mPassword = dataTable.Rows[0]["F_Password"].ToString();
            string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(modifyUserPwdInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();

            if (dbPassword.Trim() == mPassword.Trim())   //密码验证
            {
                string mSecretkey_new = Md5Helper.Encrypt(CreateRandomCode.CreateNo(), 16).ToLower();
                string dbPassword_new = Md5Helper.Encrypt(DESEncrypt.Encrypt(modifyUserPwdInput.F_Password_New.ToLower(), mSecretkey_new).ToLower(), 32).ToLower();
                var sqls = new List<string>();
                string mResetPwd = @"update bas_user set F_Password='{0}' ,F_Secretkey='{1}' where F_Account='{2}'";
                mResetPwd = string.Format(mResetPwd, dbPassword_new, mSecretkey_new, modifyUserPwdInput.F_Mobile);
                sqls.Add(mResetPwd);
                if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                {
                    host = App.HttpContext?.Request.Host.Value,
                    data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            }
            else
            {
                throw Oops.Bah("密码验证失败").StatusCode(201);
            }

            return "";
        }

        /// <summary>
        /// 通过原始密码修改密码
        /// </summary>
        /// <param name="modifyUserPwdInput"></param>
        /// <returns></returns>
        public string ModifyUserPwdByOriginalPwd(ModifyUserPwdByOriginalPwdInput modifyUserPwdInput)
        {
            DataTable dataTable = _sql.QueryUserPassword(modifyUserPwdInput.F_Account);
            string mSecretkey = dataTable.Rows[0]["F_Secretkey"].ToString();
            string mPassword = dataTable.Rows[0]["F_Password"].ToString();
            string dbPassword = Md5Helper.Encrypt(DESEncrypt.Encrypt(modifyUserPwdInput.F_Password.ToLower(), mSecretkey).ToLower(), 32).ToLower();

            if (dbPassword.Trim() == mPassword.Trim())//密码验证
            {
                string mSecretkey_new = Md5Helper.Encrypt(CreateRandomCode.CreateNo(), 16).ToLower();
                string dbPassword_new = Md5Helper.Encrypt(DESEncrypt.Encrypt(modifyUserPwdInput.F_Password_New.ToLower(), mSecretkey_new).ToLower(), 32).ToLower();
                var sqls = new List<string>();

                if (!string.IsNullOrEmpty(modifyUserPwdInput.F_UserId))
                {

                    string mResetPwd = @"update bas_user set F_Password='{0}' ,F_Secretkey='{1}' where F_UserId='{2}'";
                    mResetPwd = string.Format(mResetPwd, dbPassword_new, mSecretkey_new, modifyUserPwdInput.F_UserId);
                    sqls.Add(mResetPwd);
                }
                else
                {
                    string mResetPwd = @"update bas_user set F_Password='{0}' ,F_Secretkey='{1}' where F_Account='{2}'";
                    mResetPwd = string.Format(mResetPwd, dbPassword_new, mSecretkey_new, modifyUserPwdInput.F_Account);
                    sqls.Add(mResetPwd);
                }


                if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
                {
                    host = App.HttpContext?.Request.Host.Value,
                    data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
                })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            }
            else
            {
                throw Oops.Bah("密码验证失败").StatusCode(201);
            }
            return "";
        }



        /// <summary>
        /// 修改用户名称
        /// </summary>
        /// <param name="modifyUserNameInput"></param>
        /// <returns></returns>
        public string ModifyUserName(ModifyUserNameInput modifyUserNameInput)
        {
            //F_UserId  
            var sqls = new List<string>();
            string mUpdate = @"update bas_user set F_UserName='{0}',F_ModifyDate='{1}' where F_UserId='{2}'";
            mUpdate = string.Format(mUpdate, modifyUserNameInput.F_UserName, DateTime.Now, modifyUserNameInput.F_Id);

            sqls.Add(mUpdate);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            return "";
        }

        /// <summary>
        /// 修改用户手机号码
        /// </summary>
        /// <param name="modifyUserPhoneNumInput"></param>
        /// <returns></returns>
        public string ModifyUserPhoneNum(ModifyUserPhoneNumInput modifyUserPhoneNumInput)
        {
            //短信验证码 
            if (!CheckSmsCode(modifyUserPhoneNumInput.F_PhoneNum, modifyUserPhoneNumInput.F_VerificationCode))
                throw Oops.Bah("验证码无效").StatusCode(201);

            var sqls = new List<string>();
            string mUpdate = @"update bas_user set F_Mobile='{0}',F_Account='{1}',F_ModifyDate='{2}' where F_UserId='{3}'";
            mUpdate = string.Format(mUpdate, modifyUserPhoneNumInput.F_PhoneNum, modifyUserPhoneNumInput.F_PhoneNum, DateTime.Now, modifyUserPhoneNumInput.F_Id);

            sqls.Add(mUpdate);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);
            return "";
        }

        /// <summary>
        /// 注销账户
        /// </summary>
        /// <param name="unsubscribeUserAccountInput"></param>
        /// <returns></returns>
        public string UnsubscribeUserAccount(UnsubscribeUserAccountInput unsubscribeUserAccountInput)
        {
            if (!CheckSmsCode(unsubscribeUserAccountInput.F_Account, unsubscribeUserAccountInput.F_VerificationCode))//短信验证码 
                throw Oops.Bah("验证码无效").StatusCode(201);

            var sqls = new List<string>();
            string updateString = @"update bas_user set F_DeleteMark='1',F_EnabledMark='0' where F_UserId='{0}'";
            updateString = string.Format(updateString, unsubscribeUserAccountInput.F_Id);
            sqls.Add(updateString);
            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Bah("rabbitMQ 入列异常").StatusCode(201);

            return "";
        }

        /// <summary>
        /// 设置用户头像
        /// </summary>
        /// <param name="setUserAvatarInput"></param>
        /// <returns></returns>
        public string SetUserAvatar(SetUserAvatarInput setUserAvatarInput)
        {
            //MongoDBDataModel mongoDBDataModel = new MongoDBDataModel();
            //mongoDBDataModel.stringData = setUserAvatarInput.F_HeadIcon;
            //mongoDBDataModel.key = Guid.NewGuid().ToString();
            //string mJsonString = JsonConvert.SerializeObject(mongoDBDataModel);
            //BsonDocument document = BsonDocument.Parse(mJsonString);
            //_mongoCollection.InsertOne(document); //存入mongodb

            string imgKey = Guid.NewGuid().ToString();
            //消息队列存入数据库
            var sqls = new List<string>();
            string mUpdate = @" update bas_user set F_HeadIcon='{0}' where F_UserId='{1}' ";
            mUpdate = string.Format(mUpdate, imgKey, setUserAvatarInput.F_Id);
            sqls.Add(mUpdate);

            string mInsertHeadData = "insert into bas_user_headicon (F_Id,F_HeadImgData) values ('{0}','{1}')";
            mInsertHeadData = string.Format(mInsertHeadData, imgKey, setUserAvatarInput.F_HeadIcon);
            sqls.Add(mInsertHeadData);


            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Oh("rabbitMQ 入列异常");

            return imgKey;
        }

        /// <summary>
        /// 获取Mongodb中的头像数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string QueryUserAvatarByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            //var filter = new BsonDocument();
            //filter.Add("key", key);
            //var documents = _mongoCollection.Find(filter).FirstOrDefault();
            //string imageBase64Data = documents == null ? "" : documents["stringData"].ToString();
            //return imageBase64Data;

            DataTable dataTable = _sql.QueryUserHeadIcon(key);
            if (dataTable.Rows.Count > 0)
                return dataTable.Rows[0]["F_HeadImgData"].ToString();

            return "";
        }



        /// <summary>
        /// 设置头像
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string SetUserAvatarData2(UserAvatarDataInput data)
        {
            var sqls = new List<string>();
            string key = Guid.NewGuid().ToString();
            string mInsertHeadData = "insert into bas_user_headicon (F_Id,F_HeadImgData) values ('{0}','{1}')";
            mInsertHeadData = string.Format(mInsertHeadData, key, data.F_HeadIconData);
            sqls.Add(mInsertHeadData);

            if (!_rabbitMQ.Enqueue(MessageQueueName, new RabbitMQResponse()
            {
                host = App.HttpContext?.Request.Host.Value,
                data = JsonConvert.SerializeObject(new EqueueDataModel() { name = "BIData", businessTopic = "BIData", key = "BIData", sqls = sqls })
            })) throw Oops.Oh("rabbitMQ 入列异常");


            return key;
        }


        public string SetUserAvatarData2(IFormFile formFile, string OpenId)
        {
            string imgUrl = "";
            string fileSerialNo = DateTime.Now.ToString("yyyyMMdd");
            // 如：保存到网站根目录下的 uploads 目录
            var savePath = _FileSavePath + "\\" + fileSerialNo;//Path.Combine(App.HostEnvironment.ContentRootPath, "uploads");
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            // 遍历所有文件逐一上传
            if (formFile.Length > 0)
            {
                // 避免文件名重复，采用 GUID 生成
                // _logger.LogDebug(savePath);
                OpenId = Guid.NewGuid().ToString("N");
                var fileName = "\\" + OpenId + Path.GetExtension(formFile.FileName);
                var filePath = savePath + fileName;
                // _logger.LogDebug(filePath);
                // 保存到指定路径
                using (var stream = System.IO.File.Create(filePath))
                {
                    formFile.CopyToAsync(stream);
                }
                //模板Img 地址
                imgUrl = _FileUrl + "/" + fileSerialNo + "/" + OpenId + Path.GetExtension(formFile.FileName);
            }
            return imgUrl;

        }

        /// <summary>
        /// 获取用户MQTT Channel 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public UserMqttOutput QueryUserMqttChannel(string userId)
        {
            Random random = new Random();
            UserMqttOutput userMqttOutput = new UserMqttOutput();
            userMqttOutput.mqttaddr = mqttAddr;
            userMqttOutput.user = mqttUser;
            userMqttOutput.pwd = mqttPwd;
            userMqttOutput.MessageChannelId = Guid.NewGuid().ToString() + random.Next(100, 999);
            return userMqttOutput;
        }

        #region  加密解密
        private static byte[] Keys = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };


        /// <summary>
        /// DES加密字符串
        /// </summary>
        /// <param name="encryptString">待加密的字符串
        /// <param name="encryptKey">加密密钥,要求为8位
        /// <returns>加密成功返回加密后的字符串，失败返回源串</returns>
        public static string DESEncryptSSO(string encryptString)
        {
            try
            {
                string encryptKey = "onion123";
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        /// <summary>
        /// DES解密字符串
        /// </summary>
        /// <param name="decryptString">待解密的字符串
        /// <param name="decryptKey">解密密钥,要求为8位,和加密密钥相同
        /// <returns>解密成功返回解密后的字符串，失败返源串</returns>
        public static string DESDecryptSSO(string decryptString)
        {
            try
            {
                string decryptKey = "onion123";
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }




        #endregion



        #region  用户登录ChatPPT 
        public async Task<string> LoginChatPPT(string userId)
        {
            UserInfoOutput userInfoOutput = _sql.QueryUserInfoByUserId(userId).FirstOrDefault();

            if (string.IsNullOrEmpty(userInfoOutput.F_UserName))
            {
                userInfoOutput.F_UserName = userInfoOutput.F_Account;
            }
            return await ChatpptToken.getToken(userInfoOutput.F_UserId, userInfoOutput.F_Account, "86", userInfoOutput.F_UserName, accesskeyid, channel, signkey, "v1"); ;
        }


        #endregion



        #region  用户签名加密 


        public static string Sign(string userId, string account, string userName, string timestamp, string signkey)
        {
            string mStringKeyValue = "account={0}&timestamp={1}&userId={2}&userName={3}&{4}";
            mStringKeyValue = string.Format(mStringKeyValue, account, timestamp, userId, userName, signkey);
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


        #endregion

        public UserInfoOutput UserLoginBySSOToken(SSOLoginTokenInput token)
        {

            SSOUserInfo userInfo = DecodeJwtToken("onion123", token.token);
            string userId = userInfo.UserId;
            UserInfoOutput userInfoOutput = _sql.QueryUserInfoByUserId(userId).FirstOrDefault();
            return userInfoOutput;

        }


        public SSOUserInfo DecodeJwtToken(string secret, string token)
        {
            try
            {
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                var json = decoder.Decode(token, secret, verify: false);
                SSOUserInfo userInfo = JsonConvert.DeserializeObject<SSOUserInfo>(json);

                return userInfo;
            }
            catch (TokenExpiredException)
            {

                throw Oops.Oh("Token 已经过期！");
            }
            catch (SignatureVerificationException)
            {
                throw Oops.Oh("签名校验失败，数据可能被篡改！");

            }
            catch (Exception ex)
            {
                throw Oops.Oh($"解密JWT令牌时发生错误：{ex.Message}");
            }
        }





    }
}
