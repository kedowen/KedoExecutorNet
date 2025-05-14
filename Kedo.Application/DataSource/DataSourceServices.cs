using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Kedo.Application.DataSource.Dtos.input;
using Kedo.Application.DataSource.Dtos.output;
using Kedo.Application.DataSource.Services;
using System;
using System.Collections.Generic;

namespace Kedo.Application.DataSource
{

    public class DataSourceServices : IDynamicApiController
    {
        private readonly IDataSourceService _iDataSourceService;

        public DataSourceServices(IDataSourceService dataSourceService)
        {
            _iDataSourceService = dataSourceService;
        }


        /// <summary>
        /// 数据源类别 类别  SqlServer ,Msql,websocket
        /// </summary>
        /// <param name="mDataSourceType"> DataSourceType </param>
        /// <returns></returns>
        [HttpGet]
        public List<DataSourceTypeOutput> QueryDataSourceType([FromQuery] string mDataSourceType)
        {
            return _iDataSourceService.QueryDataSourceType(mDataSourceType);
        }


        /// <summary>
        /// 数据源类别 类别  SqlServer ,Msql  只是数据库 
        /// </summary>
        /// <param name="mDataSourceType"> DataSourceType </param>
        /// <returns></returns>
        [HttpGet]
        public List<DataSourceTypeOutput> QueryDBDataSourceType([FromQuery] string mDataSourceType)
        {
            return _iDataSourceService.QueryDBDataSourceType(mDataSourceType);
        }

        /// <summary>
        /// 创建数据源
        /// </summary>
        /// <param name="createDataSourceInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string CreateDataSource([FromBody] CreateDataSourceInput createDataSourceInput)
        {
            return _iDataSourceService.CreateDataSource(createDataSourceInput);
        }

        /// <summary>
        /// 修改数据源信息
        /// </summary>
        /// <param name="modifyDataSourceInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string ModifyDataSource([FromBody] ModifyDataSourceInput modifyDataSourceInput)
        {
            return _iDataSourceService.ModifyDataSource(modifyDataSourceInput);
        }

        /// <summary>
        /// 删除数据源信息
        /// </summary>
        /// <param name="removeDataSourceInput"></param>
        /// <returns></returns>
        [HttpPost]
        public string RemoveDataSource([FromBody] RemoveDataSourceInput removeDataSourceInput)
        {
            return _iDataSourceService.RemoveDataSource(removeDataSourceInput);
        }

        /// <summary>
        /// 根据数据源类型 查询数据源列表  列表显示 
        /// </summary>
        /// <param name="F_DataTypeId"></param>
        /// <returns></returns>
        [HttpGet]
        public List<DataSourceInfoOutput> QueryDataSourceInfoListByDataType([FromQuery] string F_DataTypeId)
        {
            return _iDataSourceService.QueryDataSourceInfoListByDataType(F_DataTypeId);
        }

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息  在编辑 查询信息的时候会用到 查询详情
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [HttpGet]
        public DataSourceDetailInfoOutput DataSourceDetailInfoById([FromQuery] string F_Id)
        {
            return _iDataSourceService.DataSourceDetailInfoById(F_Id);
        }


        /// <summary>
        /// 根据数据源Id 查询数据源详细信息  
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        [HttpGet]
        public DataSourceInfoOutput DataSourceInfoById([FromQuery] string F_Id)
        {
            return _iDataSourceService.DataSourceInfoById(F_Id);
        }

        /// <summary>
        /// 通过SQL 查询数据 
        /// </summary>
        /// <param name="executeSqlQueryInput"></param>
        /// <returns></returns>
        [HttpPost]
        public DataSourceQueryModelOutput QueryDBDataBySQL([FromBody] ExecuteSqlQueryInput executeSqlQueryInput)
        {
            return _iDataSourceService.QueryDBDataBySQL(executeSqlQueryInput);
        }

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        /// <param name="connectionInput"></param>
        /// <returns></returns>
        [HttpPost]
        public bool ConnectionCheck(CheckDataSourceConnectionInput connectionInput)
        {
            return _iDataSourceService.ConnectionCheck(connectionInput);
        }
    }
}
