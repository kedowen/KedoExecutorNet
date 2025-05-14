using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string recipients, string subject, string content);
    }
}
