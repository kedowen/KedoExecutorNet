using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.input
{
    /// <summary>
    /// 用户注册
    /// </summary>
    public class UserRegistInput
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string F_UserName { set; get; }
        /// <summary>
        /// 手机号码
        /// </summary>
        public string F_Mobile { set; get; }

        /// <summary>
        /// 公司名称
        /// </summary>
        public string F_CompanyName { set; get; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string F_Email { set; get; }

        /// <summary>
        /// 行业
        /// </summary>
        public string F_IndustryCategory { set; get; }

        /// <summary>
        /// 工作岗位
        /// </summary>
        public string F_Job { set; get; }

        /// <summary>
        /// 验证码
        /// </summary>
        public string F_VerificationCode { set; get; }

        /// <summary>
        /// 密码
        /// </summary>
        public string F_Password { set; get; }

        /// <summary>
        /// 加密key
        /// </summary>
        public string F_Secretkey { set; get; }



    }


    /// <summary>
    /// 用户账号密码登录
    /// </summary>
    public class UserLoginByAccountInput
    {
        /// <summary>
        /// 账号
        /// </summary>
        public string F_Account { set; get; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string F_Password { set; get; }
    }

    /// <summary>
    /// 用户手机号登录
    /// </summary>
    public class UserLoginByPhoneNumInput
    {
        /// <summary>
        /// 账号
        /// </summary>
        public string F_Mobile { set; get; }

        /// <summary>
        /// 验证码
        /// </summary>
        public string F_VerificationCode { set; get; }
    }



    /// <summary>
    /// 用户微信登录
    /// </summary>
    public class UserLoginByWechatInput
    {
        /// <summary>
        /// 账号
        /// </summary>
        public string F_OpenId { set; get; }


        public string F_UnionId { set; get; }

 
        public string F_UserName { set; get; }


        public string F_Sex { set; get; }


        public string F_HeadImgurl { set; get; }


        public string F_HeadIconKey { set; get; }
    }


    public class UserWechatBindingInput
    {
        public string F_UserId { set; get; }

        public string F_Nickname { set; get; }

        public string F_UserName { set; get; }

        public string F_Sex { set; get; }
        // nickname，sex
        /// <summary>
        /// OpenId账号
        /// </summary>
        public string F_OpenId { set; get; }

        public string F_UnionId { set; get; }

        public string F_HeadImgurl { set; get; }
    }


    public class UserEmailBindingInput
    {
        public string F_UserId { set; get; }

        public string F_Email { set; get; }

        public string F_VerificationCode { set; get; }

    }


    public class UserPhoneNumBindingInput
    {
        public string F_UserId { set; get; }

        public string F_Mobile { set; get; }

        public string F_VerificationCode { set; get; }

    }


    public class UserWechatUnbindingInput
    {
        public string F_UserId { set; get; }

    }
    /// <summary>
    /// 找回密码
    /// </summary>
    public class GetBackPwdUserInput
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public string F_Mobile { set; get; }

        /// <summary>
        /// 密码
        /// </summary>
        public string F_Password { set; get; }

        /// <summary>
        /// /验证码
        /// </summary>
        public string F_VerificationCode { set; get; }

    }


    /// <summary>
    /// 修改用户
    /// </summary>
    public class ModifyUserPwdInput
    {
        /// <summary>
        /// 用户账号
        /// </summary>
        public string F_Account { set; get; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string F_Mobile { set; get; }

        /// <summary>
        /// 密码
        /// </summary>
        public string F_Password { set; get; }

        /// <summary>
        /// 新密码
        /// </summary>
        public string F_Password_New { set; get; }

        /// <summary>
        /// /验证码
        /// </summary>
        public string F_VerificationCode { set; get; }

    }


    /// <summary>
    /// 通过原始密码 修改用户密码
    /// </summary>
    public class ModifyUserPwdByOriginalPwdInput
    {
        public string F_UserId { set; get; }
        /// <summary>
        /// 用户账号
        /// </summary>
        public string F_Account { set; get; }

        /// <summary>
        /// 密码
        /// </summary>
        public string F_Password { set; get; }

        /// <summary>
        /// 新密码
        /// </summary>
        public string F_Password_New { set; get; }

    }



    /// <summary>
    /// 通过原始密码 修改用户密码
    /// </summary>
    public class AddUserPwdInput
    {
        /// <summary>
        /// 用户账号
        /// </summary>
        public string F_Account { set; get; }

        /// <summary>
        /// 密码
        /// </summary>
        public string F_Password { set; get; }

    }

    public class ModifyUserNameInput
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string F_Id { set; get; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string F_UserName { set; get; }
    }


    public class SetUserAvatarInput
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string F_Id { set; get; }

        /// <summary>
        /// 头像
        /// </summary>
        public string F_HeadIcon { set; get; }

    }


    public class UserAvatarDataInput
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string F_OpenId { set; get; }

        /// <summary>
        /// 头像
        /// </summary>
        public string F_HeadIconData { set; get; }

    }

    public class ModifyUserPhoneNumInput
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string F_Id { set; get; }
        /// <summary>
        /// 用户电话
        /// </summary>
        public string F_PhoneNum { set; get; }


        public string F_VerificationCode { set; get; }
    }

    /// <summary>
    /// 注销账户
    /// </summary>
    public class UnsubscribeUserAccountInput
    {
        /// <summary>
        /// UserId
        /// </summary>
        public string F_Id { set; get; }
        /// <summary>
        /// 用户账号
        /// </summary>
        public string F_Account { set; get; }

        /// <summary>
        /// 验证码
        /// </summary>
        public string F_VerificationCode { set; get; }
    }


}
