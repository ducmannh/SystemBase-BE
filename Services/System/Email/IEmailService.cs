using System.Threading.Tasks;

namespace SystemBase.BE.Services.System.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
