# Branch notes: fix/economy-request-sender-checks (audit row 3, draft Coodsz/BannerlordCoop#49)

## Added after the PR was opened (brief 57, "add missing tests to rows 1-4")
- New E2E `GarrisonDonationTests.ManageGarrison_IsAppliedOnlyInTheSendersOwnClanTown` (own town and someone else's town, sender's party in the town both times). The PR body listed garrison manage as not E2E covered.
- Base check: with the `IsSenderPartyIn(..., requireOwnClan: true)` line removed from `PartyScreenHelperHandler.Handle_DoManageGarrison`, the not-own-town case fails and the own-town case passes; with it both pass. `GarrisonDonationTests`: 7/7.

- New E2E `WorkshopPurchaseConversationTests.WarehouseSettings_ApplyOnlyFromTheWorkshopsOwner` (owner and another player), covering `SetIsGettingInputsFromWarehouse` and `SetStockProductionInWarehouseRatio`. The PR body said the test campaign has no warehouse behavior, but `WorkshopsCampaignBehavior` is there and holds the warehouse data once `AddNewWorkshopData` runs. With `WorkshopDataHandler` at base the other player's request changes the setting (the inputs assertion fails first); with the fix both pass.

## Still not E2E covered
- Caravan and villager conversation `MainPartyId` checks: those flows need conversation state the harness doesn't build.
