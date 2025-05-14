using Furion.FriendlyException;
using Furion;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using Kedo.Comm.EmailMessage;
using Microsoft.Extensions.Configuration;

namespace Kedo.Comm.Email
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailMessageHelper> _logger;
        private readonly string mfromEmail;
        private readonly string mEmailName;
        private readonly string mCredentialCode;
        private readonly string mSmtpClientServer;
        private readonly int mSmtpClientPort;
        public EmailService(ILogger<EmailMessageHelper> logger)
        {
            _logger = logger;
            mfromEmail = App.Configuration.GetValue<string>("Email:FromEmail");
            mEmailName = App.Configuration.GetValue<string>("Email:EmailName");
            mCredentialCode = App.Configuration.GetValue<string>("Email:CredentialCode");
            mSmtpClientServer = App.Configuration.GetValue<string>("Email:SmtpClientServer");
            mSmtpClientPort = Convert.ToInt32(App.Configuration.GetValue<string>("Email:SmtpClientPort"));
        }

        /// <summary>
        /// 发送验证码短信
        /// </summary>
        /// <param name="cellphones"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<bool> SendEmailAsync(string recipients, string subject, string content)
        {
            MessageSendModel messageSendModel = new MessageSendModel();
            try
            {
                var builder = new BodyBuilder();
                builder.TextBody = content;
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(mEmailName, mfromEmail));
                message.To.Add(new MailboxAddress(mEmailName, recipients));
                message.Body = builder.ToMessageBody();
                message.Subject = subject;
                using (var client = new SmtpClient())
                {
                    //client.EnableSsl = true;
                    //client.UseDefaultCredentials = false;
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    var mSendMail = mfromEmail;
                    var mSendPwd = mCredentialCode;
                    client.Connect(mSmtpClientServer, mSmtpClientPort, true);
                    client.Authenticate(mSendMail, mSendPwd);

                    try
                    {
                        client.Send(message);
                        client.Disconnect(true);
                        messageSendModel.RetStatus = "1";
                        messageSendModel.Msg = "Success";
                    }
                    catch (SmtpCommandException ex)
                    {
                        messageSendModel.RetStatus = "2";
                        throw Oops.Bah("创建数据源信息失败:" + ex.ToString()).StatusCode(201);
                        // Console.WriteLine(ex.ErrorCode);
                    }
                    catch (Exception ex)
                    {
                        messageSendModel.RetStatus = "2";
                        throw Oops.Bah("邮件发送失败：" + ex.ToString()).StatusCode(201);
                    }
                }

            }
            catch (Exception ex)
            {
                messageSendModel.RetStatus = "3";
                messageSendModel.Msg = ex.ToString();
            }
            return true;

        }



        /// <summary>
        /// 发送验证码短信
        /// </summary>
        /// <param name="cellphones"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public MessageSendModel SendEmail(string mToEmail, string subject, string messageBody)
        {
            MessageSendModel messageSendModel = new MessageSendModel();
            try
            {
                var builder = new BodyBuilder();
                builder.HtmlBody = EmailContentToHtml.ConvertMarkdownToEmailHtml(messageBody);
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(mEmailName, mfromEmail));
                message.To.AddRange(GetMailboxAddresses(mToEmail));
                message.Body = builder.ToMessageBody();
                message.Subject = subject;

                using (var client = new SmtpClient())
                {
                    //client.EnableSsl = true;
                    //client.UseDefaultCredentials = false;
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    var mSendMail = mfromEmail;
                    var mSendPwd = mCredentialCode;
                    client.Connect(mSmtpClientServer, mSmtpClientPort, true);
                    client.Authenticate(mSendMail, mSendPwd);

                    try
                    {
                        client.Send(message);
                        client.Disconnect(true);
                        messageSendModel.RetStatus = "1";
                        messageSendModel.Msg = "Success";
                    }
                    catch (SmtpCommandException ex)
                    {
                        messageSendModel.RetStatus = "2";
                        throw Oops.Bah("创建数据源信息失败:" + ex.ToString()).StatusCode(201);
                        // Console.WriteLine(ex.ErrorCode);
                    }
                    catch (Exception ex)
                    {
                        messageSendModel.RetStatus = "2";
                        throw Oops.Bah("邮件发送失败：" + ex.ToString()).StatusCode(201);
                    }
                }

            }
            catch (Exception ex)
            {
                messageSendModel.RetStatus = "3";
                messageSendModel.Msg = ex.ToString();
            }
            return messageSendModel;

        }


        List<MailboxAddress> GetMailboxAddresses(string users)
        {
            List<MailboxAddress> mailboxAddresses = new List<MailboxAddress>();
            string[] toUser = users.Split(";");
            foreach (var v in toUser)
            {
                if (!string.IsNullOrEmpty(v.Trim()))
                {
                    MailboxAddress mailboxAddress = new MailboxAddress(mEmailName, v);
                    mailboxAddresses.Add(mailboxAddress);
                }
            }
            return mailboxAddresses;
        }
    }
}
