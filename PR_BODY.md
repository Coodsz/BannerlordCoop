## PR Checklist
<!-- Insert "x" inside square brackets to check item. -->

Please check if your PR fulfills the following requirements:

- [x] All new classes have class-level documentation comments, if there are any at all
- [x] Tests for the changes have been added (for bug fixes / features)

## PR Type
What kind of change does this PR introduce?
<!-- Please check the one that applies to this PR. -->

- [x] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Refactoring (no functional changes, no api changes)
- [ ] Build related changes
- [ ] CI related changes
- [ ] Documentation update
- [ ] Other... Please describe:


## What is the current behavior?
Trade, workshop, prisoner sale and garrison requests apply to whatever party, workshop or town they name. A trade or garrison screen left open after the party or town changed hands, or a workshop sold in the meantime, moves another player's gold, items or troops.

## What is the new behavior?
Related to #4320

The server applies a trade only for the sender's party or a clan party it may manage, workshop changes only from the owner (or the buyer and seller for a sale), prisoner sales only from the selling party, and garrison changes only when the sender's party is in the town (and its clan owns the town for managing). Caravan and villager results also check the sender's party. The client chosen ignore cost flag on a workshop type change is dropped, since both vanilla paths pay for it.

How to test. Run `MerchantTradeTests.TradeForAnotherPlayersParty_IsRefused`, the workshop tests in `WorkshopPurchaseConversationTests`, `SellPrisonersSenderTests`, `GarrisonDonationTests` and `VillagerConversationSenderTests` in E2E.

## Bot Changelog Entry
- Fixed trades, workshop changes, prisoner sales and garrison actions from an old screen affecting another player's party, workshop or town

## Other information
