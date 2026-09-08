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
            SubscriberFailuresAreIsolated();
            SaveDataIsValidatedAndDeduplicated();
            Console.WriteLine("PassengerJobs policy tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void SubscriberFailuresAreIsolated()
    {
        var delivered = 0;
        var errors = 0;
        EventHandler<PassengerJobLifecycleEventArgs> handlers = (_, __) => throw new InvalidOperationException("expected");
        handlers += (_, __) => delivered++;
        LifecycleEventDispatcher.Dispatch(new object(), handlers, new PassengerJobLifecycleEventArgs { EventId = "PJ-1:completed" }, (_, __) => errors++);
        Assert(errors == 1, "subscriber exception is reported once");
        Assert(delivered == 1, "later subscriber still receives the event");
    }

    private static void SaveDataIsValidatedAndDeduplicated()
    {
        Assert(SaveIntegrityPolicy.HasMissingReference<object>(null), "missing reference collection is rejected");
        Assert(SaveIntegrityPolicy.HasMissingReference(new object[] { new object(), null! }), "null resolved car is rejected");
        Assert(!SaveIntegrityPolicy.HasMissingReference(new[] { new object() }), "complete references are accepted");

        var existing = new[] { new SaveRow("PJ-1", "original") };
        var incoming = new[] { new SaveRow("PJ-1", "duplicate"), new SaveRow("PJ-2", "new") };
        var merged = SaveIntegrityPolicy.AppendDistinct(existing, incoming, row => row.Id);
        Assert(merged.Length == 2, "duplicate save chain is removed");
        Assert(merged[0].Value == "original" && merged[1].Id == "PJ-2", "existing chain wins and order is stable");
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

    private sealed class SaveRow
    {
        public SaveRow(string id, string value) { Id = id; Value = value; }
        public string Id { get; }
        public string Value { get; }
    }
}
