using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThatPiHunt.Domain;
using ThatPiHunt.Services;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ThatPiHunt.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BluetoothLEAdvertisementWatcher _watcher;
        private bool _isMarking;
        private Map _map;
        private BeaconService _beaconService = new BeaconService();
        private GameService _gameService;

        private List<UIElement> _beaconEllipses = new List<UIElement>();
        private LedService _ledService;
        private PushButtonService _pushButtonService;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_watcher != null)
            {
                _watcher.Received -= Receive_Watcher_Notification;
                _watcher.Stop();
                _watcher = null;
            }
            else
            {
                _beaconService.ClearBeacons();
                _watcher = new Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher();
                _watcher.ScanningMode = BluetoothLEScanningMode.Active;
                _watcher.Received += Receive_Watcher_Notification;
                _watcher.Start();
            }
        }

        private async void Receive_Watcher_Notification(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            await _beaconService.RegisterBeaconSightingAsync(args.Advertisement, args.BluetoothAddress, args.Timestamp, args.RawSignalStrengthInDBm);
        }

        private async void Mark_Click(object sender, RoutedEventArgs e)
        {
            _isMarking = !_isMarking;

            if (_isMarking)
            {
                await new MessageDialog("Click the image where you want to mark a Beacon").ShowAsync();
            }
        }

        private async void Image_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isMarking) return;

            var curPoint = e.GetCurrentPoint((Image)sender);
            var mapOrigin = new Point(FeetToMeters(110), FeetToMeters(300));

            var renderRatio = 535.0 / FeetToMeters(300); // 393.701; // pixels / m
            //var ratio = Math.Min(MapViewer.ActualWidth / MapImage.DesiredSize.Width, MapViewer.ActualHeight / MapImage.DesiredSize.Height);

            var pointX = curPoint.Position.X / renderRatio;
            var pointY = curPoint.Position.Y / renderRatio;

            var mapX = pointX - mapOrigin.X;
            var mapY = mapOrigin.Y - pointY;

            await new MessageDialog($"Got click on image at: {curPoint.Position.X}, {curPoint.Position.Y}; Map Coords ({MetersToFeet(mapX)}, {MetersToFeet(mapY)})").ShowAsync();
        }

        private void GenerateMap_Click(object sender, RoutedEventArgs e)
        {
            _map = new Map
            {
                Contestant = new Contestant() { Position = new Point(1200, 1200) },
                PointsOfInterest = new List<PointOfInterest>
                {
                    new PointOfInterest { Identifier = "20160809000000000000-000000000001", Position = new Point(FeetToMeters(31), FeetToMeters(116)), Character = "Blastoise EX" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000002", Position = new Point(FeetToMeters(43), FeetToMeters(115)), Character = "Helioptile" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000003", Position = new Point(FeetToMeters(134), FeetToMeters(181)), Character = "Chansey" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000004", Position = new Point(FeetToMeters(131), FeetToMeters(112)), Character = "Psyduck" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000005", Position = new Point(FeetToMeters(110), FeetToMeters(114)), Character = "Hippopotas" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000006", Position = new Point(FeetToMeters(134), FeetToMeters(148)), Character = "Robo Substitute" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000007", Position = new Point(FeetToMeters(122), FeetToMeters(182)), Character = "Charmander" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000008", Position = new Point(FeetToMeters(134), FeetToMeters(230)), Character = "Vanillite" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000009", Position = new Point(FeetToMeters(56), FeetToMeters(27)), Character = "Buneary" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000010", Position = new Point(FeetToMeters(83), FeetToMeters(3)), Character = "Flabebe" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000011", Position = new Point(FeetToMeters(17), FeetToMeters(105)), Character = "Gilgar" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000012", Position = new Point(FeetToMeters(84), FeetToMeters(107)), Character = "Yanma" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000013", Position = new Point(FeetToMeters(11), FeetToMeters(98)), Character = "Swirlix" },
                    new PointOfInterest { Identifier = "20160809-0000-0000-0000-000000000014", Position = new Point(FeetToMeters(20), FeetToMeters(56)), Character = "Glalie EX" },
                    new PointOfInterest { Identifier = "20160809-0000-0000-0000-000000000015", Position = new Point(FeetToMeters(121), FeetToMeters(245)), Character = "Lombre" },
                    new PointOfInterest { Identifier = "20160809-0000-0000-0000-000000000016", Position = new Point(FeetToMeters(121), FeetToMeters(136)), Character = "Archen" },
                    new PointOfInterest { Identifier = "20160809-0000-0000-0000-000000000017", Position = new Point(FeetToMeters(65), FeetToMeters(116)), Character = "Swadloon" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000018", Position = new Point(FeetToMeters(120), FeetToMeters(217)), Character = "Finneon" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000019", Position = new Point(FeetToMeters(84), FeetToMeters(22)), Character = "Torchic" },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000020", Position = new Point(FeetToMeters(42), FeetToMeters(14)), Character = "Pikachu" },
                }
            };
            _map.CurrentGoal = _map.PointsOfInterest.Last();

            var renderRatio = 535.0 / FeetToMeters(325); // 393.701; // pixels / m
            var ratio = Math.Min(MapViewer.ActualWidth / MapImage.DesiredSize.Width, MapViewer.ActualHeight / MapImage.DesiredSize.Height);
            foreach (var poi in _map.PointsOfInterest)
            {
                var x = poi.Position.X;
                var y = poi.Position.Y;

                // Adjust points to map origin (measurement origin is at the bottom, middle of the map)
                // (draw origin is at the top left of the map, with positive Y downwards)
                var pointOrigin = new Point(FeetToMeters(110), FeetToMeters(325));
                x += pointOrigin.X;
                y = pointOrigin.Y - y; // map coordinates have Y in the 'up' direction

                var point = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(poi.Color), Tag = poi };
                MapCanvas.Children.Add(point);

                var drawX = renderRatio * x * ratio;
                var drawY = renderRatio * y * ratio;
                Canvas.SetLeft(point, drawX);
                Canvas.SetTop(point, drawY);
            }

            var contestant = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.Red), Tag = _map.Contestant };
            MapCanvas.Children.Add(contestant);
            Canvas.SetLeft(contestant, renderRatio * _map.Contestant.Position.X * ratio);
            Canvas.SetTop(contestant, renderRatio * _map.Contestant.Position.Y * ratio);

            ((Button)sender).IsEnabled = false;
        }

        private double FeetToMeters(double feet)
        {
            return feet * 12 * 2.54 / 100;
        }

        private double MetersToFeet(double meters)
        {
            return meters * 100 / 2.54 / 12;
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            // If we were testing, run this code please
            if (_ledService != null)
            {
                OffButton_Click(sender, e);
                RedButton.IsEnabled =
                    BlueButton.IsEnabled =
                    GreenButton.IsEnabled = 
                    WhiteButton.IsEnabled =
                    OffButton.IsEnabled
                    = false;
            }

            if ("Pause".Equals((string)StartButton.Content))
            {
                StartButton.Content = "Resume";
                _watcher.Stop();
                await _gameService.PauseAsync();
            }
            else if ("Resume".Equals((string)StartButton.Content))
            {
                StartButton.Content = "Pause";
                _watcher.Start();
                await _gameService.ResumeAsync();
            }
            else
            {
                _beaconService.ClearBeacons();
                _watcher = new Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher();
                //_watcher.SignalStrengthFilter = new Windows.Devices.Bluetooth.BluetoothSignalStrengthFilter
                //{
                //    OutOfRangeThresholdInDBm = -80,
                //    OutOfRangeTimeout = TimeSpan.FromSeconds(30),
                //    SamplingInterval = TimeSpan.FromMilliseconds(100),
                //    InRangeThresholdInDBm = -70
                //};
                _watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                _watcher.Received += Receive_Watcher_Notification;
                _watcher.Start();

                StartButton.Content = "Pause";
#if HAS_GPIO

                _gameService = new GameService(_beaconService, new LedService(), new PushButtonService());
#else
                _gameService = new GameService(_beaconService, new FakeLedService(), new FakePushButtonService());
#endif
                _gameService.DrawBeaconRadii += _gameService_DrawBeaconRadii;
                _gameService.GameComplete += _gameService_GameComplete;
                await _gameService.StartAsync(_map);
                StopButton.IsEnabled = true;
            }
        }

        private async Task _gameService_GameComplete(Contestant winner)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // TODO: Actually display pathing sample information over the map

                var resultsText = "Captured:" + Environment.NewLine;

                foreach(var item in _gameService.Map.Contestant.VisitedPointsOfInterest)
                {
                    resultsText += item.Item1.Character + Environment.NewLine; 
                }

                ResultsText.Text = resultsText;

                StopGame_Click(this, null);
            });
        }

        private async Task _gameService_DrawBeaconRadii(IEnumerable<PointOfInterest> pointsOfInterest)
        {
            var myPoints = pointsOfInterest;
            var renderRatio = 393.701; // pixels / m

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MapCanvas.Children.ToList().ForEach(ellipse => MapCanvas.Children.Remove(ellipse));

                var ratio = Math.Min(MapViewer.ActualWidth / MapImage.DesiredSize.Width, MapViewer.ActualHeight / MapImage.DesiredSize.Height);
                foreach (var poi in myPoints)
                {
                    var point = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(poi.Color), Tag = poi };
                    MapCanvas.Children.Add(point);

                    Canvas.SetLeft(point, renderRatio * poi.Position.X * ratio);
                    Canvas.SetTop(point, renderRatio * poi.Position.Y * ratio);
                }

                foreach (var point in myPoints.Where(p => p.EstimatedRadius.HasValue))
                {
                    var ellipse = new Ellipse
                    {
                        Fill = new SolidColorBrush(point.Color),
                        Opacity = 0.20,
                        Height = renderRatio * 2 * point.EstimatedRadius.Value * ratio,
                        Width = renderRatio * 2 * point.EstimatedRadius.Value * ratio
                    };
                    MapCanvas.Children.Add(ellipse);
                    //_beaconEllipses.Add(ellipse);

                    Canvas.SetLeft(ellipse, renderRatio * (point.Position.X - point.EstimatedRadius.Value) * ratio);
                    Canvas.SetTop(ellipse, renderRatio * (point.Position.Y - point.EstimatedRadius.Value) * ratio);
                }

                var contestantEllipse = new Ellipse
                {
                    Stroke = new SolidColorBrush(_map.Contestant.Color),
                    Opacity = 0.70,
                    Height = renderRatio * 2 * _map.Contestant.CurrentEstimatedRadius * ratio,
                    Width = renderRatio * 2 * _map.Contestant.CurrentEstimatedRadius * ratio
                };
                MapCanvas.Children.Add(contestantEllipse);
                Canvas.SetLeft(contestantEllipse, renderRatio * (_map.Contestant.Position.X - _map.Contestant.CurrentEstimatedRadius) * ratio);
                Canvas.SetTop(contestantEllipse, renderRatio * (_map.Contestant.Position.Y - _map.Contestant.CurrentEstimatedRadius) * ratio);
            });
        }

        private async void StopGame_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            _watcher.Stop();
            await _gameService.StopAsync();

            _watcher.Received -= Receive_Watcher_Notification;
            _watcher = null;

            RedButton.IsEnabled =
                BlueButton.IsEnabled =
                GreenButton.IsEnabled =
                WhiteButton.IsEnabled =
                OffButton.IsEnabled
                = true;

            StartButton.Content = "Start";
        }

        private async void RedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                await _ledService.InitializeAsync();
            }

            _ledService.SetLEDColor(Colors.Red);
        }

        private async void GreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                await _ledService.InitializeAsync();
            }

            _ledService.SetLEDColor(Colors.Green);
        }

        private async void BlueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                await _ledService.InitializeAsync();
            }

            _ledService.SetLEDColor(Colors.Blue);
        }

        private async void WhiteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                await _ledService.InitializeAsync();
            }

            _ledService.SetLEDColor(Colors.White);
        }

        private void OffButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ledService != null)
            {
                _ledService.Shutdown();
                _ledService = null;
            }
        }

        private async void PushButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pushButtonService == null)
            {
                _pushButtonService = new PushButtonService();
                await _pushButtonService.InitializeAsync();
                PushButton.IsEnabled = false;

                _pushButtonService.ButtonPushed += _pushButtonService_ButtonPushed;
            }

        }

        private async void _pushButtonService_ButtonPushed(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                PushButton.IsEnabled = true;
                _pushButtonService.Shutdown();
                _pushButtonService = null;
            });
        }
    }
}
