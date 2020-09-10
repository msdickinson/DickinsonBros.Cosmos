using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading;

namespace DickinsonBros.Cosmos.Runner.Services
{
    public class ApplicationLifetime : IApplicationLifetime, IDisposable
    {
        internal readonly CancellationTokenSource _ctsStart = new CancellationTokenSource();
        internal readonly CancellationTokenSource _ctsStopped = new CancellationTokenSource();
        internal readonly CancellationTokenSource _ctsStopping = new CancellationTokenSource();
        public ApplicationLifetime()
        {
        }
        public void Started()
        {
            _ctsStart.Cancel();
        }
        CancellationToken IApplicationLifetime.ApplicationStarted => _ctsStart.Token;
        CancellationToken IApplicationLifetime.ApplicationStopping => _ctsStopping.Token;
        CancellationToken IApplicationLifetime.ApplicationStopped => _ctsStopped.Token;
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
