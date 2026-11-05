# Branch notes: fix/battle-request-sender-checks (brief 56 part 1, audit row 4)

Base: `base/upstream-215f01db9` (upstream development at 215f01db9). The fork's own `development` branch is far behind upstream, so it wasn't used. No PR opened, per the later instruction.

## What changed
On the server, a battle, siege or army request from a `NetPeer` is refused with a warning unless the sender is the player the request is about. Honest failure paths: a client that already left the battle still sends a late leave, a double click after the siege ended, a player who is no longer the army leader still has the army screen open. Before, each of these was applied to another player's party and replicated to everyone. Server-local publishes are not checked.

| Handler | Rule |
| --- | --- |
| `BattleJoinLeaveHandler` leave | the leaving party is the sender's; the check now runs inside the game-thread closure, before the leave is applied (it used to run after) |
| `ServerSiegeAftermathHandler` | the sender's party (handler now takes `IPlayerManager`) |
| `ServerSiegeEntryHandler` assault, break siege | the sender's party |
| `ArmyHandler` add/remove party | the sender's party or the army leader |
| `ArmyHandler` influence | the sender's own clan and amount >= 0. The amount is bounded, not recomputed |
| `ArmyHandler` cohesion | the army leader, non-negative values |
| `DisbandArmyHandler` | the sender's party |
| `BattleSimulationRunHandler` advance | the peer running the simulation (cancel already checked this) |
| `MapEventHandler` surrender | the sender's party is in the battle |
| `BattleFinalizeHandler` | refused from an unregistered peer while a host is elected |

Harness: `SiegeLeaveMenuTests`, `SiegeAssaultLeaveTests`, `SiegeDisconnectTests` and `ExpiredClientRequestTests` now register their client players (link peer to player), otherwise their own requests are refused. `ServerSiegeAftermathHandlerTests` pass `Mock.Of<IPlayerManager>()`.

## Tests
- New E2E `Services/Missions/BattleRequestSenderTests`: the refusal cases fail on base and pass with the fix.
- Full E2E sets Missions, MapEvents, Siege, Arm(ies), Villag(es): 1970/1970 passed.
- `CoopUnitTests.slnf`: Common 394, Coop.Tests 1454, GameInterface.Tests 2270, IntegrationTests 177 (2 skipped), CrashReporter 19. All green.

## Not sure / left out
- `ServerSiegeEngineHandler` deploy/remove: the battle host also deploys engines for NPC sides, so "sender's party" would be wrong and the right rule isn't clear.
- `BattleFinalizeHandler` / `BattleState` when no host is elected: who may finalize then isn't clear, so only the elected-host case is checked.
- Influence: bounded (own clan, non-negative) rather than recomputed. The amount comes from the client's army screen total (`ArmyVMPatches.cs:78`) or the abandon-army cost (`PlayerArmyWaitBehaviorPatches.cs:205`); recomputing the army screen total on the server wasn't attempted.

## Added later (brief 57, "add missing tests to rows 1-4")
- New E2E `Services/Armies/ArmyRequestSenderTests.PartyIsRemovedFromTheArmyOnlyOnItsOwnPlayersRequest` (member's player and an outsider). With `ArmyHandler` and `DisbandArmyHandler` at base the outsider case fails; with the fix both pass.
- `ArmyRequestSenderTests.PartyIsAddedToTheArmyOnlyOnItsOwnPlayersRequest` (own party allowed, another party refused) and `CohesionBoostFromAPlayerWhoDoesNotLeadTheArmy_IsRefused`. With the handlers at base the refusal cases fail; with the fix all pass. The cohesion test needed the leader's clan to have influence, otherwise the boost doesn't apply even on base (that's why the first version was dropped).
- `ArmyRequestSenderTests.Disband_IsAppliedOnlyAsTheSendersOwnParty` (member's player and an outsider). Why the first version passed on base: `DisbandArmyActionPatches.DisbandArmyApplyInternal` calls vanilla `DefaultDiplomacyModel.GetInfluenceCostOfDisbandingArmy()`, which threw a `NullReferenceException` because `Clan.PlayerClan` is null in the test campaign; `RunSafe` logged it and the army stayed. Setting `ResolvedMainHeroContext` doesn't help, because that context is only read by a few coop patches, not by `Clan.PlayerClan`. The test now sets `Campaign.Current.PlayerDefaultFaction` to the disbanding party's clan; then the outsider case fails on base and both cases pass with the fix.
- Possible separate issue, not verified on a live server: the disband cost is read through `Clan.PlayerClan`, not the disbanding player's clan. On a dedicated server that is the save's player faction, or null, in which case every disband request would throw as it did in the test. Worth a live check of a player disbanding an army.
- `VillageHostileActionTests.BattleSimulationAdvance_IsAppliedOnlyFromThePeerRunningIt` (pacer and the other raider). The battle is set to decided so an advance finishes the simulation. With `BattleSimulationRunHandler` at base the other raider's advance finishes it; with the fix only the pacer's does.
- `SiegeAssaultLeaveTests.AssaultRequest_IsAppliedOnlyForTheSendersParty` (camp party's player and another player). The assault is staged as already live, so an applied request re-broadcasts `NetworkPromptSiegeAssault`. With `ServerSiegeEntryHandler` at base the other player's request re-broadcasts it; with the fix only the owner's does.
- `BattleRequestSenderTests.SiegeAftermathChoice_IsAppliedOnlyForTheSendersParty` (capturing leader's player and another player). A pending aftermath is staged for the leader's party; applying the choice takes it out of `SiegeAftermathPatches.PendingAftermaths`. With `ServerSiegeAftermathHandler` at base the other player's request takes it; with the fix only the owner's does. The test removes its entry from that static dictionary afterwards.

## Left-out items re-checked (brief 58)
No check was added for either; there's no sure owner record to check against.
- `ServerSiegeEngineHandler` deploy/remove: the request names a side and slot, not a party. It already rejects stale and double-click requests with the slot's expected occupant and revision. A sender check would need a record of which player may order engines for that side (for example the camp leader for the besiegers); there is none, and vanilla's siege-engine menu condition wasn't confirmed here (no decompiler on this machine).
- `BattleFinalizeHandler` with no elected host: "the sender's party is on a side of this battle" is the only candidate, but the comment at `FinalizeAttemptedOnGameThread` says a leaving side leader sends the finalize and closes its menu, so its party can already be off the sides when the request arrives. This would need a record of the parties that were in the battle when it ended.

## Known regression, found by the tech debt review (brief 60)
`ArmyHandler.IsSenderSpendingOwnInfluence` only accepts an influence change for the sender's own clan. Both mercenary dismissal paths in `MercenaryServiceConversationPatch.cs` (fire without paying, fire with paying) publish `ChangeClanInfluence` for the mercenary's clan to clear its influence, so that is now refused and firing a mercenary no longer clears the mercenary clan's influence on the server. Not fixed yet: on the "without paying" path the dismissal reaches the server first, so the clan is no longer a mercenary of the sender's kingdom by the time the influence message is checked. Likely fix: clear the mercenary clan's influence in the server's dismissal apply (`MercenaryServiceHandler`) and stop relying on the client's `ChangeClanInfluence` for another clan. Details in `NOTES.md` on `notes/pr-batch`.
