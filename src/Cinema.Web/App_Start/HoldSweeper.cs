using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Hosting;
using Autofac;
using Autofac.Core.Lifetime;
using Cinema.Core.Services;

namespace Cinema.Web
{
    /// <summary>
    /// Background loop that flips timed-out holds to Expired. Availability checks already ignore
    /// expired holds by timestamp, so this only keeps the table tidy and the admin view honest.
    /// QueueBackgroundWorkItem lets IIS delay app-pool shutdown until the current sweep finishes.
    /// </summary>
    public static class HoldSweeper
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public static void Start(IContainer container)
        {
            HostingEnvironment.QueueBackgroundWorkItem(async cancellation =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        // InstancePerRequest registrations resolve inside a scope tagged as a request.
                        using (var scope = container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag))
                        {
                            var swept = scope.Resolve<ISeatHoldService>().SweepExpired();
                            if (swept > 0)
                            {
                                Trace.TraceInformation("HoldSweeper expired {0} seat hold(s).", swept);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("HoldSweeper failed: {0}", ex);
                    }

                    try
                    {
                        await Task.Delay(Interval, cancellation);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            });
        }
    }
}
