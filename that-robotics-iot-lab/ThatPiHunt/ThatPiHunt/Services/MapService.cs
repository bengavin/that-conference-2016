using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThatPiHunt.Domain;
using Windows.Foundation;

namespace ThatPiHunt.Services
{
    public class MapService
    {
        private static readonly Random __rnd = new Random();
        public static Map GenerateMap(int width, int height, int numberOfPoints)
        {
            return new Map
            {
                MapSize = new Windows.Foundation.Size(width, height),
                MapBounds = new Windows.Foundation.Rect(0, 0, width, height),
                Contestant = new Contestant { Identifier = "Test", Position = new Point(__rnd.Next(width), __rnd.Next(height)) },
                PointsOfInterest = GeneratePointsOfInterest(0, 0, width, height, numberOfPoints)
            };
        }

        private static ICollection<PointOfInterest> GeneratePointsOfInterest(int v1, int v2, int width, int height, int numberOfPoints)
        {
            var closestDist = (width + height) / 2f * 0.10;

            var retVal = new Collection<PointOfInterest>();
            for(var i = 0; i < numberOfPoints; i++)
            {
                var candidatePoint = new Point(__rnd.Next(width), __rnd.Next(height));
                while (retVal.Any(p => Math.Sqrt(Math.Pow(p.Position.X - candidatePoint.X, 2) + Math.Pow(p.Position.Y - candidatePoint.Y, 2)) < closestDist))
                {
                    candidatePoint = new Point(__rnd.Next(width), __rnd.Next(height));
                }

                retVal.Add(new PointOfInterest
                {
                    BeaconKey = $"Beacon {i}",
                    Identifier = Guid.NewGuid().ToString(),
                    Position = candidatePoint
                });
            }

            return retVal;
        }
    }
}
