# Branch notes: fix/row5-crafting-bandit-sender-checks (audit row 5, the part brief 56 didn't cover)

Base: `base/upstream-215f01db9` (upstream development at 215f01db9). The fork's own `development` is far behind upstream. No PR opened. The rest of row 5 (party roles, perks, renames, barber, wage, objective, auto recruit) is on `fix/party-hero-request-sender-checks`.

## What changed
On the server, these requests from a `NetPeer` are refused with a warning unless they're about the sender's own party, clan or battle. Honest failure paths: a stale screen after the player's party or clan changed, a late request after a hero switch, a double click on a bandit result after the encounter moved on. Server-local publishes are not checked.

| Handler | Request | Rule |
| --- | --- | --- |
| `CraftingCampaignBehaviorCraftingHandler` | `NetworkDoSmelting`, `NetworkDoRefinement` | the crafting hero is in the sender's party (the smithy only lists those heroes) |
| `ClanKingdomHandler` | `NetworkOnClanSupported` | the sender leads the supporter clan (`CanManageClan`), because it spends that clan's influence |
| `BanditInteractionsHandler` | `NetworkRosterScreenAfterBanditEncounter` | `MainPartyId` is the sender's party |
| `BanditInteractionsHandler` | `NetworkGetBanditMemberAndPrisonerRosters` | `MainPartyId` is the sender's party and `PlayerClanId` is the sender's clan |
| `BanditInteractionsHandler` | `NetworkBanditsSurrenderAsPrisoners` | the sender's party is on `PlayerSide` of that battle |
| `PatrolInteractionsHandler` | `NetworkPatrolPartyHostileAction` | `MainPartyId` (a PartyBase) belongs to the sender's party |

`Coop.Tests/TestComponentBase.cs` registers a mock `ICoopClanPermissions`, since `ClanKingdomHandler` now takes it.

## Tests
- New E2E `Services/Clans/SmithyClanSupportRequestSenderTests` (smelting and refinement owner/outsider, clan support refusal) and `Services/Bandits/BanditRequestSenderTests` (surrender refusal, recruit owner/outsider, prisoner release refusal): 9 cases. On base the 6 refusal cases fail and the 3 owner cases pass; with the fix all 9 pass.
- E2E regression sets Services.Clans, Bandit, MerchantOutlaws, Patrol, Crafting, Smith, Hideout: 241/241.
- Unit: Coop.Tests 1454, GameInterface.Tests 2270, all passed.

## Not sure / left out
- Added later (brief 57): `SmithyClanSupportRequestSenderTests.PatrolHostileAction_IsAppliedOnlyForTheSendersParty` (owner and outsider). The visible effect: with the two parties' kingdoms at peace, the hostile action declares war between them. With `PatrolInteractionsHandler` at base the outsider's request declares war; with the fix only the owner's does. `SmithyClanSupportRequestSenderTests` plus `BanditRequestSenderTests`: 11/11.
- `NetworkBanditPartyScreenDoneCondition` (sets fugitive heroes active) left out, re-checked in brief 58: it carries only character ids, no party, encounter or map event. A check would need a server record of the bandit encounter the sender just won and the heroes that encounter freed; none is kept, so nothing ties the ids to the sender.
- The smithy tests use zero stamina, so the owner case only proves the request reached the stamina reply; the actual smelt/refine with materials isn't exercised.
