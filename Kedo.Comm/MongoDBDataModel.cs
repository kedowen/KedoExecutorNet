using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm
{
    public class MongoDBDataModel
    {
        public string key { set; get; }

        public string stringData { set; get; }

        public string fieldsMap { set; get; }

        public int dataRowsCount { set; get; }
    }
   
}
