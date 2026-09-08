using System;
using System.Collections.Generic;

namespace PassengerJobs.Integration
{
    internal sealed class GenerationSuspensionPolicy
    {
        private const int MaximumRememberedOperations = 1024;
        private readonly object gate = new object();
        private readonly Dictionary<string, bool> operations = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Queue<string> operationOrder = new Queue<string>();
        private bool suspended;

        public bool IsSuspended { get { lock (gate) return suspended; } }

        public bool TrySet(string operationId, bool value)
        {
            if (string.IsNullOrWhiteSpace(operationId)) return false;

            lock (gate)
            {
                if (operations.TryGetValue(operationId, out var prior)) return prior == value;

                operations.Add(operationId, value);
                operationOrder.Enqueue(operationId);
                suspended = value;

                while (operationOrder.Count > MaximumRememberedOperations)
                    operations.Remove(operationOrder.Dequeue());

                return true;
            }
        }
    }
}
