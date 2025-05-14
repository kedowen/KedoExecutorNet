using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.WechatWork
{
    public interface IWechatWorkService
    {
        Task<bool> SendMessageAsync(string recipients, string subject, string content);
    }
}
