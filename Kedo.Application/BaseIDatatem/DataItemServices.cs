using Furion.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using Kedo.Application.BaseIDatatem.Dtos.output;
using System.Collections.Generic;
using Kedo.Application.BaseIDatatem.input;
using Kedo.Application.BaseIDatatem.output;
using Furion.DependencyInjection;
using Kedo.Application.BaseIDatatem.Dtos.input;
using Kedo.Application.BaseIDatatem.Service;
namespace Kedo.Application;

public class DataItemServices : IDynamicApiController,ITransient
{

    private readonly IDataItemService _dataItemService;

    public DataItemServices(IDataItemService itemDataService)
    {
        _dataItemService = itemDataService;
    }
    [HttpPost]
    public List<DataItemOutput> GetItemData([FromBody] DataItemInput dataItemInput)
    {
        return _dataItemService.GetDataItems(dataItemInput);
    }

    [HttpPost]
    public AliOSSBaseData OnionbitOSSConfig()
    {
        return _dataItemService.OnionbitOSSConfig();
    }

   



    //[HttpPost]
    //public string GetWeather([FromBody]weather weather)
    //{
    //    if (weather.location == "北京")
    //    {
    //        return "天气晴朗";
    //    }

    //    else if (weather.location == "徐州")
    //    {
    //        return "下小雨";
    //    }
    //    else
    //    {
    //        return "大风天气";
    //    }
    //}

    //[HttpPost]
    //public string BookTrainTicket([FromBody] Mydestination mydestination)
    //{
    //    return "高特商务座 预定完毕";
    //}

}

//public class weather
//{
//  public string location { set; get; }


//    public DateTime date { set; get; }

//}

//public class Mydestination {

//    public string departure { set; get; }
//    public string destination { set; get; }

//    public string date { set; get; }

//}

//"{\"departure\": \"你的出发城市\", \"destination\": \"北京\", \"date\": \"2023-11-01\"}"
