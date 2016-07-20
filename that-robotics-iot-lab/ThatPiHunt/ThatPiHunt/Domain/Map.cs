using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ThatPiHunt.Domain
{
    public class Map
    {
        public Size MapSize { get; set; }
        public Rect MapBounds { get; set; }

        public ICollection<PointOfInterest> PointsOfInterest { get; set; }
        public Contestant Contestant { get; set; }
        public PointOfInterest CurrentGoal { get; set; }

        public double? CalculateDistance(PointOfInterest rangingTo, PointOfInterest destination)
        {
            if (rangingTo == destination)
            {
                return destination.EstimatedRadius;
            }

            // This is a little hokey, but basically we add the distance from
            // the destination to our ranging beacon, then the estimated distance
            // to the ranging beacon.
            return rangingTo.EstimatedRadius ?? 0 +
                   MathNet.Numerics.Distance.Euclidean(new[] { destination.Position.X, destination.Position.Y }, new[] { rangingTo.Position.X, rangingTo.Position.Y });

            // Exercise for the user (come up and talk about these, or show some working code for a BONUS):
            // 1. Given that we have a good idea of the closest beacon, might there be
            //    a better way to estimate distance?  
            // 2. Would it be useful to know that the closest beacon is closer or further 
            //    from the ranging beacon than the current ranging beacon estimate?
        }

        /// <summary>
        /// EXTRA BONUS POINTS: 
        ///   Make this work to Trilaterate (or Multi-laterate) your position
        ///   based on relative beacon signal strengths.  NOTE:  It probably
        ///   isn't sufficient to just work this out, you probably would also
        ///   need to adjust the distance estimation method in GameService to
        ///   more accurately fit the beacon signal strength curve.
        ///   
        /// This just doesn't work, the beacons are simply not accurate enough
        /// for a reasonable measurement and they move all over the place, 
        /// plus, this math isn't even right :)
        /// </summary>
        /// <param name="visiblePoints"></param>
        /// <returns></returns>
        public Point CalculatePosition(IEnumerable<Tuple<string, double>> visiblePoints)
        {
            if (!visiblePoints.Any()) { return new Windows.Foundation.Point(0, 0); }

            // find the points of interest that map to the visible points
            var poi = PointsOfInterest.Where(p => visiblePoints.Any(vp => vp.Item1.Equals(p.Identifier, StringComparison.OrdinalIgnoreCase)))
                                      .ToList();

            if (poi.Count < 2) { return new Windows.Foundation.Point(0, 0); }

            foreach(var point in poi)
            {
                var myId = point.Identifier;
                point.EstimatedRadius = visiblePoints.FirstOrDefault(vp => vp.Item1.Equals(myId, StringComparison.OrdinalIgnoreCase)).Item2;
            }

            //var vector = WeightedRegression.Weighted(
            //    poi.Select(p => Tuple.Create(new[] { p.Position.X, p.Position.Y }, visiblePoints.FirstOrDefault(vp => vp.Item1.Equals(p.Identifier, StringComparison.OrdinalIgnoreCase)).Item2)).ToArray(),
            //    poi.Select(p => 1 / (visiblePoints.FirstOrDefault(vp => vp.Item1.Equals(p.Identifier, StringComparison.OrdinalIgnoreCase)).Item2)).ToArray(),
            //    false); // no-intercept

            var xMatrix = DenseMatrix.OfColumnVectors(
                new DenseVector(poi.Select(p => p.Position.X).ToArray()), 
                new DenseVector(poi.Select(p => p.Position.Y).ToArray()));

            var output = Fit.MultiDimWeighted(
                xMatrix.ToRowArrays(), 
                poi.Select(p => p.EstimatedRadius.Value).ToArray(),
                poi.Select(p => 1 / Math.Pow(p.EstimatedRadius.Value, 2)).ToArray());

            // TODO: Actually calculate where the contestant is
            return new Point(output[0], output[1]);
        }
    }
}
