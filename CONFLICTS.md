# PR batch conflict map

Checked against upstream `development` at e2057ad80 (Merge pull request #4332), with `git merge-tree` on 2026-11-05. PR heads were fetched read only from `refs/pull/N/head`; nothing was pushed upstream.

## Coodsz's open upstream PRs against current development

All 18 merge cleanly into current development on their own.

| PR | Head | Files | Commits behind development | Merge into development |
| --- | --- | --- | --- | --- |
| 4193 | 1d125680b | 3 | 609 | clean |
| 4295 | 6d17a3ea9 | 3 | 367 | clean |
| 4308 | 10dd8768d | 2 | 219 | clean |
| 4309 | 486e7b9e3 | 2 | 219 | clean |
| 4310 | f870a03c2 | 2 | 219 | clean |
| 4311 | 215f9f5ce | 5 | 219 | clean |
| 4312 | 802b319d4 | 3 | 219 | clean |
| 4313 | e2c874421 | 2 | 219 | clean |
| 4314 | ab4ae26af | 2 | 219 | clean |
| 4315 | c4b003b1d | 2 | 367 | clean |
| 4316 | e5fdb78bc | 2 | 219 | clean |
| 4317 | 608857a3e | 2 | 207 | clean |
| 4319 | 8b5187d46 | 9 | 2 | clean |
| 4320 | 7e3242287 | 5 | 207 | clean |
| 4325 | da60e8df3 | 1 | 207 | clean |
| 4326 | c2ef3c800 | 9 | 207 | clean |
| 4327 | 0bd375eba | 2 | 207 | clean |
| 4328 | baf42ef85 | 2 | 207 | clean |

"Behind" is how many development commits the PR head doesn't have yet; a long-behind PR (4193 at 609) still merges cleanly, but its CI ran on an old base.

## Files touched by more than one PR or safety branch

Each overlapping pair was merged on top of development, one after the other, to see whether the second still applies.

| Pair | Shared files | Result |
| --- | --- | --- |
| 4193 + 4327 | `CoopMod.cs` | clean |
| 4315 + `fix/battle-request-sender-checks` | `SiegeLeaveMenuTests.cs` | clean |
| 4320 + `fix/alley-request-sender-checks` | `AlleyCampaignBehaviorPatches.cs`, `AlleyClearTests.cs`, `AlleyManagementHandler.cs`, `AlleyManagementMessages.cs`, `AlleyTestEnvironment.cs` | clean (the alley branch is built on 4320's branch, so it carries 4320's commits; merge 4320 first) |
| 4326 + `fix/controller-id-sender-checks` | `BattleHostHandler.cs` | **conflict**. 4326 holds the first election for a loading preferred host (`PendingElection`, `PreferredHostWait`) inside `Handle_NetworkRequestBattleHost`; the controller-id branch adds `IsSenderController(requester, requesterId)` to the participant check in the same method. Resolve by keeping 4326's method and adding the sender check to its participant check before the election is held or replayed. |
| `fix/battle-request-sender-checks` + `fix/controller-id-sender-checks` | `VillageHostileActionTests.cs` | code merges clean; only `BRANCH_NOTES.md` conflicts |
| `fix/battle-request-sender-checks` + `fix/disband-army-cost-clan` | `DisbandArmyHandler.cs` | code merges clean; only `BRANCH_NOTES.md` conflicts. Once the disband fix is in, the `PlayerDefaultFaction` line in the battle branch's `ArmyRequestSenderTests` disband test isn't needed |
| `fix/clan-service-sender-checks` + `fix/kingdom-request-sender-checks` | `PlayerKingdomCreationFlowTests.cs` | clean |
| `fix/party-hero-request-sender-checks` + `fix/row5-crafting-bandit-sender-checks` | `Coop.Tests/TestComponentBase.cs` | code merges clean (both add the same `RegisterMock<ICoopClanPermissions>` line; keep one); only `BRANCH_NOTES.md` conflicts |

No other PR or safety branch shares a file with another.

`BRANCH_NOTES.md` exists only on the fork branches as a report for the team and should be dropped before any of them becomes a PR, which removes those conflicts.

## Safety branches rebased onto current development

Every safety branch rebased onto development e2057ad80 without conflicts and is pushed as `<name>-rebased` (the originals are unchanged): `fix/alley-request-sender-checks`, `fix/clan-service-sender-checks`, `fix/kingdom-request-sender-checks`, `fix/hero-request-sender-checks`, `fix/economy-request-sender-checks`, `fix/battle-request-sender-checks`, `fix/party-hero-request-sender-checks`, `fix/row5-crafting-bandit-sender-checks`, `fix/controller-id-sender-checks`, `fix/map-tracks-sender-check`, `fix/disband-army-cost-clan`. Test results for the rebased branches are in the next section.
