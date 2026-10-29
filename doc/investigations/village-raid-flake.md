# VillageHostileActionTests flake: what is known so far

Start sha `e2fdce02`. No test file is changed here. `VillageHostileActionTests.cs` and `MapEventTestBase.cs` are in
ShoT's open drafts #4108 and #4165, so any fix that belongs there goes to ShoT.

## What CI showed on 2026-10-29

The failures were always in E2E shard 2 (of 8), on branches that don't touch villages, including a doc-only branch.
- `RaidFinalization_ClientActionsWaitForServer` (finalize, finish, request, settlement-leave, stale-reply):
  - `Expected "raiding_village"`, strings differ at position 0, at the client menu assert
    (`VillageHostileActionTests.cs:1313`);
  - or a NullReferenceException at `VillageHostileActionTests.cs:1324`
    (`mapEvent.MapEventSettlement.SettlementHitPoints`, so the raid map event had no settlement on some instance).
- `RaidSurrender_NativeOwnerHandoff_ResolvesServerDefendersBeforeManagerProgress`: a NullReferenceException inside
  vanilla `EncounterGameMenuBehavior.ConsiderVillageSurrenderPossibility`.
- `RaidSurrender_AfterEncounterInit_TransitionsOnlyOwnerWhenAuthoritativeStateArrives(militiaLast, rosterLast)`:
  `Assert.Equal() Failure: Strings differ`.

Each failing run failed about 12 village raid cases together, and a single re-run of the same commit passed every
time. So when it fails, the whole raid setup is broken for that run, not one assertion racing. That points at state
left over from an earlier test in the same shard process, or at something on the shared game thread, rather than at
one missing pump inside one test.

## Local reproduction (docker image `garrettluskey/bannerlordcoop`, Release build of `e2fdce02`)

- The two failing tests on their own (8 cases), 15 runs in a row: 15 passed, 0 failed.
- The full shard 2 class filter, computed with `.github/scripts/run-e2e-shard.sh` (416 tests): run 1 passed all 416.
  Further runs are noted below as they finish.

It has not been reproduced locally yet, so no root cause with file:line is claimed.

## Where to look next (for ShoT)

- Statics a raid test reads and an earlier test in shard 2 can leave set: `Campaign.Current.MapStateData.GameMenuId`,
  `PlayerEncounter.Current`, `Settlement` components set through `SetSettlementComponent` under `AllowedThread` (the
  test does this on clients only), and patch statics in the raid and village services.
- The test order inside shard 2. Assignment is greedy by class duration (`.github/e2e-test-durations.tsv`), so a new
  class elsewhere can move the classes that run before `VillageHostileActionTests` into or out of shard 2.
- A per-test reset in the village tests' setup (menu id, player encounter and settlement component on each client)
  would make the class independent of what ran before it. That edit is in `VillageHostileActionTests.cs` or
  `MapEventTestBase.cs`, so it is left to ShoT.
