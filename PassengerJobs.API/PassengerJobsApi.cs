using System;

namespace PassengerJobs.API
{
    public enum PassengerJobLifecycle
    {
        Available,
        Taken,
        Completed,
        Abandoned,
        Expired
    }

    public sealed class PassengerJobSnapshot
    {
        public string JobId { get; set; } = "";
        public string JobType { get; set; } = "";
        public string NativeState { get; set; } = "";
        public long BasePayment { get; set; }
        public long CurrentPayment { get; set; }
    }

    public sealed class PassengerJobLifecycleEventArgs : EventArgs
    {
        public string EventId { get; set; } = "";
        public PassengerJobLifecycle Lifecycle { get; set; }
        public PassengerJobSnapshot Job { get; set; } = new PassengerJobSnapshot();
        public long ObservedPayment { get; set; }
    }

    public interface IPassengerJobsApiV1
    {
        string ApiVersion { get; }
        string PassengerJobsVersion { get; }
        bool TryGetJob(string jobId, out PassengerJobSnapshot snapshot);
        event EventHandler<PassengerJobLifecycleEventArgs>? JobLifecycleChanged;
    }

    public static class PassengerJobsApi
    {
        private static readonly object Gate = new object();
        public static IPassengerJobsApiV1? Current { get; private set; }

        public static bool TryRegister(IPassengerJobsApiV1 implementation)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            lock (Gate)
            {
                if (Current != null) return ReferenceEquals(Current, implementation);
                Current = implementation;
                return true;
            }
        }
    }
}
