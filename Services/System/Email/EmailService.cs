using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SystemBase.BE.Services.System.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var portString = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var enableSslString = _configuration["Smtp:EnableSsl"];
            
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                Console.WriteLine("================= EMAIL SENDING (MOCK) =================");
                Console.WriteLine($"To: {to}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body:\n{body}");
                Console.WriteLine("========================================================");
                return;
            }

            int port = int.TryParse(portString, out int p) ? p : 587;
            bool enableSsl = bool.TryParse(enableSslString, out bool e) ? e : true;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username, "Hệ Thống Quản Lý Khuyến Công"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            using var smtpClient = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: {ex.Message}");
                throw new Exception("Lỗi xác thực Email (SMTP). Vui lòng kiểm tra lại cấu hình tài khoản gửi email (Smtp:Username và Smtp:Password).", ex);
            }
        }
    }
}
