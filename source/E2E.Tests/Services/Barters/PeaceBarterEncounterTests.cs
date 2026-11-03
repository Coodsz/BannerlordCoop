using Common.Network;
using E2E.Tests.Services.MapEvents;
using GameInterface.Services.Barters.Messages;
using GameInterface.Services.Entity;
using GameInterface.Services.MapEvents.Messages.Conversation;
using GameInterface.Services.Players;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Xunit;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Barters;

/// <summary>
/// #3294: a player at war with a lord opened a map conversation with them and offered peace, and the server rejected
/// every offer because it no longer saw the conversation the player was in.
/// </summary>
public class PeaceBarterEncounterTests : MapEventTestBase
{
    public PeaceBarterEncounterTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void PeaceOffer_InAServerApprovedMapConversation_IsNotRejectedAsInactive()
    {
        var client = Clients.First();
        var player = CreatePartyWithRegisteredLeader();
        var lord = CreatePartyWithRegisteredLeader();
        client.Resolve<IControllerIdProvider>().SetControllerId("PeacePlayer");
        RegisterAsPlayerParty("PeacePlayer", player.HeroId, player.MobilePartyId);
        Server.Resolve<IPlayerManager>().SetPeer("PeacePlayer", client.NetPeer);

        client.Call(() => client.Resolve<INetwork>().SendAll(new NetworkRequestConversation(
            lord.PartyId,
            player.PartyId,
            forcePlayerOutFromSettlement: false,
            ConversationRestartSource.PlayerEncounter,
            false)));
        Assert.Empty(Server.NetworkSentMessages.GetMessages<NetworkConversationDenied>());

        Server.NetworkSentMessages.Clear();
        client.Call(() => client.Resolve<INetwork>().SendAll(new NetworkRequestPeaceBarter(
            lord.HeroId,
            PeaceConversationContext.MapParty,
            lord.PartyId,
            Array.Empty<PeaceBarterTerm>(),
            Guid.NewGuid().ToString("N"))));

        var result = Assert.Single(Server.NetworkSentMessages.GetMessages<NetworkPeaceBarterResult>());
        Assert.NotEqual(NetworkPeaceBarterResult.InactiveEncounterReason, result.Reason);
    }

    private (string HeroId, string MobilePartyId, string PartyId) CreatePartyWithRegisteredLeader()
    {
        var mobilePartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        string? heroId = null;
        string? partyId = null;
        Server.Call(() =>
        {
            Assert.True(Server.ObjectManager.TryGetObject<MobileParty>(mobilePartyId, out var party));
            Assert.NotNull(party.LeaderHero);
            Assert.True(Server.ObjectManager.TryGetId(party.LeaderHero, out heroId));
            Assert.True(Server.ObjectManager.TryGetId(party.Party, out partyId));
        });
        return (heroId!, mobilePartyId, partyId!);
    }
}
