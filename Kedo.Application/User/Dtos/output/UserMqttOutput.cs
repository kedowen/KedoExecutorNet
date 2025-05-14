using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Application.User.Dtos.output
{
    public class UserMqttOutput
    {
        public string mqttaddr { set; get; }

        public int port { set; get; } = 1883;

        public string user { set; get; }

        public string pwd { set; get; }



        public string MessageChannelId { set; get; }

    }
}
