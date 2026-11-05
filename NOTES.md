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

## Tech debt review of the pr/ branches (item 4)
Run with `.agents/skills/tech-debt-review` (scan.sh plus the fit, defensive, flow and tests lenses, each finding re-checked against the branch head). Four read-only review passes ran in parallel over the eleven branches. Nothing was fixed. No decompiler here, so nothing below depends on vanilla code that isn't in the repo. Below are the real findings; "looks fine" items are left out.

### pr/battle-request-sender-checks
- **Regression, fix before merge (verified).** `ArmyHandler.IsSenderSpendingOwnInfluence` (`ArmyHandler.cs:282`, `:299-305`) only accepts an influence change for the sender's own clan. Both mercenary dismissal paths in `MercenaryServiceConversationPatch.cs` (`:105` without paying, `:114` with paying) publish `ChangeClanInfluence` for `Hero.OneToOneConversationHero.Clan`, the mercenary's clan, to clear its influence. That is now refused, so firing a mercenary no longer clears the mercenary clan's influence on the server. Not a one-line fix: on the "without paying" path the dismissal message reaches the server before the influence message, so by then the clan is no longer a mercenary in the sender's kingdom. Likely fix: have the server's dismissal apply (`MercenaryServiceHandler`) clear the mercenary clan's influence itself and stop relying on the client's `ChangeClanInfluence` for another clan.
- Fix before merge: `SiegeAssaultLeaveTests.SetMainParty` (`:909-926`) and `SiegeLeaveMenuTests.SetMainParty` (`:798-816`) register a `leaver-<partyId>` player and call `SetPeer`, which silently re-links a client that a test already connected as `PlayerOne` to a hero-less player. Skip the registration when a player already owns the party.
- Follow-up: `IsAllowedArmyMemberChange` (`ArmyHandler.cs:287-296`) and `IsPeerInMapEvent` (`MapEventHandler.cs:275-280`) use `TryGetPlayer(peer)` instead of `IsSenderControllingParty`'s current-controller rule, and `ArmyHandler.cs:290` refuses without a log line.
- Follow-up: drop the `PlayerDefaultFaction` workaround in the disband test once `pr/disband-army-cost-clan` is in (checked above).
- Open questions: the negative-amount guards on cohesion and influence only matter for a tampered client, and the influence one would also refuse a clan with negative influence. Break-siege and leave refusals send no reply, although a `Rejected` outcome exists.

### pr/disband-army-cost-clan
- Nothing material. Follow-up only: once both disband changes are in, `ArmyDisbandCostTests` and the battle branch's disband sender test overlap.

### pr/economy-request-sender-checks
- Fix before merge: identical `IsSenderOwner(object, Workshop)` helpers in `ChangeProductionTypeOfWorkshopHandler.cs:73` and `WorkshopDataHandler.cs:247`, plus the same shape in `SellPrisonersHandler.cs:85` (and in the hero and alley branches). Add `IsSenderControllingHero(object, Hero)` and `IsSenderControllingParty(object, PartyBase)` overloads to `IPlayerManager` and use them everywhere.
- Follow-up: four more copies of the "create party, AddPlayer, SetPeer" E2E helper; one shared helper on `E2ETestEnvironment` would do.
- Open question: `ChangeProductionTypeOfWorkshopHandler.cs:60-66` forces `ignoreCost = false` for peer requests, so the message's `IgnoreCost` field is now never read. Confirm every vanilla client path really pays (needs a decompile), then drop the field.

### pr/hero-request-sender-checks
- Fix before merge: `TeleportHeroHandler.CanSenderTeleport` (`:89-97`) copies the client gate in `TeleportHeroActionPatches.cs:26-27`. Move it into `ICoopClanPermissions` so the two can't drift.
- Fix before merge: the take hero, companion rejoin, take lord prisoner, lord helped and teleport tests in `HeroRequestSenderTests.cs` (`:58,66,90,98,106`) only test refusal. Make them owner/outsider pairs like the execution and rescue tests, so they can fail.
- Fix before merge: `LordConversationsCampaignBehaviorHandler.cs:125` duplicates the economy branch's `IsSenderControllingParty(object, PartyBase)`.
- Open question: rescue allows any clan member (`CompanionRolesHandler.cs:681`), firing needs `CanManageHero` (`:239`). Intended?

### pr/alley-request-sender-checks (only its own last commit; the rest is #4320)
- Fix before merge: `AlleyClearTests.CreateClearScenario` (`:249-256`) reconnects players that `AlleyTestEnvironment` now already connects; delete it.
- Fix before merge: `AlleyManagementHandler.cs:348-349`, `:394-395` resolve the alley twice (two "Failed to get Alley" lines on a miss). Resolve once and pass the alley in.
- Follow-up: with no owner (double-clicked abandon, defense resolve after a timeout) `IsSenderControllingHero(sender, null)` logs a warning blaming the sender. Return quietly when the owner is null.

### pr/clan-service-sender-checks
- Fix before merge: the rebellion and mercenary join/dismissal tests (`PlayerKingdomCreationFlowTests.cs:649,661,681`) only test refusal; make them leader/member pairs.
- Follow-up: two more hand-rolled "sender leads the clan" helpers (`VassalServiceHandler.cs:231-237`, `MercenaryServiceHandler.cs:151-157`) next to existing copies in `ChangeGovernorHandler` and `PlayerBannerHandler`, and the leave log wording is reused for a refused rebellion.
- Follow-up: `MercenaryServiceHandler.cs:104`, `:146` now have an always-true `clan == hero.Clan` condition.

### pr/kingdom-request-sender-checks
- Fix before merge: `ServerKingdomHandler.IsSenderInKingdom` (`:290`, `:313-321`) resolves the kingdom and reads clan state on the poll thread, against AGENTS.md's game-thread rule. Wrap the check and the apply in one `GameThread.RunSafe`.
- Fix before merge: the non-peer pass-through (`ServerKingdomHandler.cs:98-111`, `:290`, `AllianceHandler.cs:182`, `TradeAgreementsHandler.cs:131`) and its "server-published requests keep theirs" comment only serve tests that publish with `this`; no production code publishes these on the server. Send from a linked peer in those tests and drop the bypass, or at least fix the comment.
- Fix before merge: `IsSenderRuling` is copied in `AllianceHandler.cs:189-194` and `TradeAgreementsHandler.cs:140-145`.
- Follow-up: the `ControllerId` fields on `NetworkRequestCreateKingdom` and `NetworkRequestKingdomDecisionVote` are now ignored for every real sender.

### pr/controller-id-sender-checks
- Fix before merge: the new controller check shares the old "(not a participant)" / "(party not in the settlement)" log line (`BattleHostHandler.cs:361-366`, `:442-447`, `LocationHostHandler.cs:123-127`), so a stale-controller refusal is logged as the wrong cause. Give it its own line.
- Follow-up: `IsSenderController` is copied in both host handlers, and the village handler has a stricter version under a different lookup; put both rules on `IPlayerManager` with names that say which rule they apply. The lenient "unlinked peer passes" branch only serves tests (every real peer is linked during the join); say so in the comment, or link all peers in the mission harness and go strict.
- Follow-up: `BattleHostControllerIdTests` reserve test only checks refusal.

### pr/party-hero-request-sender-checks, pr/row5-crafting-bandit-sender-checks, pr/map-tracks-sender-check
Review still running when this section was written; see the next section if it landed.

## 4309 and 4310 red/green check (items 6 and 7)
- **4309**: already has red/green tests. Its `DefaultNotificationsHandlerNullListTests` on development without the fix: 3 of 6 fail (`GivenBirth_StillbornOnly_ArrivesWithANullListAndStillNotifies`, `PartyJoinedArmy_PartyWithoutArmy_DoesNotThrow`, `PartyAttachedAnotherParty_PartyWithoutArmy_DoesNotThrow`), so the null list and null army fixes are covered red/green. The other three are regression checks; `GivenBirth_UnresolvableChild_SkipsInsteadOfShowingTheWrongCount` passes on development too, so the "skip a birth with a missing child" part has no red/green test. No `test/4309-repro` branch was needed.
- **4310**: already has a red/green test. `EncounterContinueSiegeConditionTests.ContinueSiegeCondition_MainPartyOutsideTheBattle_HidesTheOptionWithoutThrowing` fails on development without the fix with the `IndexOutOfRangeException` the patch describes. The file as a whole doesn't compile on development, because its second test calls the new patch's `Prefix` by name; running only the first test shows it red.
