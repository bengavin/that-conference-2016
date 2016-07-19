using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace ThatPiHunt.Domain
{
    public class PointOfInterest : MapObject
    {
        public PointOfInterest()
        {
            Color = Colors.Blue;
        }

        public string BeaconKey { get; set; }
        public Color Color { get; set; }
        public double? EstimatedRadius { get; set; }
    }
}
