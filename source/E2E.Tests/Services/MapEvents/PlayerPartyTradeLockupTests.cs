using Common.Network;
using Common.Network.Messages;
using E2E.Tests.Environment.Instance;
using E2E.Tests.Services.MapEvents;
using GameInterface.Services.Entity;
using GameInterface.Services.MapEvents;
using GameInterface.Services.MapEvents.Messages.Conversation;
using GameInterface.Services.MapEvents.PlayerPartyInteractions;
using GameInterface.Services.Players;
using TaleWorlds.CampaignSystem.Party;
using Xunit.Abstractions;

namespace E2E.Tests.Services.MapEvents;

public class PlayerPartyTradeLockupTests : MapEventTestBase
{
    public PlayerPartyTradeLockupTests(ITestOutputHelper output) : base(output)
    {
        foreach (var instance in Clients.Prepend(Server))
            instance.Call(() =>
            {
                PlayerPartyInteractionDialogState.Clear();
                PlayerPartyTradeContext.End();
            });
    }

    public enum TradeExit
    {
        BothAccept,
        InitiatorDisconnects,
        ResponderDisconnects,
        ResponderRequestsAgain,
        InitiatorRequestsAgain,
    }

    [Theory]
    [InlineData(TradeExit.BothAccept)]
    [InlineData(TradeExit.InitiatorDisconnects)]
    [InlineData(TradeExit.ResponderDisconnects)]
    [InlineData(TradeExit.ResponderRequestsAgain)]
    [InlineData(TradeExit.InitiatorRequestsAgain)]
    public void TradeExit_LeavesNoSessionHoldingEitherParty(TradeExit exit)
    {
        var clients = Clients.ToArray();
        var initiatorClient = clients[0];
        var responderClient = clients[1];
        initiatorClient.Resolve<IControllerIdProvider>().SetControllerId("PlayerOne");
        responderClient.Resolve<IControllerIdProvider>().SetControllerId("PlayerTwo");
        var (_, initiatorMobilePartyId) = CreatePlayerHeroParty("PlayerOne");
        var (_, responderMobilePartyId) = CreatePlayerHeroParty("PlayerTwo");
        var aiPartyId = GetPartyBaseId(TestEnvironment.CreateRegisteredObject<MobileParty>());
        Server.Call(() =>
        {
            var playerManager = Server.Resolve<IPlayerManager>();
            playerManager.SetPeer("PlayerOne", initiatorClient.NetPeer);
            playerManager.SetPeer("PlayerTwo", responderClient.NetPeer);
        });
        var initiatorPartyId = GetPartyBaseId(initiatorMobilePartyId);
        var responderPartyId = GetPartyBaseId(responderMobilePartyId);

        Request(initiatorClient, initiatorPartyId, responderPartyId);
        var sessionId = Assert.Single(Server.NetworkSentMessages.GetMessages<NetworkPlayerPartyInteractionStarted>()).SessionId;
        Submit(initiatorClient, sessionId, initiatorPartyId, PlayerPartyInteractionOption.TradeProposal);
        Submit(responderClient, sessionId, responderPartyId, PlayerPartyInteractionOption.AcceptProposal);
        Assert.Contains(Server.NetworkSentMessages.GetMessages<NetworkPlayerPartyInteractionState>(), s =>
            s.SessionId == sessionId && s.PartyId == responderPartyId && s.Phase == PlayerPartyInteractionPhase.TradeActive);
        Server.NetworkSentMessages.Clear();

        switch (exit)
        {
            case TradeExit.BothAccept:
                Accept(initiatorClient, sessionId);
                Accept(responderClient, sessionId);
                break;
            case TradeExit.InitiatorDisconnects:
                Server.SimulateMessage(this, new PlayerDisconnected(initiatorClient.NetPeer, default));
                break;
            case TradeExit.ResponderDisconnects:
                Server.SimulateMessage(this, new PlayerDisconnected(responderClient.NetPeer, default));
                break;
            case TradeExit.ResponderRequestsAgain:
                Request(responderClient, responderPartyId, initiatorPartyId);
                Accept(initiatorClient, sessionId);
                Accept(responderClient, sessionId);
                break;
            case TradeExit.InitiatorRequestsAgain:
                Request(initiatorClient, initiatorPartyId, responderPartyId);
                Accept(initiatorClient, sessionId);
                Accept(responderClient, sessionId);
                break;
        }
        Server.PumpGameThread();

        var ended = Assert.Single(Server.NetworkSentMessages.GetMessages<NetworkPlayerPartyInteractionEnded>());
        Assert.Equal(sessionId, ended.SessionId);
        Server.Call(() =>
        {
            var tracker = Server.Resolve<ConversationPartyTracker>();
            Assert.False(tracker.IsInOtherConversation(initiatorClient.NetPeer, initiatorPartyId, aiPartyId));
            Assert.False(tracker.IsInOtherConversation(responderClient.NetPeer, responderPartyId, aiPartyId));
        });
    }

    private void Request(EnvironmentInstance client, string initiatorPartyId, string responderPartyId)
    {
        client.Call(() => client.Resolve<INetwork>().SendAll(new NetworkRequestConversation(
            responderPartyId,
            initiatorPartyId,
            forcePlayerOutFromSettlement: false,
            ConversationRestartSource.PlayerEncounter,
            false,
            requestId: "trade-lockup-request")));
    }

    private static void Submit(EnvironmentInstance client, string sessionId, string partyId, PlayerPartyInteractionOption option)
        => client.Call(() => client.Resolve<INetwork>().SendAll(new NetworkSubmitPlayerPartyInteractionOption(sessionId, option, partyId)));

    private static void Accept(EnvironmentInstance client, string sessionId)
        => client.Call(() => client.Resolve<INetwork>().SendAll(new NetworkPlayerPartyTradeAcceptChanged(sessionId, accepted: true)));

    private string GetPartyBaseId(string mobilePartyId)
    {
        string partyBaseId = null;
        Server.Call(() =>
        {
            Assert.True(Server.ObjectManager.TryGetObject<MobileParty>(mobilePartyId, out var party));
            Assert.True(Server.ObjectManager.TryGetId(party.Party, out partyBaseId));
        });
        return partyBaseId;
    }
}
