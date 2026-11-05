# Branch notes: fix/party-hero-request-sender-checks (brief 56 part 2, audit row 5)

Base: `base/upstream-215f01db9` (upstream development at 215f01db9). The fork's own `development` branch is far behind upstream, so it wasn't used. No PR opened, per the later instruction.

## What changed
On the server, a request from a `NetPeer` is refused with a warning unless the sender's hero passes the matching `ICoopClanPermissions` rule. Honest failure path: a stale client screen (the player left the clan, lost the party, or another player took over) still sends the request, and before this the server applied it to someone else's party or hero and replicated it to everyone. Server-local publishes are not checked.

| Handler | Rule |
| --- | --- |
| `PartyRolesHandler` (set/remove scout, quartermaster, engineer, surgeon, first mate, navigator) | `CanAssignRoles` or `CanManageParty` |
| `ClanNameHandler` | `CanManageClan` |
| `BarberHandler` | `CanManageHero` on the character's hero |
| `CharacterDeveloperHandler` (perks, attributes, focus) | `CanManageHero` on the developer's hero |
| `PartyWageLimitHandler` | `CanManageParty` |
| `ClanPartyItemVMHandler` objective | `CanManageParty` |
| `ClanPartyItemVMHandler` auto recruit | `CanManageClan` on the settlement owner clan |
| `ServerHeroHandler` rename | `CanRenameHero`, now resolved inside `GameThread.RunSafe` |

Test harness: `Coop.Tests/TestComponentBase.cs` registers a mock `ICoopClanPermissions`; `HeroDeveloperSyncTests` now registers its first client as the developer hero's player, otherwise its focus request is refused.

## Tests
- New E2E `Services/Clans/PartyHeroRequestSenderTests`: 7 cases. On base the 4 refusal cases fail and the 3 owner cases pass; with the fix all 7 pass.
- E2E regression sets Clans, MobileParties, Party, Heroes, HeroDevelopers, Companions, Barber: 335/336 on the first run; the one failure (`Client_FocusTarget_SpendsOnceAndPropagatesToClients`) was the harness gap above. After the fix, HeroDevelopers plus the new tests: 31/31.
- Unit: GameInterface.Tests 2270 passed, Coop.Tests 1454 passed.

## Not sure / left out
- `HeroHitPointsHandler` left out: the mission host sets other heroes' HP through it, so a "sender controls the hero" rule would break battles. The right rule isn't clear.
- Added later (brief 57): `PartyHeroRequestSenderTests.WageLimit_IsSetOnlyByTheClanLeader` and `TownAutoRecruit_IsSetOnlyByTheOwnerClansLeader` (owner and outsider), `HeroRenameOfAnotherPlayersHero_IsRefused` and `AttributePointsOnAnotherPlayersHero_AreRefused`. With the handlers at base every outsider case fails; with the fix all 13 tests in the class pass.
- Perks have no separate test: they go through the same `NetworkApplyChanges` request and the same `CanSenderDevelop` check as the attribute test.
- Rest of audit row 5 (smelting/refinement, `ClanKingdomHandler` OnClanSupported, bandit and patrol interactions) is on its own branch, `fix/row5-crafting-bandit-sender-checks`.
