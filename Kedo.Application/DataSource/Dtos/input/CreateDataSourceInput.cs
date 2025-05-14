using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.DataSource.Dtos.input
{
    public class CreateDataSourceInput
    {
        // public string F_Id { set; get; }
        public string F_DataSourceTypeId { set; get; }
        
        public string F_Caption { set; get; }

        public string F_Ip { set; get; }

        public int F_Port { set; get; }

        public string F_DBName { set; get; }

        public string F_DataSourceUserId { set; get; }

        public string F_Pwd { set; get; }

        public string F_Remark { set; get; }

        public string F_ConnectionString { set; get; }//保存明文 返回加密字符串  存入数据库 也为加密字符串

        public string F_CreateUserId { set; get; }

        //public string F_AccountNo { set; get; }

        //public string F_Pwd { set; get; }
    }
}
