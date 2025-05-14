using Furion.DatabaseAccessor;
using Kedo.Application.DataSource.Dtos.output;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.DataSource.Services
{
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        /// 获取基础资料信息
        /// </summary>
        /// <param name="F_ItemCode">DataSourceType</param>
        /// <returns></returns>
        [SqlExecute("select a.F_ItemDetailId,a.F_ItemId,a.F_ItemName,a.F_ItemValue,a.F_ItemCode from bas_dataitemdetail a  LEFT JOIN bas_dataitem b on a.F_ItemId=b.F_ItemId where b.F_ItemCode =@F_ItemCode and a.F_DeleteMark='0'")]
        DataTable QueryDataSourceType(string F_ItemCode);


        /// <summary>
        /// 获取基础资料信息  F_ItemType='1'  代表数据库 
        /// </summary>
        /// <param name="F_ItemCode">DataSourceType</param>
        /// <returns></returns>
        [SqlExecute("select a.F_ItemDetailId,a.F_ItemId,a.F_ItemName,a.F_ItemValue,a.F_ItemCode from bas_dataitemdetail a  LEFT JOIN bas_dataitem b on a.F_ItemId=b.F_ItemId where b.F_ItemCode =@F_ItemCode and a.F_DeleteMark='0'")]
        DataTable QueryDBDataSourceType(string F_ItemCode);

        /// <summary>
        /// 根据数据源类型 查询数据源列表
        /// </summary>
        /// <param name="F_DataTypeId"></param>
        /// <returns></returns>
        [SqlExecute(" select F_Id,F_Caption,F_Remark,F_DataSourceUserId,F_Pwd,F_ConnectionString from bas_datasource where F_DataSourceTypeId=@F_DataTypeId and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<DataSourceInfoOutput> QueryDataSourceInfoListByDataType(string F_DataTypeId);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [SqlExecute(" select F_Id,F_DataSourceTypeId,F_Caption,F_Ip,F_Port,F_DBName,F_DataSourceUserId,F_Pwd,F_Remark,F_ConnectionString from bas_datasource where F_Id=@F_Id and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<DataSourceDetailInfoOutput> QueryDataSourceDetailInfo(string F_Id);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [SqlExecute("select F_Id,F_Caption,F_Remark,F_ConnectionString from bas_datasource where F_Id=@F_Id and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<DataSourceInfoOutput> QueryDataSourceInfoById(string F_Id);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [SqlExecute("select F_ItemName from bas_dataitemdetail where F_ItemDetailId =@F_Id and F_DeleteMark='0'")]
        DataTable QueryDataSourceTypeById(string F_Id);
        

    }
}
