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
        /// Don't range to any beacons with an estimated distance of
        /// this many meters (or more), even if we think we can see
        /// them
        /// </summary>
        public static double MaxReliableBeaconRange = 15;

        /// <summary>
        /// The number of points needed to visit in order to 'win' the game
        /// </summary>
        public static int VisitedPointsToWin = 10;

        /// <summary>
        /// What is our desired frame rate? [200ms -> 5 fps]
        /// </summary>
        public static int LoopIntervalMs = 200;

        /// <summary>
        /// How long should we consider beacons visible after we stop seeing advertisements?
        /// </summary>
        public static int LiveBeaconTimePeriodSec = 30;

        public event Func<IEnumerable<PointOfInterest>, Task> DrawBeaconRadii;
        public event Func<Contestant, Task> GameComplete;

        private readonly BeaconService _beaconService;
        private readonly MapService _mapService;
        private readonly LedService _ledService;
        private readonly PushButtonService _buttonService;
        private readonly Random _random;

        private CancellationTokenSource _cancelSource;
        private Task _workerTask;
        private bool _isPaused;

        public GameService(MapService mapService, BeaconService beaconService, LedService ledService, PushButtonService buttonService)
        {
            _beaconService = beaconService;
            _mapService = mapService;
            _ledService = ledService;
            _buttonService = buttonService;

            _random = new Random();
        }

        public Map Map { get; private set; }

        public async Task<bool> StartAsync(Map map)
        {
            if (_cancelSource != null) { return true; } // already started
            _cancelSource = new CancellationTokenSource();

            if (!await _ledService.InitializeAsync() ||
                !await _buttonService.InitializeAsync())
            {
                return false;
            }

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
            _ledService.PushLEDColor(Colors.Purple);

            _isPaused = true;
            return true;
        }

        public async Task<bool> ResumeAsync()
        {
            _ledService.PopLEDColor();

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

            _ledService.Shutdown();
            _buttonService.Shutdown();

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

                if (Map.CurrentGoal.EstimatedRadius <= 2 && DateTime.Now.Subtract(_buttonService.LastButtonPush() ?? DateTime.MinValue).TotalMinutes < 1)
                {
                    Map.Contestant.VisitedPointsOfInterest.Add(Tuple.Create(Map.CurrentGoal, DateTime.Now));
                    _buttonService.ClearButtonPush();

                    if (!AssignNextGoal())
                    {
                        await GameComplete(Map.Contestant);
                        break;
                    }
                }
                else
                {
                    await DrawBeaconRadii(Map.PointsOfInterest);
                }

                var sleepTime = LoopIntervalMs - (int)(DateTime.UtcNow - loopStart).TotalMilliseconds;
                if (sleepTime > 0)
                {
                    await Task.Delay(sleepTime);
                }
            }
        }

        private bool AssignNextGoal()
        {
            if (Map.Contestant.VisitedPointsOfInterest.Count >= VisitedPointsToWin)
            {
                return false;
            }

            var nextGoal = Map.PointsOfInterest
                                         .Where(poi => !Map.Contestant.VisitedPointsOfInterest.Any(vpoi => poi == vpoi.Item1))
                                         .OrderBy(poi => _random.Next())
                                         .FirstOrDefault();
            if (nextGoal == null)
            {
                return false;
            }

            Map.CurrentGoal = nextGoal;
            return true;
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

            // Now, filter by the max reliable distance
            currentBeaconList = currentBeaconList.Where(b => EstimateDistance(b.BaseTransmitPower ?? 0, b.SignalStrength) <= MaxReliableBeaconRange).ToList();

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

                var rangeToDistance = Map.CalculateDistance(rangeToPoi, Map.CurrentGoal);
                if (rangeToDistance > MaxReliableBeaconRange)
                {
                    _ledService.SetBlinkRate(TimeSpan.FromSeconds(5));
                }
                else if(rangeToDistance > (MaxReliableBeaconRange * 0.75))
                {
                    _ledService.SetBlinkRate(TimeSpan.FromSeconds(3));
                }
                else if(rangeToDistance > (MaxReliableBeaconRange * 0.4))
                {
                    _ledService.SetBlinkRate(TimeSpan.FromSeconds(1));
                }
                else if(rangeToDistance > 2)
                {
                    _ledService.SetBlinkRate(TimeSpan.FromSeconds(0.5));
                }
                else if(rangeToPoi != Map.CurrentGoal)
                {
                    _ledService.StopBlinking();
                }
                else
                {
                    _ledService.GoRainbow();
                }

                if (rangeToPoi == Map.CurrentGoal)
                {
                    _ledService.SetLEDColor(Colors.Green);
                }
                else if (rangeToPoi != null)
                {
                    _ledService.SetLEDColor(Colors.Blue);
                }
                else
                {
                    _ledService.SetLEDColor(Colors.Red);
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

                    var rangeToDistance = Map.CalculateDistance(Map.Contestant.RangeToPointOfInterest, Map.CurrentGoal);
                    if (rangeToDistance > 25)
                    {
                        _ledService.SetBlinkRate(TimeSpan.FromSeconds(5));
                    }
                    else if (rangeToDistance > 15)
                    {
                        _ledService.SetBlinkRate(TimeSpan.FromSeconds(3));
                    }
                    else if (rangeToDistance > 8)
                    {
                        _ledService.SetBlinkRate(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        _ledService.GoRainbow();
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

        /// <summary>
        /// Provide estimated distance from the beacon in meters
        /// </summary>
        /// <param name="txPower"></param>
        /// <param name="signalStrength"></param>
        /// <returns></returns>
        private double EstimateDistance(int txPower, double signalStrength)
        {
            if (txPower == 0 || signalStrength == 0)
            {
                return double.MaxValue; // can't know the distance
            }

            var ratio = txPower - signalStrength;
            var linRatio = Math.Pow(10, ratio / 10);
            return Math.Sqrt(linRatio);

            // BONUS POINTS:
            // This is the version suggested by the Android Beacon library
            // It doesn't seem to perform any better, and we haven't done
            // the curve fitting to determine the Pi 3's signal strength 
            // falloff curve.
            //
            // 1.  Gather some average signal strength measurements at known
            //     intervals and use the MathNET.Numerics library to actually
            //     fit a curve to your device, then see if the ranging works
            //     better.  Talk about your findings.
            //
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
