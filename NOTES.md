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
4326's head c2ef3c800 merged into development e2057ad80 (local merge commit 2f1270f5a, not pushed upstream), built in Release, then all 8 CI shards run one after another with `.github/scripts/run-e2e-shard.sh <i> 8`, the same script CI uses. The script only prints "completed" when `dotnet test` passes.

| Shard | Test cases | Result | Time |
| --- | --- | --- | --- |
| 0 | 385 | pass | 615 s |
| 1 | 424 | pass | 540 s |
| 2 | 449 | pass | 569 s |
| 3 | 374 | pass | 498 s |
| 4 | 630 | pass | 577 s |
| 5 | 389 | pass | 551 s |
| 6 | 398 | pass | 556 s |
| 7 | 391 | pass | 637 s |

All 3440 cases passed, no failures.

## Upstream draft PR bodies (item 7)
Not done. This session can read upstream's git history (anonymous reads through the proxy) but not its pull requests: the GitHub API here covers only the Coodsz fork, and attaching the upstream repo with credentials would go beyond read only, which nobody asked for. So the PR bodies of 4193, 4295, 4308 to 4317, 4319, 4320 and 4325 to 4328 weren't checked for a missing "How was this tested?" section or Closes/Related links. A session with read access to upstream PRs, or `gh pr view N --repo Bannerlord-Coop-Team/BannerlordCoop --json body` on Coodsz's machine, can do it.

## PR-ready branches
Each `fix/<name>-rebased` is copied to `pr/<name>` with `BRANCH_NOTES.md` and `PR_BODY.md` filtered out of every commit (`git filter-branch --index-filter`, notes-only commits pruned), so the diff against development is only code and tests. The code is identical to the `-rebased` branch in every case. The originals and the `-rebased` branches are untouched.

`pr/alley-request-sender-checks` (4 commits, the first three are #4320's), `pr/clan-service-sender-checks` (1), `pr/kingdom-request-sender-checks` (1), `pr/hero-request-sender-checks` (4), `pr/economy-request-sender-checks` (4), `pr/battle-request-sender-checks` (7), `pr/party-hero-request-sender-checks` (3), `pr/row5-crafting-bandit-sender-checks` (2), `pr/controller-id-sender-checks` (1), `pr/map-tracks-sender-check` (1), `pr/disband-army-cost-clan` (1).

The `RegisterMock<ICoopClanPermissions>` line in `Coop.Tests/TestComponentBase.cs` stays in both `pr/party-hero-request-sender-checks` and `pr/row5-crafting-bandit-sender-checks`. Each branch's handlers now take `ICoopClanPermissions`, so without the line that branch's `Coop.Tests` fail to build their container on their own. Both add the identical line at the same spot, and merging the two gives one line (checked with `git merge-tree`: one `RegisterMock<ICoopClanPermissions>` in the result), so nothing needs removing whichever lands second.

## Disband test workaround once the disband fix is in
Yes, the `Campaign.Current.PlayerDefaultFaction = memberHero.Clan;` line (and its comment) in `ArmyRequestSenderTests.Disband_IsAppliedOnlyAsTheSendersOwnParty` on the battle branch can go once `fix/disband-army-cost-clan` is in. Checked locally (nothing pushed): `pr/battle-request-sender-checks` merged with `pr/disband-army-cost-clan` merges cleanly, and the merged `DisbandArmyHandler` keeps both the battle branch's sender check and the disband branch's `MainHeroSubstitutionScope`. With the two lines removed, `Services.Armies` passes 37/37, including the disband sender test. The test still sets a clan on the member hero, which the scope needs to set `PlayerDefaultFaction`. Whichever of the two lands second should drop the line; the branches themselves weren't changed.

## Controller-id change on top of 4326
Branch `pr/controller-id-sender-checks-on-4326`: 4326's head merged with development (local merge 2f1270f5a), then the `pr/controller-id-sender-checks` commit cherry-picked on top (a219216bf).

The only conflict was in `Missions/Battles/BattleHostHandler.cs`, where 4326 adds `ProcessHostRequest`, `TryHoldElection` and the preferred-host helpers at the spot the controller-id commit adds its `IsSenderController` helper. Resolved by keeping all of 4326's methods and adding `IsSenderController` after `SuccessorRank`. The participant check edits applied without conflict: in `Handle_NetworkRequestBattleHost` the `IsSenderController(requester, requesterId)` check sits in the participant check before `MarkPresent` and `TryHoldElection`, so a request is checked once when it arrives, before it can be held, and a held election only replays requests that already passed. `Handle_NetworkRequestBattleReserves` has the same check.

E2E on that branch: ControllerIdTests, Services.Locations, HostElection, PreferredHost, BattleHost and MissionReadyElection tests, 115/115.
