using Furion.DependencyInjection;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Kedo.Comm;
using System.Collections.Generic;
using Furion.FriendlyException;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Furion.DatabaseAccessor;

namespace Kedo.Application.Oss.Services
{
    public class OssService : IOssService, ITransient
    {

        private readonly string _FileSavePath;
        private readonly string _FileUrl;
        private readonly ISqlRepository _sqlRepository;
        private readonly ILogger<OssService> _logger;
        private readonly IDistributedCache _redis;
        private readonly RabbitMQHelper _rabbitMQ;
        private readonly ISql _sql;
        private readonly string MessageQueueName;

        public OssService(ISqlRepository sqlRepository, [FromServices] IConfiguration configuration, ILogger<OssService> logger, ISql sql, IDistributedCache redis, RabbitMQHelper rabbitMQ)
        {
            _FileSavePath = configuration["GPTDataFile:FileSavePath"];
            _FileUrl = configuration["GPTDataFile:FileUrl"];
            _sql = sql;
            MessageQueueName = configuration["RabbitMQConfigurations:BIData"];
            _rabbitMQ = rabbitMQ;
            _logger = logger;
            _sqlRepository = sqlRepository;
        }

        public object GetPostObjectParams(string imgAppType)
        {
            UploadOssHelper mpHelper = new();
            // 生成参数
            return mpHelper.CreateUploadParams(imgAppType);
        }

     

        /// <summary>
        /// 上传文件并保存到关联数据库中
        /// </summary>
        /// <param name="formFiles"></param>
        /// <param name="flowAgentId"></param>
        /// <param name="tagId"></param>
        /// <returns></returns>
        public List<string> UploadDocuments(List<IFormFile> formFiles, string flowAgentId, string tagId)
        {
            var results = new List<string>();
            var fileUrls = new List<string>();
            string fileSerialNo = DateTime.Now.ToString("yyyyMMdd");
            var savePath = Path.Combine(_FileSavePath, fileSerialNo, tagId);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            foreach (var formFile in formFiles)
            {
                if (formFile.Length > 0)
                {
                    var fileName = Path.GetFileName(formFile.FileName);
                    var filePath = Path.Combine(savePath, fileName);
                    using (var stream = System.IO.File.Create(filePath))
                    {
                        formFile.CopyTo(stream);
                    }
                    var fileUrl = Path.Combine(_FileUrl, fileSerialNo, tagId, fileName).Replace('\\', '/');
                    fileUrls.Add(fileUrl);
                }
            }
            var sqls = new List<string>();
            foreach (var mItem in fileUrls)
            {
                string mId = Guid.NewGuid().ToString();
                string mInsert = @"insert into gpt_flow_chatfilesource(F_Id,F_AgentFlowId,F_SourceType,F_SourceUrl,F_CreateDate,F_DeleteMark)
                               VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')";
                mInsert = string.Format(mInsert, mId, flowAgentId, "文件上传", mItem, DateTime.Now.ToString("yyyy-MM-dd"), "0");
      
                if (_sqlRepository.SqlNonQuery(mInsert) == 0)
                    throw Oops.Bah("数据异常").StatusCode(201);
            }
           

            results.Add("成功");
            return fileUrls;
        }

     /// <summary>
     /// 上传图片
     /// </summary>
     /// <param name="formFile"></param>
     /// <param name="userId"></param>
     /// <returns></returns>
     /// <exception cref="Exception"></exception>
        public string UploadImg(IFormFile formFile, string userId)
        {
            if (formFile == null || formFile.Length == 0)
                throw new Exception("No file uploaded");

            string fileSerialNo = DateTime.Now.ToString("yyyyMMdd");
            var savePath = Path.Combine(_FileSavePath, fileSerialNo);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var fileName = Path.GetFileName(formFile.FileName);
            var filePath = Path.Combine(savePath, fileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                formFile.CopyTo(stream);
            }
            var fileUrl = Path.Combine(_FileUrl, fileSerialNo, fileName).Replace('\\', '/');
            return fileUrl;
        }
    }
}
