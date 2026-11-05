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
Disbanding an army from a client does nothing on the server. Vanilla works out the influence cost from the player clan, and the server has no player clan, so the disband throws and the army stays.

## What is the new behavior?
Related to #4320

The server runs the disband as the disbanding player, the same way other server handlers run vanilla code that reads the player. The cost is worked out and charged to the disbanding player's own clan, and the server goes back to having no player clan afterwards.

How to test. Run `ArmyDisbandCostTests` and the other `Services.Armies` tests in E2E.

## Bot Changelog Entry
- Fixed disbanding an army not working

## Other information
