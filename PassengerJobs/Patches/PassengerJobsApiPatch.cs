using DV.Logic.Job;
using HarmonyLib;
using PassengerJobs.API;
using PassengerJobs.Integration;

namespace PassengerJobs.Patches
{
    [HarmonyPatch]
    internal static class PassengerJobsApiPatch
    {
        [HarmonyPatch(typeof(JobsManager), nameof(JobsManager.RegisterGeneratedJob))]
        [HarmonyPostfix]
        private static void RegisterGeneratedJobPostfix(Job __0) => PJMain.Api.Publish(__0, PassengerJobLifecycle.Available);

        [HarmonyPatch(typeof(Job), nameof(Job.TakeJob))]
        [HarmonyPostfix]
        private static void TakeJobPostfix(Job __instance) => PJMain.Api.Publish(__instance, PassengerJobLifecycle.Taken);

        [HarmonyPatch(typeof(Job), nameof(Job.CompleteJob))]
        [HarmonyPrefix]
        private static void CompleteJobPrefix(Job __instance, out long __state)
        {
            try { __state = System.Convert.ToInt64(System.Math.Round(__instance.GetWageForTheJob())); }
            catch { __state = 0L; }
        }

        [HarmonyPatch(typeof(Job), nameof(Job.CompleteJob))]
        [HarmonyPostfix]
        private static void CompleteJobPostfix(Job __instance, long __state) => PJMain.Api.Publish(__instance, PassengerJobLifecycle.Completed, __state);

        [HarmonyPatch(typeof(Job), nameof(Job.AbandonJob))]
        [HarmonyPostfix]
        private static void AbandonJobPostfix(Job __instance) => PJMain.Api.Publish(__instance, PassengerJobLifecycle.Abandoned);
    }
}
