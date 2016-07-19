using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ThatPiHunt.Domain
{
    public abstract class MapObject
    {
        public Point Position { get; set; }
        public string Identifier { get; set; }
    }
}
