using Furion.DynamicApiController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kedo.Application.User.Dtos.input;
using Kedo.Application.User.Dtos.output;
using Kedo.Application.User.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kedo.Application.User
{
    public class UserServices : IDynamicApiController
    {
        private readonly IUserService _userService;

        public UserServices(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        /// <param name="userRegistInput">用户注册</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public object UserRegist([FromBody] UserRegistInput userRegistInput)
        {
            return _userService.UserRegist(userRegistInput);
        }

        /// <summary>
        /// 检测手机号码是否存在
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public string CheckPhoneNoIsUsed([FromBody] string phoneNum)
        {
            return _userService.CheckPhoneNoIsUsed(phoneNum);
        }

        /// <summary>
        /// 获取手机验证码
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public string GetVerificationCode([FromQuery] string phoneNum)
        {
            return _userService.GetVerificationCode(phoneNum);
        }


        /// <summary>
        /// 获取邮箱验证码
        /// </summary>
        /// <param name="EmailNum"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public string GetEmailVerificationCode([FromQuery] string EmailNum)
        {
            return _userService.GetEmailVerificationCode(EmailNum);
        }

        /// <summary>
        /// 用户登录--账号 密码
        /// </summary>
        /// <param name="userLoginByAccountInput"></param>
        /// <returns></returns>
        [HttpPost]
        public UserInfoOutput UserLoginByAccount(UserLoginByAccountInput userLoginByAccountInput)
        {
            return _userService.UserLoginByAccount(userLoginByAccountInput);
        }


        /// <summary>
        /// 用户登录--手机号码
        /// </summary>
        /// <param name="userLoginByPhoneNumInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserLoginByMobile(UserLoginByPhoneNumInput userLoginByPhoneNumInput)
        {
            return _userService.UserLoginByMobile(userLoginByPhoneNumInput);
        }


        /// <summary>
        /// 用户微信绑定
        /// </summary>
        /// <param name="userWechatBindingInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserWechatBinding(UserWechatBindingInput userWechatBindingInput)
        {
            return _userService.UserWechatBinding(userWechatBindingInput);
        }

        /// <summary>
        /// 用户邮箱绑定
        /// </summary>
        /// <param name="userEmailBindingInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserEmailBinding(UserEmailBindingInput userEmailBindingInput)
        {
            return _userService.UserEmailBinding(userEmailBindingInput);
        }



        /// <summary>
        /// 用户微信解除绑定
        /// </summary>
        /// <param name="userWechatUnbindingInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserWechatUnbinding(UserWechatUnbindingInput userWechatUnbindingInput)
        {
            return _userService.UserWechatUnbinding(userWechatUnbindingInput);
        }

        /// <summary>
        /// 用户登录--微信登录
        /// </summary>
        /// <param name="userLoginByWechatInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserLoginByWechat(UserLoginByWechatInput userLoginByWechatInput)
        {
            return _userService.UserLoginByWechat(userLoginByWechatInput);
        }

        /// <summary>
        /// 添加用户密码
        /// </summary>
        /// <param name="addUserPwdInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string AddUserPwd(AddUserPwdInput addUserPwdInput)
        {
            return _userService.AddUserPwd(addUserPwdInput);
        }


        /// <summary>
        /// 用户首次微信登录绑定手机号码
        /// </summary>
        /// <param name="userPhoneNumBindingInput"></param>
        /// <returns></returns>
        [HttpPost]
        public UserInfoOutput UserPhoneNumBinding(UserPhoneNumBindingInput userPhoneNumBindingInput)
        {
            return _userService.UserPhoneNumBinding(userPhoneNumBindingInput);
        }

        /// <summary>
        /// 通过Token 获取用户信息  SSO 登录 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        public UserInfoOutput UserLoginByToken([FromQuery] string token)
        {
            return _userService.UserLoginByToken(token);
        }

        /// <summary>
        /// 企业登录 AppId 和 Secret
        /// </summary>
        /// <param name="enterpriseUserLoginInput"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput EnterpriseUserLogin([FromBody]EnterpriseUserLoginInput enterpriseUserLoginInput)
        {
            return _userService.EnterpriseUserLoginByAppIdAndSecret(enterpriseUserLoginInput);
        }


            /// <summary>
            ///JWT SSO 验证token
            /// </summary>
            /// <param name="token"></param>
            /// <returns></returns>
            [HttpPost]
        [AllowAnonymous]
        public UserInfoOutput UserLoginBySSOToken(SSOLoginTokenInput token)
        {
            return _userService.UserLoginBySSOToken(token);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        public UserInfoOutput QueryUserInfoByUserId([FromQuery] string userId)
        {
            return _userService.UserLoginByUserId(userId);
        }


        /// <summary>
        /// 通过OpenId 获取用户信息
        /// </summary>
        /// <param name="UserLoginByOpenId"></param>
        /// <returns></returns>
        [HttpGet]
        public UserInfoOutput UserLoginByOpenId([FromQuery]string UserLoginByOpenId)
        {
            return _userService.UserLoginByOpenId(UserLoginByOpenId);
        }

        /// <summary>
        /// 获取用户简略信息  邀请用户加入团队时 用
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpGet]
        public UserSimpleInfoOutput QueryUserSimpleInfo([FromQuery] string account)
        {
            return _userService.QueryUserSimpleInfo(account);
        }

        /// <summary>
        /// 找回密码
        /// </summary>
        /// <param name="getBackPwdUserInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string GetBackPassword(GetBackPwdUserInput getBackPwdUserInput)
        {
            return _userService.GetBackPassword(getBackPwdUserInput);
        }

        /// <summary>
        /// 修改用户名称
        /// </summary>
        /// <param name="modifyUserNameInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string ModifyUserName(ModifyUserNameInput modifyUserNameInput)
        {
            return _userService.ModifyUserName(modifyUserNameInput);
        }

        /// <summary>
        /// 修改头像
        /// </summary>
        /// <param name="setUserAvatarInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string SetUserAvatar(SetUserAvatarInput setUserAvatarInput)
        {
            return _userService.SetUserAvatar(setUserAvatarInput);
        }


        /// <summary>
        /// 设置用户图像
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="OpenId"></param>
        /// <returns></returns>
        public string SetUserAvatarData2(IFormFile formFile, [FromForm] string OpenId)
        {
            return _userService.SetUserAvatarData2(formFile, OpenId);
        }


        /// <summary>
        /// 修改用户手机号码
        /// </summary>
        /// <param name="modifyUserPhoneNumInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string ModifyUserPhoneNum(ModifyUserPhoneNumInput modifyUserPhoneNumInput)
        {
            return _userService.ModifyUserPhoneNum(modifyUserPhoneNumInput);
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="modifyUserPwdInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string ModifyUserPwd(ModifyUserPwdInput modifyUserPwdInput)
        {
            return _userService.ModifyUserPwd(modifyUserPwdInput);
        }

        /// <summary>
        /// 通过原始密码 修改用户密码
        /// </summary>
        /// <param name="modifyUserPwdInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string ModifyUserPwdByOriginalPwd(ModifyUserPwdByOriginalPwdInput modifyUserPwdInput)
        {
            return _userService.ModifyUserPwdByOriginalPwd(modifyUserPwdInput);
        }

        /// <summary>
        /// 注销账户
        /// </summary>
        /// <param name="unsubscribeUserAccountInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string UnsubscribeUserAccount(UnsubscribeUserAccountInput unsubscribeUserAccountInput)
        {
            return _userService.UnsubscribeUserAccount(unsubscribeUserAccountInput);
        }

        /// <summary>
        /// 查询头像数据
        /// </summary>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        [HttpGet]
        public string QueryUserAvatarDataByKey([FromQuery] string dataKey)
        {
            return _userService.QueryUserAvatarByKey(dataKey);
        }

        /// <summary>
        /// 获取用户MQTT 通道
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        public UserMqttOutput QueryUserMqttChannel([FromQuery] string userId)
        {
            return _userService.QueryUserMqttChannel(userId);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<string> LoginChatPPT([FromBody] string userId)
        {
            return await _userService.LoginChatPPT(userId);
        }

    }
}
