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
Village raid, battle host, battle reserve and location host requests carry a controller id that the server checks against the battle or party but never against the sender. A client that switched seats or reconnected under a new id can still send its old id and act for the other player.

## What is the new behavior?
Related to #4320

A village raid now needs the sender's current controller to match the one in the request. Battle host, battle reserve and location host requests are ignored when the sender is linked to a different controller than the request names.

How to test. Run `BattleHostControllerIdTests`, `LocationHostControllerIdTests` and `VillageHostileActionTests.ClientRequestRaid_WithAnotherPlayersControllerId_IsDenied` in E2E.

## Bot Changelog Entry


## Other information
