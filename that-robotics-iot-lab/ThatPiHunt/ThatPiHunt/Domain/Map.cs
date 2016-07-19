using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using MathNet.Numerics.LinearRegression;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
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
