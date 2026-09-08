using PassengerJobs.API;
using System;

namespace PassengerJobs.Integration
{
    internal static class LifecycleEventDispatcher
    {
        public static void Dispatch(
            object sender,
            EventHandler<PassengerJobLifecycleEventArgs>? handlers,
            PassengerJobLifecycleEventArgs args,
            Action<string, Exception> onError)
        {
            if (handlers == null) return;

            foreach (EventHandler<PassengerJobLifecycleEventArgs> handler in handlers.GetInvocationList())
            {
                try { handler(sender, args); }
                catch (Exception exception) { onError(args.EventId, exception); }
            }
        }
    }
}
