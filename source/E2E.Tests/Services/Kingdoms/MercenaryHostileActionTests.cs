using E2E.Tests.Environment.Instance;
using E2E.Tests.Services.MapEvents;
using GameInterface.Services.Entity;
using GameInterface.Services.Players;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using Xunit.Abstractions;

namespace E2E.Tests.Services.Kingdoms;

public class MercenaryHostileActionTests : MapEventTestBase
{
    private const string ControllerId = "mercenary-player";

    public MercenaryHostileActionTests(ITestOutputHelper output) : base(output) { }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MercenaryPlayer_AfterHostileEncounterAtPeace_ClientSeesTheServersJoinState(bool attack)
    {
        var client = Clients.First();
        client.Resolve<IControllerIdProvider>().SetControllerId(ControllerId);
        var playerPartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        var employerPartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        var targetPartyId = TestEnvironment.CreateRegisteredObject<MobileParty>();
        var employerId = TestEnvironment.CreateRegisteredObject<Kingdom>();
        var targetKingdomId = TestEnvironment.CreateRegisteredObject<Kingdom>();
        string playerHeroId = null;
        Server.Call(() => Assert.True(Server.ObjectManager.TryGetId(Get<MobileParty>(Server, playerPartyId).LeaderHero, out playerHeroId)));
        RegisterAsPlayerParty(ControllerId, playerHeroId, playerPartyId);
        Server.Call(() => Server.Resolve<IPlayerManager>().SetPeer(ControllerId, client.NetPeer));

        Server.Call(() =>
        {
            var player = Get<MobileParty>(Server, playerPartyId);
            var employer = Get<Kingdom>(Server, employerId);
            var targetKingdom = Get<Kingdom>(Server, targetKingdomId);
            // Stance lookups order factions by id, which the test objects never get from the object manager
            employer.Id = new MBGUID(11);
            targetKingdom.Id = new MBGUID(12);
            player.LeaderHero.Clan.Id = new MBGUID(13);
            Get<MobileParty>(Server, employerPartyId).LeaderHero.Clan.Kingdom = employer;
            employer.RulingClan = Get<MobileParty>(Server, employerPartyId).LeaderHero.Clan;
            Get<MobileParty>(Server, targetPartyId).LeaderHero.Clan.Kingdom = targetKingdom;
            targetKingdom.RulingClan = Get<MobileParty>(Server, targetPartyId).LeaderHero.Clan;
            Campaign.Current.Kingdoms.Add(employer);
            Campaign.Current.Kingdoms.Add(targetKingdom);
            player.LeaderHero.Clan.Tier = 2;
            CharacterRelationManager.SetHeroRelation(player.LeaderHero, employer.Leader, 71);
            CharacterRelationManager.SetHeroRelation(player.LeaderHero, targetKingdom.Leader, 71);
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(player.LeaderHero.Clan, employer, default, 50, true);
            Assert.True(player.LeaderHero.Clan.IsUnderMercenaryService);
            Assert.False(FactionManager.IsAtWarAgainstFaction(player.MapFaction, targetKingdom));

            if (attack)
                BeHostileAction.ApplyEncounterHostileAction(player.Party, Get<MobileParty>(Server, targetPartyId).Party);
        }, MapEventDisabledMethods);

        TestEnvironment.FlushCoalescer();
        Server.PumpGameThread();
        foreach (var instance in Clients) instance.PumpGameThread();

        var serverState = ReadJoinState(Server, playerPartyId, employerId, targetKingdomId);
        var clientState = ReadJoinState(client, playerPartyId, employerId, targetKingdomId);
        Assert.Equal(serverState, clientState);
        if (!attack)
            Assert.Equal((employerId, true, false, false, 71, 71), serverState);
    }

    private static (string KingdomId, bool IsMercenary, bool AtWarWithEmployer, bool AtWarWithTarget, int EmployerLeaderRelation, int TargetLeaderRelation) ReadJoinState(
        EnvironmentInstance instance, string playerPartyId, string employerId, string targetKingdomId)
    {
        (string, bool, bool, bool, int, int) state = default;
        instance.Call(() =>
        {
            var clan = Get<MobileParty>(instance, playerPartyId).LeaderHero.Clan;
            var employer = Get<Kingdom>(instance, employerId);
            var targetKingdom = Get<Kingdom>(instance, targetKingdomId);
            string kingdomId = null;
            if (clan.Kingdom != null) Assert.True(instance.ObjectManager.TryGetId(clan.Kingdom, out kingdomId));
            state = (
                kingdomId,
                clan.IsUnderMercenaryService,
                clan.IsAtWarWith(employer),
                clan.IsAtWarWith(targetKingdom),
                CharacterRelationManager.GetHeroRelation(clan.Leader, employer.Leader),
                CharacterRelationManager.GetHeroRelation(clan.Leader, targetKingdom.Leader));
        });
        return state;
    }

    private static T Get<T>(EnvironmentInstance instance, string id) where T : class
    {
        Assert.True(instance.ObjectManager.TryGetObject<T>(id, out var value));
        return value;
    }
}
