# Celestriad development build

## Behavior

This extends the existing `UltDancingMad` module (territory 1363). Inspection found existing Double-trouble Trap, Forsaken, Ultima Blaster, Black Hole and Kefka Says automarkers, but no Celestriad implementation or matching cast/status IDs.

Enable Ultimate > Dancing Mad (Ultimate) > Celestriad automarker (Bind 1/2) > Enabled. It defaults to disabled and is experimental. Enable its parent category/content and the appropriate global automarker or overlay toggle. English labels fall back through the existing translation system.

Cast `0xBB42` opens a bounded collection window: cast duration plus five seconds (maximum 30 seconds). Gains of Fire `0xB56`, Lightning `0xBB6` and Ice `0xB57` must identify six distinct members of the current eight-person party. Duplicate gains do not advance the count. After a 200 ms settling interval, the six recipients must match live party status lists exactly. The two remaining actors are sorted by ascending actor ID: lower gets Bind 1, higher gets Bind 2. This order is independent of packet arrival and local party display order; actor IDs may change between encounters.

Celestriad does not automatically clear markers. Assigned Bind 1/2 remain for your in-game macro to remove; there is no ten-second removal timer, reset cleanup, late-event cleanup or partial-assignment rollback. Wipes/combat transitions, director initialization/fade-out, leaving the territory, disabling the feature, mode changes, party changes and unload reset only detection state. Duplicate-cast lockout remains (at least ten seconds after assignment), independent of marker lifetime. Incomplete or conflicting event sets are rejected before assignment.

Assignments run on the framework thread using existing marker-controller/sign conventions, without deferred queues. Occupied signs and already marked targets are left alone. Clear the previous pair with your macro before the next Celestriad; uncleared signs will cause assignment to be skipped. If only one assignment succeeds, that marker also remains for manual clearing. Soft-marker and self-mark-only modes are supported, but in-game marker macros affect hard markers; use Lemegeton's soft-marker clear controls for soft markers. Hard-marker mode requires the function-pointer/controller path; command-only mode or unavailable signatures cause assignment to be skipped.

Network callbacks do not carry a cast instance token or original packet timestamp. Bounded windows, duplicate lockout and live status confirmation reject stale/incomplete sets; they cannot establish the provenance of arbitrarily delayed, otherwise identical events. After assignment, delayed status corrections do not clear or alter markers. Existing plugin-wide combat-end/wipe marker-removal settings still operate independently; disable those settings as well if you want clearing to be entirely manual.

## Build and simulations

Use .NET 10 SDK and the repository's pinned `Dalamud.NET.Sdk/15.0.0`, with compatible Dalamud API 15 reference libraries. Windows defaults to `%APPDATA%\XIVLauncher\addon\Hooks\dev\`. The three developer-specific reference paths now use `$(DalamudLibPath)` consistently. References are read only; no installed plugin files are changed.

From the repository root:

```powershell
dotnet run --project tests/Celestriad.Tests/Celestriad.Tests.csproj
dotnet build Lemegeton/Lemegeton.csproj -c "Debug (Full)"
dotnet build Lemegeton/Lemegeton.csproj -c "Debug (Mini)"
```

For a different reference-library directory, supply a trailing separator:

```powershell
dotnet build Lemegeton/Lemegeton.csproj -c "Debug (Full)" '-p:DalamudLibPath=C:\path\to\dalamud\'
```

Linux uses the repository's `DALAMUD_HOME` convention. The simulations have no Dalamud dependency and also run in GitHub Actions; use `dotnet run`, not `dotnet test`, because this is a dependency-free executable test harness with a nonzero exit code on failure.

The simulations link the production tracker and marker-ownership rules. They cover persistence past the former ten-second cleanup boundary, 100 shuffled party/event orders, duplicates, missing/expired/pre-cast events, unrelated actors/statuses, live-status mismatches, delayed seventh recipients, status conflicts/loss, wipes, disable, party changes, invalid parties, repeated casts, manual clearing between casts, partial assignment persistence and preexisting/reassigned markers. They simulate the marker transport; they do not execute game memory or Dalamud services.

## Load separately for manual validation

1. Keep the installed plugin directory untouched. Extract the supplied development ZIP into a separate directory. It contains separate `Full` and `Mini` folders; choose one.
2. Disable the installed Lemegeton instance in Dalamud before loading the development copy, to avoid two instances responding to the same events. Do not overwrite its DLL or configuration.
3. Open Dalamud's `/xlsettings` developer plugin settings and add the absolute path to the extracted `Full/Lemegeton.dll` (or `Mini/Lemegeton.dll`) as a development plugin location. UI labels can differ by Dalamud version. Each supplied folder includes a development manifest beside the DLL. Enable/load the development plugin through the development plugin controls (`/xlplugins`).
4. Enable the feature and parents described above. Start with client-side soft markers. Use an eight-person party in territory 1363 and confirm the six debuffs, deterministic unaffected pair, then confirm markers persist past ten seconds until your macro clears them. Check wipe/retry, disable, party changes and a marker manually moved/replaced before cleanup. Hard-marker testing additionally requires available marking signatures and command-only mode disabled.
5. To return to the installed version, unload the development copy/remove its development location and re-enable the installed plugin. No automatic deployment is performed by these changes.

The user reported that the previous development build works in-game, but its automatic clearing was too early. This manual-clear revision has been checked through compilation and simulations; the agent has not performed in-game validation. Manual checks remain necessary for actual packet ordering, status visibility, marking signatures/controller behavior and loading on the user's live Dalamud version. The supplied sidecar manifest is for development loading only and does not publish an update to any plugin repository.
