using Sarah.API.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.API.BusinessObjects
{ 
    public abstract class Animation : IDisposable
    {
        private bool _isDisposed = false;
        private Task _animationTask;
        private CancellationTokenSource _animationCancellation = new CancellationTokenSource();

        public ILamp LampDevice { get; }

        public Animation(ILamp lamp)
        {
            if (lamp == null)
            {
                throw new ArgumentNullException(nameof(lamp));
            }

            this.LampDevice = lamp;

        }

        #region IDisposable
        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                this.Stop();

                // free managed resources
                _animationCancellation.Dispose();
            }

            // free native resources if there are any.
            _animationCancellation = null;

            _isDisposed = true;
        }

        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources, but leave the other methods
        // exactly as they are.
        ~Animation()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        #endregion

        public void Start()
        {
            if (this._animationTask == null)
            {

                if (this.LampDevice.CurrentAnimation != null && this.LampDevice.CurrentAnimation != this)
                {
                    /* dann die andere Animation zuerst anhalten */
                    this.LampDevice.CurrentAnimation.Stop();
                }

                /* dann diese Animation starten */
                this._animationTask = AnimationLoopInternal(_animationCancellation.Token);
            }
        }

        public void Stop()
        {
            if (this._animationTask != null)
            {
                this._animationCancellation.Cancel();
                this._animationTask = null;
                this.LampDevice.CurrentAnimation = null;
            }
        }

        protected abstract Task AnimationLoopInternal(CancellationToken cancellationToken);
    }
}
