using DV.Logic.Job;
using PassengerJobs.API;
using PassengerJobs.Generation;
using System;
using System.Linq;

namespace PassengerJobs.Integration
{
    internal sealed class PassengerJobsApiAdapter : IPassengerJobsApiV2
    {
        private readonly GenerationSuspensionPolicy generationPolicy = new GenerationSuspensionPolicy();
        private readonly LifecyclePublicationGuard publicationGuard = new LifecyclePublicationGuard();

        public string ApiVersion => "1.1";
        public string PassengerJobsVersion => PJMain.ModEntry.Info.Version;
        public bool CanControlAutomaticGeneration => !MultiplayerShim.IsInitialized || MultiplayerShim.IsHost;
        public bool IsAutomaticGenerationSuspended => generationPolicy.IsSuspended;
        public event EventHandler<PassengerJobLifecycleEventArgs>? JobLifecycleChanged;

        public bool SetAutomaticGenerationSuspended(string operationId, bool suspended)
        {
            if (!CanControlAutomaticGeneration || !generationPolicy.TrySet(operationId, suspended)) return false;
            PJMain.Log($"[PassengerJobs.API] generation-policy suspended={suspended}, operation={operationId}");
            return true;
        }

        public bool TryGetJob(string jobId, out PassengerJobSnapshot snapshot)
        {
            snapshot = new PassengerJobSnapshot();
            if (string.IsNullOrWhiteSpace(jobId) || JobsManager.Instance == null) return false;
            var job = JobsManager.Instance.allJobs.FirstOrDefault(candidate => candidate != null && candidate.ID == jobId && PassJobType.IsPJType(candidate.jobType));
            if (job == null) return false;
            snapshot = Snapshot(job);
            return true;
        }

        internal void Publish(Job job, PassengerJobLifecycle lifecycle, long? observedPayment = null)
        {
            if (job == null || !PassJobType.IsPJType(job.jobType) || (MultiplayerShim.IsInitialized && !MultiplayerShim.IsHost)) return;
            var snapshot = Snapshot(job);
            if (!publicationGuard.TryPublish(snapshot.JobId, lifecycle))
            {
                PJMain.LogDebug($"[PassengerJobs.API] duplicate lifecycle suppressed job={snapshot.JobId}, lifecycle={lifecycle}");
                return;
            }

            var args = new PassengerJobLifecycleEventArgs
            {
                EventId = snapshot.JobId + ":" + lifecycle.ToString().ToLowerInvariant(),
                Lifecycle = lifecycle,
                Job = snapshot,
                ObservedPayment = lifecycle == PassengerJobLifecycle.Completed ? observedPayment ?? SafePayment(job) : 0L
            };

            PJMain.Log($"[PassengerJobs.API] lifecycle event={args.EventId}, state={snapshot.NativeState}, payment={args.ObservedPayment}");
            var handlers = JobLifecycleChanged;
            if (handlers == null) return;
            foreach (EventHandler<PassengerJobLifecycleEventArgs> handler in handlers.GetInvocationList())
            {
                try { handler(this, args); }
                catch (Exception ex) { PJMain.Error($"PassengerJobs.API subscriber failed for {args.EventId}", ex); }
            }
        }

        private static PassengerJobSnapshot Snapshot(Job job) => new PassengerJobSnapshot
        {
            JobId = job.ID ?? "",
            JobType = job.jobType.ToString(),
            NativeState = job.State.ToString(),
            BasePayment = Convert.ToInt64(Math.Round(job.initialWage)),
            CurrentPayment = SafePayment(job)
        };

        private static long SafePayment(Job job)
        {
            try { return Convert.ToInt64(Math.Round(job.GetWageForTheJob())); }
            catch { return 0L; }
        }
    }
}
