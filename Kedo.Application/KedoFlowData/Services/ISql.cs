using Furion.DatabaseAccessor;
using Kedo.Application.OnionFlowData.Dtos.input;
using Kedo.Application.OnionFlowData.Dtos.output;
using Kedo.Comm;
using System.Collections.Generic;
using System.Data;

namespace Kedo.Application.OnionFlowData.Service
{
    public interface ISql : ISqlDispatchProxy
    {
        /// <summary>
        /// 获取用户创建 OnionFlowData列表 [分页查询]
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <param name="QueryField"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0 LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryOnionFlowSchemeDataList(string F_CreateUserId, int rowIndex, int pageSize);

        /// <summary>
        /// 获取用户创建 OnionFlowData列表 [分页查询] - 带标题查询
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <param name="QueryField"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_Caption LIKE CONCAT('%', @QueryField, '%') AND F_DeleteMark = 0 LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryOnionFlowSchemeDataListWithTitle(string F_CreateUserId, string QueryField, int rowIndex, int pageSize);

        /// <summary>
        /// 获取用户创建 OnionFlowData总数
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <returns></returns>
        [SqlExecute("SELECT COUNT(1) FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0")]
        int QueryOnionFlowSchemeDataListTotalCount(string F_CreateUserId);

        /// <summary>
        /// 获取用户创建 OnionFlowData总数 - 带标题查询
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [SqlExecute("SELECT COUNT(1) FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_Caption LIKE CONCAT('%', @QueryField, '%') AND F_DeleteMark = 0")]
        int QueryOnionFlowSchemeDataListWithTitleTotalCount(string F_CreateUserId, string QueryField);

        /// <summary>
        /// OnionFlow回收站
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 1")]
        List<OnionFlowDataOutput> OnionFlowRecycleBin(string F_CreateUserId);

        /// <summary>
        /// OnionFlow回收站 - 带标题查询
        /// </summary>
        /// <param name="F_CreateUserId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_CreateUserId = @F_CreateUserId AND F_Caption LIKE CONCAT('%', @QueryField, '%') AND F_DeleteMark = 1")]
        List<OnionFlowDataOutput> OnionFlowRecycleBinWithTitle(string F_CreateUserId, string QueryField);

        /// <summary>
        /// 查询热门OnionFlow列表
        /// </summary>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_DeleteMark = 0 ORDER BY F_CreateDate DESC LIMIT 10")]
        List<OnionFlowDataOutput> QueryHotOnionFlowSchemeDataList();

        /// <summary>
        /// OnionFlow 类别查询
        /// </summary>
        /// <returns></returns>
        [SqlExecute("SELECT DISTINCT F_IndustryCategory as F_Id, F_IndustryCategory, '' as F_Description FROM onionflow_schemedata WHERE F_DeleteMark = 0")]
        List<OnionFlowIndustryCategoryOutput> QueryOnionFlowIndustryCategory();

        /// <summary>
        /// OnionFlow 类别查询 - 带条件
        /// </summary>
        /// <param name="mOnionFlowIndustryCategory"></param>
        /// <returns></returns>
        [SqlExecute("SELECT DISTINCT F_IndustryCategory as F_Id, F_IndustryCategory, '' as F_Description FROM onionflow_schemedata WHERE F_DeleteMark = 0 AND F_IndustryCategory LIKE CONCAT('%', @mOnionFlowIndustryCategory, '%')")]
        List<OnionFlowIndustryCategoryOutput> QueryOnionFlowIndustryCategoryWithFilter(string mOnionFlowIndustryCategory);

        /// <summary>
        /// 获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_OnionFlowSchemeData FROM onionflow_schemedata_hisversion WHERE F_Id = @key")]
        string QueryOnionFlowSchemeDataByKey(string key);

        /// <summary>
        /// V2 获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="onionFlowId"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_OnionFlowSchemeData FROM onionflow_schemedata WHERE F_Id = @onionFlowId")]
        string QueryOnionFlowSchemeDataById(string onionFlowId);

        /// <summary>
        /// 通过类别从市场中查询OnionFlow - 分页
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_IndustryCategory = @mCategory AND F_DeleteMark = 0 ORDER BY F_CreateDate DESC LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryOnionFlowSchemeDataListFromMarketByCategory(string mCategory, int rowIndex, int pageSize);

        /// <summary>
        /// 通过类别从市场中查询OnionFlow - 带标题查询
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="QueryField"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_IndustryCategory = @mCategory AND F_Caption LIKE CONCAT('%', @QueryField, '%') AND F_DeleteMark = 0 ORDER BY F_CreateDate DESC LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryOnionFlowSchemeDataListFromMarketByCategoryWithTitle(string mCategory, string QueryField, int rowIndex, int pageSize);

        /// <summary>
        /// 通过类别从市场中查询OnionFlow - 总数
        /// </summary>
        /// <param name="mCategory"></param>
        /// <returns></returns>
        [SqlExecute("SELECT COUNT(1) FROM onionflow_schemedata WHERE F_IndustryCategory = @mCategory AND F_DeleteMark = 0")]
        int QueryOnionFlowSchemeDataListFromMarketByCategoryTotalCount(string mCategory);

        /// <summary>
        /// 通过类别从市场中查询OnionFlow - 带标题查询 - 总数
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [SqlExecute("SELECT COUNT(1) FROM onionflow_schemedata WHERE F_IndustryCategory = @mCategory AND F_Caption LIKE CONCAT('%', @QueryField, '%') AND F_DeleteMark = 0")]
        int QueryOnionFlowSchemeDataListFromMarketByCategoryWithTitleTotalCount(string mCategory, string QueryField);

        /// <summary>
        /// 通过标题从市场中查询OnionFlow - 分页
        /// </summary>
        /// <param name="mTitle"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_Caption LIKE CONCAT('%', @mTitle, '%') AND F_DeleteMark = 0 ORDER BY F_CreateDate DESC LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryOnionFlowSchemeDataListFromMarketByTitle(string mTitle, int rowIndex, int pageSize);

        /// <summary>
        /// 通过标题从市场中查询OnionFlow - 总数
        /// </summary>
        /// <param name="mTitle"></param>
        /// <returns></returns>
        [SqlExecute("SELECT COUNT(1) FROM onionflow_schemedata WHERE F_Caption LIKE CONCAT('%', @mTitle, '%') AND F_DeleteMark = 0")]
        int QueryOnionFlowSchemeDataListFromMarketByTitleTotalCount(string mTitle);

        /// <summary>
        /// 查询用户查看历史记录 - 带标题查询 - 分页
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="rowIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [SqlExecute(@"SELECT s.* FROM onionflow_schemedata s 
                    RIGHT JOIN onionflow_user_view_history h ON s.F_Id = h.F_OnionFlowId 
                    WHERE h.F_CreateUserId = @userId AND s.F_Caption LIKE CONCAT('%', @QueryField, '%') AND s.F_DeleteMark = 0 
                    ORDER BY h.F_ViewDate DESC LIMIT @rowIndex, @pageSize")]
        List<OnionFlowDataOutput> QueryUserViewOnionFlowHistoryDataListByTitle(string userId, string QueryField, int rowIndex, int pageSize);

        /// <summary>
        /// 查询用户查看历史记录 - 带标题查询 - 总数
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [SqlExecute(@"SELECT COUNT(1) FROM onionflow_schemedata s 
                    RIGHT JOIN onionflow_user_view_history h ON s.F_Id = h.F_OnionFlowId 
                    WHERE h.F_CreateUserId = @userId AND s.F_Caption LIKE CONCAT('%', @QueryField, '%') AND s.F_DeleteMark = 0")]
        int QueryUserViewOnionFlowHistoryDataListByTitleTotalCount(string userId, string QueryField);

        /// <summary>
        /// 查询OnionFlow历史版本
        /// </summary>
        /// <param name="OnionFlowId"></param>
        /// <returns></returns>
        [SqlExecute(@"SELECT * FROM onionflow_schemedata_hisversion WHERE F_OnionFlowId = @OnionFlowId AND F_DeleteMark = 0 ORDER BY F_CreateDate DESC")]
        List<OnionFlowHisVersionOutput> QueryOnionFlowHisVersion(string OnionFlowId);

        /// <summary>
        /// 查询图形智能体数据根据ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_Id, F_Caption, F_AgentData FROM onionflow_prompt_agent_graphic WHERE F_Id = @Id")]
        List<GraphicAgentOutput> QueryGraphicAgentDataById(string Id);

        /// <summary>
        /// 通过ID查询OnionFlowData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SqlExecute("SELECT * FROM onionflow_schemedata WHERE F_Id = @id")]
        OnionFlowDataOutput QueryOnionFlowDataById(string id);
    }
} 