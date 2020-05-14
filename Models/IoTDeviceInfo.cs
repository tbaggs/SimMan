using System.Text;

namespace SimManager.Models
{
    public class IoTDeviceInfo
    {
        public string DeviceId { get; set; }
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public string HostName { get; set; }

        public string IoTHubConnectionString
        {
            get
            {
                StringBuilder connString = new StringBuilder();

                connString.AppendFormat("HostName={0};DeviceId={1};SharedAccessKey={2}",
                    HostName,
                    DeviceId,
                    PrimaryKey);

                return connString.ToString();
            }
        }

    }
}
