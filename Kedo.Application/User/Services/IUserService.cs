using Microsoft.AspNetCore.Http;
using Kedo.Application.User.Dtos.input;
using Kedo.Application.User.Dtos.output;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Kedo.Application.User.Services
{
    public interface IUserService
    {
        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="userRegistInput"></param>
        /// <returns></returns>
        string UserRegist(UserRegistInput userRegistInput);

        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        string GetVerificationCode(string phoneNum);
        /// <summary>
        /// 获取邮箱验证码
        /// </summary>
        /// <param name="emailNum"></param>
        /// <returns></returns>
        string GetEmailVerificationCode(string emailNum);
        /// <summary>
        /// 用户登录--账号 密码
        /// </summary>
        /// <param name="userLoginByAccountInput"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByAccount(UserLoginByAccountInput userLoginByAccountInput);

        /// <summary>
        /// 用户登录--手机号码
        /// </summary>
        /// <param name="userLoginByPhoneNumInput"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByMobile(UserLoginByPhoneNumInput userLoginByPhoneNumInput);




        /// <summary>
        /// 用户通过微信登录
        /// </summary>
        /// <param name="userLoginByWechatInput"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByWechat(UserLoginByWechatInput userLoginByWechatInput);

        /// <summary>
        /// 用户微信绑定
        /// </summary>
        /// <param name="userWechatBindingInput"></param>
        /// <returns></returns>
        UserInfoOutput UserWechatBinding(UserWechatBindingInput userWechatBindingInput);



        /// <summary>
        /// 用户邮箱绑定
        /// </summary>
        /// <param name="userEmailBindingInput"></param>
        /// <returns></returns>
        UserInfoOutput UserEmailBinding(UserEmailBindingInput userEmailBindingInput);


        /// <summary>
        /// 用户手机号码绑定
        /// </summary>
        /// <param name="userPhoneNumBindingInput"></param>
        /// <returns></returns>
        UserInfoOutput UserPhoneNumBinding(UserPhoneNumBindingInput userPhoneNumBindingInput);


        /// <summary>
        /// 微信账号解绑
        /// </summary>
        /// <param name="userWechatUnbindingInput"></param>
        /// <returns></returns>
        UserInfoOutput UserWechatUnbinding(UserWechatUnbindingInput userWechatUnbindingInput);

        /// <summary>
        /// 单点登录验证token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByToken(string token);


        /// <summary>
        /// 用户登录验证
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginBySSOToken(SSOLoginTokenInput token);


        /// <summary>
        /// 企业用户登录  
        /// </summary>
        /// <param name="enterpriseUserLoginInput"></param>
        /// <returns></returns>
        UserInfoOutput EnterpriseUserLoginByAppIdAndSecret(EnterpriseUserLoginInput enterpriseUserLoginInput);


        /// <summary>
        /// 通过UserId 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByUserId(string userId);

        /// <summary>
        /// OpenId
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        UserInfoOutput UserLoginByOpenId(string openId);

        /// <summary>
        /// 验证用户手机号码是否已经被占用
        /// </summary>
        /// <param name="phoneNum"></param>
        /// <returns></returns>
        string CheckPhoneNoIsUsed(string phoneNum);

        /// <summary>
        /// 通过账号获取用户简单信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        UserSimpleInfoOutput QueryUserSimpleInfo(string account);
        /// <summary>
        /// 找回密码
        /// </summary>
        /// <param name="getBackPwdUserInput"></param>
        /// <returns></returns>
        string GetBackPassword(GetBackPwdUserInput getBackPwdUserInput);

        /// <summary>
        /// 修改用户名
        /// </summary>
        /// <param name="modifyUserNameInput"></param>
        /// <returns></returns>
        string ModifyUserName(ModifyUserNameInput modifyUserNameInput);

        /// <summary>
        /// 修改用户电话
        /// </summary>
        /// <param name="modifyUserPhoneNumInput"></param>
        /// <returns></returns>
        string ModifyUserPhoneNum(ModifyUserPhoneNumInput modifyUserPhoneNumInput);

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="modifyUserPwdInput"></param>
        /// <returns></returns>
        string ModifyUserPwd(ModifyUserPwdInput modifyUserPwdInput);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifyUserPwdByOriginalPwdInput"></param>
        /// <returns></returns>
        string ModifyUserPwdByOriginalPwd(ModifyUserPwdByOriginalPwdInput modifyUserPwdByOriginalPwdInput);


        /// <summary>
        /// 添加密码（微信登录后）
        /// </summary>
        /// <param name="addUserPwdInput"></param>
        /// <returns></returns>
        string AddUserPwd(AddUserPwdInput addUserPwdInput);

        /// <summary>
        /// 注销账户 
        /// </summary>
        /// <param name="unsubscribeUserAccountInput"></param>
        /// <returns></returns>
        string UnsubscribeUserAccount(UnsubscribeUserAccountInput unsubscribeUserAccountInput);

        /// <summary>
        /// 设置用户头像
        /// </summary>
        /// <param name="setUserAvatarInput"></param>
        /// <returns></returns>
        string SetUserAvatar(SetUserAvatarInput setUserAvatarInput);

        ///// <summary>
        ///// 设置用户头像
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        // string SetUserAvatarData2(UserAvatarDataInput UserAvatarDataInput);



        string SetUserAvatarData2(IFormFile files, string OpenId);

        /// <summary>
        /// 通过Key  查询头像数据
        /// </summary>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        // string QueryUserAvatarByKey(string dataKey,string userId);




        string QueryUserAvatarByKey(string dataKey);


        /// <summary>
        /// 用户Mqtt 通道信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        UserMqttOutput QueryUserMqttChannel(string userId);

        /// <summary>
        /// 获取用户登录ChatPPT token 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<string> LoginChatPPT(string userId);


        /// <summary>
        /// 用户刷新token 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
      //  string RefreshToken(string userId);

    }
}
