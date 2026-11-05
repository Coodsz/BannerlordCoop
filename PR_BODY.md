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
When a player switches heroes the client asks the server to set up map track keys for its party. A late request can still name the old party, which may belong to another player by then, and the server sets up tracks for that party.

## What is the new behavior?
Related to #4320

The server sets up track keys only for the sender's own party.

How to test. Run `MapTracksKeySenderTests` in E2E.

## Bot Changelog Entry


## Other information
