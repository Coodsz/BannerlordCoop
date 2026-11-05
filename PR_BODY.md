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
Party role, clan name, barber, party objective, wage limit, auto recruit, perk, attribute and hero rename requests apply without checking the sender may manage that party, clan or hero. A clan screen left open after leaving the clan or losing the party still changes someone else's party or hero.

## What is the new behavior?
Related to #4320

The server applies each of these only when the sender's hero passes the matching clan permission. Party roles need the party leader or someone who may manage the party, clan names and town auto recruit need the clan leader, barber and perk or attribute changes need a hero the sender may manage, wage limits and party objectives need someone who may manage the party, and hero renames use the same rule as the client's rename button. Requests the server makes itself aren't checked.

How to test. Run `PartyHeroRequestSenderTests` and `HeroDeveloperSyncTests` in E2E. The clan permissions are mocked in `TestComponentBase` for the unit tests.

## Bot Changelog Entry
- Fixed clan screen changes from an old screen affecting another player's party or hero

## Other information
