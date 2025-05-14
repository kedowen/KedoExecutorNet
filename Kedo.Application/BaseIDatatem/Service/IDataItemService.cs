using Kedo.Application.BaseIDatatem.Dtos.output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Application.BaseIDatatem.input;
using Kedo.Application.BaseIDatatem.output;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Kedo.Application.BaseIDatatem.Service;
using Kedo.Application.BaseIDatatem.Dtos.input;


namespace Kedo.Application;

public interface IDataItemService
{
    List<DataItemOutput> GetDataItems(DataItemInput dataItemInput);
    AliOSSBaseData OnionbitOSSConfig();
}



