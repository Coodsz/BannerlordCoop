using E2E.Tests.Environment.Instance;
using E2E.Tests.Environment.MockEngine;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using Xunit;
using Xunit.Abstractions;

namespace E2E.Tests.Services.MapEvents;

/// <summary>
/// #4141: the server finalizing a battle (peace or any other path) while players are still in its mission must end
/// their missions, otherwise they keep fighting a battle the server no longer has and their result is thrown away.
/// </summary>
public class MapEventFinalizedMidMissionTests : MapEventTestBase
{
    public MapEventFinalizedMidMissionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void ServerFinalizesBattle_WhilePlayersFightInItsMission_EndsTheirMissions()
    {
        var battle = CreateServerMapEvent();
        var clients = Clients.ToArray();
        var playerPartyIds = new[]
        {
            CreatePlayerHeroParty("MissionPlayerA").partyId,
            CreatePlayerHeroParty("MissionPlayerB").partyId,
        };
        foreach (var partyId in playerPartyIds)
            JoinServerPartyToSide(battle.MapEventId, partyId, BattleSideEnum.Defender);

        using var fixture = new MissionEngineFixture();
        var missions = new MockMission[clients.Length];
        for (int i = 0; i < clients.Length; i++)
            missions[i] = OpenBattleMission(fixture, clients[i], playerPartyIds[i]);

        DestroyServerMapEvent(battle.MapEventId);

        for (int i = 0; i < clients.Length; i++)
        {
            var client = clients[i];
            var mission = missions[i];
            client.Call(() =>
            {
                Assert.False(client.ObjectManager.TryGetObject<MapEvent>(battle.MapEventId, out _));
                Assert.True(mission.EndMissionCalled);
            });
        }
    }

    [Fact]
    public void ServerFinalizesBattle_LeavesAnotherBattlesMissionRunning()
    {
        var battle = CreateServerMapEvent();
        var client = Clients.First();
        var outsidePartyId = CreatePlayerHeroParty("OtherBattlePlayer").partyId;

        using var fixture = new MissionEngineFixture();
        var mission = OpenBattleMission(fixture, client, outsidePartyId);

        DestroyServerMapEvent(battle.MapEventId);

        client.Call(() => Assert.False(mission.EndMissionCalled));
    }

    private static MockMission OpenBattleMission(MissionEngineFixture fixture, EnvironmentInstance client, string mainPartyId)
    {
        MockMission? mission = null;
        client.Call(() =>
        {
            Assert.True(client.ObjectManager.TryGetObject<MobileParty>(mainPartyId, out var mainParty));
            Campaign.Current.MainParty = mainParty;
            mission = fixture.CreateMission(client);
        });
        return mission!;
    }
}
