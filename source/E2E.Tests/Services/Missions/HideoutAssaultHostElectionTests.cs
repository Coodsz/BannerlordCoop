using Common.Util;
using E2E.Tests.Environment.Instance;
using E2E.Tests.Util;
using GameInterface.Services.MapEvents;
using SandBox.GauntletUI.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Missions;

/// <summary>
/// #3989: the hideout boss duel runs on the battle host with its own main agent, so the host must be
/// the player who leads the assault, not whichever player finished loading first.
/// </summary>
public class HideoutAssaultHostElectionTests : MissionTestEnvironment
{
    private const string LeaderControllerId = "ctrl-Leader";
    private const string AllyControllerId = "ctrl-Ally";

    public HideoutAssaultHostElectionTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void AllyLoadsBeforeAssaultLeader_LeaderBecomesHost()
    {
        var mapEventId = SetupHideoutAssault();
        var clients = Clients.ToArray();

        EnterBattle(clients[1], mapEventId);
        EnterBattle(clients[0], mapEventId);

        foreach (var instance in Clients.Append(Server))
            AssertHostController(instance, mapEventId, LeaderControllerId);
        AssertIsLocalHost(clients[0], mapEventId, true);
        AssertIsLocalHost(clients[1], mapEventId, false);
    }

    [Fact]
    public void AssaultLeaderLoadsFirst_LeaderBecomesHost()
    {
        var mapEventId = SetupHideoutAssault();
        var clients = Clients.ToArray();

        EnterBattle(clients[0], mapEventId);
        EnterBattle(clients[1], mapEventId);

        foreach (var instance in Clients.Append(Server))
            AssertHostController(instance, mapEventId, LeaderControllerId);
        AssertIsLocalHost(clients[0], mapEventId, true);
        AssertIsLocalHost(clients[1], mapEventId, false);
    }

    // The leader's party starts the hideout battle, the ally joins the attacker side and the bandits defend
    private string SetupHideoutAssault()
    {
        var clients = Clients.ToArray();
        SetControllerId(clients[0], LeaderControllerId);
        SetControllerId(clients[1], AllyControllerId);

        string? mapEventId = null;
        string? leaderPartyId = null;
        string? allyPartyId = null;

        Server.Call(() =>
        {
            var leader = GameObjectCreator.CreateInitializedObject<MobileParty>();
            var ally = GameObjectCreator.CreateInitializedObject<MobileParty>();
            var bandits = GameObjectCreator.CreateInitializedObject<MobileParty>();

            var mapEvent = GameObjectCreator.CreateInitializedObject<MapEvent>();
            mapEvent.MapEventVisual = ObjectHelper.SkipConstructor<GauntletMapEventVisual>();
            mapEvent.Initialize(leader.Party, bandits.Party);
            ally.Party.MapEventSide = mapEvent.AttackerSide;
            mapEvent.MapEventVisual = null;
            Campaign.Current.MapEventManager.OnMapEventCreated(mapEvent);

            Assert.True(Server.ObjectManager.TryGetId(mapEvent, out mapEventId));
            Assert.True(Server.ObjectManager.TryGetId(leader, out leaderPartyId));
            Assert.True(Server.ObjectManager.TryGetId(ally, out allyPartyId));
        }, MapEventDisabledMethods);

        foreach (var instance in Clients.Append(Server))
        {
            instance.Call(() =>
            {
                Assert.True(instance.ObjectManager.TryGetObject<MapEvent>(mapEventId!, out var mapEvent));
                mapEvent._mapEventType = MapEvent.BattleTypes.Hideout;
                Assert.True(mapEvent.IsHideoutBattle);
            });
        }

        RegisterAsPlayerParty(LeaderControllerId, CreateRegisteredObject<Hero>(), leaderPartyId!);
        RegisterAsPlayerParty(AllyControllerId, CreateRegisteredObject<Hero>(), allyPartyId!);

        Server.Call(() =>
        {
            Assert.True(Server.ObjectManager.TryGetObject<MapEvent>(mapEventId!, out var mapEvent));
            Assert.True(Server.ObjectManager.TryGetObject<MobileParty>(leaderPartyId!, out var leader));
            Assert.Same(leader.Party, mapEvent.AttackerSide.LeaderParty);
        });

        return mapEventId!;
    }

    private static void AssertHostController(EnvironmentInstance instance, string mapEventId, string expectedHost)
    {
        instance.Call(() =>
        {
            Assert.True(instance.Resolve<IBattleHostRegistry>().TryGet(mapEventId, out var assignment));
            Assert.Equal(expectedHost, assignment.HostControllerId);
        });
    }
}
