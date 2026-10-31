using Common.Network.Messages;
using Common.Util;
using Coop.Core.Client.Messages;
using Coop.Core.Server.Services.MobileParties;
using E2E.Tests.Environment.Instance;
using E2E.Tests.Util;
using GameInterface.Services.Players;
using GameInterface.Services.Players.Data;
using TaleWorlds.CampaignSystem.Party;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Connections;

/// <summary>
/// Verifies a player who rejoins as a new peer after the server parked their party still applies the
/// join campaign baseline, like a first join does.
/// </summary>
public class RejoinParkedPartyBaselineTests : SyncTestBase
{
    private const string ControllerId = "rejoining-player";
    private string? aiPartyId;

    public RejoinParkedPartyBaselineTests(ITestOutputHelper output) : base(output)
    {
        // The harness keeps an unregistered host main party, which a dedicated server never has
        foreach (var instance in Clients.Append(Server))
        {
            instance.Call(() =>
            {
                using (new AllowedThread())
                {
                    MobileParty.MainParty.IsActive = false;
                }
            });
        }
    }

    [Fact]
    public void FirstJoin_AppliesTheJoinBaseline()
    {
        EnvironmentInstance client = Clients.First();
        RegisterPlayer();
        TestEnvironment.ConnectRegisteredPlayer(client, ControllerId);

        SendJoinBaseline(client);

        Assert.True(Assert.Single(client.InternalMessages.GetMessages<JoinCampaignBaselineApplied>()).Success);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RejoinAsNewPeerAfterParking_AppliesTheJoinBaseline(bool oldDisconnectLandsLate, bool aiPartyEngagesPlayer)
    {
        EnvironmentInstance leaving = Clients.First();
        EnvironmentInstance rejoining = Clients.Last();
        string partyId = RegisterPlayer();
        TestEnvironment.ConnectRegisteredPlayer(leaving, ControllerId);
        if (aiPartyEngagesPlayer)
        {
            Server.Call(() => Server.GetRegisteredObject<MobileParty>(aiPartyId!).SetMoveEngageParty(
                Server.GetRegisteredObject<MobileParty>(partyId), MobileParty.NavigationType.Default));
            PumpAll();
        }

        if (oldDisconnectLandsLate)
        {
            TestEnvironment.ConnectRegisteredPlayer(rejoining, ControllerId);
            Server.SimulateMessage(this, new PlayerDisconnected(leaving.NetPeer, default));
            PumpAll();
        }
        else
        {
            Server.SimulateMessage(this, new PlayerDisconnected(leaving.NetPeer, default));
            PumpAll();
            TestEnvironment.ConnectRegisteredPlayer(rejoining, ControllerId);
        }
        Server.Call(() => Assert.False(Server.GetRegisteredObject<MobileParty>(partyId).IsActive));
        rejoining.Call(() => Assert.False(rejoining.GetRegisteredObject<MobileParty>(partyId).IsActive));

        SendJoinBaseline(rejoining);

        Assert.True(Assert.Single(rejoining.InternalMessages.GetMessages<JoinCampaignBaselineApplied>()).Success);
    }

    private string RegisterPlayer()
    {
        string partyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        // A live world always has active AI parties, otherwise the parked player leaves the baseline empty
        aiPartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        // Harness rosters are not mirrored to clients, so an empty roster keeps the troop XP part of the baseline out of the comparison
        foreach (var instance in Clients.Append(Server))
        {
            instance.Call(() =>
            {
                using (new AllowedThread())
                {
                    instance.GetRegisteredObject<MobileParty>(partyId).MemberRoster.Clear();
                    instance.GetRegisteredObject<MobileParty>(aiPartyId).MemberRoster.Clear();
                }
            });
        }
        Server.Call(() => Assert.True(Server.Resolve<IPlayerManager>().AddPlayer(
            new Player(ControllerId, null, partyId, null, null))));
        return partyId;
    }

    private void SendJoinBaseline(EnvironmentInstance client)
    {
        Server.Call(() => Server.Resolve<IJoinCampaignBaselineSender>().Send(client.NetPeer));
        PumpAll();
    }

    private void PumpAll()
    {
        Server.PumpGameThread();
        foreach (var client in Clients) client.PumpGameThread();
    }
}
