using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace DickinsonBros.Cosmos.Runner.Services
{
    public class HostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        internal readonly CancellationTokenSource _ctsStart = new CancellationTokenSource();
        internal readonly CancellationTokenSource _ctsStopped = new CancellationTokenSource();
        internal readonly CancellationTokenSource _ctsStopping = new CancellationTokenSource();
        public HostApplicationLifetime()
        {
        }
        public void Started()
        {
            _ctsStart.Cancel();
        }
        CancellationToken IHostApplicationLifetime.ApplicationStarted => _ctsStart.Token;
        CancellationToken IHostApplicationLifetime.ApplicationStopping => _ctsStopping.Token;
        CancellationToken IHostApplicationLifetime.ApplicationStopped => _ctsStopped.Token;
        public void Dispose()
        {
            _ctsStopped.Cancel();
            _ctsStart.Dispose();
            _ctsStopped.Dispose();
            _ctsStopping.Dispose();
        }
        public void StopApplication()
        {
            _ctsStopping.Cancel();
        }
    }
}
