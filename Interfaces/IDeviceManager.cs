using SimManager.Models;
using System.Threading.Tasks;

namespace SimManager.Interfaces
{
    public interface IDeviceManager
    {
        Task<IoTDeviceInfo> AddDeviceAsync(string deviceId);
        Task RemoveDeviceAsync(string deviceId);
    }
}
