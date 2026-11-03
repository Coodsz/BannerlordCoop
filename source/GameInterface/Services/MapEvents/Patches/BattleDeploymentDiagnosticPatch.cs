using Common.Logging;
using GameInterface.Services.MapEvents.TroopSupply;
using HarmonyLib;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace GameInterface.Services.MapEvents.Patches;

/// <summary>
/// TEMP diagnostic for #4223 (the player side never spawns): logs the per-side conditions
/// <c>DefaultBattleMissionAgentSpawnLogic.CheckDeployment</c> reads before it spawns a side, and who reads the
/// supplier's whole reserve through <c>CoopTroopSupplier.GetAllTroops</c>. Each line is logged only when its text
/// changes, so a per-tick CheckDeployment cannot flood the log. Remove once #4223 is understood.
/// </summary>
[HarmonyPatch]
internal class BattleDeploymentDiagnosticPatch
{
    private static readonly ILogger Logger = LogManager.GetLogger<BattleDeploymentDiagnosticPatch>();
    private const int StackFrames = 8;

    // Weak keys so a finished mission or supplier never stays alive through the dedupe cache
    private static readonly ConditionalWeakTable<DefaultBattleMissionAgentSpawnLogic, HashSet<string>> LoggedStates = new();
    private static readonly ConditionalWeakTable<CoopTroopSupplier, HashSet<string>> LoggedReads = new();

    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), "CheckDeployment")]
    [HarmonyPrefix]
    private static void CheckDeploymentPrefix(DefaultBattleMissionAgentSpawnLogic __instance)
        => LogIfChanged(__instance, "CheckDeployment before");

    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), "CheckDeployment")]
    [HarmonyPostfix]
    private static void CheckDeploymentPostfix(DefaultBattleMissionAgentSpawnLogic __instance)
        => LogIfChanged(__instance, "CheckDeployment after");

    // A throw inside CheckDeployment skips its postfix and the spawn loop, so name it here without swallowing it
    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), "CheckDeployment")]
    [HarmonyFinalizer]
    private static void CheckDeploymentFinalizer(DefaultBattleMissionAgentSpawnLogic __instance, Exception __exception)
    {
        if (__exception == null || !IsEnabled()) return;

        Logger.Error(__exception, "[BattleDiag] CheckDeployment THREW: {State}", DescribeDeploymentState(__instance));
    }

    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), nameof(DefaultBattleMissionAgentSpawnLogic.SetSpawnTroops))]
    [HarmonyPrefix]
    private static void SetSpawnTroopsPrefix(
        DefaultBattleMissionAgentSpawnLogic __instance, BattleSideEnum side, bool spawnTroops, bool enforceSpawning)
        => LogIfChanged(__instance, $"SetSpawnTroops({side}, spawn={spawnTroops}, enforce={enforceSpawning}) before");

    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), nameof(DefaultBattleMissionAgentSpawnLogic.SetSpawnTroops))]
    [HarmonyPostfix]
    private static void SetSpawnTroopsPostfix(
        DefaultBattleMissionAgentSpawnLogic __instance, BattleSideEnum side, bool spawnTroops, bool enforceSpawning)
        => LogIfChanged(__instance, $"SetSpawnTroops({side}, spawn={spawnTroops}, enforce={enforceSpawning}) after");

    [HarmonyPatch(typeof(CoopTroopSupplier), nameof(CoopTroopSupplier.GetAllTroops))]
    [HarmonyPrefix]
    private static void GetAllTroopsPrefix(CoopTroopSupplier __instance)
    {
        if (!IsEnabled()) return;

        string stack = DescribeCaller(new StackTrace(1, false));
        if (!LoggedReads.GetOrCreateValue(__instance).Add(stack)) return;

        Logger.Information("[BattleDiag] {Side} GetAllTroops caller: {Stack}", __instance.Side, stack);
    }

    private static bool IsEnabled() => BattleSpawnConfig.Enabled && BattleSpawnGate.IsCoopBattleActive;

    private static void LogIfChanged(DefaultBattleMissionAgentSpawnLogic logic, string context)
    {
        if (!IsEnabled() || logic == null) return;

        string state = DescribeDeploymentState(logic);
        // Keyed on the call too, so a SetSpawnTroops call that leaves the state unchanged still shows once
        if (!LoggedStates.GetOrCreateValue(logic).Add(context + ": " + state)) return;

        Logger.Information("[BattleDiag] {Context}: {State}", context, state);
    }

    internal static string DescribeDeploymentState(DefaultBattleMissionAgentSpawnLogic logic)
    {
        if (logic == null) return "no spawn logic";

        var mission = logic.Mission;
        var builder = new StringBuilder();
        builder.Append("mode=").Append(mission?.Mode.ToString() ?? "no mission");
        for (int side = 0; side < 2; side++)
        {
            var sideEnum = (BattleSideEnum)side;
            builder.Append(" | ").Append(sideEnum).Append(':');
            var context = logic._battleSideSpawnContexts?.ElementAtOrDefault(side);
            builder.Append(" spawnActive=").Append(context?.TroopSpawnActive.ToString() ?? "n/a");
            builder.Append(" reserved=").Append(SafeReservedCount(context));
            var phase = logic._phases?.ElementAtOrDefault(side)?.FirstOrDefault();
            builder.Append(" initial=").Append(phase?.InitialSpawnNumber.ToString() ?? "n/a");
            builder.Append(" plans=[").Append(DescribeTeamPlans(logic, mission, sideEnum)).Append(']');
        }
        return builder.ToString();
    }

    // The reserved list is only built by the native constructor, so a partially built context has none
    private static string SafeReservedCount(MissionBattleSideSpawnContext context)
        => context?._reservedTroops == null ? "n/a" : context.ReservedTroopsCount.ToString();

    private static string DescribeTeamPlans(DefaultBattleMissionAgentSpawnLogic logic, Mission mission, BattleSideEnum side)
    {
        var teams = mission?.Teams;
        var plan = logic._deploymentPlan;
        if (teams == null || plan == null) return "n/a";

        return string.Join(", ", teams
            .Where(team => team.Side == side)
            .Select(team => $"{(team == mission.PlayerTeam ? "player" : team == mission.PlayerAllyTeam ? "ally" : "other")}" +
                $" agents={team.ActiveAgents.Count} planMade={plan.IsPlanMade(team)}" +
                $" reinforcementPlanMade={plan.IsReinforcementPlanMade(team)}"));
    }

    private static string DescribeCaller(StackTrace trace)
    {
        var frames = trace.GetFrames() ?? Array.Empty<StackFrame>();
        return string.Join(" <- ", frames
            .Take(StackFrames)
            .Select(frame => frame.GetMethod())
            .Where(method => method != null)
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}"));
    }
}
