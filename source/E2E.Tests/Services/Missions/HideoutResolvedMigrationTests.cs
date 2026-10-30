using Common.Messaging;
using Common.Util;
using E2E.Tests.Environment.Instance;
using E2E.Tests.Environment.MockEngine;
using GameInterface.Services.MapEvents.TroopSupply;
using HarmonyLib;
using Missions;
using Missions.Battles;
using Missions.Hideouts;
using Missions.Messages;
using Missions.Services.Network;
using Moq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Missions;

/// <summary>
/// #3902: a joiner promoted to host after a hideout already resolved with an attacker win (duel won, defenders
/// surrendered) must not wake and charge the leftover bandits it adopts. A live fight and a field battle still do.
/// </summary>
public sealed class HideoutResolvedMigrationTests : MissionTestEnvironment
{
    public HideoutResolvedMigrationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void PromotedAfterADuelWin_LeavesTheSurrenderedBanditsIdle()
    {
        var (mirror, formation, hideout) = AdoptDefender(HideoutPhase.Resolved, BattleSideEnum.Attacker);

        Assert.NotEqual(AgentControllerType.AI, mirror.Controller);
        Assert.False(formation?.MovementOrderSet ?? false);
        Assert.False(formation?.IsAIControlled ?? false);
        Assert.Equal(HideoutPhase.Resolved, hideout!.Native.Phase);
    }

    [Fact]
    public void PromotedDuringALiveHideoutFight_ChargesTheAdoptedBandits()
    {
        var (mirror, formation, _) = AdoptDefender(HideoutPhase.BossBattle, BattleSideEnum.None);

        Assert.Equal(AgentControllerType.AI, mirror.Controller);
        Assert.NotNull(formation);
        Assert.True(formation!.IsAIControlled);
        Assert.True(formation.MovementOrderSet);
    }

    [Fact]
    public void PromotedInAFieldBattle_ChargesTheAdoptedEnemies()
    {
        var (mirror, formation, _) = AdoptDefender(phase: null, BattleSideEnum.None);

        Assert.Equal(AgentControllerType.AI, mirror.Controller);
        Assert.NotNull(formation);
        Assert.True(formation!.IsAIControlled);
        Assert.True(formation.MovementOrderSet);
    }

    // The old host "A" ran a defender NPC that this client mirrors as an inert puppet; A leaves and this client adopts it
    private (MirrorAgent Mirror, MockFormation? Formation, CoopHideoutMissionLogic? Hideout) AdoptDefender(
        HideoutPhase? phase, BattleSideEnum winner)
    {
        using var fixture = new MissionEngineFixture();
        var newHost = Clients.First();
        SetControllerId(newHost, "B");
        MirrorAgent? mirror = null;
        MockFormation? formation = null;
        CoopHideoutMissionLogic? hideout = null;

        newHost.Call(() =>
        {
            var mission = fixture.CreateMission(newHost);
            var controller = newHost.Resolve<CoopBattleController>();
            var registry = newHost.Resolve<INetworkAgentRegistry>();
            controller.Session.TryBegin("mapEvent1");

            if (phase.HasValue)
            {
                hideout = CreateHideoutLogic(phase.Value, winner);
                mission.Shell.MissionBehaviors.Add(hideout);
            }

            var defenders = new MockTeam(BattleSideEnum.Defender);
            var bandit = mission.SpawnAgent(new AgentBuildData(Game.Current.PlayerTroop)
                .Controller(AgentControllerType.None).Team(defenders.Shell));
            Assert.True(registry.TryRegisterAgent("A", Guid.NewGuid(), bandit));

            newHost.Resolve<IMessageBroker>().Publish(this, new BattleHostMigrated("mapEvent1", "A"));

            Assert.True(AgentMirror.TryGet(bandit, out mirror));
            if (mirror!.Formation != null && MockFormation.ForShell(mirror.Formation, out var assigned))
                formation = assigned;
            GC.KeepAlive(controller);
        });

        return (mirror!, formation, hideout);
    }

    private static CoopHideoutMissionLogic CreateHideoutLogic(HideoutPhase phase, BattleSideEnum winner)
    {
        var attacker = new CoopTroopSupplier("mapEvent1", BattleSideEnum.Attacker, null, null);
        var defender = new CoopTroopSupplier("mapEvent1", BattleSideEnum.Defender, null, null);
        var logic = new CoopHideoutMissionLogic(Mock.Of<IBattleNetwork>(), Mock.Of<IMessageBroker>(),
            Mock.Of<IMissionContext>(), defender, attacker, false);
        logic.Native = new FixedPhaseController(phase);
        AccessTools.Field(typeof(CoopHideoutMissionLogic), "winner").SetValue(logic, winner);
        return logic;
    }

    private sealed class FixedPhaseController : ICoopHideoutNativeController
    {
        public FixedPhaseController(HideoutPhase phase) => Phase = phase;
        public HideoutPhase Phase { get; private set; }
        public Agent Boss => null!;
        public void Initialize(bool authority, bool migration) { }
        public void TickAuthority(float dt) { }
        public void Apply(HideoutPhase phase, Agent boss, int initialPopulation) => Phase = phase;
        public void ResumeBossBattle() { }
    }
}
