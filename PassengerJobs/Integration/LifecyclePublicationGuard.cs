using PassengerJobs.API;
using System.Collections.Generic;

namespace PassengerJobs.Integration
{
    internal sealed class LifecyclePublicationGuard
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, HashSet<PassengerJobLifecycle>> published = new Dictionary<string, HashSet<PassengerJobLifecycle>>();

        public bool TryPublish(string jobId, PassengerJobLifecycle lifecycle)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return false;

            lock (gate)
            {
                if (!published.TryGetValue(jobId, out var states))
                {
                    states = new HashSet<PassengerJobLifecycle>();
                    published.Add(jobId, states);
                }

                return states.Add(lifecycle);
            }
        }

        public void Clear()
        {
            lock (gate) published.Clear();
        }
    }
}
