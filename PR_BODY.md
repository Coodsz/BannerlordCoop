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
Smelting, refining, supporting a clan, bandit encounter results and patrol hostile actions apply to whatever hero, clan, party or battle they name. A smithy, clan or bandit screen left open after the player's party or clan changed still acts on someone else.

## What is the new behavior?
Related to #4320

The server checks the sending player first. Smelting and refining need the crafting hero in the sender's party, supporting a clan needs the leader of the clan paying the influence, bandit recruiting and prisoner release need the sender's party and clan, a bandit surrender needs the sender on that battle side, and a patrol hostile action needs the sender's party. Requests the server makes itself aren't checked.

How to test. Run `SmithyClanSupportRequestSenderTests` and `BanditRequestSenderTests` in E2E.

## Bot Changelog Entry
- Fixed smithy, clan support, bandit and patrol actions from an old screen affecting another player

## Other information
