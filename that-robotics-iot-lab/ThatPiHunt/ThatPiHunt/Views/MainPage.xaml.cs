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
        private MapService _mapService = new MapService();
        private GameService _gameService;

        private List<UIElement> _beaconEllipses = new List<UIElement>();

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
                _watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                _watcher.Received += Receive_Watcher_Notification;
                _watcher.Start();
            }
        }

        private async void Receive_Watcher_Notification(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            await _beaconService.RegisterBeaconSightingAsync(args.Advertisement, args.BluetoothAddress, args.Timestamp, args.RawSignalStrengthInDBm);
            //var serviceUuids = "Service UUID: ";
            //if (args.Advertisement.ServiceUuids != null && args.Advertisement.ServiceUuids.Any())
            //{
            //    serviceUuids = String.Join("; ", args.Advertisement.ServiceUuids.Select(uid => uid.ToString()));
            //}

            //var manufacturerData = $"Manufacturer Data: {Environment.NewLine}";
            //if (args.Advertisement.ManufacturerData != null && args.Advertisement.ManufacturerData.Count > 0)
            //{
            //    foreach(var dataPoint in args.Advertisement.ManufacturerData)
            //    {
            //        var data = new byte[dataPoint.Data.Length];
            //        using (var reader = DataReader.FromBuffer(dataPoint.Data))
            //        {
            //            reader.ReadBytes(data);
            //        }

            //        manufacturerData += String.Format("0x{0}: {1}", dataPoint.CompanyId.ToString("X"), BitConverter.ToString(data)) + Environment.NewLine;
            //    }
            //}

            //var dataSectionData = $"Data Sections: {Environment.NewLine}";
            //if (args.Advertisement.DataSections != null && args.Advertisement.DataSections.Count > 0)
            //{
            //    foreach(var dataSection in args.Advertisement.DataSections)
            //    {
            //        var data = new byte[dataSection.Data.Length];
            //        using (var reader = DataReader.FromBuffer(dataSection.Data))
            //        {
            //            reader.ReadBytes(data);
            //        }

            //        if (dataSection.DataType == 0x16)
            //        {
            //            // This appears to be how the Gimbal Beacons send us the URL
            //            if (data.Length >= 3 && data[0] == 0xAA && data[1] == 0xFE)
            //            {
            //                if (data.Length > 4 && data[2] == 0x10 && data[4] <= 0x03)
            //                {
            //                    var prefix = new string[]
            //                    {
            //                    "http://www.",
            //                    "https://www.",
            //                    "http://",
            //                    "https://"
            //                    };

            //                    dataSectionData += $"Url Detected (Transmit Power: {data[3]}dBm): " + prefix[data[4]] + (data.Length > 5 ? System.Text.Encoding.UTF8.GetString(data, 5, data.Length - 5) : String.Empty) + Environment.NewLine;
            //                }
            //                else if (data[2] == 0x20 && data.Length == 16)
            //                {
            //                    dataSectionData += $"TLM Data Detected (Version: {data[3]}), Battery Voltage: {BitConverter.ToInt16(new[] { data[5], data[4] }, 0)}, Temperature: {data[6] + ((float)data[7] / 256f)}, Publish Count: {BitConverter.ToInt32(new[] { data[11], data[10], data[9], data[8] }, 0)}, Seconds alive: {BitConverter.ToInt32(new[] { data[15], data[14], data[13], data[12] }, 0)}{Environment.NewLine}";
            //                }
            //                else
            //                {
            //                    dataSectionData += $"Error {dataSection.DataType.ToString("X")}: {BitConverter.ToString(data)}{Environment.NewLine}";
            //                }
            //            }
            //            else
            //            {
            //                dataSectionData += $"!{dataSection.DataType.ToString("X")}: {BitConverter.ToString(data)}{Environment.NewLine}";
            //            }
            //        }
            //        else
            //        {
            //            dataSectionData += $"{dataSection.DataType.ToString("X")}: {BitConverter.ToString(data)}{Environment.NewLine}";
            //        }
            //    }
            //}

            //System.Diagnostics.Debug.WriteLine($"Notification Received: {args.AdvertisementType}, {args.BluetoothAddress}, {args.RawSignalStrengthInDBm}, {args.Timestamp}, {args.Advertisement.LocalName} {Environment.NewLine} {dataSectionData} {manufacturerData} {serviceUuids}");
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
            await new MessageDialog($"Got click on image at: {curPoint.Position.X}, {curPoint.Position.Y}").ShowAsync();
        }

        private void GenerateMap_Click(object sender, RoutedEventArgs e)
        {
            _map = new Map
            {
                Contestant = new Contestant() { Position = new Point(1200, 1200) },
                PointsOfInterest = new List<PointOfInterest>
                {
                    new PointOfInterest { Identifier = "20160809000000000000-000000000001", Position = new Windows.Foundation.Point(7.600, 0.667) },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000002", Position = new Point(2.743, 0.100) },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000003", Position = new Point(1.219, 6.706) },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000004", Position = new Point(7.315, 6.401) },
                    new PointOfInterest { Identifier = "20160809000000000000-000000000005", Position = new Point(3.962, 3.658) }
                }
            };

            //MapService.GenerateMap((int)MapCanvas.ActualWidth, (int)MapCanvas.ActualHeight, 10);

            var renderRatio = 393.701; // pixels / m
            var ratio = Math.Min(MapViewer.ActualWidth / MapImage.DesiredSize.Width, MapViewer.ActualHeight / MapImage.DesiredSize.Height);
            foreach (var poi in _map.PointsOfInterest)
            {
                var point = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(poi.Color), Tag = poi };
                MapCanvas.Children.Add(point);
                 
                Canvas.SetLeft(point, renderRatio * poi.Position.X * ratio);
                Canvas.SetTop(point, renderRatio * poi.Position.Y * ratio);
            }

            var contestant = new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.Red), Tag = _map.Contestant };
            MapCanvas.Children.Add(contestant);
            Canvas.SetLeft(contestant, renderRatio * _map.Contestant.Position.X * ratio);
            Canvas.SetTop(contestant, renderRatio * _map.Contestant.Position.Y * ratio);

            ((Button)sender).IsEnabled = false;
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
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
                _watcher.SignalStrengthFilter = new Windows.Devices.Bluetooth.BluetoothSignalStrengthFilter
                {
                    OutOfRangeThresholdInDBm = -80,
                    OutOfRangeTimeout = TimeSpan.FromSeconds(30),
                    SamplingInterval = TimeSpan.FromMilliseconds(100),
                    InRangeThresholdInDBm = -70
                };
                _watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                _watcher.Received += Receive_Watcher_Notification;
                _watcher.Start();

                StartButton.Content = "Pause";
                _gameService = new GameService(_mapService, _beaconService);
                _gameService.Start(_map);
                _gameService.DrawBeaconRadii += _gameService_DrawBeaconRadii;
                StopButton.IsEnabled = true;
            }
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

            StartButton.Content = "Start";
        }
    }
}
