# Branch notes: fix/economy-request-sender-checks (audit row 3, draft Coodsz/BannerlordCoop#49)

## Added after the PR was opened (brief 57, "add missing tests to rows 1-4")
Each new test was checked against base: with the named handler back at base the refusal case fails, and with the fix every case passes.

- `GarrisonDonationTests.ManageGarrison_IsAppliedOnlyInTheSendersOwnClanTown` (own town and someone else's town, sender's party in the town both times). Base check: the `IsSenderPartyIn(..., requireOwnClan: true)` line removed from `PartyScreenHelperHandler.Handle_DoManageGarrison`. `GarrisonDonationTests`: 7/7.
- `WorkshopPurchaseConversationTests.WarehouseSettings_ApplyOnlyFromTheWorkshopsOwner` (owner and another player), covering `SetIsGettingInputsFromWarehouse` and `SetStockProductionInWarehouseRatio` (`WorkshopDataHandler`). The PR body said the test campaign has no warehouse behavior, but `WorkshopsCampaignBehavior` is there and holds the warehouse data once `AddNewWorkshopData` runs.
- `Villages/VillagerConversationSenderTests`: `VillagersTakenPrisoner_IsAppliedOnlyForTheSendersOwnParty` and `CaravanTakenPrisoner_IsAppliedOnlyForTheSendersOwnParty`. The sender names its own hero but another player's party (a stale screen); taking the villagers or caravan prisoner destroys their party, so a refused request leaves it on the map (`VillagersConversationsHandler`, `CaravansConversationsHandler`).

## Still not E2E covered
- The other villager and caravan results (bought from villagers, hostile interaction, looted leave, surrender leave) use the same `IsSenderControllingParty(MainPartyId)` line as the two prisoner tests but have no test of their own; their effects (prices from a village, `BeHostileAction`) don't show in the test campaign as plainly.
