using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;

namespace ThatPiHunt.Services
{
    public interface IPushButtonService
    {
        Task<bool> InitializeAsync();
        bool Shutdown();
        DateTime? LastButtonPush { get; }
        void ClearButtonPush();
        event EventHandler ButtonPushed;
    }

    public class PushButtonService : IPushButtonService
    {
        private const int PUSH_BUTTON_PIN = 26;
        private GpioPin _pushButtonPin;
        private bool _isInitialized = false;
        private DateTime? _lastButtonPush;

        public event EventHandler ButtonPushed = delegate { };

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) { return true; }

            // Set the Lightning Provider as the default if Lightning driver is enabled on the target device
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            var gpio = await GpioController.GetDefaultAsync();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                _pushButtonPin = null;
                return false;
            }

            try
            {
                _pushButtonPin = gpio.OpenPin(PUSH_BUTTON_PIN);

                // Check if input pull-up resistors are supported
                if (_pushButtonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                {
                    _pushButtonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                }
                else
                {
                    _pushButtonPin.SetDriveMode(GpioPinDriveMode.Input);
                }

                // Set a debounce timeout to filter out switch bounce noise from a button press
                _pushButtonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

                // Register for the ValueChanged event so our buttonPin_ValueChanged 
                // function is called when the button is pressed
                _pushButtonPin.ValueChanged += Button_ValueChanged;

                _isInitialized = true;
            }
            catch
            {
                // TODO: Add logging
                return false;
            }

            return true;
        }

        public bool Shutdown()
        {
            if (!_isInitialized) { return false; }

            try
            {
                _isInitialized = false;

                _pushButtonPin.ValueChanged -= Button_ValueChanged;
                _pushButtonPin.Dispose();
                _pushButtonPin = null;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void Button_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                // Button was pushed
                _lastButtonPush = DateTime.Now;
                ButtonPushed(this, EventArgs.Empty);
            }
        }

        public DateTime? LastButtonPush
        {
            get
            {
                return _lastButtonPush;
            }
        }

        public void ClearButtonPush()
        {
            _lastButtonPush = null;
        }
    }
}
