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
- Disband test dropped: it passes on base too, even with a clan on the disbanding party. `DisbandArmyApplyInternal` doesn't disperse the army in the test campaign; the cause wasn't found.
- `VillageHostileActionTests.BattleSimulationAdvance_IsAppliedOnlyFromThePeerRunningIt` (pacer and the other raider). The battle is set to decided so an advance finishes the simulation. With `BattleSimulationRunHandler` at base the other raider's advance finishes it; with the fix only the pacer's does.
- `SiegeAssaultLeaveTests.AssaultRequest_IsAppliedOnlyForTheSendersParty` (camp party's player and another player). The assault is staged as already live, so an applied request re-broadcasts `NetworkPromptSiegeAssault`. With `ServerSiegeEntryHandler` at base the other player's request re-broadcasts it; with the fix only the owner's does.
- `BattleRequestSenderTests.SiegeAftermathChoice_IsAppliedOnlyForTheSendersParty` (capturing leader's player and another player). A pending aftermath is staged for the leader's party; applying the choice takes it out of `SiegeAftermathPatches.PendingAftermaths`. With `ServerSiegeAftermathHandler` at base the other player's request takes it; with the fix only the owner's does. The test removes its entry from that static dictionary afterwards.
- Still not E2E covered: disband (see above).
