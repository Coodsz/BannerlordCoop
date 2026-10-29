# Configurable number of autosaves (#4067): where the limit lives and the planned change

Start sha `e2fdce02`. Implemented on this branch as planned below; see "Overlap with #4005".

## Where the limit of 3 lives

The limit is vanilla, in `TaleWorlds.Core.MBSaveLoad` (1.4.8):
- `MaxNumberOfAutoSaveFiles = 3`.
- `IncrementAutoSaveIndex` wraps `AutoSaveIndex` back to 1 after 3.
- `InitializeAutoSaveIndex` only accepts an existing `saveautoN` with `N <= 3`.
- `IsSaveFileNameReserved` reserves `saveauto1` to `saveauto3`.
- `AutoSaveCurrentGame` increments the index and overwrites `saveauto<index>` in place.

Coop has no autosave limit of its own. Its save code (`GameInterface/Services/Save`, `SaveHandler.ForceAutoSave` in
`SaveDebugCommand`) goes through this rotation.

Vanilla never deletes an autosave. It only overwrites the next slot. So lowering the count should delete nothing:
slots above the new count stop being written and stay on disk as ordinary saves. That keeps the guarantee of never
deleting a non-autosave or the current save.

## Planned change

1. **Option.** Follow how `KingdomDecisionVoteTimeoutSeconds` was added (`43cba2e4b`, `7b2913e26`):
   - `ModOptionsData.AutosaveCount` (`int?`, `ModConfigData.cs`);
   - `ModOptions.AutosaveCount` with `[ProtoMember(29)]` and default 3, mapped in
     `ModOptions(ModOptionsData)` (`ModConfigProvider.cs`);
   - `"autosaveCount": 3` with a comment in `deploy/mod-config.default.json`.
   - Number 29 avoids the 28 that #4005 already uses.
2. **Bounds.** Clamp to 1 to 20 where the option is read, so a bad value falls back into range.
3. **Rotation.** A new `MBSaveLoadAutosaveCountPatches` (Harmony, new file under `GameInterface/Services/Save/Patches`).
   It replaces the literal 3 in `IncrementAutoSaveIndex`, `InitializeAutoSaveIndex` and `IsSaveFileNameReserved` with
   the clamped option, and runs on whichever side writes the autosave.
4. **Tests.**
   - Rotation with the default (1, 2, 3, 1) and with 5 (1 to 5, then 1).
   - Lowering the count from 5 to 2 wraps at 2 without touching the existing `saveauto3` to `saveauto5`.
   - The clamp: 0 and 21 resolve to 1 and 20.
   - `IsSaveFileNameReserved` follows the count.
   - The new `ModOptions` member round-trips through the config serialization test.

## Overlap with #4005

garrettluskey's open #4005 edits the same config files at the same spots: `ModConfigData.cs`, `ModConfigProvider.cs`
(it adds `[ProtoMember(28)]` `KingdomDecisionMissionHoldLimitMinutes`), `deploy/mod-config.default.json` and
`Coop.IntegrationTests/Serialization/ModConfigNetworkMessageSerializationTest.cs`. This branch uses `ProtoMember(29)`,
so the wire tags don't clash once both merge. The text will conflict in those files, and resolving it means keeping
both options. The patch file and its tests touch no maintainer file.
