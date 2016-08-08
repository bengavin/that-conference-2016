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
    public class FakeLedService : ILedService
    {
        private Timer _timer = null;

        private bool _isInitialized = false;
        private Stack<Color> _colorStack = new Stack<Color>();
        private Color _currentColor;
        private bool _isOff;

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) { return true; }
            _isInitialized = true;
            return true;
        }

        public bool Shutdown()
        {
            if (!_isInitialized) { return false; }

            _isInitialized = false;

            if (_timer != null)
            {
                _timer.Change(-1, -1);
                _timer.Dispose();
                _timer = null;
                _isOff = true;
                _isRainbow = false;
            }

            return true;
        }

        private bool SetLEDColor(bool red, bool green, bool blue)
        {
            if (!_isInitialized) { return false; }

            try
            {
                // TODO: Set current color?
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

        public bool IsRainbow {  get { return _isRainbow; } }

        byte gpioRainbow = 0x0;
        byte gpioRainbowMax = 0x7;
        private bool _isRainbow;

        private void rainbow_Tick(object state)
        {
            gpioRainbow++;
            if (gpioRainbow > gpioRainbowMax) { gpioRainbow = 0; }

            // TODO: Figure out how to reflect out that we're a rainbow now
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
