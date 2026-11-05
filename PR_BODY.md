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
Alliance, trade agreement, kingdom decision, vote and create kingdom requests apply for whatever kingdom or controller they carry. An offer popup still open after a player lost the throne or left the kingdom, or a controller id from before a reconnect, can act for the wrong kingdom or player.

## What is the new behavior?
Related to #4320

The sender has to rule the kingdom for alliances and trade agreements, and its clan has to be in the kingdom to add a decision. Votes and new kingdoms use the sending player's own controller instead of the one in the request. Requests the server makes itself keep their ids.

How to test. Run the `AllianceAcceptRequest_`, `TradeAgreementAcceptRequest_` and `AddDecisionRequest_` tests, `KingdomDecisionVote_IsCastAsTheSendersOwnController` and `CreateKingdomRequest_IsMadeForTheSendersOwnController` in `PlayerKingdomCreationFlowTests`.

## Bot Changelog Entry
- Fixed alliance, trade agreement and kingdom vote requests landing on the wrong kingdom or player after a ruler change or reconnect

## Other information
