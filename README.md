# DVPassengerJobs — BDVM integration fork

This repository is a fork of [katycat5e/DVPassengerJobs](https://github.com/katycat5e/DVPassengerJobs) at base revision `9bb668cbc2f3d270d282b2b3297667f01bec3e18`.

The `bdvm-integration` branch preserves PassengerJobs gameplay and adds a small, versioned `PassengerJobs.API` assembly. The API exposes passenger-job lookup plus available, taken, completed and abandoned lifecycle observations. It exists so integrations such as BDVM can identify a PassengerJobs job and reconcile its observed payout without reflecting over PassengerJobs implementation types.

API 1.0 remains observational: it does not grant another mod authority to complete jobs, alter payouts, create consists or mutate PassengerJobs saves. API 1.1 adds one narrow host-only control that suspends automatic job generation before any passenger consist is created. It does not delete existing jobs or cars. Consumers remain responsible for their own idempotency and must fail closed when the API version is unsupported.

Lifecycle notifications are authoritative on single-player and multiplayer hosts only. Event IDs are stable (`jobId:lifecycle`) and duplicate native callbacks are suppressed for the lifetime of the process. Completed payments are captured immediately before native completion and subscriber failures are isolated and logged. In strict mode, fresh free consists are refused at the final spawn boundary while an existing consist is still allowed to continue onto its next passenger leg.

The fork manifest reports 5.3.0 and intentionally omits the upstream auto-update feed. Following that feed could replace the integration build with an upstream package that does not contain `PassengerJobs.API.dll`. A fork-specific update channel will be added only after runtime validation and release packaging.

## Build

Copy `Directory.Build.targets.EXAMPLE` to `Directory.Build.targets`, set the local Derail Valley paths, then build `PassengerJobs.sln`. The PassengerJobs build copies both `PassengerJobs.dll` and `PassengerJobs.API.dll` to its configured staging/install directory.

Run the dependency-free policy tests with `dotnet run --project PassengerJobs.Tests/PassengerJobs.Tests.csproj -c Release`. The Unity, save/reload, payout and host/client acceptance matrix is in [VALIDATION.md](VALIDATION.md); the build never starts the game.

## Upstream and license

Original project and copyright: Katy Fox / [katycat5e/DVPassengerJobs](https://github.com/katycat5e/DVPassengerJobs). The upstream and fork remain under the MIT license in [LICENSE](LICENSE). BDVM-specific changes are intentionally kept on `bdvm-integration` so upstream history and the integration delta remain reviewable.
