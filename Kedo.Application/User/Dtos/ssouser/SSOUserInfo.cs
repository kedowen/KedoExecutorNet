using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.ssouser
{
    public class SSOUserInfo
    {
        public string UserId { get; set; }
        public string Account { get; set; }
        public string UserName { get; set; }
        public string Timestamp { get; set; }
        public string Sign { get; set; }
        public int iat { get; set; }
        public int nbf { get; set; }
        public int exp { get; set; }
        public string iss { get; set; }
        public string aud { get; set; }
    }

}
