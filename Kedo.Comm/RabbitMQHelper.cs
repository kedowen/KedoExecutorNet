using Furion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RabbitMQ.Client;

using System.Text;

namespace Kedo.Comm;

/// <summary>
/// RabbitMQ消息队列生产处理类
/// </summary>
public class RabbitMQHelper
{
    private readonly ILogger<RabbitMQHelper> _logger;
    private ConnectionFactory? _connectionFactory = null;
    public IConnection? connection = null;
    private IModel? _channel = null;
    private readonly Dictionary<string, object> _args = new() { { "x-message-ttl", 1000 * 60 * 60 * 12 } };

    /// <summary>
    /// 构造函数
    /// </summary>
    public RabbitMQHelper(ILogger<RabbitMQHelper> logger)
    {
        _logger = logger;
        // 连接RabbitMQ
        var vhost = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_VHost");
        var username = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_User");
        var password = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_Pwassword");
        var hostname = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_Host");
        var port = App.Configuration.GetValue<int>("RabbitMQConfigurations:RabiitMQ_Port");

        _connectionFactory = new ConnectionFactory()
        {
            HostName = hostname,
            Port = port,
            UserName = username,
            Password = password,
            VirtualHost = vhost,
            AutomaticRecoveryEnabled = true
        };
    }

    /// <summary>
    /// 创建链接
    /// </summary>
    public void CreateConn()
    {
        if (connection == null || !connection.IsOpen)
        {
            connection = _connectionFactory?.CreateConnection();
        }
    }


    /// <summary>
    /// 消息入队列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queueName"></param>
    /// <param name="msgModel"></param>
    /// <returns></returns>
    public bool Enqueue<T>(string queueName, T msgModel)
    {
        if (msgModel != null && !string.IsNullOrEmpty(queueName))
        {
            try
            {
                if (connection == null || !connection.IsOpen)
                {
                    CreateConn();
                }
                using (var channel = connection?.CreateModel())
                {
                    // 声明队列
                    channel?.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: _args);
                    //channel.ConfirmSelect();

                    // 消息组装
                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgModel));
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    // 入队列
                    channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);

                    // 到达确认
                    //channel.WaitForConfirmsOrDie();
                }
            }
            catch (Exception err)
            {
                _logger.LogError(err.Message);
                return false;
            }
            return true;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 原始消息入队列
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public int EnqueueByte(string queueName, byte[] bytes)
    {
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                if (connection == null || !connection.IsOpen)
                {
                    CreateConn();
                }
                using (var channel = connection?.CreateModel())
                {
                    // 声明队列
                    channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: _args);
                    //channel.ConfirmSelect();

                    // 消息组装
                    var body = bytes;
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    // 入队列
                    channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);

                    // 到达确认
                    //channel.WaitForConfirmsOrDie();
                }
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取channel
    /// </summary>
    /// <param name="queueName"></param>
    /// <param name="prefetchCount"></param>
    /// <returns></returns>
    public IModel getChannel(string queueName, ushort prefetchCount)
    {
        if (connection == null || !connection.IsOpen)
        {
            CreateConn();
        }

        _channel = connection?.CreateModel();

        _channel.QueueDeclare(queue: queueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: _args);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);

        return _channel;
    }

}