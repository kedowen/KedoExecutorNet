using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using Kedo.Application.OnionFlowData.Dtos.input;
using Kedo.Application.OnionFlowData.Dtos.output;
using Kedo.Application.OnionFlowData.Service;
using Kedo.Comm;
using System.Collections.Generic;

namespace Kedo.Application
{
    public class OnionFlowDataServices : IDynamicApiController
    {
        private readonly IOnionFlowDataService _iOnionFlowDataService;
       // private readonly IRabbitMQService _rabbitMQ;
      //  private const string MessageQueueName = "MQ";

        public OnionFlowDataServices(IOnionFlowDataService onionFlowDataService)
        {
            _iOnionFlowDataService = onionFlowDataService;
        }

        /// <summary>
        /// 提交OnionFlow 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public string SubmitOnionFlowData([FromBody] OnionFlowDataInput onionFlowDataInput)
        {
            return _iOnionFlowDataService.SubmitOnionFlowData(onionFlowDataInput);
        }

        /// <summary>
        /// 编辑 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string EditOnionFlowData([FromBody] OnionFlowDataInputForEdit onionFlowDataInput)
        {
            return _iOnionFlowDataService.EditOnionFlowData(onionFlowDataInput);
        }

        /// <summary>
        /// 复制 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        [HttpPost]
        public OnionFlowDataOutput CopyOnionFlowData([FromBody] OnionFlowDataInputForCopy onionFlowDataInput)
        {
            return _iOnionFlowDataService.SubmitOnionFlowDataForCopy(onionFlowDataInput);
        }

        /// <summary>
        /// 发布 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string PublishOnionFlowData([FromBody] OnionFlowDataInputForPublishment onionFlowDataInput)
        {
            return _iOnionFlowDataService.PublishOnionFlowData(onionFlowDataInput);
        }

        /// <summary>
        /// 删除表单
        /// </summary>
        /// <param name="mOnionFlowDataInputForRemove">表单唯一编号 F_Id</param>
        /// <returns></returns>
        [HttpPost]
        public string RemoveOnionFlowData(OnionFlowDataInputForRemove mOnionFlowDataInputForRemove)
        {
            return _iOnionFlowDataService.RemoveOnionFlowData(mOnionFlowDataInputForRemove);
        }

        /// <summary>
        /// OnionFlow回收站
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [HttpGet]
        public List<OnionFlowDataOutput> OnionFlowRecycleBin([FromQuery] string userId, [FromQuery] string QueryField)
        {
            return _iOnionFlowDataService.OnionFlowRecycleBin(userId, QueryField);
        }

        /// <summary>
        /// 获取流程数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public OnionFlowDataOutput QueryOnionFlowDataById([FromQuery] string id)
        {
            return _iOnionFlowDataService.QueryOnionFlowDataById(id);
        }


        /// <summary>
        /// 获取用户创建 OnionFlowData列表 [分页查询]
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        [HttpGet]
        public PageListModel QueryOnionFlowSchemeDataList([FromQuery] string userId, [FromQuery] string QueryField, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            return _iOnionFlowDataService.QueryOnionFlowSchemeDataList(userId, QueryField, pageIndex, pageSize);
        }

        /// <summary>
        /// 查询用户创建 数据表单列表----模糊查询
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="userId"></param>
        /// <param name="mCaption"></param>
        /// <returns></returns>
        [HttpGet]
        public PageListModel QueryMyOnionFlowSchemeDataListByTitle([FromQuery] string userId, [FromQuery] string mCaption, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            return _iOnionFlowDataService.QueryMyOnionFlowSchemeDataListByTitle(userId, mCaption, pageIndex, pageSize);
        }

        /// <summary>
        /// 查询热门OnionFlow列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public List<OnionFlowDataOutput> QueryHotOnionFlowSchemeDataList()
        {
            return _iOnionFlowDataService.QueryHotOnionFlowSchemeDataList();
        }

        /// <summary>
        /// OnionFlow 类别查询  智慧政务  智慧校园 智慧工厂  传入参数:IndustryCategory
        /// </summary>
        /// <param name="mOnionFlowIndustryCategory"></param>
        /// <returns></returns>
        [HttpGet]
        public List<OnionFlowIndustryCategoryOutput> QueryOnionFlowIndustryCategory([FromQuery] string mOnionFlowIndustryCategory)
        {
            return _iOnionFlowDataService.QueryOnionFlowIndustryCategory(mOnionFlowIndustryCategory);
        }

        /// <summary>
        ///  获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="onionFlowId">OnionFlow 文件 ID</param>
        /// <param name="F_UserId">用户ID </param>
        /// <returns></returns>
        [HttpGet]
        public string QueryOnionFlowSchemeDataByKey([FromQuery] string onionFlowId, [FromQuery] string F_UserId)
        {
            return _iOnionFlowDataService.QueryOnionFlowSchemeDataByIdAndUserId(onionFlowId, F_UserId);
        }

        /// <summary>
        /// 通过类别 从市场中查询 OnionFlow   
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public PageListModel QueryOnionFlowSchemeDataListFromMarketByCategory([FromQuery] string mCategory, [FromQuery] string QueryField, [FromQuery] int pageIndex=1, [FromQuery] int pageSize=10)
        {
            return _iOnionFlowDataService.QueryOnionFlowSchemeDataListFromMarketByCategory(mCategory, QueryField, pageIndex, pageSize);
        }

        /// <summary>
        ///通过标题 OnionFlow 市场中查询
        /// </summary>
        /// <param name="mTitle"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public PageListModel QueryOnionFlowSchemeDataListFromMarketByTitle([FromQuery] string mTitle, [FromQuery] int pageIndex=1, [FromQuery] int pageSize=10)
        {
            return _iOnionFlowDataService.QueryOnionFlowSchemeDataListFromMarketByTitle(mTitle, pageIndex, pageSize);
        }

        /// <summary>
        ///查询用户查看历史记录--通过标题检索
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public PageListModel QueryUserViewOnionFlowHistoryDataList([FromQuery] string userId, [FromQuery] string QueryField, [FromQuery] int pageIndex=1, [FromQuery] int pageSize=10)
        {
            return _iOnionFlowDataService.QueryUserViewOnionFlowHistoryDataListByTitle(userId, QueryField, pageIndex, pageSize);
        }

        /// <summary>
        /// 查询OnionFlow历史版本
        /// </summary>
        /// <param name="OnionFlowId"></param>
        /// <returns></returns>
        [HttpGet]
        public List<OnionFlowHisVersionOutput> QueryOnionFlowHisVersion([FromQuery] string OnionFlowId)
        {
            return _iOnionFlowDataService.QueryOnionFlowHisVersion(OnionFlowId);
        }

        /// <summary>
        /// 查看OnionFlow历史版本  预览
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string QueryOnionFlowHisVerDataByKey([FromQuery] string key)
        {
            return _iOnionFlowDataService.QueryOnionFlowSchemeDataByKey(key);
        }

        /// <summary>
        /// 设置主版本
        /// </summary>
        /// <param name="onionFlowMajorVersionInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string SetOnionFlowMajorVersion([FromBody] OnionFlowMajorVersionInput onionFlowMajorVersionInput)
        {
            return _iOnionFlowDataService.SetOnionFlowMajorVersion(onionFlowMajorVersionInput);
        }
        
        //#region 创建图形化智能体 
        ///// <summary>
        ///// 创建图形化智能体
        ///// </summary>
        ///// <param name="createGraphicAgentInput"></param>
        ///// <returns></returns>
        //[HttpPost]
        //public string CreateGraphicAgent([FromBody] CreateGraphicAgentInput createGraphicAgentInput)
        //{
        //    return _iOnionFlowDataService.CreateGraphicAgent(createGraphicAgentInput);
        //}

        ///// <summary>
        ///// 修改智能体流程
        ///// </summary>
        ///// <param name="modifyGraphicAgentInput"></param>
        ///// <returns></returns>
        //[HttpPost]
        //public string ModifyGraphicAgent([FromBody] ModifyGraphicAgentInput modifyGraphicAgentInput)
        //{
        //    return _iOnionFlowDataService.ModifyGraphicAgent(modifyGraphicAgentInput);
        //}

        ///// <summary>
        ///// 查询智能体图形数据根据ID
        ///// </summary>
        ///// <param name="Id">图形ID</param>
        ///// <returns></returns>
        //[HttpGet]
        //public GraphicAgentOutput QueryGraphicAgentDataById([FromQuery] string Id)
        //{
        //    return _iOnionFlowDataService.QueryGraphicAgentDataById(Id).FirstOrDefault();
        //}
        //#endregion
    }
} 