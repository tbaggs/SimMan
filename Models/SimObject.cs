using SimManager.Interfaces;
using System;

namespace SimManager.Models
{
    public class SimObject: ISimObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Guid GridLocation { get; set; }
        public IoTDeviceInfo IoTDeviceInfo { get; set; }
    }
}