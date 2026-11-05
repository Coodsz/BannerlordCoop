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
Execute, surrender, take prisoner, companion and recall requests apply to whatever hero or party id they carry. A dialog left open after the party, hero or companion changed hands acts on another player's hero or party.

## What is the new behavior?
Related to #4320

The server checks the sending player before applying each request. An execution has to come from the executor's player, a surrender or prisoner request from the party's player, a rescue from the companion's clan, and a recall or companion dismissal from a player who may manage that hero. Requests the server makes itself aren't checked.

How to test. Run `HeroRequestSenderTests`, `PlayerSurrenderSenderTests` and `CompanionDismissalSyncTests.FireCompanion_FromAPlayerWhoCannotManageIt_ReturnsFailureWithoutMutatingCompanion` in E2E.

## Bot Changelog Entry
- Fixed execute, surrender, prisoner, companion and recall actions from an old dialog affecting another player's hero or party

## Other information
