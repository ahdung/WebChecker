using AhDung.WebChecker.Models;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(Web web);
    }
}
