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
Battle, siege and army requests apply for whatever party, clan or battle they name. A late leave after the player already left the battle, a double click after the siege ended, or an army screen still open after losing the lead changes another player's party and replicates to everyone. Leaving a battle was checked only after the leave already happened.

## What is the new behavior?
Related to #4320

The server checks the sending player before applying each request. Leaving a battle, siege assault, siege aftermath, break siege and disband need the sender's own party. Army member changes need the member's player or the army leader, and cohesion boosts need the army leader with no negative amounts. Influence spends have to be the sender's own clan and a real spend (the amount is bounded, not recomputed). Advancing a battle simulation needs the client running it, a surrender needs the sender's party in the battle, and finalizing a battle with an elected host needs a registered player. Requests the server makes itself aren't checked.

How to test. Run `BattleRequestSenderTests`, `ArmyRequestSenderTests`, `SiegeAssaultLeaveTests.AssaultRequest_IsAppliedOnlyForTheSendersParty` and `VillageHostileActionTests.BattleSimulationAdvance_IsAppliedOnlyFromThePeerRunningIt` in E2E. The siege leave, siege disconnect and expired request tests now link their players like a real join does.

## Bot Changelog Entry
- Fixed leaving a battle, siege orders and army changes from an old screen affecting another player's party

## Other information
