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
Leaving a kingdom, starting a rebellion, and taking or ending mercenary service apply to whatever clan they name. A clan member whose client still shows the leave or mercenary dialog after joining someone else's clan can take the leader's whole clan in or out of a kingdom.

## What is the new behavior?
Related to #4320

The server applies these four requests only when the sender's hero leads the clan, using the same clan permission check as changing a governor.

How to test. Run the `_FromAClanMemberWhoDoesNotLeadIt_IsIgnored` tests and `LeaveKingdomRequest_FromTheClanLeader_LeavesTheKingdom` in `PlayerKingdomCreationFlowTests`.

## Bot Changelog Entry
- Fixed a clan member being able to take their leader's whole clan into or out of a kingdom

## Other information
