using DV.Logic.Job;
using PassengerJobs.API;
using PassengerJobs.Generation;
using System;
using System.Linq;

namespace PassengerJobs.Integration
{
    internal sealed class PassengerJobsApiAdapter : IPassengerJobsApiV2
    {
        private readonly object generationGate = new object();
        private readonly System.Collections.Generic.Dictionary<string, bool> generationCommands = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal);
        private bool automaticGenerationSuspended;

        public string ApiVersion => "1.1";
        public string PassengerJobsVersion => PJMain.ModEntry.Info.Version;
        public bool CanControlAutomaticGeneration => !MultiplayerShim.IsInitialized || MultiplayerShim.IsHost;
        public bool IsAutomaticGenerationSuspended { get { lock (generationGate) return automaticGenerationSuspended; } }
        public event EventHandler<PassengerJobLifecycleEventArgs>? JobLifecycleChanged;

        public bool SetAutomaticGenerationSuspended(string operationId, bool suspended)
        {
            if (string.IsNullOrWhiteSpace(operationId) || !CanControlAutomaticGeneration) return false;
            lock (generationGate)
            {
                if (generationCommands.TryGetValue(operationId, out var prior)) return prior == suspended;
                generationCommands.Add(operationId, suspended);
                automaticGenerationSuspended = suspended;
                PJMain.Log($"PassengerJobs automatic generation policy changed: suspended={suspended}, operation={operationId}");
                return true;
            }
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

        internal void Publish(Job job, PassengerJobLifecycle lifecycle)
        {
            if (job == null || !PassJobType.IsPJType(job.jobType)) return;
            var snapshot = Snapshot(job);
            var observedPayment = lifecycle == PassengerJobLifecycle.Completed ? SafePayment(job) : 0L;
            JobLifecycleChanged?.Invoke(this, new PassengerJobLifecycleEventArgs
            {
                EventId = snapshot.JobId + ":" + lifecycle.ToString().ToLowerInvariant(),
                Lifecycle = lifecycle,
                Job = snapshot,
                ObservedPayment = observedPayment
            });
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
