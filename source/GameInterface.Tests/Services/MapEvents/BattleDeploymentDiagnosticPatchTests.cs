using System.Collections.Generic;
using Common.Util;
using GameInterface.Services.MapEvents.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace GameInterface.Tests.Services.MapEvents;

public class BattleDeploymentDiagnosticPatchTests
{
    [Fact]
    public void DescribeDeploymentState_NullLogic_DoesNotThrow()
    {
        Assert.Equal("no spawn logic", BattleDeploymentDiagnosticPatch.DescribeDeploymentState(null));
    }

    [Fact]
    public void DescribeDeploymentState_UnbuiltLogic_ReportsEverySideAsUnavailable()
    {
        var logic = ObjectHelper.SkipConstructor<DefaultBattleMissionAgentSpawnLogic>();

        string state = BattleDeploymentDiagnosticPatch.DescribeDeploymentState(logic);

        Assert.Equal(
            "mode=no mission | Defender: spawnActive=n/a reserved=n/a initial=n/a plans=[n/a]" +
            " | Attacker: spawnActive=n/a reserved=n/a initial=n/a plans=[n/a]",
            state);
    }

    [Fact]
    public void DescribeDeploymentState_EmptySides_DoesNotThrow()
    {
        var logic = ObjectHelper.SkipConstructor<DefaultBattleMissionAgentSpawnLogic>();
        AccessTools.Field(typeof(DefaultBattleMissionAgentSpawnLogic), "_phases").SetValue(logic,
            new[] { new List<MissionSpawnPhase>(), new List<MissionSpawnPhase>() });
        AccessTools.Field(typeof(DefaultBattleMissionAgentSpawnLogic), "_battleSideSpawnContexts").SetValue(logic,
            new[] { ObjectHelper.SkipConstructor<MissionBattleSideSpawnContext>(), null });

        string state = BattleDeploymentDiagnosticPatch.DescribeDeploymentState(logic);

        Assert.Equal(
            "mode=no mission | Defender: spawnActive=False reserved=n/a initial=n/a plans=[n/a]" +
            " | Attacker: spawnActive=n/a reserved=n/a initial=n/a plans=[n/a]",
            state);
    }
}
