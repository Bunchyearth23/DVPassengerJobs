# PassengerJobs integration validation

Automated builds and policy tests cover deterministic behavior that does not require Unity. The following checks require a real Derail Valley session and are intentionally not started by the build.

## Preconditions

- Install matching outputs of `PassengerJobs.dll`, `PassengerJobs.API.dll`, `PassengerJobs.MP.dll` and, when Skin Manager is present, `PassengerJobs.Skins.dll`.
- Install the matching `BDVM.PassengerJobsBridge` build and enable its passenger economy and strict world-population settings.
- Preserve a backup save. Run the single-player cases first, then repeat the multiplayer cases with one host and one client.
- Filter the Unity Mod Manager log for `[PassengerJobs.API]`. Every lifecycle line contains a stable event ID and completed events contain the wage captured before native completion.

## Single-player matrix

1. With strict population disabled, enter a passenger station generation radius. Verify one consist is created and exactly one `available` event is logged for its job.
2. Take, complete and separately abandon passenger jobs. Verify exactly one `taken`, `completed` or `abandoned` event per job/lifecycle. Confirm the completed payment equals the amount credited by Derail Valley.
3. Save with one available and one taken passenger job, reload, then finish the taken job. Verify both chains and their original car GUIDs survive; no duplicate save chain or replacement consist appears.
4. Enable strict population before entering a new station radius. Verify the generation policy is accepted and no new passenger consist appears. Complete an already existing passenger leg and verify the same car GUIDs continue to its next leg.
5. Repeat generation with DVLangHelper enabled and with Skin Manager enabled. Verify station names/signs resolve and the selected consist skin is cleared after each spawn.

## Multiplayer and reconnection matrix

1. Host a session and join with a client. Verify only the host logs authoritative lifecycle events and only the host can change automatic generation policy.
2. Take and complete a job while both peers observe it. Verify a single host completion event and matching payout; the client must not settle a duplicate.
3. Disconnect and reconnect the client twice. Generate another job and change a platform state. Verify each sign/platform update occurs once and readiness is released after station data arrives.
4. Save on the host, restart the session, reconnect the client and finish the restored job. Verify job ID, consist GUIDs, lifecycle event ID and payout remain consistent.
5. Enable strict population on the host. Verify neither peer causes a fresh consist to spawn; verify a client-side suspension request is refused.

Record the game version, Multiplayer API versions, mod hashes, relevant log excerpt, save used and observed wallet delta. Runtime validation is complete only when all rows pass without duplicate lifecycle events, orphaned cars or free replacement consists.
