using Kedo.Application.OnionFlowData.Dtos.input;
using Kedo.Application.OnionFlowData.Dtos.output;
using Kedo.Comm;
using System.Collections.Generic;

namespace Kedo.Application.OnionFlowData.Service
{
    public interface IOnionFlowDataService
    {
        /// <summary>
        /// 提交OnionFlow表单设计模板
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        string SubmitOnionFlowData(OnionFlowDataInput onionFlowDataInput);

        /// <summary>
        /// 编辑 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        string EditOnionFlowData(OnionFlowDataInputForEdit onionFlowDataInput);

        /// <summary>
        /// 复制 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        OnionFlowDataOutput SubmitOnionFlowDataForCopy(OnionFlowDataInputForCopy onionFlowDataInput);

        /// <summary>
        /// 发布 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        string PublishOnionFlowData(OnionFlowDataInputForPublishment onionFlowDataInput);

        /// <summary>
        /// 删除表单
        /// </summary>
        /// <param name="mOnionFlowDataInputForRemove">表单唯一编号 F_Id</param>
        /// <returns></returns>
        string RemoveOnionFlowData(OnionFlowDataInputForRemove mOnionFlowDataInputForRemove);

        /// <summary>
        /// OnionFlow回收站
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        List<OnionFlowDataOutput> OnionFlowRecycleBin(string userId, string QueryField);


        /// <summary>
        /// 获取用户创建 OnionFlowData列表 [分页查询]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        PageListModel QueryOnionFlowSchemeDataList(string userId, string QueryField, int pageIndex, int pageSize);

        /// <summary>
        /// 查询用户创建 数据表单列表----模糊查询
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="mCaption"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        PageListModel QueryMyOnionFlowSchemeDataListByTitle(string userId, string mCaption, int pageIndex, int pageSize);

        /// <summary>
        /// 查询热门OnionFlow列表
        /// </summary>
        /// <returns></returns>
        List<OnionFlowDataOutput> QueryHotOnionFlowSchemeDataList();

        /// <summary>
        /// OnionFlow 类别查询  智慧政务  智慧校园 智慧工厂  传入参数:IndustryCategory
        /// </summary>
        /// <param name="mOnionFlowIndustryCategory"></param>
        /// <returns></returns>
        List<OnionFlowIndustryCategoryOutput> QueryOnionFlowIndustryCategory(string mOnionFlowIndustryCategory);

        /// <summary>
        ///  获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string QueryOnionFlowSchemeDataByKey(string key);

        /// <summary>
        /// V2  获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="onionFlowId"></param>
        /// <param name="F_UserId"></param>
        /// <returns></returns>
        string QueryOnionFlowSchemeDataByIdAndUserId(string onionFlowId, string F_UserId);

        /// <summary>
        /// 通过类别 从市场中查询 OnionFlow   
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        PageListModel QueryOnionFlowSchemeDataListFromMarketByCategory(string mCategory, string QueryField, int pageIndex, int pageSize);

        /// <summary>
        ///通过标题 OnionFlow 市场中查询
        /// </summary>
        /// <param name="mTitle"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        PageListModel QueryOnionFlowSchemeDataListFromMarketByTitle(string mTitle, int pageIndex, int pageSize);

        /// <summary>
        ///查询用户查看历史记录--通过标题检索
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        PageListModel QueryUserViewOnionFlowHistoryDataListByTitle(string userId, string QueryField, int pageIndex, int pageSize);

        /// <summary>
        /// 查询OnionFlow历史版本
        /// </summary>
        /// <param name="OnionFlowId"></param>
        /// <returns></returns>
        List<OnionFlowHisVersionOutput> QueryOnionFlowHisVersion(string OnionFlowId);

        /// <summary>
        /// 设置主版本
        /// </summary>
        /// <param name="onionFlowMajorVersionInput"></param>
        /// <returns></returns>
        string SetOnionFlowMajorVersion(OnionFlowMajorVersionInput onionFlowMajorVersionInput);

        /// <summary>
        /// 查询图形智能体数据根据ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        List<GraphicAgentOutput> QueryGraphicAgentDataById(string Id);

        /// <summary>
        /// 获取流程数据ById
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
         OnionFlowDataOutput QueryOnionFlowDataById(string id);

    }
} 