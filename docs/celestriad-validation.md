# Celestriad validation

Validated November 1, 2026 (America/Los_Angeles), against base commit `f20fb7fb6905b0c9a5a8ae03ec4a02b4f661a053` of AlexGMCode/Lemegeton.

| Check | Result |
| --- | --- |
| SDK | .NET SDK 10.0.403, repository-pinned Dalamud.NET.Sdk 15.0.0 |
| Simulated combat harness | 23/23 scenarios passed, including 100 shuffled party/event sequences |
| Debug (Full) plugin build | Passed, 0 errors, 4847 warnings |
| Debug (Mini) plugin build | Passed, 0 errors, 4370 warnings |
| Actual Dalamud/game execution | User reported prior build works; manual-clear revision not validated in-game by the agent |

The build uses local compatible Dalamud reference assemblies as read-only inputs. Build products, dependency restoration and the SDK are confined to the development workspace. No installed plugin DLL or configuration was modified. The builds retain compiler/platform/nullability warnings; they are not warning-free builds.

The tests link production `CelestriadTracker` and `ScopedMarkerRules` source. The marker transport and combat events are simulated, with deterministic time advancement, including persistence past ten seconds and through resets, disable and party changes. The test executable returns a nonzero exit status on any failed assertion. GitHub Actions runs this same harness; a local pass is not a claim that remote CI has completed.

Manual validation remains required for actual cast/status timing, live status visibility, native controller offsets/signatures and development plugin loading. See [development and loading instructions](celestriad-development.md).
