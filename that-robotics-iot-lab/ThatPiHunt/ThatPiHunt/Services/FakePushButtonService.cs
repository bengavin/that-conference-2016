using System;
using System.Threading.Tasks;

namespace ThatPiHunt.Services
{
    public class FakePushButtonService : IPushButtonService
    {
        private bool _isInitialized = false;
        private DateTime? _lastButtonPush;

        public event EventHandler ButtonPushed = delegate { };

        public async Task<bool> InitializeAsync()
        {
            _isInitialized = true;
            return true;
        }

        public bool Shutdown()
        {
            if (!_isInitialized) { return false; }
            _isInitialized = false;

            return true;
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
