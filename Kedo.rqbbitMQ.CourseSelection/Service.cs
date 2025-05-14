using Furion.DatabaseAccessor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Kedo.Comm;
using System.Data;
using Kedo.rabbitMQ.BIData.Utils;


namespace Kedo.rqbbitMQ.BIData;

internal class Service : IHostedService, IDisposable
{
    private readonly ILogger<Service> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQHelper _rabbitMQ;
    private readonly IModel? Channel = null;
    private readonly string MessageQueueName;
    private readonly IConfiguration _configuration;
    public Service(ILogger<Service> logger, [FromServices] IConfiguration configuration, RabbitMQHelper rabbitMQ, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _rabbitMQ = rabbitMQ;
        _rabbitMQ.CreateConn();
        _rabbitMQ.connection?.CreateModel();
        Channel = _rabbitMQ.connection?.CreateModel();
        MessageQueueName = "Embeddings";//configuration["RabbitMQConfigurations:BIData"];
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartService();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Channel?.Dispose();
    }

    #region Embeddings 文件
    private void StartService()
    {
        try
        {
            // 消费队列
            IModel consumechannel = _rabbitMQ.getChannel(MessageQueueName, 10);
            var consumer = new EventingBasicConsumer(consumechannel);
            _logger.LogInformation("步骤1");
            consumer.Received += async (model, ea) =>
            {
                var rabbitMQResponse = JsonConvert.DeserializeObject<RabbitMQResponse>(Encoding.UTF8.GetString(ea.Body.Span));
                _logger.LogInformation("数据：" + rabbitMQResponse.data);
                var data = JsonConvert.DeserializeObject<EqueueDataModel>(rabbitMQResponse.data);
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        _logger.LogInformation("步骤2.1----------->");
                        //切换租户
                        scope.ServiceProvider.GetService<IMemoryCache>().GetOrCreate("host", cache => rabbitMQResponse.host);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("步骤3 异常：" + ex.ToString());
                    }

                    try
                    {

                        _logger.LogInformation("开始Embeddings 文件----------->");
                        var _repository = Db.GetSqlRepository();
                        FileReaderUtil fileReaderUtil = new FileReaderUtil();
                        foreach (var sql in data.sqls)
                        {
                            DataTable dataTable = _repository.Database.ExecuteReader(sql);
                            if (dataTable.Rows.Count > 0)
                            {
                                string mFileId = dataTable.Rows[0]["F_Id"]?.ToString() ?? string.Empty;
                                string mFilePath = dataTable.Rows[0]["F_FilePath"]?.ToString() ?? string.Empty;
                                string mF_FileType = dataTable.Rows[0]["F_FileType"]?.ToString() ?? string.Empty;

                                string fileContent = "";
                                if (mF_FileType.Trim().ToLower().Contains("pdf"))
                                {
                                    UmiOcrClient umiOcrClient = new UmiOcrClient(_configuration);
                                    umiOcrClient.mFileId = mFileId;
                                    mFilePath = "D:\\123.pdf";
                                    string newFilePath= await umiOcrClient.RecognizePdfAsync(mFilePath);
                                    fileContent = fileReaderUtil.ReadFile(newFilePath);
                                }
                                else  //目前支持 word doc docx txt 格式文件
                                {
                                    fileContent = fileReaderUtil.ReadFile(mFilePath);
                                }
                                HttpEmbeddings httpEmbeddings = new HttpEmbeddings();
                                await httpEmbeddings.EmbeddingsContent(fileContent, mFileId);
                                using (var transaction = _repository.Database.BeginTransaction())
                                {
                                    string updateEmbeddingsStatus = " update kb_document set F_EmbeddingsStatus='1' where F_Id='{0}'";
                                    updateEmbeddingsStatus = string.Format(updateEmbeddingsStatus, mFileId);
                                    _repository.SqlNonQuery(updateEmbeddingsStatus);
                                    transaction.Commit();
                                }
                            }
                        }

                        _logger.LogInformation("Embeddings文件结束----------->");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("执行异常Embedding 异常" + ex.ToString());
                    }
                };
            };
            consumechannel.BasicConsume(queue: MessageQueueName, autoAck: true, consumer: consumer);
        }
        catch (Exception e)
        {
            _logger.LogInformation("异常消息：" + e.Message.ToString());
            _logger.LogError("rabbitMQ-> 异常：" + e.Message);
        }
    }

    #endregion


    #region 执行数据库消息队列逻辑
    /// <summary>
    /// 获取运单  
    /// </summary>
    //private void StartService()
    //{
    //    try
    //    {
    //        // 消费队列
    //        IModel consumechannel = _rabbitMQ.getChannel(MessageQueueName, 10);

    //        var consumer = new EventingBasicConsumer(consumechannel);

    //       // _logger.LogInformation("步骤1");
    //        // 数据入库逻辑
    //        consumer.Received += (model, ea) =>
    //        {

    //            var rabbitMQResponse = JsonConvert.DeserializeObject<RabbitMQResponse>(Encoding.UTF8.GetString(ea.Body.Span));

    //            //_logger.LogInformation("数据：" + rabbitMQResponse.data);

    //            var data = JsonConvert.DeserializeObject<EqueueDataModel>(rabbitMQResponse.data);

    //           // _logger.LogInformation("步骤2");

    //            using (var scope = _serviceProvider.CreateScope())
    //            {
    //                try
    //                {
    //                   // _logger.LogInformation("步骤2.1----------->");
    //                    //切换租户
    //                    scope.ServiceProvider.GetService<IMemoryCache>().GetOrCreate("host", cache => rabbitMQResponse.host);
    //                  //  _logger.LogInformation("步骤3"+ scope.ToString());
    //                }
    //                catch (Exception ex)
    //                {
    //                    _logger.LogInformation("步骤3 异常："+ex.ToString());
    //                }

    //                try
    //                {
    //                    var _repository = Db.GetSqlRepository();
    //                    using (var transaction = _repository.Database.BeginTransaction())
    //                    {
    //                        foreach (var sql in data.sqls)
    //                        {
    //                            _repository.SqlNonQuery(sql);
    //                           // _logger.LogInformation("步骤4" + sql);
    //                        }

    //                        transaction.Commit();

    //                        //consumechannel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    _logger.LogInformation("SQL执行异常" + ex.ToString());
    //                }
    //            };
    //        };
    //        consumechannel.BasicConsume(queue: MessageQueueName, autoAck: true, consumer: consumer);
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogInformation("异常消息：" + e.Message.ToString());
    //        _logger.LogError("rabbitMQ-> 异常：" + e.Message);
    //    }
    //}

    #endregion
}
