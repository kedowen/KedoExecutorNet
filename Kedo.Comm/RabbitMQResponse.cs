namespace Kedo.Comm;

public class RabbitMQResponse
{
    /// <summary>
    /// 多租户 Host
    /// </summary>
    public string host { get; set; }
    /// <summary>
    /// 数据文本
    /// </summary>
    public string data { get; set; }
}
