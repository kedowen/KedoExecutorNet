using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.input
{
    public class EnterpriseUserLoginInput
    {
        public string AppID { set; get; }

        public string SecretKey { set; get; }


        public string UserId { set; get; }

        public string UserName { set; get; }
    }
}
