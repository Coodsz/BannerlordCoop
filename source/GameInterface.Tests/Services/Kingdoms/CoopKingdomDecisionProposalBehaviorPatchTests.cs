using Common.Util;
using GameInterface.Services.Kingdoms.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using Xunit;

namespace GameInterface.Tests.Services.Kingdoms;

public class CoopKingdomDecisionProposalBehaviorPatchTests
{
    [Fact]
    public void LeaderlessVotingClan_StopsTheDailyProposals()
    {
        var kingdom = CreateKingdom(CreateClan(hasLeader: true), CreateClan(hasLeader: false));

        Assert.True(CoopKingdomDecisionProposalBehaviorPatch.HasLeaderlessVoter(kingdom));
    }

    [Fact]
    public void LeaderlessMercenaryClan_DoesNotVoteSoProposalsContinue()
    {
        var mercenary = CreateClan(hasLeader: false);
        mercenary.IsUnderMercenaryService = true;
        var kingdom = CreateKingdom(CreateClan(hasLeader: true), mercenary);

        Assert.False(CoopKingdomDecisionProposalBehaviorPatch.HasLeaderlessVoter(kingdom));
    }

    [Fact]
    public void EveryClanLed_ProposalsContinue()
    {
        var kingdom = CreateKingdom(CreateClan(hasLeader: true), CreateClan(hasLeader: true));

        Assert.False(CoopKingdomDecisionProposalBehaviorPatch.HasLeaderlessVoter(kingdom));
    }

    private static Kingdom CreateKingdom(params Clan[] clans)
    {
        var kingdom = ObjectHelper.SkipConstructor<Kingdom>();
        kingdom._clans = new MBList<Clan>(clans);
        return kingdom;
    }

    private static Clan CreateClan(bool hasLeader)
    {
        var clan = ObjectHelper.SkipConstructor<Clan>();
        clan._leader = hasLeader ? ObjectHelper.SkipConstructor<Hero>() : null;
        return clan;
    }
}
