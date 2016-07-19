using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThatPiHunt.Domain;
using Windows.UI;

namespace ThatPiHunt.Services
{
    public class GameService
    {
        /// <summary>
        /// What is our desired frame rate? [200ms -> 5 fps]
        /// </summary>
        public static int LoopIntervalMs = 200;

        /// <summary>
        /// How long should we consider beacons visible after we stop seeing advertisements?
        /// </summary>
        public static int LiveBeaconTimePeriodSec = 30;

        public event Func<IEnumerable<PointOfInterest>, Task> DrawBeaconRadii;

        private readonly BeaconService _beaconService;
        private readonly MapService _mapService;
        private readonly Random _random;

        private CancellationTokenSource _cancelSource;
        private Task _workerTask;
        private bool _isPaused;

        public GameService(MapService mapService, BeaconService beaconService)
        {
            _beaconService = beaconService;
            _mapService = mapService;
            _random = new Random();
        }

        public Map Map { get; private set; }

        public bool Start(Map map)
        {
            if (_cancelSource != null) { return true; } // already started
            _cancelSource = new CancellationTokenSource();

            try
            {
                Map = map;

                // Select a random point of interest to be the 'goal'
                var item = _random.Next(map.PointsOfInterest.Count - 1);
                map.CurrentGoal = map.PointsOfInterest.ElementAt(item);

                _workerTask = Task.Run(ExecuteGameLoop, _cancelSource.Token);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<bool> PauseAsync()
        {
            _isPaused = true;
            return true;
        }

        public async Task<bool> ResumeAsync()
        {
            _isPaused = false;
            return true;
        }

        public async Task<bool> StopAsync()
        {
            if (_cancelSource == null || _workerTask == null) { return true; } // already stopped

            var cancelSource = _cancelSource;
            _cancelSource = null;

            var workerTask = _workerTask;
            _workerTask = null;

            cancelSource.Cancel();
            try
            {
                await workerTask;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private async Task ExecuteGameLoop()
        {
            var cancelToken = _cancelSource.Token;

            while(!cancelToken.IsCancellationRequested)
            {
                var loopStart = DateTime.UtcNow;

                UpdateGameState();

                if (cancelToken.IsCancellationRequested) { break; }

                await DrawBeaconRadii(Map.PointsOfInterest);

                var sleepTime = LoopIntervalMs - (int)(DateTime.UtcNow - loopStart).TotalMilliseconds;
                if (sleepTime > 0)
                {
                    await Task.Delay(sleepTime);
                }
            }
        }

        private void UpdateGameState()
        {
            if (_isPaused) { return; }

            var lastVisibleTime = DateTimeOffset.Now.AddSeconds(-1 * GameService.LiveBeaconTimePeriodSec);
            var currentBeaconList = _beaconService.GetVisibleBeacons(lastVisibleTime);

            // Pull Current Points of interest from map, update contestant's 'view' of the world
            try
            {
                Map.Contestant.RegisterCurrentPointsOfInterest(currentBeaconList.Select(b => Tuple.Create($"{b.Namespace}-{b.Instance}", EstimateDistance(b.BaseTransmitPower ?? 0, b.SignalStrength))).ToList());
            }
            catch
            {
                // TODO: log this somehow...
            }

            // If we can't see any beacons right now (contestant values will tail off), there's nothing more to do here
            if (currentBeaconList.Any())
            {
                // If we can see the goal, then home in on that
                var rangeToBeacon = currentBeaconList.FirstOrDefault(b => $"{b.Namespace}-{b.Instance}".Equals(Map.CurrentGoal.Identifier));
                var rangeToPoi = Map.CurrentGoal;

                if (rangeToBeacon == null)
                {
                    // Get the list of points of interest, sorted by distance from the goal
                    var poiByGoalDistance = Map.PointsOfInterest
                                               .Where(poi => poi != Map.CurrentGoal)
                                               .OrderBy(poi => MathNet.Numerics.Distance.Euclidean(new[] { Map.CurrentGoal.Position.X, Map.CurrentGoal.Position.Y }, new[] { poi.Position.X, poi.Position.Y }))
                                               .ToList();
                    rangeToPoi = poiByGoalDistance.First(p => currentBeaconList.Any(b => $"{b.Namespace}-{b.Instance}".Equals(p.Identifier)));
                    rangeToBeacon = currentBeaconList.First(b => $"{b.Namespace}-{b.Instance}".Equals(rangeToPoi.Identifier));
                }

                // Calculate contestants current position given their visible points of interest
                try
                {
                    // Set the beacons we can see to an appropriate color
                    foreach (var poi in Map.PointsOfInterest)
                    {
                        var vp = Map.Contestant.VisiblePointsOfInterest.FirstOrDefault(p => p.Item1 == poi.Identifier);
                        if (vp != null)
                        {
                            poi.Color = Colors.Purple;
                            poi.EstimatedRadius = poi == rangeToPoi ? (double?)vp.Item2 : null;
                        }
                        else
                        {
                            poi.Color = Colors.Blue;
                            poi.EstimatedRadius = null;
                        }
                    }

                    // Set the beacon we're homing to an appropriate color
                    rangeToPoi.Color = rangeToPoi == Map.CurrentGoal ? Colors.Green : Colors.Orange;

                    // Set the contestant to 'live' at the beacon we're ranging to
                    Map.Contestant.RangeToPointOfInterest = rangeToPoi;
                    Map.Contestant.Position = rangeToPoi.Position;
                    Map.Contestant.CurrentEstimatedRadius = EstimateDistance(rangeToBeacon.BaseTransmitPower.Value, rangeToBeacon.SignalStrength);
                }
                catch
                {
                    // TODO: log this somehow
                }
            }
            else if (Map.Contestant.RangeToPointOfInterest != null)
            {
                // Calculate contestants current position given their visible points of interest
                try
                {
                    // Set the beacons we can see to an appropriate color
                    foreach (var poi in Map.PointsOfInterest)
                    {
                        var vp = Map.Contestant.VisiblePointsOfInterest.FirstOrDefault(p => p.Item1 == poi.Identifier);
                        if (vp != null)
                        {
                            poi.Color = Colors.Purple;
                            poi.EstimatedRadius = poi == Map.Contestant.RangeToPointOfInterest ? (double?)vp.Item2 : null;
                        }
                        else
                        {
                            poi.Color = Colors.Blue;
                            poi.EstimatedRadius = null;
                        }
                    }

                    // Set the beacon we're homing to an appropriate color
                    Map.Contestant.RangeToPointOfInterest.Color = Map.Contestant.RangeToPointOfInterest == Map.CurrentGoal ? Colors.Green : Colors.Orange;

                    //// Set the contestant to 'live' at the beacon we're ranging to
                    //Map.Contestant.Position = rangeToPoi.Position;
                    //Map.Contestant.CurrentEstimatedRadius = EstimateDistance(rangeToBeacon.BaseTransmitPower.Value, rangeToBeacon.SignalStrength);
                }
                catch
                {
                    // TODO: log this somehow
                }

            }
        }

        private double EstimateDistance(int txPower, double signalStrength)
        {
            // Distance Factor: 10 pixels / in, calculated distance is meters
            // So (approx.)m * 100cm/m * 1in/2.54cm * 10 = 393.7
            //var outputRatio = 1f; // 00 / 2.54;
            if (txPower == 0 || signalStrength == 0)
            {
                return double.MaxValue; // can't know the distance
            }

            var ratio = txPower - signalStrength;
            var linRatio = Math.Pow(10, ratio / 10);
            return Math.Sqrt(linRatio);

            //var ratio = signalStrength * 1f / txPower;
            //if (ratio < 1f)
            //{
            //    return outputRatio * Math.Pow(ratio, 10);
            //}
            //else
            //{
            //    return outputRatio * (0.89976) * Math.Pow(ratio, 7.7095) + 0.111;
            //}
        }
    }
}
