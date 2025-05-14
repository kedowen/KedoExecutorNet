using Furion.DatabaseAccessor;
using Kedo.Application.User.Dtos.output;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Services
{
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [SqlExecute("select F_UserId,F_Account,F_UserName,F_Nickname,F_Mobile,F_CompanyName,F_DepartmentName,F_Email,F_OpenId,F_IndustryCategory,F_Job,F_HeadIcon,F_Gender,F_HeadImgurl,F_TenantId,F_EnabledMark,F_DeleteMark,F_IsPwdSetted from bas_user where F_Account=@account and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<UserInfoOutput> QueryUserInfo(string account);

        [SqlExecute("select F_UserId,F_Account,F_UserName,F_Nickname,F_Mobile,F_CompanyName,F_DepartmentName,F_Email,F_OpenId,F_IndustryCategory,F_Job,F_HeadIcon,F_Gender,F_HeadImgurl,F_TenantId,F_EnabledMark,F_DeleteMark,F_IsPwdSetted from bas_user where F_ThirdPartyUserId=@mF_ThirdPartyUserId and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<UserInfoOutput> QueryUserInfoByThirdPartyUserId(string mF_ThirdPartyUserId);
        
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [SqlExecute("select F_UserId,F_Account,F_UserName,F_UserName as F_RealName,F_Nickname,F_Mobile,F_CompanyName,F_DepartmentName,F_Email,F_IsPwdSetted from bas_user where F_Account=@account and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<UserSimpleInfoOutput> QueryUserSimpleInfo(string account);
        
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        [SqlExecute("select F_UserId,F_Account,F_UserName,F_Nickname,F_Mobile,F_CompanyName,F_DepartmentName,F_Email,F_OpenId,F_IndustryCategory,F_Job,F_HeadIcon,F_Gender,F_HeadImgurl,F_TenantId,F_EnabledMark,F_DeleteMark,F_IsPwdSetted from bas_user where F_OpenId=@openId  and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<UserInfoOutput> QueryUserInfoByOpenId(string openId);

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [SqlExecute("select F_UserId,F_Account,F_UserName,F_UserName as F_RealName,F_Nickname,F_Mobile,F_Mobile as F_Telephone,F_CompanyName,F_DepartmentName,F_Email,F_OpenId,F_IndustryCategory,F_Job,F_HeadIcon,F_Gender,F_HeadImgurl,F_TenantId,F_EnabledMark,F_DeleteMark,F_IsPwdSetted from bas_user where F_UserId=@userId")]
        List<UserInfoOutput> QueryUserInfoByUserId(string userId);


      
        /// <summary>不能
        /// 账号查询用户密码和加密Key
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [SqlExecute("select F_Password,F_Secretkey,F_EnabledMark from bas_user where F_Account=@account and F_DeleteMark='0' and F_EnabledMark='1'")]
        DataTable QueryUserPassword(string account);



        [SqlExecute("select F_UserId,F_Account,F_UserName from bas_user where F_AppId=@appId and F_Secret=@secret and F_DeleteMark='0' and F_EnabledMark='1'")]
        DataTable QueryEnterpriseUser(string appId,string secret);


        /// <summary>
        /// 获取创建此AppId 和 SecretKey 的企业用户 ID  
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_CreateUserId as UserId FROM gpt_project  where F_AppId=@appId and F_SecretKey =@secret")]
        DataTable QueryEnterpriseUserId(string appId, string secret);
        




        [SqlExecute("select F_HeadImgData from bas_user_headicon where F_Id=@mId")]
        DataTable QueryUserHeadIcon(string mId);

        /// <summary>
        /// 查询验证码
        /// </summary>
        /// <param name="mNum"></param>
        /// <param name="timeBegin"></param>
        /// <param name="timeEnd"></param>
        /// <returns></returns>
        [SqlExecute("select F_SmsCode from his_nodifycode where F_FPhoneNum=@mNum and (F_CreateDate between @timeBegin and @timeEnd)  order by  F_CreateDate desc")]
        DataTable QuerySmsCodeMins(string mNum,string timeBegin,string timeEnd);

        /// <summary>
        /// 查询电话号码
        /// </summary>
        /// <param name="F_Account"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_Account FROM onionbitbi.bas_user where F_Account=@F_Account")]
        DataTable QueryPhoneNo(string F_Account);




    }
}
