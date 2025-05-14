using Newtonsoft.Json.Linq;
using Kedo.Application.DataSource.Dtos.input;
using Kedo.Application.DataSource.Dtos.output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.DataSource.Services
{
    public interface IDataSourceService
    {
        /// <summary>
        /// 数据源类别 类别
        /// </summary>
        /// <param name="mDataSourceType"></param>
        /// <returns></returns>
        List<DataSourceTypeOutput> QueryDataSourceType(string mDataSourceType);


        /// <summary>
        /// 数据源类别 类别
        /// </summary>
        /// <param name="mDataSourceType"></param>
        /// <returns></returns>
        List<DataSourceTypeOutput> QueryDBDataSourceType(string mDataSourceType);

        /// <summary>
        /// 创建数据源
        /// </summary>
        /// <param name="createDataSourceInput"></param>
        /// <returns></returns>
        string CreateDataSource(CreateDataSourceInput createDataSourceInput);

        /// <summary>
        /// 修改数据源信息
        /// </summary>
        /// <param name="modifyDataSourceInput"></param>
        /// <returns></returns>
        string ModifyDataSource(ModifyDataSourceInput modifyDataSourceInput);

        /// <summary>
        /// 删除数据源信息
        /// </summary>
        /// <param name="removeDataSourceInput"></param>
        /// <returns></returns>
        string RemoveDataSource(RemoveDataSourceInput removeDataSourceInput);


        /// <summary>
        /// 根据数据源类型 查询数据源列表
        /// </summary>
        /// <param name="F_DataTypeId"></param>
        /// <returns></returns>
         List<DataSourceInfoOutput> QueryDataSourceInfoListByDataType(string F_DataTypeId);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        DataSourceDetailInfoOutput DataSourceDetailInfoById(string F_Id);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        DataSourceInfoOutput DataSourceInfoById(string F_Id);

        /// <summary>
        /// 通过数据库查询执行结果
        /// </summary>
        /// <param name="executeSqlQueryInput">数据源类型</param>
        /// <returns></returns>
        // JArray QueryDBDataBySQL(ExecuteSqlQueryInput executeSqlQueryInput);
        DataSourceQueryModelOutput QueryDBDataBySQL(ExecuteSqlQueryInput executeSqlQueryInput);
        /// <summary>
        /// 数据源连接测试
        /// </summary>
        /// <param name="connectionInput"></param>
        /// <returns></returns>
        bool ConnectionCheck(CheckDataSourceConnectionInput connectionInput);
    }
}
