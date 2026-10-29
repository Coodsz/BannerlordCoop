# Coop Settings left open on the Network tab: what runs while it is open

Trace for #4024 ("game crashes when Coop Settings stays open on the Network tab for about three
minutes"). Start sha `552c3713`. The issue has no stack trace in its text; its server log and
evidence archive are behind expiring private links that could not be downloaded from this session.

## The screen and the Network tab

- `CoopOptionsUI` (`source/GameInterface/Services/UI/CoopOptions/CoopOptionsUI.cs:30-70`) and the in-game
  `CoopOptionsOverlay` (`CoopOptionsOverlay.cs:17-47`) build one `CoopOptionsVM` with every tab and one
  `CoopOptionsGauntletLayer`.
- The Network tab is `NetworkOptionsTabProvider` (`Providers/NetworkTab/NetworkOptionsTabProvider.cs:20-49`)
  with one `NetworkSection` (`Providers/NetworkTab/Sections/NetworkSection.cs`): two clamped float
  properties for the movement upload and download limits, read once from the saved options. No timer,
  task, thread, ping, port check or server query.
- `LocalMovementBandwidth` (`Providers/NetworkTab/LocalMovementBandwidth.cs:21-38`) only reads the saved
  options in its constructor.
- Nothing under `Services/UI/CoopOptions` uses `Task`, `async`, `Thread`, `ThreadPool`, `Timer`,
  `ContinueWith`, `DateTime.UtcNow` or `Stopwatch`.

## What runs every frame while the screen is open

- `CoopOptionsGauntletLayer.Tick` (`CoopOptionsGauntletLayer.cs:28-33`) ticks or cancels the keybinding
  popup on the game thread.
- Every tab's view model exists while the screen is open, whichever tab is shown. The only one with a
  live subscription is `VoiceSection` (`Providers/VoiceTab/Sections/VoiceSection.cs:39`), which refreshes on
  `IVoiceClient.StatusChanged`. That event is raised from `VoiceClient.Tick`
  (`source/GameInterface/Services/Voice/VoiceClient.cs:223-281`), which `CoopMod` calls on the game thread
  (`source/Coop/CoopMod.cs:672-674`) and only while `Coop.Running`, so not at the main menu.
- At the main menu `CoopMod`'s application tick also runs `TryApplyAutomaticCrashReports`
  (`CoopMod.cs:410-429`, retries every 1 s or 30 s), the consent and module-warning prompts and
  `TryInitializeBugReportOverlay` (`CoopMod.cs:707-726`). None of them is tied to the options screen or to
  a three-minute interval.

## Timeouts near three minutes

The only timeouts between two and five minutes in the client are bug-report collection and upload
(`BugReportService.cs:36-37`, `BugReportUploader.cs:46`, 2 minutes), which the options screen does not
start (`Providers/BugReportTab/Sections/BugReportSection.cs` only edits a checkbox), and server-side
join, shutdown and mission timeouts that a client at the main menu does not run.

## Conclusion

Nothing the Coop Settings screen or its Network tab starts runs on a timer, a background thread or a task,
so there is no code path here that fails after about three minutes. The crash needs its client log or the
crash dump from the evidence archive (`n0rt21s.rar`): the faulting module and the stack decide whether it
is a Gauntlet or engine crash (for example the float sliders in `NetworkTab.xml`) or something unrelated
that runs at the main menu.
