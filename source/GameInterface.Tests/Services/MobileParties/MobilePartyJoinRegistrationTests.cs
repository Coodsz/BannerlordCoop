using Common;
using Common.Messaging;
using Common.Util;
using GameInterface.Registry.Auto;
using GameInterface.Services.Entity;
using GameInterface.Services.MobileParties;
using GameInterface.Services.ObjectManager;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Xunit;

namespace GameInterface.Tests.Services.MobileParties;

[Collection("CampaignCurrentCollection")]
public class MobilePartyJoinRegistrationTests
{
    public enum ServerPartyShape
    {
        SaveLoaded,
        LiveCreated,
        PlayerParty,
        RenamedAfterRegistration,
        DuplicateStringId,
    }

    // A joining client loads the same save, then registers it with the server's id remap and handle map
    [Theory]
    [InlineData(ServerPartyShape.SaveLoaded)]
    [InlineData(ServerPartyShape.LiveCreated)]
    [InlineData(ServerPartyShape.PlayerParty)]
    [InlineData(ServerPartyShape.RenamedAfterRegistration)]
    [InlineData(ServerPartyShape.DuplicateStringId)]
    public void JoiningClient_ResolvesEveryServerPartyHandle(ServerPartyShape shape)
    {
        bool wasServer = ModInformation.IsServer;
        try
        {
            ModInformation.IsServer = true;
            var serverObjectManager = new ObjectManager(Mock.Of<ILogger>());
            var serverParties = new[] { CreateParty("lord_1_1_party_1"), CreateParty("looters_1") };
            var remap = new Dictionary<string, string>();

            RunWithParties(serverParties, () =>
            {
                switch (shape)
                {
                    case ServerPartyShape.SaveLoaded:
                        CreateRegistry(serverObjectManager).RegisterAllObjects();
                        break;
                    case ServerPartyShape.LiveCreated:
                        CreateRegistry(serverObjectManager).RegisterAllObjects();
                        Assert.True(serverObjectManager.Remove(serverParties[1]));
                        Assert.True(serverObjectManager.AddNewObject(serverParties[1], out _));
                        break;
                    case ServerPartyShape.PlayerParty:
                        serverParties[1].StringId = "Player";
                        Assert.True(serverObjectManager.AddExisting("MobileParty_Player", serverParties[1]));
                        CreateRegistry(serverObjectManager).RegisterAllObjects();
                        break;
                    case ServerPartyShape.RenamedAfterRegistration:
                        CreateRegistry(serverObjectManager).RegisterAllObjects();
                        serverParties[1].StringId = "looters_2";
                        break;
                    case ServerPartyShape.DuplicateStringId:
                        serverParties[1].StringId = serverParties[0].StringId;
                        CreateRegistry(serverObjectManager).RegisterAllObjects();
                        Assert.True(serverObjectManager.AddNewObject(serverParties[1], out _));
                        break;
                }

                CreateRegistry(serverObjectManager).CollectIdRemap(remap);
            });
            var handles = serverObjectManager.GetHandleMap().ToDictionary(entry => entry.Key, entry => entry.Value);

            ModInformation.IsServer = false;
            var clientObjectManager = new ObjectManager(Mock.Of<ILogger>());
            clientObjectManager.SetJoinHandleMap(handles);
            var clientParties = serverParties.Select(party => CreateParty(party.StringId)).ToArray();
            RunWithParties(clientParties, () => CreateRegistry(clientObjectManager).RegisterAllObjectsWithRemap(remap));

            for (int i = 0; i < serverParties.Length; i++)
            {
                Assert.True(serverObjectManager.TryGetHandle(serverParties[i], out var handle));
                Assert.True(clientObjectManager.TryGetObject<MobileParty>(handle, out var clientParty),
                    $"server party {i} ({serverParties[i].StringId}) has handle {handle}, which the client never registered");
                Assert.Same(clientParties[i], clientParty);
            }
        }
        finally
        {
            ModInformation.IsServer = wasServer;
        }
    }

    private static MobilePartyRegistry CreateRegistry(IObjectManager objectManager)
        => new MobilePartyRegistry(
            Mock.Of<IControllerIdProvider>(),
            Mock.Of<IMessageBroker>(),
            Mock.Of<ILogger>(),
            Mock.Of<IAutoRegistryFactory>(),
            objectManager);

    private static MobileParty CreateParty(string stringId)
    {
        var party = ObjectHelper.SkipConstructor<MobileParty>();
        party.StringId = stringId;
        return party;
    }

    private static void RunWithParties(IEnumerable<MobileParty> parties, Action action)
    {
        Campaign previousCampaign = Campaign.Current;
        try
        {
            var campaignObjectManager = new CampaignObjectManager();
            foreach (var party in parties)
                campaignObjectManager._mobileParties.Add(party);
            var campaign = ObjectHelper.SkipConstructor<Campaign>();
            campaign.CampaignObjectManager = campaignObjectManager;
            Campaign.Current = campaign;

            action();
        }
        finally
        {
            Campaign.Current = previousCampaign;
        }
    }
}
