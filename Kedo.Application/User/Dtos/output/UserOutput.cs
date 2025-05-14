using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.output
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public class UserInfoOutput
    {
        /// <summary>
        /// 用户编号
        /// </summary>
        public string F_UserId { get; set; }
        /// <summary>
        /// 用户账号
        /// </summary>
        public string F_Account { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string F_UserName { set; get; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string F_Nickname { set; get; }
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
        /// 邮箱
        /// </summary>
        public string F_OpenId { set; get; }

        /// <summary>
        /// 行业
        /// </summary>
        public string F_IndustryCategory { set; get; }
        /// <summary>
        /// 工作岗位
        /// </summary>
        public string F_Job { set; get; }

        /// <summary>
        /// 头像
        /// </summary>
        public string F_HeadIcon { set; get; }

        /// <summary>
        /// 微信头像地址
        /// </summary>
        public string F_HeadImgurl { set; get; }
        /// <summary>
        /// 性别
        /// </summary>
        public string F_Gender { set; get; }
        /// <summary>
        /// 租户编号
        /// </summary>
        public string F_TenantId { set; get; }

        public string token { set; get; } = Guid.NewGuid().ToString();

        public string refreshtoken { set; get; }

        public string F_EnabledMark { set; get; }

        /// <summary>
        /// 是否设置了密码
        /// </summary>
        public int F_IsPwdSetted { set; get; }


        public string F_DepartmentName { set; get; }


    }

    ///// <summary>
    ///// 登录成功返回用户信息
    ///// </summary>
    //public class UserLoginModel
    //{
    //    public int code { set; get; }

    //    public string msg { set; get; }

    //    public string token { set; get; }

    //    public UserInfoOutput userInfo { set; get; }

    //}
}
