using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SystemBase.BE.Hubs
{
    public class AppHub : Hub
    {
        // Hub này được sử dụng để đẩy thông báo realtime từ Server tới Client.
        // Các client sẽ kết nối tới đây và lắng nghe các sự kiện như "ForceLogout".
    }
}
