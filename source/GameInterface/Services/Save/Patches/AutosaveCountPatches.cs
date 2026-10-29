using GameInterface.Configuration;
using HarmonyLib;
using System;
using TaleWorlds.Core;

namespace GameInterface.Services.Save.Patches;

/// <summary>
/// Rotates autosaves through the host's configured number of slots instead of vanilla's fixed three. Like vanilla,
/// a slot is only ever overwritten, so lowering the count never deletes an autosave or any other save.
/// </summary>
[HarmonyPatch(typeof(MBSaveLoad))]
internal class AutosaveCountPatches
{
    internal const int MinAutosaveCount = 1;
    internal const int MaxAutosaveCount = 20;

    private static int AutosaveCount => ClampAutosaveCount(ModConfigProvider.ModOptions.AutosaveCount);

    [HarmonyPatch(nameof(MBSaveLoad.IncrementAutoSaveIndex))]
    [HarmonyPrefix]
    private static bool IncrementAutoSaveIndexPrefix()
    {
        MBSaveLoad.AutoSaveIndex = NextAutosaveIndex(MBSaveLoad.AutoSaveIndex, AutosaveCount);
        return false;
    }

    [HarmonyPatch(nameof(MBSaveLoad.InitializeAutoSaveIndex))]
    [HarmonyPrefix]
    private static bool InitializeAutoSaveIndexPrefix(string saveName)
    {
        string autosaveName = string.Empty;
        if (saveName.StartsWith(MBSaveLoad.AutoSaveNamePrefix))
        {
            autosaveName = saveName;
        }
        else
        {
            foreach (string fileName in MBSaveLoad.GetSaveFileNames())
            {
                if (!fileName.StartsWith(MBSaveLoad.AutoSaveNamePrefix)) continue;
                autosaveName = fileName;
                break;
            }
        }

        if (string.IsNullOrEmpty(autosaveName))
            MBSaveLoad.AutoSaveIndex = 1;
        else if (TryGetAutosaveSlot(autosaveName, MBSaveLoad.AutoSaveNamePrefix, AutosaveCount, out var slot))
            MBSaveLoad.AutoSaveIndex = slot;
        return false;
    }

    [HarmonyPatch(nameof(MBSaveLoad.IsSaveFileNameReserved))]
    [HarmonyPrefix]
    private static bool IsSaveFileNameReservedPrefix(string name, ref bool __result)
    {
        __result = TryGetAutosaveSlot(name, MBSaveLoad.AutoSaveNamePrefix, AutosaveCount, out _);
        return false;
    }

    internal static int ClampAutosaveCount(int count) => Math.Max(MinAutosaveCount, Math.Min(MaxAutosaveCount, count));

    internal static int NextAutosaveIndex(int currentIndex, int autosaveCount) =>
        currentIndex >= autosaveCount ? 1 : currentIndex + 1;

    // A slot above the count is left alone, the same as vanilla treats a saveauto4 it never writes
    internal static bool TryGetAutosaveSlot(string saveName, string autosavePrefix, int autosaveCount, out int slot)
    {
        slot = 0;
        return saveName != null && saveName.StartsWith(autosavePrefix) &&
            int.TryParse(saveName.Substring(autosavePrefix.Length), out slot) &&
            slot > 0 && slot <= autosaveCount;
    }
}
