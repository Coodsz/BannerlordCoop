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
The alley requests for acquire, abandon, overseer, garrison, recruit and defense apply to whatever alley and hero they name. An alley menu left open after the alley changed hands still changes that alley for its new owner.

## What is the new behavior?
Related to #4320

The server applies each alley request only when the sending player controls the hero the request is about, the same way #4320 checks a clear request. A refused request is logged and nothing changes. Requests the server makes itself, like the debug alley commands, still apply.

This is built on top of #4320, so merge that one first.

How to test. Run `AlleyRequestSenderTests` in E2E. Each test has the second client ask about the first client's alley and checks nothing changed. The other alley tests now link both players to their clients like a real join does.

## Bot Changelog Entry
- Fixed an alley menu left open after the alley changed hands still changing that alley

## Other information
