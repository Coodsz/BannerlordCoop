# PR batch notes (brief 60)

## Fork PR #18 / #4153 (deployment null player agent)
Nothing left to fix. The fix is #3811 (merge 20db6a0a4), which is in `v0.1.7` and in current development. #18 only adds `DeploymentMissionControllerNullPlayerAgentTests` (one file, 55 lines), which isn't in development yet; on current development e2057ad80 it passes together with #3811's own `DeploymentSetupTeamsNullAgentPatchTests` (4/4). It can go up as a test-only PR or be closed.

## Fork PR #16 / #3989 (hideout assault leader wait)
Already raised from 30 s to 90 s on the branch (commit cadd97c45), and the wait already ends as soon as the leader loads, leaves or disconnects. Waiting for every member wasn't added because only the leader's load matters for the duel host; details in `BRANCH_NOTES.md` on `test/hideout-duel-assault-leader`. Branch tests 21/21.

## Rebased safety branches
All rebased onto upstream development e2057ad80 with no conflicts, pushed as `<name>-rebased` (originals unchanged). Each branch's E2E set was run on the rebased head. No new failures.

| Branch | E2E set | Result |
| --- | --- | --- |
| alley-request-sender-checks | Services.Alleys | 33/33 |
| clan-service-sender-checks | PlayerKingdomCreationFlowTests, Services.Clans | 129/129 |
| kingdom-request-sender-checks | PlayerKingdomCreationFlowTests | 99/99 |
| hero-request-sender-checks | HeroRequestSenderTests, PlayerSurrenderSenderTests, CompanionDismissalSyncTests | 19/19 |
| economy-request-sender-checks | MerchantTradeTests, Services.Workshops, SellPrisonersSenderTests, GarrisonDonationTests, VillagerConversationSenderTests, Inventory | 44/44 |
| battle-request-sender-checks | BattleRequestSenderTests, Services.Armies, siege leave/assault/disconnect, ExpiredClientRequestTests, VillageHostileActionTests | 197/197 |
| party-hero-request-sender-checks | PartyHeroRequestSenderTests, HeroDevelopers | 37/37 |
| row5-crafting-bandit-sender-checks | SmithyClanSupportRequestSenderTests, BanditRequestSenderTests, MerchantOutlaws | 61/61 |
| controller-id-sender-checks | ControllerIdTests, VillageHostileActionTests, Services.Locations | 166/166 |
| map-tracks-sender-check | Services.MapTracks | 6/6 |
| disband-army-cost-clan | Services.Armies | 30/30 |

`CoopUnitTests.slnf` on `fix/battle-request-sender-checks-rebased`: Common 394, Coop.Tests 1454, GameInterface.Tests 2273, IntegrationTests 177 (2 skipped), CrashReporter 19, all passed.

The rebased branches were cut before the `PR_BODY.md` commits, so those files are only on the original branches.

## Upstream PR 4326 merged into development, full E2E
Pending, see below once the run finishes.

## Upstream draft PR bodies (item 7)
Not done. This session can read upstream's git history (anonymous reads through the proxy) but not its pull requests: the GitHub API here covers only the Coodsz fork, and attaching the upstream repo with credentials would go beyond read only, which nobody asked for. So the PR bodies of 4193, 4295, 4308 to 4317, 4319, 4320 and 4325 to 4328 weren't checked for a missing "How was this tested?" section or Closes/Related links. A session with read access to upstream PRs, or `gh pr view N --repo Bannerlord-Coop-Team/BannerlordCoop --json body` on Coodsz's machine, can do it.
