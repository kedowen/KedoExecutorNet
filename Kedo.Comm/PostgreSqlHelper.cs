using Furion;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Kedo.Comm
{
    //private readonly ILogger<RabbitMQHelper> _logger;
    //private ConnectionFactory? _connectionFactory = null;
    //public IConnection? connection = null;
    //public class PostgreSqlHelper
    //{
    //}


    //private readonly ILogger<RabbitMQHelper> _logger;
    //private ConnectionFactory? _connectionFactory = null;
    //public IConnection? connection = null;
    //private IModel? _channel = null;
    //private readonly Dictionary<string, object> _args = new() { { "x-message-ttl", 1000 * 60 * 60 * 12 } };

    ///// <summary>
    ///// 构造函数
    ///// </summary>
    //public RabbitMQHelper(ILogger<RabbitMQHelper> logger)
    //{
    //    _logger = logger;
    //    // 连接RabbitMQ
    //    var vhost = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_VHost");
    //    var username = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_User");
    //    var password = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_Pwassword");
    //    var hostname = App.Configuration.GetValue<string>("RabbitMQConfigurations:RabiitMQ_Host");
    //    var port = App.Configuration.GetValue<int>("RabbitMQConfigurations:RabiitMQ_Port");

    //    _connectionFactory = new ConnectionFactory()
    //    {
    //        HostName = hostname,
    //        Port = port,
    //        UserName = username,
    //        Password = password,
    //        VirtualHost = vhost,
    //        AutomaticRecoveryEnabled = true
    //    };
    //}

    ///// <summary>
    ///// 创建链接
    ///// </summary>
    //public void CreateConn()
    //{
    //    if (connection == null || !connection.IsOpen)
    //    {
    //        connection = _connectionFactory?.CreateConnection();
    //    }
    //}


}
