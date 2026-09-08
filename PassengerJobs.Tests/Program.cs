using PassengerJobs.API;
using PassengerJobs.Integration;
using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            GenerationCommandsAreIdempotent();
            LifecycleEventsAreIdempotentPerJobAndState();
            Console.WriteLine("PassengerJobs policy tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void GenerationCommandsAreIdempotent()
    {
        var policy = new GenerationSuspensionPolicy();
        Assert(!policy.IsSuspended, "generation starts enabled");
        Assert(policy.TrySet("strict:on", true), "first suspension succeeds");
        Assert(policy.IsSuspended, "suspension is visible");
        Assert(policy.TrySet("strict:on", true), "exact replay succeeds");
        Assert(!policy.TrySet("strict:on", false), "conflicting replay is refused");
        Assert(policy.IsSuspended, "conflicting replay does not mutate state");
        Assert(policy.TrySet("strict:off", false), "new operation resumes generation");
        Assert(!policy.IsSuspended, "resume is visible");
        Assert(!policy.TrySet("", true), "empty operation is refused");
    }

    private static void LifecycleEventsAreIdempotentPerJobAndState()
    {
        var guard = new LifecyclePublicationGuard();
        Assert(guard.TryPublish("PJ-1", PassengerJobLifecycle.Available), "available is published once");
        Assert(!guard.TryPublish("PJ-1", PassengerJobLifecycle.Available), "duplicate available is suppressed");
        Assert(guard.TryPublish("PJ-1", PassengerJobLifecycle.Taken), "taken follows available");
        Assert(guard.TryPublish("PJ-1", PassengerJobLifecycle.Completed), "completed follows taken");
        Assert(!guard.TryPublish("PJ-1", PassengerJobLifecycle.Completed), "duplicate completion is suppressed");
        Assert(guard.TryPublish("PJ-2", PassengerJobLifecycle.Available), "another job has independent state");
        Assert(!guard.TryPublish(" ", PassengerJobLifecycle.Available), "empty job id is refused");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Assertion failed: " + message);
    }
}
