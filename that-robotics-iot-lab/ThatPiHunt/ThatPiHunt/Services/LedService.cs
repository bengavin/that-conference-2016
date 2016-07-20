using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.UI;

namespace ThatPiHunt.Services
{
    public class LedService
    {
        // TODO: Define which GPIO pins the color wires are attached to
        private const int LED_PIN_R = 5;
        private const int LED_PIN_G = 6;
        private const int LED_PIN_B = 13;

        private GpioPin _pinR;
        private GpioPin _pinG;
        private GpioPin _pinB;

        private Timer _timer = null;

        private bool _isInitialized = false;
        private Stack<Color> _colorStack = new Stack<Color>();
        private Color _currentColor;
        private bool _isOff;

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
                _pinR = null;
                return false;
            }

            var pinValue = GpioPinValue.High;

            try
            {
                _pinR = gpio.OpenPin(LED_PIN_R);
                _pinR.Write(pinValue);
                _pinR.SetDriveMode(GpioPinDriveMode.Output);

                _pinG = gpio.OpenPin(LED_PIN_G);
                _pinG.Write(pinValue);
                _pinG.SetDriveMode(GpioPinDriveMode.Output);

                _pinB = gpio.OpenPin(LED_PIN_B);
                _pinB.Write(pinValue);
                _pinB.SetDriveMode(GpioPinDriveMode.Output);

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

                _pinR.Write(GpioPinValue.High);
                _pinR.Dispose();
                _pinR = null;

                _pinG.Write(GpioPinValue.High);
                _pinG.Dispose();
                _pinG = null;

                _pinB.Write(GpioPinValue.High);
                _pinB.Dispose();
                _pinB = null;

            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool SetLEDColor(bool red, bool green, bool blue)
        {
            if (!_isInitialized) { return false; }

            try
            {
                _pinR.Write(red ? GpioPinValue.Low : GpioPinValue.High);
                _pinG.Write(green ? GpioPinValue.Low : GpioPinValue.High);
                _pinB.Write(blue ? GpioPinValue.Low : GpioPinValue.High);
            }
            catch
            {
                // TODO: Add logging
                return false;
            }

            return true;
        }

        public bool SetLEDColor(Color color)
        {
            _currentColor = color;

            // Attempt to set to the color they've asked for, but we need
            // to 'round' the color, because we only support 'on/off', not
            // grades of colors
            return SetLEDColor(color.R >= 128, color.G >= 128, color.B >= 128);
        }

        public bool PushLEDColor(Color color)
        {
            _colorStack.Push(_currentColor);
            return SetLEDColor(color);
        }

        public bool PopLEDColor()
        {
            if (_colorStack.Count <= 0) { return false; }
            return SetLEDColor(_colorStack.Pop());
        }

        public bool SetBlinkRate(TimeSpan blinkAtRate)
        {
            if (_isRainbow)
            {
                StopBlinking();
            }

            if (_timer == null)
            {
                _timer = new Timer(timer_Tick, null, TimeSpan.Zero, blinkAtRate);
            }
            else
            {
                _timer.Change(TimeSpan.Zero, blinkAtRate);
            }

            return true;
        }

        public bool StopBlinking()
        {
            if (_timer != null)
            {
                _timer.Change(-1, -1);
                _timer.Dispose();
                _timer = null;
            }
            _isOff = false;
            _isRainbow = false;

            return SetLEDColor(_currentColor);
        }

        public bool GoRainbow()
        {
            StopBlinking();
            _isRainbow = true;
            _timer = new Timer(rainbow_Tick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));

            return true;
        }

        byte gpioRainbow = 0x0;
        byte gpioRainbowMax = 0x7;
        private bool _isRainbow;

        private void rainbow_Tick(object state)
        {
            gpioRainbow++;
            if (gpioRainbow > gpioRainbowMax) { gpioRainbow = 0; }

            _pinR.Write((gpioRainbow & 0x1) == 0 ? GpioPinValue.High : GpioPinValue.Low);
            _pinG.Write((gpioRainbow & 0x2) == 0 ? GpioPinValue.High : GpioPinValue.Low);
            _pinB.Write((gpioRainbow & 0x4) == 0 ? GpioPinValue.High : GpioPinValue.Low);
        }

        private void timer_Tick(object state)
        {
            if (_isOff)
            {
                _isOff = false;
                SetLEDColor(_currentColor);
            }
            else
            {
                _isOff = true;
                SetLEDColor(Colors.Black);
            }
        }
    }
}
