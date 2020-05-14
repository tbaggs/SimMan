using System;

namespace SimManager.Models
{
    public class GridBlock
    {
        public Guid Id { get; set; }
        public Guid? OccupierId { get; set; }
        public Location Location { get; set; }
        public Guid? ZoneId { get; set; }

        public bool IsOccupied {
            get
            {
                return OccupierId == null ? false : true;
            }   
        }
        public bool IsZone
        {
            get
            {
                return ZoneId == null ? false : true;
            }
        }

    }
}
