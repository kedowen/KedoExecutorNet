using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.DataSource.Dtos.input
{
    public class ExecuteSqlQueryInput
    {
        public string SqlString { set; get; }
        public string DataSourceTypeId { set; get; }
        public string ConnectionStringId { set; get; }
    }
}
