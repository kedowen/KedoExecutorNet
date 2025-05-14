using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kedo.Application.User.Dtos.output;

namespace Kedo.Application.User.Dtos.ssouser
{
    public class SSOUserCache
    {
        public DateTime AuthTime { set; get; }

        public UserInfoOutput UserInfoOutput { set; get; }
    }
}
