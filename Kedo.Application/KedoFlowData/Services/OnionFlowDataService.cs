using Furion.DatabaseAccessor;
using Furion.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Kedo.Application.OnionFlowData.Dtos.input;
using Kedo.Application.OnionFlowData.Dtos.output;
using Kedo.Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using Furion.FriendlyException;
using Kedo.Application.OnionFlowData.Utils;

namespace Kedo.Application.OnionFlowData.Service
{
    public class OnionFlowDataService : IOnionFlowDataService, ITransient
    {
        private readonly ISqlRepository _sqlRepository;
        private readonly ILogger<OnionFlowDataService> _logger;
        private readonly IDistributedCache _redis;
        private readonly RabbitMQHelper _rabbitMQ;
        private readonly ISql _sql;
        private readonly string MessageQueueName;

        public OnionFlowDataService(ISqlRepository sqlRepository, ILogger<OnionFlowDataService> logger, ISql sql, IDistributedCache redis, RabbitMQHelper rabbitMQ, [FromServices] IConfiguration configuration)
        {
            _sqlRepository = sqlRepository;
            _logger = logger;
            _sql = sql;
            _redis = redis;
            _rabbitMQ = rabbitMQ;
            MessageQueueName = configuration["RabbitMQConfigurations:BIData"];
        }

        /// <summary>
        /// 提交OnionFlow表单设计模板
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        public string SubmitOnionFlowData(OnionFlowDataInput onionFlowDataInput)
        {
           
            //获取开始节点的参数  调用接口   或者到 会话界面的时候 都需要的  转Base64 位存储
            //  string refStartNodePara = FlowParaHelper.ExtractStartNodeOutputs(onionFlowDataInput.F_OnionFlowSchemeData).ToString();

            string uniqueId = Guid.NewGuid().ToString();
            string insertSql = @"INSERT INTO onionflow_schemedata
                (F_Id, F_Caption, F_CreateUserId, F_IndustryCategory, F_Description, F_OnionFlowSchemeData,F_Type,F_TeamId, F_TeamOnionFlowFileGroup, F_DeleteMark, F_EnabledMark, F_CreateDate)
                VALUES
                ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', 0, 1, '{9}')";

            insertSql = string.Format(insertSql,
                uniqueId,
                onionFlowDataInput.F_Caption,
                onionFlowDataInput.F_CreateUserId,
                onionFlowDataInput.F_IndustryCategory,
                onionFlowDataInput.F_Description,
                onionFlowDataInput.F_OnionFlowSchemeData,
                onionFlowDataInput.F_Type,
                onionFlowDataInput.F_TeamId,
                onionFlowDataInput.F_TeamOnionFlowFileGroup,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            if (_sqlRepository.SqlNonQuery(insertSql) == 0)
                throw Oops.Bah("数据异常").StatusCode(201);

            return uniqueId;
        }

        /// <summary>
        /// 编辑 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        public string EditOnionFlowData(OnionFlowDataInputForEdit onionFlowDataInput)
        {
            //获取开始节点的参数  调用接口   或者到 会话界面的时候 都需要的  转Base64 位存储
            string refStartNodePara = FlowParaHelper.ExtractStartNodeOutputs(onionFlowDataInput.F_OnionFlowSchemeData).ToString();
            string updateSql = @"UPDATE onionflow_schemedata SET 
                F_Caption = '{0}',
                F_Description = '{1}',
                F_OnionFlowSchemeData = '{2}',
                F_Type = '{3}',
                F_ModifyUserId = '{4}',
                F_FlowPara='{5}',
                F_ImgUrl='{6}'
                WHERE F_Id = '{7}'";

            updateSql = string.Format(updateSql,
                onionFlowDataInput.F_Caption,
                onionFlowDataInput.F_Description,
                onionFlowDataInput.F_OnionFlowSchemeData,
                onionFlowDataInput.F_Type,
                onionFlowDataInput.F_ModifyUserId,
                refStartNodePara,
                onionFlowDataInput.F_ImgUrl,
                onionFlowDataInput.F_Id);

            if (_sqlRepository.SqlNonQuery(updateSql) == 0)
                throw Oops.Bah("数据异常").StatusCode(201);

            return onionFlowDataInput.F_Id;
        }

        /// <summary>
        /// 复制 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        public OnionFlowDataOutput SubmitOnionFlowDataForCopy(OnionFlowDataInputForCopy onionFlowDataInput)
        {
            // 先获取要复制的数据
            string selectSql = @"SELECT F_Caption, F_IndustryCategory, F_Description, F_OnionFlowSchemeData, F_OnionFlowFileData, F_Type, F_OnionFlowSize,F_FlowPara,F_ImgUrl
                              FROM onionflow_schemedata 
                              WHERE F_Id = @F_Id";
            var sourceData = _sqlRepository.SqlQuery<OnionFlowDataOutput>(selectSql, new { F_Id = onionFlowDataInput.F_Id }).FirstOrDefault();

            // 没有找到源数据
            if (sourceData == null)
                return null;

            // 创建新记录
            string newId = Guid.NewGuid().ToString();
            string insertSql = @"INSERT INTO onionflow_schemedata
                (F_Id, F_Caption, F_CreateUserId, F_IndustryCategory, F_Description, F_OnionFlowSchemeData, F_OnionFlowFileData, F_Type, F_OnionFlowSize, F_TeamId, F_TeamOnionFlowFileGroup, F_DeleteMark, F_EnabledMark, F_CreateDate)
                VALUES
                (@F_Id, @F_Caption, @F_CreateUserId, @F_IndustryCategory, @F_Description, @F_OnionFlowSchemeData, @F_OnionFlowFileData, @F_Type, @F_OnionFlowSize, @F_TeamId, @F_TeamOnionFlowFileGroup, 0, 1, @F_CreateDate,@F_FlowPara)";

            _sqlRepository.SqlNonQuery(insertSql, new
            {
                F_Id = newId,
                F_Caption = $"Copy of {sourceData.F_Caption}",
                F_CreateUserId = onionFlowDataInput.F_CreateUserId,
                F_IndustryCategory = sourceData.F_IndustryCategory,
                F_Description = sourceData.F_Description,
                F_OnionFlowSchemeData = sourceData.F_OnionFlowSchemeData,
                F_OnionFlowFileData = sourceData.F_OnionFlowFileData,
                F_Type = sourceData.F_Type,
                F_OnionFlowSize = sourceData.F_OnionFlowSize,
                F_TeamId = onionFlowDataInput.F_TeamId,
                F_TeamOnionFlowFileGroup = onionFlowDataInput.F_TeamOnionFlowFileGroup,
                F_CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                F_FlowPara = sourceData.F_FlowPara,
            });

            // 返回新创建的数据
            return QueryOnionFlowDataById(newId);
        }

        /// <summary>
        /// 发布 OnionFlow Json  
        /// </summary>
        /// <param name="onionFlowDataInput"></param>
        /// <returns></returns>
        public string PublishOnionFlowData(OnionFlowDataInputForPublishment onionFlowDataInput)
        {
            // 存储历史版本
            string historyId = Guid.NewGuid().ToString();
            string insertHistorySql = @"INSERT INTO onionflow_schemedata_hisversion
                (F_Id, F_OnionFlowId, F_Caption, F_OnionFlowSchemeData, F_IsMasterVersion, F_DeleteMark, F_EnabledMark, F_CreateDate, F_CreateUserId)
                VALUES
                (@F_Id, @F_OnionFlowId, @F_Caption, @F_OnionFlowSchemeData, '0', 0, 1, @F_CreateDate, @F_CreateUserId)";

            // 查询现有数据获取标题
            string captionSql = "SELECT F_Caption FROM onionflow_schemedata WHERE F_Id = @F_Id";
            var caption = _sqlRepository.SqlQuery<string>(captionSql, new { F_Id = onionFlowDataInput.F_Id }).FirstOrDefault();

            _sqlRepository.SqlNonQuery(insertHistorySql, new
            {
                F_Id = historyId,
                F_OnionFlowId = onionFlowDataInput.F_Id,
                F_Caption = caption ?? "Version " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                F_OnionFlowSchemeData = onionFlowDataInput.F_OnionFlowSchemeData,
                F_CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                F_CreateUserId = onionFlowDataInput.F_CreateUserId
            });

            // 更新当前数据
            string updateSql = @"UPDATE onionflow_schemedata SET 
                F_OnionFlowSchemeData = @F_OnionFlowSchemeData,
                F_OnionFlowFileData = @F_OnionFlowFileData
                WHERE F_Id = @F_Id";

            _sqlRepository.SqlNonQuery(updateSql, new
            {
                F_Id = onionFlowDataInput.F_Id,
                F_OnionFlowSchemeData = onionFlowDataInput.F_OnionFlowSchemeData,
                F_OnionFlowFileData = onionFlowDataInput.F_OnionFlowFileData
            });

            return onionFlowDataInput.F_Id;
        }

        /// <summary>
        /// 删除表单
        /// </summary>
        /// <param name="mOnionFlowDataInputForRemove">表单唯一编号 F_Id</param>
        /// <returns></returns>
        public string RemoveOnionFlowData(OnionFlowDataInputForRemove mOnionFlowDataInputForRemove)
        {
            string updateSql = @"UPDATE onionflow_schemedata SET 
                F_DeleteMark = 1,
                F_DeleteUserId = @F_UserId,
                F_DeleteDate = @F_DeleteDate
                WHERE F_Id = @F_Id";

            _sqlRepository.SqlNonQuery(updateSql, new
            {
                F_Id = mOnionFlowDataInputForRemove.F_Id,
                F_UserId = mOnionFlowDataInputForRemove.F_UserId,
                F_DeleteDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return mOnionFlowDataInputForRemove.F_Id;
        }

        /// <summary>
        /// OnionFlow回收站
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <returns></returns>
        public List<OnionFlowDataOutput> OnionFlowRecycleBin(string userId, string QueryField)
        {
            string sql = @"SELECT * FROM onionflow_schemedata 
                          WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 1";

            if (!string.IsNullOrEmpty(QueryField))
            {
                sql += " AND F_Caption LIKE @QueryField";
                QueryField = $"%{QueryField}%";
            }

            return _sqlRepository.SqlQuery<OnionFlowDataOutput>(sql, new { F_CreateUserId = userId, QueryField });
        }

       

        /// <summary>
        /// 获取用户创建 OnionFlowData列表 [分页查询]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PageListModel QueryOnionFlowSchemeDataList(string userId, string QueryField, int pageIndex, int pageSize)
        {
            string countSql = @"SELECT COUNT(1) FROM onionflow_schemedata 
                              WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0";

            string dataSql = @"SELECT * FROM onionflow_schemedata 
                            WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0";

            if (!string.IsNullOrEmpty(QueryField))
            {
                countSql += " AND F_Caption LIKE @QueryField";
                dataSql += " AND F_Caption LIKE @QueryField";
                QueryField = $"%{QueryField}%";
            }

            dataSql += " ORDER BY F_CreateDate DESC LIMIT @Skip, @Take";

            int totalCount = _sqlRepository.SqlQuery<int>(countSql, new { F_CreateUserId = userId, QueryField }).FirstOrDefault();
            var data = _sqlRepository.SqlQuery<OnionFlowDataOutput>(dataSql, new
            {
                F_CreateUserId = userId,
                QueryField,
                Skip = (pageIndex - 1) * pageSize,
                Take = pageSize
            });

            return new PageListModel
            {

                TotalCount = totalCount,
                Items = data
            };
        }

        /// <summary>
        /// 查询用户创建 数据表单列表----模糊查询
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="mCaption"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PageListModel QueryMyOnionFlowSchemeDataListByTitle(string userId, string mCaption, int pageIndex, int pageSize)
        {
            string countSql = @"SELECT COUNT(1) FROM onionflow_schemedata 
                              WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0";

            string dataSql = @"SELECT * FROM onionflow_schemedata 
                            WHERE F_CreateUserId = @F_CreateUserId AND F_DeleteMark = 0";

            if (!string.IsNullOrEmpty(mCaption))
            {
                countSql += " AND F_Caption LIKE @mCaption";
                dataSql += " AND F_Caption LIKE @mCaption";
                mCaption = $"%{mCaption}%";
            }

            dataSql += " ORDER BY F_CreateDate DESC LIMIT @Skip, @Take";

            int totalCount = _sqlRepository.SqlQuery<int>(countSql, new { F_CreateUserId = userId, mCaption }).FirstOrDefault();
            var data = _sqlRepository.SqlQuery<OnionFlowDataOutput>(dataSql, new
            {
                F_CreateUserId = userId,
                mCaption,
                Skip = (pageIndex - 1) * pageSize,
                Take = pageSize
            });

            return new PageListModel
            {

                TotalCount = totalCount,
                Items = data
            };
        }

        /// <summary>
        /// 查询热门OnionFlow列表
        /// </summary>
        /// <returns></returns>
        public List<OnionFlowDataOutput> QueryHotOnionFlowSchemeDataList()
        {
            // 这里可以根据业务需求定义热门的规则，例如访问量最高或最近更新的
            string sql = @"SELECT * FROM onionflow_schemedata 
                         WHERE F_DeleteMark = 0 
                         ORDER BY F_CreateDate DESC 
                         LIMIT 10";

            return _sqlRepository.SqlQuery<OnionFlowDataOutput>(sql);
        }

        /// <summary>
        /// OnionFlow 类别查询  智慧政务  智慧校园 智慧工厂  传入参数:IndustryCategory
        /// </summary>
        /// <param name="mOnionFlowIndustryCategory"></param>
        /// <returns></returns>
        public List<OnionFlowIndustryCategoryOutput> QueryOnionFlowIndustryCategory(string mOnionFlowIndustryCategory)
        {
            string sql = @"SELECT DISTINCT F_IndustryCategory as F_Id, F_IndustryCategory, '' as F_Description 
                         FROM onionflow_schemedata 
                         WHERE F_DeleteMark = 0";

            if (!string.IsNullOrEmpty(mOnionFlowIndustryCategory))
            {
                sql += " AND F_IndustryCategory LIKE @mOnionFlowIndustryCategory";
                mOnionFlowIndustryCategory = $"%{mOnionFlowIndustryCategory}%";
            }

            return _sqlRepository.SqlQuery<OnionFlowIndustryCategoryOutput>(sql, new { mOnionFlowIndustryCategory });
        }

        /// <summary>
        ///  获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string QueryOnionFlowSchemeDataByKey(string key)
        {
            string sql = @"SELECT F_OnionFlowSchemeData FROM onionflow_schemedata_hisversion 
                         WHERE F_Id = @key";

            return _sqlRepository.SqlQuery<string>(sql, new { key }).FirstOrDefault();
        }

        /// <summary>
        /// V2  获取OnionFlow Json 数据 Mongodb
        /// </summary>
        /// <param name="onionFlowId"></param>
        /// <param name="F_UserId"></param>
        /// <returns></returns>
        public string QueryOnionFlowSchemeDataByIdAndUserId(string onionFlowId, string F_UserId)
        {
            string sql = @"SELECT F_OnionFlowSchemeData FROM onionflow_schemedata 
                         WHERE F_Id = @onionFlowId";

            // 可以加入用户ID验证等逻辑
            // AND (F_CreateUserId = @F_UserId OR '公开分享规则')

            return _sqlRepository.SqlQuery<string>(sql, new { onionFlowId }).FirstOrDefault();
        }

        /// <summary>
        /// 通过类别 从市场中查询 OnionFlow   
        /// </summary>
        /// <param name="mCategory"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PageListModel QueryOnionFlowSchemeDataListFromMarketByCategory(string mCategory, string QueryField, int pageIndex, int pageSize)
        {
            string countSql = @"SELECT COUNT(1) FROM onionflow_schemedata 
                              WHERE F_DeleteMark = 0";

            string dataSql = @"SELECT * FROM onionflow_schemedata 
                            WHERE F_DeleteMark = 0";

            if (!string.IsNullOrEmpty(mCategory))
            {
                countSql += " AND F_IndustryCategory = @mCategory";
                dataSql += " AND F_IndustryCategory = @mCategory";
            }

            if (!string.IsNullOrEmpty(QueryField))
            {
                countSql += " AND F_Caption LIKE @QueryField";
                dataSql += " AND F_Caption LIKE @QueryField";
                QueryField = $"%{QueryField}%";
            }

            dataSql += " ORDER BY F_CreateDate DESC LIMIT @Skip, @Take";

            int totalCount = _sqlRepository.SqlQuery<int>(countSql, new { mCategory, QueryField }).FirstOrDefault();
            var data = _sqlRepository.SqlQuery<OnionFlowDataOutput>(dataSql, new
            {
                mCategory,
                QueryField,
                Skip = (pageIndex - 1) * pageSize,
                Take = pageSize
            });

            return new PageListModel
            {

                TotalCount = totalCount,
                Items = data
            };
        }

        /// <summary>
        ///通过标题 OnionFlow 市场中查询
        /// </summary>
        /// <param name="mTitle"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PageListModel QueryOnionFlowSchemeDataListFromMarketByTitle(string mTitle, int pageIndex, int pageSize)
        {
            string countSql = @"SELECT COUNT(1) FROM onionflow_schemedata 
                              WHERE F_DeleteMark = 0";

            string dataSql = @"SELECT * FROM onionflow_schemedata 
                            WHERE F_DeleteMark = 0";

            if (!string.IsNullOrEmpty(mTitle))
            {
                countSql += " AND F_Caption LIKE @mTitle";
                dataSql += " AND F_Caption LIKE @mTitle";
                mTitle = $"%{mTitle}%";
            }

            dataSql += " ORDER BY F_CreateDate DESC LIMIT @Skip, @Take";

            int totalCount = _sqlRepository.SqlQuery<int>(countSql, new { mTitle }).FirstOrDefault();
            var data = _sqlRepository.SqlQuery<OnionFlowDataOutput>(dataSql, new
            {
                mTitle,
                Skip = (pageIndex - 1) * pageSize,
                Take = pageSize
            });

            return new PageListModel
            {

                TotalCount = totalCount,
                Items = data
            };
        }

        /// <summary>
        ///查询用户查看历史记录--通过标题检索
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="QueryField"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PageListModel QueryUserViewOnionFlowHistoryDataListByTitle(string userId, string QueryField, int pageIndex, int pageSize)
        {
            // 这里假设有一个用户历史浏览表，如果没有，可以根据实际情况设计
            string countSql = @"SELECT COUNT(1) FROM onionflow_schemedata s
                              WHERE s.F_DeleteMark = 0 
                              AND EXISTS (SELECT 1 FROM onionflow_user_view_history h WHERE h.F_OnionFlowId = s.F_Id AND h.F_CreateUserId = @userId)";

            string dataSql = @"SELECT s.* FROM onionflow_schemedata s
                            WHERE s.F_DeleteMark = 0
                            AND EXISTS (SELECT 1 FROM onionflow_user_view_history h WHERE h.F_OnionFlowId = s.F_Id AND h.F_CreateUserId = @userId)";

            if (!string.IsNullOrEmpty(QueryField))
            {
                countSql += " AND s.F_Caption LIKE @QueryField";
                dataSql += " AND s.F_Caption LIKE @QueryField";
                QueryField = $"%{QueryField}%";
            }

            dataSql += " ORDER BY (SELECT h.F_ViewDate FROM onionflow_user_view_history h WHERE h.F_OnionFlowId = s.F_Id AND h.F_CreateUserId = @userId ORDER BY h.F_ViewDate DESC LIMIT 1) DESC";
            dataSql += " LIMIT @Skip, @Take";

            int totalCount = _sqlRepository.SqlQuery<int>(countSql, new { userId, QueryField }).FirstOrDefault();
            var data = _sqlRepository.SqlQuery<OnionFlowDataOutput>(dataSql, new
            {
                userId,
                QueryField,
                Skip = (pageIndex - 1) * pageSize,
                Take = pageSize
            });
            return new PageListModel
            {

                TotalCount = totalCount,
                Items = data
            };
        }

        /// <summary>
        /// 查询OnionFlow历史版本
        /// </summary>
        /// <param name="OnionFlowId"></param>
        /// <returns></returns>
        public List<OnionFlowHisVersionOutput> QueryOnionFlowHisVersion(string OnionFlowId)
        {
            string sql = @"SELECT * FROM onionflow_schemedata_hisversion 
                         WHERE F_OnionFlowId = @OnionFlowId 
                         AND F_DeleteMark = 0
                         ORDER BY F_CreateDate DESC";

            return _sqlRepository.SqlQuery<OnionFlowHisVersionOutput>(sql, new { OnionFlowId });
        }

        /// <summary>
        /// 设置主版本
        /// </summary>
        /// <param name="onionFlowMajorVersionInput"></param>
        /// <returns></returns>
        public string SetOnionFlowMajorVersion(OnionFlowMajorVersionInput onionFlowMajorVersionInput)
        {
            // 先将所有版本设为非主版本
            string resetSql = @"UPDATE onionflow_schemedata_hisversion SET 
                              F_IsMasterVersion = '0'
                              WHERE F_OnionFlowId = @F_OnionFlowId";

            _sqlRepository.SqlNonQuery(resetSql, new { F_OnionFlowId = onionFlowMajorVersionInput.F_OnionFlowId });

            // 设置指定版本为主版本
            string updateSql = @"UPDATE onionflow_schemedata_hisversion SET 
                               F_IsMasterVersion = '1'
                               WHERE F_Id = @F_Id";

            _sqlRepository.SqlNonQuery(updateSql, new { F_Id = onionFlowMajorVersionInput.F_Id });

            // 查询此版本的数据
            string querySql = @"SELECT F_OnionFlowSchemeData FROM onionflow_schemedata_hisversion 
                              WHERE F_Id = @F_Id";
            var schemeData = _sqlRepository.SqlQuery<string>(querySql, new { F_Id = onionFlowMajorVersionInput.F_Id }).FirstOrDefault();

            // 更新主数据
            string updateMainSql = @"UPDATE onionflow_schemedata SET 
                                  F_OnionFlowSchemeData = @F_OnionFlowSchemeData
                                  WHERE F_Id = @F_OnionFlowId";

            _sqlRepository.SqlNonQuery(updateMainSql, new
            {
                F_OnionFlowId = onionFlowMajorVersionInput.F_OnionFlowId,
                F_OnionFlowSchemeData = schemeData
            });

            return onionFlowMajorVersionInput.F_Id;
        }

        /// <summary>
        /// 查询图形智能体数据根据ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [SqlExecute("SELECT F_Id, F_Caption, F_AgentData FROM onionflow_prompt_agent_graphic WHERE F_Id = @Id")]
        public List<GraphicAgentOutput> QueryGraphicAgentDataById(string Id) => null;




        /// <summary>
        /// 辅助方法：通过ID查询OnionFlowData
        /// </summary>
        public OnionFlowDataOutput QueryOnionFlowDataById(string id)
        {
            string sql = "SELECT * FROM onionflow_schemedata WHERE F_Id = @id";
            return _sqlRepository.SqlQuery<OnionFlowDataOutput>(sql, new { id }).FirstOrDefault();
        }
    }

}




