# Celestriad development build

## Behavior

This extends the existing `UltDancingMad` module (territory 1363). Inspection found existing Double-trouble Trap, Forsaken, Ultima Blaster, Black Hole and Kefka Says automarkers, but no Celestriad implementation or matching cast/status IDs.

Enable Ultimate > Dancing Mad (Ultimate) > Celestriad automarker (Bind 1/2) > Enabled. It defaults to disabled and is experimental. Enable its parent category/content and the appropriate global automarker or overlay toggle. English labels fall back through the existing translation system.

Cast `0xBB42` opens a bounded collection window: cast duration plus five seconds (maximum 30 seconds). Gains of Fire `0xB56`, Lightning `0xBB6` and Ice `0xB57` must identify six distinct members of the current eight-person party. Duplicate gains do not advance the count. After a 200 ms settling interval, the six recipients must match live party status lists exactly. The two remaining actors are sorted by ascending actor ID: lower gets Bind 1, higher gets Bind 2. This order is independent of packet arrival and local party display order; actor IDs may change between encounters.

The timer uses a monotonic clock. Only successfully applied markers are tracked, and they are removed on the first framework update at or after ten seconds from assignment. Wipes/combat transitions, director initialization/fade-out, leaving the territory, disabling the feature, changing marker mode, party membership changes and plugin unload cancel the state and clean up earlier. A delayed seventh recipient invalidates an already assigned pair. A lost/conflicting status during collection cancels rather than guesses. The cast window remains locked after cancellation, so a delayed duplicate cannot reopen it; an unusually early legitimate repeat may therefore be skipped.

Markers apply and clear on the framework thread using existing marker-controller/sign conventions. They do not use the global clear-all method or the deferred marker queues. Occupied signs and already marked targets are left alone; a failed second assignment rolls back the first. Cleanup rechecks the exact actor/sign pair, preserving markers moved to other players or replaced with a different sign. Soft-marker mode and self-mark-only mode are supported. Hard-marker mode requires the function-pointer/controller path; command-only mode or unavailable signatures cause assignment to be skipped because safe ownership checks are unavailable. Cleanup of an existing pair is still attempted after a global toggle is turned off.

Network callbacks do not carry a cast instance token or original packet timestamp. Bounded windows, duplicate lockout and live status confirmation reject stale/incomplete sets; they cannot establish the provenance of arbitrarily delayed, otherwise identical events. Likewise, an external actor removing and reapplying the identical sign to the identical player cannot be distinguished by a holder check. Existing plugin-wide combat-end/wipe marker-removal settings still operate independently of this handler.

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

The simulations link the production tracker and marker-ownership rules. They cover exact cleanup boundaries, 100 shuffled party/event orders, duplicates, missing/expired/pre-cast events, unrelated actors/statuses, live-status mismatches, delayed seventh recipients, status conflicts/loss, wipes, disable, party changes, invalid parties, repeated casts, partial assignment rollback and preexisting/reassigned markers. They simulate the marker transport; they do not execute game memory or Dalamud services.

## Load separately for manual validation

1. Keep the installed plugin directory untouched. Extract the supplied development ZIP into a separate directory. It contains separate `Full` and `Mini` folders; choose one.
2. Disable the installed Lemegeton instance in Dalamud before loading the development copy, to avoid two instances responding to the same events. Do not overwrite its DLL or configuration.
3. Open Dalamud's `/xlsettings` developer plugin settings and add the absolute path to the extracted `Full/Lemegeton.dll` (or `Mini/Lemegeton.dll`) as a development plugin location. UI labels can differ by Dalamud version. Each supplied folder includes a development manifest beside the DLL. Enable/load the development plugin through the development plugin controls (`/xlplugins`).
4. Enable the feature and parents described above. Start with client-side soft markers. Use an eight-person party in territory 1363 and confirm the six debuffs, deterministic unaffected pair and ten-second cleanup. Check wipe/retry, disable, party changes and a marker manually moved/replaced before cleanup. Hard-marker testing additionally requires available marking signatures and command-only mode disabled.
5. To return to the installed version, unload the development copy/remove its development location and re-enable the installed plugin. No automatic deployment is performed by these changes.

**No in-game validation has been performed.** Compilation and event simulations are the validation available here. Manual checks remain necessary for actual packet ordering, status visibility, marking signatures/controller behavior and loading on the user's live Dalamud version. The supplied sidecar manifest is for development loading only and does not publish an update to any plugin repository.
