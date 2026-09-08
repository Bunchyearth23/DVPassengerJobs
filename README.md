# DVPassengerJobs — BDVM integration fork

This repository is a fork of [katycat5e/DVPassengerJobs](https://github.com/katycat5e/DVPassengerJobs) at base revision `9bb668cbc2f3d270d282b2b3297667f01bec3e18`.

The `bdvm-integration` branch preserves PassengerJobs gameplay and adds a small, versioned `PassengerJobs.API` assembly. The API exposes passenger-job lookup plus available, taken, completed and abandoned lifecycle observations. It exists so integrations such as BDVM can identify a PassengerJobs job and reconcile its observed payout without reflecting over PassengerJobs implementation types.

The API is observational: it does not grant another mod authority to complete jobs, alter payouts, create consists or mutate PassengerJobs saves. Consumers remain responsible for their own idempotency and must fail closed when the API version is unsupported.

## Build

Copy `Directory.Build.targets.EXAMPLE` to `Directory.Build.targets`, set the local Derail Valley paths, then build `PassengerJobs.sln`. The PassengerJobs build copies both `PassengerJobs.dll` and `PassengerJobs.API.dll` to its configured staging/install directory.

## Upstream and license

Original project and copyright: Katy Fox / [katycat5e/DVPassengerJobs](https://github.com/katycat5e/DVPassengerJobs). The upstream and fork remain under the MIT license in [LICENSE](LICENSE). BDVM-specific changes are intentionally kept on `bdvm-integration` so upstream history and the integration delta remain reviewable.
