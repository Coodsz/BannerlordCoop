# Branch notes: fix/hero-request-sender-checks (audit row 2, draft Coodsz/BannerlordCoop#48)

## Added after the PR was opened (brief 57, "add missing tests to rows 1-4")
- New E2E `Services/Missions/PlayerSurrenderSenderTests.Surrender_IsAppliedOnlyForTheSendersParty` (owner and outsider), using the mission battle harness. The PR body listed surrender as not E2E covered because it needs a live map event; `MissionTestEnvironment.SetupCoopBattle` provides one.
- Base check: with the `IsSenderControllingParty` line removed from `PlayerCaptivityServerHandler.Handle_NetworkPlayerSurrendered`, the outsider case fails and the owner case passes; with it both pass.

## Still not E2E covered
- `NetworkExecuteHero`: an execution doesn't complete in the test campaign even on base (unchanged from the PR body).
