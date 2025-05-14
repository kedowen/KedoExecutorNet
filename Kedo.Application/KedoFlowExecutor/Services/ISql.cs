using Furion.DatabaseAccessor;
using Kedo.Application.DataSource.Dtos.output;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.OnionFlowExecutor.Services
{
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        ///查询流程ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_OnionFlowSchemeData FROM onionbitbi.onionflow_schemedata where F_Id=@Id ")]
        DataTable QueryAgentGraphicData(string Id);

        #region
        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [SqlExecute("select F_Id,F_Caption,F_Remark,F_ConnectionString,F_DataSourceTypeId from bas_datasource where F_Id=@F_Id and F_EnabledMark='1' and F_DeleteMark='0'")]
        List<DataSourceInfoOutput> QueryDataSourceInfoById(string F_Id);

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [SqlExecute("select F_ItemName from bas_dataitemdetail where F_ItemDetailId =@F_Id and F_DeleteMark='0'")]
        DataTable QueryDataSourceTypeById(string F_Id);


        #endregion
    }
}
