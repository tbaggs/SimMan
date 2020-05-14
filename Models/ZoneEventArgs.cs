using System;
using System.Collections.Generic;
using System.Text;

namespace SimManager.Models
{
    public class ZoneEventArgs : EventArgs
    {
        public GridBlock GridBlock { get; set; }
        public SimObject SimObject { get; set; }
    }
}
