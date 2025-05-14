using Furion.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Kedo.Comm;
using System;
using System.Collections.Generic;
using Furion;
using Furion.FriendlyException;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Kedo.Application.DataSource.Dtos.input;
using Kedo.Application.DataSource.Dtos.output;
using System.Data;
using System.Linq;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.CodeAnalysis;
using Furion.DatabaseAccessor;

namespace Kedo.Application.DataSource.Services
{

    public class DataSourceService : IDataSourceService, IScoped
    {
        private readonly ILogger<DataSourceService> _logger;
        private readonly IDistributedCache _redis;
        private readonly RabbitMQHelper _rabbitMQ;
        private readonly ISql _sql;
        private readonly string MessageQueueName;
        private readonly ISqlRepository _sqlRepository;
        public DataSourceService(ISqlRepository sqlRepository, ILogger<DataSourceService> logger, ISql sql, IDistributedCache redis, RabbitMQHelper rabbitMQ, [FromServices] IConfiguration configuration)
        {
            _sql = sql;
            _logger = logger;
            _redis = redis;
            _rabbitMQ = rabbitMQ;
            _sqlRepository = sqlRepository;
            MessageQueueName = configuration["RabbitMQConfigurations:BIData"];
        }

        /// <summary>
        /// 创建数据源
        /// </summary>
        /// <param name="createDataSourceInput"></param>
        /// <returns></returns>
        public string CreateDataSource(CreateDataSourceInput createDataSourceInput)
        {
            string mConnectionString = getConnectStringByDataSourceType(createDataSourceInput.F_Ip, createDataSourceInput.F_Port, createDataSourceInput.F_DBName, createDataSourceInput.F_DataSourceUserId, createDataSourceInput.F_Pwd, createDataSourceInput.F_DataSourceTypeId);
            //消息队列存入数据库
            //  string mTeamId = Guid.NewGuid().ToString();
            var sqls = new List<string>();
            string mInsertInto = " insert into bas_datasource(F_Id,F_DataSourceTypeId,F_Caption,F_Ip,F_Port,F_DBName,F_DataSourceUserId,F_Pwd,F_Remark,F_ConnectionString,F_CreateUserId,F_CreateDate,F_DeleteMark,F_EnabledMark)" +
                " values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}')";
            string mId = Guid.NewGuid().ToString();
            mInsertInto = string.Format(mInsertInto, mId, createDataSourceInput.F_DataSourceTypeId, createDataSourceInput.F_Caption, createDataSourceInput.F_Ip, createDataSourceInput.F_Port, createDataSourceInput.F_DBName,
                createDataSourceInput.F_DataSourceUserId, createDataSourceInput.F_Pwd, createDataSourceInput.F_Remark, mConnectionString, createDataSourceInput.F_CreateUserId, DateTime.Now.ToString(), "0", "1");

            if (_sqlRepository.SqlNonQuery(mInsertInto) == 0)
                throw Oops.Bah("数据异常").StatusCode(201);
            return mId;
        }


        /// <summary>
        /// 修改数据源信息
        /// </summary>
        /// <param name="modifyDataSourceInput"></param>
        /// <returns></returns>
        public string ModifyDataSource(ModifyDataSourceInput modifyDataSourceInput)
        {
            string mConnectionString = getConnectStringByDataSourceType(modifyDataSourceInput.F_Ip, modifyDataSourceInput.F_Port, modifyDataSourceInput.F_DBName, modifyDataSourceInput.F_DataSourceUserId, modifyDataSourceInput.F_Pwd, modifyDataSourceInput.F_DataSourceTypeId);

            //消息队列存入数据库  F_AllowEndTime
            var sqls = new List<string>();
            string mUpdate = " update bas_datasource set F_Caption='{0}',F_Ip='{1}',F_Port='{2}',F_DBName='{3}'," +
                "F_DataSourceUserId='{4}',F_Pwd='{5}',F_Remark='{6}',F_ConnectionString='{7}',F_ModifyUserId='{8}',F_ModifyDate='{9}' where F_Id='{10}'";
            mUpdate = string.Format(mUpdate, modifyDataSourceInput.F_Caption, modifyDataSourceInput.F_Ip, modifyDataSourceInput.F_Port, modifyDataSourceInput.F_DBName,
                modifyDataSourceInput.F_DataSourceUserId, modifyDataSourceInput.F_Pwd, modifyDataSourceInput.F_Remark, mConnectionString,
               modifyDataSourceInput.F_ModifyUserId, DateTime.Now, modifyDataSourceInput.F_Id);

            if (_sqlRepository.SqlNonQuery(mUpdate) == 0)
                throw Oops.Bah("数据异常").StatusCode(201);
            return "";
        }

        /// <summary>
        /// 删除数据源信息
        /// </summary>
        /// <param name="removeDataSourceInput"></param>
        /// <returns></returns>
        public string RemoveDataSource(RemoveDataSourceInput removeDataSourceInput)
        {
            //消息队列存入数据库  F_AllowEndTime
            string mTeamId = Guid.NewGuid().ToString();
            var sqls = new List<string>();

            string mRemoveData = "update bas_datasource set F_DeleteMark='1',F_DeleteUserId='{0}',F_DeleteDate='{1}' where F_Id='{2}'";

            mRemoveData = string.Format(mRemoveData, removeDataSourceInput.F_DeleteUserId, DateTime.Now, removeDataSourceInput.F_Id);

            if (_sqlRepository.SqlNonQuery(mRemoveData) == 0)
                throw Oops.Bah("数据异常").StatusCode(201);
            return "";
        }


        /// <summary>
        /// 根据数据源类型 查询数据源列表
        /// </summary>
        /// <param name="F_DataTypeId"></param>
        /// <returns></returns>
        public List<DataSourceInfoOutput> QueryDataSourceInfoListByDataType(string F_DataTypeId)
        {
            List<DataSourceInfoOutput> dataSourcenIfoOutput = _sql.QueryDataSourceInfoListByDataType(F_DataTypeId);

            return dataSourcenIfoOutput;
        }

        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        public DataSourceDetailInfoOutput DataSourceDetailInfoById(string F_Id)
        {
            DataSourceDetailInfoOutput dataSoureDetailInfoOutput = _sql.QueryDataSourceDetailInfo(F_Id).FirstOrDefault();

            return dataSoureDetailInfoOutput;
        }


        /// <summary>
        /// 根据数据源Id 查询数据源详细信息
        /// </summary>
        /// <param name="F_Id"></param>
        /// <returns></returns>
        public DataSourceInfoOutput DataSourceInfoById(string F_Id)
        {
            DataSourceInfoOutput dataSourceInfoOutput = _sql.QueryDataSourceInfoById(F_Id).FirstOrDefault();

            return dataSourceInfoOutput;
        }


        /// <summary>
        /// 数据源类别
        /// </summary>
        /// <param name="mDataSourceType"></param>
        /// <returns></returns>
        public List<DataSourceTypeOutput> QueryDataSourceType(string mDataSourceType)
        {
            DataTable dataTable = _sql.QueryDataSourceType(mDataSourceType);
            if (dataTable.Rows.Count < 1) return null;

            List<DataSourceTypeOutput> mList = new();
            foreach (DataRow item in dataTable.Rows) mList.Add(new DataSourceTypeOutput
            {
                ItemDetailId = item["F_ItemDetailId"].ToString(),
                ItemCode = item["F_ItemCode"].ToString(),
                ItemName = item["F_ItemName"].ToString(),
                ItemValue = item["F_ItemValue"].ToString()
            });
            return mList;
        }


        /// <summary>
        /// 数据库类别
        /// </summary>
        /// <param name="mDataSourceType"></param>
        /// <returns></returns>
        public List<DataSourceTypeOutput> QueryDBDataSourceType(string mDataSourceType)
        {
            DataTable dataTable = _sql.QueryDBDataSourceType(mDataSourceType);
            if (dataTable.Rows.Count < 1) return null;

            List<DataSourceTypeOutput> mList = new();
            foreach (DataRow item in dataTable.Rows) mList.Add(new DataSourceTypeOutput
            {
                ItemDetailId = item["F_ItemDetailId"].ToString(),
                ItemCode = item["F_ItemCode"].ToString(),
                ItemName = item["F_ItemName"].ToString(),
                ItemValue = item["F_ItemValue"].ToString()
            });
            return mList;
        }



        /// <summary>
        /// 通过SQL 查询数据 
        /// </summary>
        /// <param name="executeSqlQueryInput"></param>
        /// <returns></returns>
        public DataSourceQueryModelOutput QueryDBDataBySQL(ExecuteSqlQueryInput executeSqlQueryInput)
        {
            string sql = executeSqlQueryInput.SqlString;
            string DataSourceTypeId = executeSqlQueryInput.DataSourceTypeId;
            string connectionStringId = executeSqlQueryInput.ConnectionStringId;
            string sourceType = "";
            string connectionString = "";
            DataTable dataTableSourceType = _sql.QueryDataSourceTypeById(DataSourceTypeId);
            if (dataTableSourceType.Rows.Count > 0)
            {
                sourceType = dataTableSourceType.Rows[0]["F_ItemName"].ToString();
            }

            List<DataSourceInfoOutput> dataSourceInfoOutputs = _sql.QueryDataSourceInfoById(connectionStringId);
            DataSourceInfoOutput dataSourceInfoOutput = dataSourceInfoOutputs.FirstOrDefault();
            connectionString = dataSourceInfoOutput.F_ConnectionString;

            DataTable dataTable = new DataTable();

            switch (sourceType.ToLower())
            {
                case "sqlserver":
                    DbDataAccess_SqlServer dbDataAccess_SqlServer = new DbDataAccess_SqlServer();
                    dataTable = dbDataAccess_SqlServer.ExecDataTable(sql, connectionString);
                    break;

                case "mysql":
                    DbDataAccess_MySql dbDataAccess_MySql = new DbDataAccess_MySql();
                    dataTable = dbDataAccess_MySql.ExecDataTable(sql, connectionString);
                    break;

                case "postgresql":
                    DbDataAccess_PostgreSQL dbDataAccess_PostgreSQL = new DbDataAccess_PostgreSQL();
                    dataTable = dbDataAccess_PostgreSQL.ExecDataTable(sql, connectionString);
                    break;
            }

            //  var data= JsonConvert.SerializeObject(dataTable);


            JArray arrayList = new JArray();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                JObject dictionary = new JObject();
                foreach (DataColumn dataColumn in dataTable.Columns)
                {
                    dictionary[dataColumn.ColumnName] = dataRow[dataColumn.ColumnName].ToString();
                }
                arrayList.Add(dictionary);
            }
            DataSourceQueryModelOutput dataSourceQueryModelOutput = new DataSourceQueryModelOutput();
            dataSourceQueryModelOutput.data = arrayList;
            return dataSourceQueryModelOutput;
        }

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        /// <param name="connectionInput"></param>
        /// <returns></returns>
        public bool ConnectionCheck(CheckDataSourceConnectionInput connectionInput)
        {
            string sourceType = "";
            string connectionString = "";
            bool result = false;
            DataTable dataTableSourceType = _sql.QueryDataSourceTypeById(connectionInput.F_DataSourceTypeId);
            if (dataTableSourceType.Rows.Count > 0)
            {
                sourceType = dataTableSourceType.Rows[0]["F_ItemName"].ToString();
            }
            switch (sourceType.ToLower())
            {
                case "sqlserver":
                    DbDataAccess_SqlServer dbDataAccess_SqlServer = new DbDataAccess_SqlServer();
                    connectionString = "Server={0};Initial Catalog={1};User ID={2};Password={3}";
                    connectionString = string.Format(connectionString, connectionInput.F_Ip, connectionInput.F_DBName, connectionInput.F_DataSourceUserId, connectionInput.F_Pwd);
                    result = dbDataAccess_SqlServer.ExecConnection(connectionString);
                    break;

                case "mysql":
                    DbDataAccess_MySql dbDataAccess_MySql = new DbDataAccess_MySql();
                    connectionString = "Server={0};port={1};database={2};uid={3};pwd={4};";
                    connectionString = string.Format(connectionString, connectionInput.F_Ip, connectionInput.F_Port, connectionInput.F_DBName, connectionInput.F_DataSourceUserId, connectionInput.F_Pwd);
                    result = dbDataAccess_MySql.ExecConnection(connectionString);
                    break;

                case "postgresql":
                    DbDataAccess_PostgreSQL dbDataAccess_PostgreSQL = new DbDataAccess_PostgreSQL();
                    connectionString = "HOST={0};PORT={1};DATABASE={2};PASSWORD={3};USER ID={4}";
                    connectionString = string.Format(connectionString, connectionInput.F_Ip, connectionInput.F_Port, connectionInput.F_DBName, connectionInput.F_DataSourceUserId, connectionInput.F_Pwd);
                    result = dbDataAccess_PostgreSQL.ExecConnection(connectionString);
                    break;
            }

            if (!result)
            {
                throw Oops.Bah("数据源连接测试不通过1" + sourceType + "--" + connectionString).StatusCode(201);
            }
            return result;
        }



        private string getConnectStringByDataSourceType(string F_Ip, int port, string F_DBName, string F_DataSourceUserId, string F_Pwd, string typeId)
        {
            string sourceType = "";
            string connectionString = "";
            DataTable dataTableSourceType = _sql.QueryDataSourceTypeById(typeId);
            if (dataTableSourceType.Rows.Count > 0)
            {
                sourceType = dataTableSourceType.Rows[0]["F_ItemName"].ToString();
            }



            switch (sourceType.ToLower())
            {
                case "sqlserver":
                    connectionString = "Server={0};Initial Catalog={1};User ID={2};Password={3}";
                    connectionString = string.Format(connectionString, F_Ip, F_DBName, F_DataSourceUserId, F_Pwd);

                    break;

                case "mysql":
                    connectionString = "Server={0};port={1};database={2};uid={3};pwd={4};";
                    connectionString = string.Format(connectionString, F_Ip, port, F_DBName, F_DataSourceUserId, F_Pwd);
                    break;

                case "postgresql":
                    connectionString = "HOST={0};PORT={1};DATABASE={2};USER ID={3};PASSWORD={4};";
                    connectionString = string.Format(connectionString, F_Ip, port, F_DBName, F_DataSourceUserId, F_Pwd);
                    break;

                case "websocket":

                    if (F_Ip.Contains("ws://"))
                    {
                        connectionString = F_Ip;
                    }
                    else
                    {
                        connectionString = "ws://" + connectionString;
                    }

                    break;
                case "mqtt":
                    connectionString = F_Ip;
                    if (F_Ip.Contains("ws://"))
                    {
                        connectionString = F_Ip;
                    }
                    else
                    {
                        connectionString = "ws://" + connectionString;
                    }

                    break;
            }
            return connectionString;

        }

    }
}
