using Furion.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Kedo.Application.BaseIDatatem.Dtos.output;
using System.Collections.Generic;
using System.Data;
using Kedo.Application.BaseIDatatem.input;
using Kedo.Application.BaseIDatatem.output;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using Kedo.Application.BaseIDatatem.Dtos.input;


namespace Kedo.Application.BaseIDatatem.Service;
public class DataItemService : IDataItemService, IScoped
{
    private readonly ISql _sql;

    private readonly string _AccessKeyId;
    private readonly string _AccessSecret;
    private readonly string _BucketName;
    private readonly string _EndPoint;
    public DataItemService(ISql sql, [FromServices] IConfiguration configuration)
    {
        _sql = sql;
        _AccessKeyId = configuration["AliOSS:AccessKeyId"].ToString();
        _AccessSecret = configuration["AliOSS:AccessSecret"].ToString();
        _BucketName = configuration["AliOSS:BucketName"].ToString();
        _EndPoint = configuration["AliOSS:EndPoint"].ToString();
    }

    public List<DataItemOutput> GetDataItems(DataItemInput dataItemInput)
    {
        DataTable dataTable = _sql.GetDataItem(dataItemInput.ItemCode);
        if (dataTable.Rows.Count < 1) return null;
        List<DataItemOutput> mList = new();
        foreach (DataRow item in dataTable.Rows) mList.Add(new DataItemOutput
        {
            ItemDetailId = item["F_ItemDetailId"].ToString(),
            ItemCode = item["F_ItemCode"].ToString(),
            ItemName = item["F_ItemName"].ToString(),
            ItemValue = item["F_ItemValue"].ToString()
        });
        return mList;
    }

    /// <summary>
    /// OSS 配置
    /// </summary>
    /// <returns></returns>
    public AliOSSBaseData OnionbitOSSConfig()
    {
        AliOSSBaseData aliOSSBaseData = new AliOSSBaseData();
        aliOSSBaseData.AccessKeyId = _AccessKeyId;
        aliOSSBaseData.AccessSecret = _AccessSecret;
        aliOSSBaseData.BucketName = _BucketName;
        aliOSSBaseData.EndPoint = _EndPoint;

        return aliOSSBaseData;
    }


    public APIMessageModel GetCustomerFormData(FormData requestData)
    {
        APIMessageModel messageModel = new APIMessageModel();
        string mFormId = requestData.FormId;
        DataTable dataTable = _sql.GetFormSchemeData(mFormId);
        if (dataTable.Rows.Count > 0)
        {
            string dataForm = dataTable.Rows[0]["F_Scheme"].ToString();
            messageModel.data = ConvertJsonToBase64(dataForm);
        }
        messageModel.msg = "自定义表单数据";
        messageModel.msgCode = "0000";
        return messageModel;

    }

    public static string ConvertJsonToBase64(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException("JSON字符串不能为空", nameof(json));
        }
        // 将JSON字符串转换为字节数组
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        // 将字节数组转换为Base64字符串
        return Convert.ToBase64String(bytes);
    }
}

public class APIMessageModel
{
    public int status { set; get; } = 1;
    public string msgCode { set; get; } = "0000";
    public string msg { set; get; }
    public JObject resultData { set; get; }
    public Object data { set; get; }
    public int totalCount { set; get; }
    public Object mapping { set; get; }

}


