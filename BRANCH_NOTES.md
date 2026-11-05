# Branch notes: fix/economy-request-sender-checks (audit row 3, draft Coodsz/BannerlordCoop#49)

## Added after the PR was opened (brief 57, "add missing tests to rows 1-4")
- New E2E `GarrisonDonationTests.ManageGarrison_IsAppliedOnlyInTheSendersOwnClanTown` (own town and someone else's town, sender's party in the town both times). The PR body listed garrison manage as not E2E covered.
- Base check: with the `IsSenderPartyIn(..., requireOwnClan: true)` line removed from `PartyScreenHelperHandler.Handle_DoManageGarrison`, the not-own-town case fails and the own-town case passes; with it both pass. `GarrisonDonationTests`: 7/7.

## Still not E2E covered
- Workshop warehouse settings: the test campaign has no warehouse behavior.
- Caravan and villager conversation `MainPartyId` checks: those flows need conversation state the harness doesn't build.
