using Furion.DatabaseAccessor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.BaseIDatatem.Service
{
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        /// 获取基础资料信息
        /// </summary>
        /// <param name="F_ItemCode"></param>
        /// <returns></returns>
        [SqlExecute("select a.F_ItemDetailId,a.F_ItemId,a.F_ItemName,a.F_ItemValue,a.F_ItemCode from bas_dataitemdetail a  LEFT JOIN bas_dataitem b on a.F_ItemId=b.F_ItemId where b.F_ItemCode =@F_ItemCode")]
        DataTable GetDataItem(string F_ItemCode);


        /// <summary>
        /// 获取自定义表单数据
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [SqlExecute("SELECT  t2.F_Scheme FROM CU_Form_SchemeInfo t1 INNER JOIN CU_Form_Scheme t2 ON t1.F_SchemeId = t2.f_id WHERE t1.F_Id =@Id")]
        DataTable GetFormSchemeData(string Id);
    }
}
