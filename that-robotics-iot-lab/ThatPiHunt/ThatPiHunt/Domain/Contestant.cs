using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

namespace ThatPiHunt.Domain
{
    public class Contestant : MapObject
    {
        public Contestant()
        {
            Color = Colors.Red;
            VisitedPointsOfInterest = new Collection<Tuple<PointOfInterest, DateTime>>();
        }

        public Color Color { get; set;}
        public double CurrentEstimatedRadius { get; set; }

        public ICollection<Tuple<string, double>> VisiblePointsOfInterest
        {
            get
            {
                return _pointsOfInterestSeen
                            .Select(
                                kvp => Tuple.Create(
                                        kvp.Key, 
                                        kvp.Value.Item2.Where(v => v.HasValue).Average(v => v.Value))
                            )
                            .OrderBy(t => t.Item2)
                            .ToList();
            }
        }

        public ICollection<Tuple<PointOfInterest, DateTime>> VisitedPointsOfInterest { get; set; }
        public PointOfInterest RangeToPointOfInterest { get; internal set; }

        private Dictionary<string, Tuple<int, List<double?>>> _pointsOfInterestSeen = new Dictionary<string, Tuple<int, List<double?>>>();

        public void RegisterCurrentPointsOfInterest(List<Tuple<string, double>> points)
        {
            // Add / Update
            foreach (var point in points)
            {
                if (_pointsOfInterestSeen.ContainsKey(point.Item1))
                {
                    // we have an existing point, let's record this distance measurement
                    var dist = _pointsOfInterestSeen[point.Item1];
                    var distIndex = dist.Item1;
                    var distList = dist.Item2;

                    distList[distIndex] = point.Item2;
                    distIndex = (distIndex + 1) % distList.Count; // next storage location

                    // Assign back to the dictionary
                    _pointsOfInterestSeen[point.Item1] = Tuple.Create(distIndex, distList);
                }
                else
                {
                    // We'll record up to 25 distance measurements [about 5 seconds]
                    var distList = new List<double?>(Enumerable.Repeat<double?>(null, 25));
                    distList[0] = point.Item2;

                    _pointsOfInterestSeen.Add(point.Item1, Tuple.Create(1, distList));
                }
            }

            // Clean up points
            foreach (var point in _pointsOfInterestSeen.Keys.Where(k => !points.Any(p => p.Item1 == k)).ToList())
            {
                // loop through and remove the oldest measurement for that point, when all measurements
                // are gone, consider this point no longer 'visible'
                var dist = _pointsOfInterestSeen[point];
                var distIndex = dist.Item1;
                var distList = dist.Item2;

                var distSearch = distIndex;
                while (distList[distSearch] == null)
                {
                    distSearch++;
                    if (distSearch == distList.Count) { distSearch = 0; }
                    if (distSearch == distIndex) break;
                }

                distList[distIndex] = null;
                distIndex = (distIndex + 1) % distList.Count; // next storage location

                if (!distList[distIndex].HasValue)
                {
                    // list is now empty
                    _pointsOfInterestSeen.Remove(point);
                }
                else
                {
                    _pointsOfInterestSeen[point] = Tuple.Create(distIndex, distList);
                }
            }
        }
    }
}
