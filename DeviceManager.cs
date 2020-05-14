using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimManager.Interfaces;
using SimManager.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimManager
{
    public class DeviceManager: IDeviceManager
    {
        private readonly ILogger _logger;
        private readonly IOptions<IoTHubSettings> _iotHubSettings;
        private readonly string _hubConnectionString = null;
        RegistryManager _registryManager;

        public DeviceManager(ILogger<DeviceManager> logger, IOptions<IoTHubSettings> iotHubSettings)
        {
            _logger = logger;
            _iotHubSettings = iotHubSettings;

            StringBuilder connString = new StringBuilder();
            connString.AppendFormat("HostName={0};SharedAccessKeyName={1};SharedAccessKey={2}", 
                _iotHubSettings.Value.HostName,
                _iotHubSettings.Value.SharedAccessKeyName,
                _iotHubSettings.Value.SharedAccessKey);

            _hubConnectionString = connString.ToString();

            _logger.LogInformation("configuring device manager");


            if (string.IsNullOrEmpty(_hubConnectionString))
                throw new ArgumentNullException("IoT Hub connection string is missing or null");

            _logger.LogInformation("connecting to iot hub registry manager"); 
            
            
            _registryManager = RegistryManager.CreateFromConnectionString(_hubConnectionString);
        }

        public async Task<IoTDeviceInfo> AddDeviceAsync(string deviceId)
        {
            _logger.LogInformation($"adding device '{deviceId}' with default authentication . . . ");
            Device device = await _registryManager.AddDeviceAsync(new Device(deviceId)).ConfigureAwait(false);

            IoTDeviceInfo deviceInfo = new IoTDeviceInfo(){
                DeviceId = deviceId,
                HostName = _iotHubSettings.Value.HostName,
                PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey,
                SecondaryKey = device.Authentication.SymmetricKey.SecondaryKey
            };

            return deviceInfo;
        }

        public async Task RemoveDeviceAsync(string deviceId)
        {
            _logger.LogInformation($"removing device '{deviceId}' . . . ");
            await _registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
        }
    }
}
