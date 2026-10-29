using Common.Network.Messages;
using E2E.Tests.Environment.Instance;
using E2E.Tests.Util;
using GameInterface.Services.Players;
using GameInterface.Services.Players.Data;
using GameInterface.Services.Tournaments;
using GameInterface.Services.Tournaments.Data;
using GameInterface.Services.Tournaments.Messages;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Tournaments;

public class TournamentLeaveCleanupTests : SyncTestBase
{
    private const string SessionId = "leave-cleanup-session";
    private const string HostControllerId = "tournament-host";
    private const string LeaverControllerId = "tournament-leaver";

    public TournamentLeaveCleanupTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ContestantLeaving_ByExitButtonOrDisconnect_IsRemovedFromTheSession(bool disconnect)
    {
        EnvironmentInstance host = Clients.First();
        EnvironmentInstance leaver = Clients.Skip(1).First();
        string townId = TestEnvironment.CreateRegisteredObject<Town>();
        string settlementId = TestEnvironment.CreateRegisteredObject<Settlement>();
        string townPartyId = TestEnvironment.CreateRegisteredObject<PartyBase>();
        string cultureId = TestEnvironment.CreateRegisteredObject<CultureObject>();
        string troopId = TestEnvironment.CreateRegisteredObject<CharacterObject>();
        string leaverPartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();

        Server.Call(() =>
        {
            Assert.True(Server.ObjectManager.TryGetObject<Town>(townId, out var town));
            Assert.True(Server.ObjectManager.TryGetObject<Settlement>(settlementId, out var settlement));
            Assert.True(Server.ObjectManager.TryGetObject<PartyBase>(townPartyId, out var townParty));
            Assert.True(Server.ObjectManager.TryGetObject<CultureObject>(cultureId, out var culture));
            Assert.True(Server.ObjectManager.TryGetObject<CharacterObject>(troopId, out var troop));
            Assert.True(Server.ObjectManager.TryGetObject<MobileParty>(leaverPartyId, out var leaverParty));
            culture.BasicTroop = troop;
            settlement.Culture = culture;
            settlement.Town = town;
            townParty.Settlement = settlement;
            town._owner = townParty;
            Assert.True(Server.ObjectManager.TryGetId(leaverParty.LeaderHero, out var leaverHeroId));

            var playerManager = Server.Resolve<IPlayerManager>();
            Assert.True(playerManager.AddPlayer(new Player(HostControllerId, string.Empty, string.Empty, string.Empty, "host-character")));
            Assert.True(playerManager.AddPlayer(new Player(LeaverControllerId, leaverHeroId, leaverPartyId, string.Empty, "leaver-character")));
            playerManager.SetPeer(HostControllerId, host.NetPeer);
            playerManager.SetPeer(LeaverControllerId, leaver.NetPeer);
            Assert.True(leaverParty.IsActive);

            Assert.True(Server.Resolve<ITournamentSessionRegistry>().ApplySnapshot(CreateSnapshot(townId)));
        });

        Server.SimulateMessage(host.NetPeer, new NetworkTournamentMissionEntered(SessionId, 1));
        Server.SimulateMessage(leaver.NetPeer, new NetworkTournamentMissionEntered(SessionId, 1));
        long revision = 0;
        Server.Call(() =>
        {
            Assert.True(Server.Resolve<ITournamentSessionRegistry>().TryGet(SessionId, out var entered));
            revision = entered.Revision;
        });

        // The exit button sends the leave request, Alt+F4 or a crash only drops the peer
        if (disconnect)
            Server.SimulateMessage(this, new PlayerDisconnected(leaver.NetPeer, default));
        else
            Server.SimulateMessage(leaver.NetPeer, new NetworkRequestLeaveActiveTournament(SessionId, revision));

        Server.Call(() =>
        {
            Assert.True(Server.Resolve<ITournamentSessionRegistry>().TryGet(SessionId, out var left));
            Assert.Equal(HostControllerId, left.HostControllerId);
            Assert.Empty(left.SuccessorControllerIds);
            Assert.DoesNotContain(left.SpectatorControllerIds, controllerId => controllerId == LeaverControllerId);
            Assert.Single(left.Contestants, contestant => contestant.IsHuman);
            Assert.DoesNotContain(left.Contestants, contestant =>
                contestant.IsHuman && contestant.ControllerId == LeaverControllerId);

            // A disconnect also parks the party until the player rejoins, an exit button leave keeps it on the map
            Assert.True(Server.ObjectManager.TryGetObject<MobileParty>(leaverPartyId, out var leaverParty));
            Assert.Equal(!disconnect, leaverParty.IsActive);
        });
        Server.PumpGameThread();
    }

    private static TournamentSessionSnapshot CreateSnapshot(string townId)
    {
        TournamentContestantData[] contestants =
        {
            CreateContestant("host-slot", "host-character", HostControllerId, "Tournament Host"),
            CreateContestant("leaver-slot", "leaver-character", LeaverControllerId, "Tournament Leaver")
        };
        var match = new TournamentMatchData(
            "match-1",
            "round-1",
            0,
            2,
            1,
            new[]
            {
                new TournamentTeamData(
                    "team-1",
                    contestants.Select(contestant => contestant.SlotId).ToArray(),
                    0,
                    false,
                    0,
                    null)
            },
            Array.Empty<string>());

        return new TournamentSessionSnapshot(
            SessionId,
            "leave-cleanup-mission",
            townId,
            "arena",
            "prize",
            TournamentSessionPhase.AwaitingChoices,
            1,
            1,
            match.MatchId,
            null,
            Array.Empty<string>(),
            contestants,
            Array.Empty<string>(),
            Array.Empty<TournamentPlayerChoiceData>(),
            new[] { new TournamentRoundData("round-1", 0, 0, new[] { match }) },
            0,
            0,
            2,
            true,
            false,
            null);
    }

    private static TournamentContestantData CreateContestant(
        string slotId,
        string characterId,
        string controllerId,
        string displayName)
        => new(
            slotId,
            characterId,
            1,
            controllerId,
            displayName,
            true,
            false,
            true,
            characterId);
}
