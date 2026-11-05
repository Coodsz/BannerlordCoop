# PR batch notes (brief 60)

## Fork PR #18 / #4153 (deployment null player agent)
Nothing left to fix. The fix is #3811 (merge 20db6a0a4), which is in `v0.1.7` and in current development. #18 only adds `DeploymentMissionControllerNullPlayerAgentTests` (one file, 55 lines), which isn't in development yet; on current development e2057ad80 it passes together with #3811's own `DeploymentSetupTeamsNullAgentPatchTests` (4/4). It can go up as a test-only PR or be closed.

## Fork PR #16 / #3989 (hideout assault leader wait)
Already raised from 30 s to 90 s on the branch (commit cadd97c45), and the wait already ends as soon as the leader loads, leaves or disconnects. Waiting for every member wasn't added because only the leader's load matters for the duel host; details in `BRANCH_NOTES.md` on `test/hideout-duel-assault-leader`. Branch tests 21/21.

## Rebased safety branches
Pending, see below once the run finishes.

## Upstream PR 4326 merged into development, full E2E
Pending, see below once the run finishes.
