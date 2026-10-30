using Autofac;
using Common.Messaging;
using Coop.Core.Client.Messages;
using Coop.Core.Server.Connections;
using Coop.Core.Server.Connections.Messages;
using Coop.Core.Server.Connections.States;
using Coop.Tests.Mocks;
using GameInterface.Services.Heroes.Data;
using GameInterface.Services.Heroes.Interfaces;
using GameInterface.Services.ObjectManager;
using GameInterface.Services.Players;
using GameInterface.Services.Players.Data;
using LiteNetLib;
using Moq;
using System.Linq;
using System.Runtime.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Xunit;
using Xunit.Abstractions;

namespace Coop.Tests.Server.Connections.States;

/// <summary>
/// #4095: a second joiner still in character creation while the first one sends its hero and takes its save
/// must still be created and sent its own save when its hero arrives.
/// </summary>
public class ConcurrentCharacterCreationTests
{
    private static readonly byte[] FirstHeroData = { 1 };
    private static readonly byte[] SecondHeroData = { 2 };

    private readonly ServerTestComponent serverComponent;
    private readonly IMessageBroker messageBroker;
    private readonly NetPeer firstPeer;
    private readonly NetPeer secondPeer;
    private readonly IConnectionLogic firstConnection;
    private readonly IConnectionLogic secondConnection;

    public ConcurrentCharacterCreationTests(ITestOutputHelper output)
    {
        serverComponent = new ServerTestComponent(output);
        var container = serverComponent.Container;
        messageBroker = container.Resolve<IMessageBroker>();

        var network = container.Resolve<TestNetwork>();
        firstPeer = network.CreatePeer();
        secondPeer = network.CreatePeer();
        firstConnection = container.Resolve<ConnectionLogic>(new TypedParameter(typeof(NetPeer), firstPeer));
        secondConnection = container.Resolve<ConnectionLogic>(new TypedParameter(typeof(NetPeer), secondPeer));

        SetupUnpackedHero("First", FirstHeroData);
        SetupUnpackedHero("Second", SecondHeroData);
    }

    [Fact]
    public void SecondHero_ArrivingAfterTheFirstJoinersSaveTransfer_CreatesTheSecondPlayerAndSendsItsSave()
    {
        firstConnection.SetState<CreateCharacterState>();
        secondConnection.SetState<CreateCharacterState>();

        messageBroker.Publish(firstPeer, new NetworkTransferNewHero("FirstController", FirstHeroData));
        Assert.IsType<LoadingState>(firstConnection.State);
        Assert.IsType<CreateCharacterState>(secondConnection.State);

        messageBroker.Publish(secondPeer, new NetworkTransferNewHero("SecondController", SecondHeroData));

        AssertJoined(secondPeer, secondConnection, "SecondController");
        Assert.IsType<LoadingState>(firstConnection.State);
    }

    [Fact]
    public void SecondHero_ArrivingAfterTheFirstJoinerIsOnTheMap_CreatesTheSecondPlayerAndSendsItsSave()
    {
        firstConnection.SetState<CampaignState>();
        secondConnection.SetState<CreateCharacterState>();

        messageBroker.Publish(secondPeer, new NetworkTransferNewHero("SecondController", SecondHeroData));

        AssertJoined(secondPeer, secondConnection, "SecondController");
        Assert.IsType<CampaignState>(firstConnection.State);
    }

    private void AssertJoined(NetPeer peer, IConnectionLogic connection, string controllerId)
    {
        var playerManager = serverComponent.Container.Resolve<Mock<IPlayerManager>>();
        playerManager.Verify(p => p.AddPlayer(It.Is<Player>(player => player.ControllerId == controllerId)), Times.Once);
        Assert.Single(serverComponent.TestNetwork.ImmediateSends,
            send => send.Peer == peer && send.Payload is NetworkHeroRecieved);
        Assert.IsType<LoadingState>(connection.State);
    }

    // Each joiner unpacks to its own registered hero graph, as two real character creations would
    private void SetupUnpackedHero(string name, byte[] heroData)
    {
        var objectManager = serverComponent.Container.Resolve<IObjectManager>();
        var heroInterface = serverComponent.Container.Resolve<Mock<IHeroInterface>>();
        var playerManager = serverComponent.Container.Resolve<Mock<IPlayerManager>>();

        var party = (MobileParty)FormatterServices.GetUninitializedObject(typeof(MobileParty));
        var hero = (Hero)FormatterServices.GetUninitializedObject(typeof(Hero));
        var clan = (Clan)FormatterServices.GetUninitializedObject(typeof(Clan));
        var characterObject = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
        hero._partyBelongedTo = party;
        hero._clan = clan;
        hero._characterObject = characterObject;

        Assert.True(objectManager.AddExisting($"Hero_{name}", hero));
        Assert.True(objectManager.AddExisting($"MobileParty_{name}", party));
        Assert.True(objectManager.AddExisting($"Clan_{name}", clan));
        Assert.True(objectManager.AddExisting($"CharacterObject_{name}", characterObject));

        heroInterface
            .Setup(h => h.ServerUnpackHero(It.Is<byte[]>(data => data.SequenceEqual(heroData))))
            .Returns(hero);
        var handles = new PlayerRegistrationHandles(1, 2, 3, 4, 5, 6, 7, 8, 9);
        heroInterface
            .Setup(h => h.TryGetRegistrationHandles(hero, out handles))
            .Returns(true);
        playerManager
            .Setup(p => p.AddPlayer(It.IsAny<Player>()))
            .Returns(true);
    }
}
