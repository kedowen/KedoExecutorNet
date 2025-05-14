using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.output
{
    public class UserSimpleInfoOutput
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
        /// 是否设置了密码
        /// </summary>
        public int F_IsPwdSetted { set; get; }
    }
}
