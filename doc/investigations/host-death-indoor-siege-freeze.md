# Host freezes after dying in an indoor or siege battle (#4139): step 1 trace

Start sha `bddf7ab4`. The issue was reported on build `0.1.6+5f4d63cc`. It says "sometimes": the host who dies freezes
and can't open Esc, while the other players carry on. It has no steps and no quoted log lines.

## Logs

The client and server logs are Discord CDN attachments, and this session's network policy refuses
`cdn.discordapp.com`. The preserved copies are private, time-limited bearer links, so they were not fetched. Without
the host's `Coop_client.log`, a hard freeze (a deadlock or an endless loop) can't be told apart from a UI wedge (no
death or spectator screen, input layers blocked).

## Ruled out in code

- **Blocking game-thread waits in mission code.**
  - `GameThread.RunSafe(..., blocking: true)` is used in tournament, tavern and location joins and in map event
    finalize, join and leave. Its 30 s `BlockingTimeout` would log and return, not freeze for good.
  - The only raw wait is `MovementRateController.Dispose` waiting on its heartbeat timer
    (`Missions/Agents/Handlers/MovementRateController.cs:614-619`). That callback only publishes a network message
    and never waits on the game thread, so it can't deadlock.
- **Unbounded loops in battle, siege and agent code.**
  - `SiegeEngineDeploymentReplicator` drain loops (`:255`, `:271`) always shrink or stop.
  - The `RemoteAgentActionProcessor` epoch CAS loop (`:1532`) ends.
  - Every other `while` in `Missions/Battles`, `Missions/Taverns`, `Missions/Locations` and `Missions/Agents`
    dequeues or shrinks.
- **Fixes since `v0.1.6`.** None targets a host freezing on death. `c6877e5e0` ("end battles left with no healthy
  troops on one side") changes battle ending, but it doesn't explain one client freezing.

## Needed to go further

The host's `Coop_client.log` (or the diagnostic zips) around the death: the last lines before it stopped, and any
"blocking Run action was not processed within 30 seconds", exception or spectator line. These say whether to look at
the death or spectator camera path, the battle host handover in `BattleAuthorityMigrator`, or the location
(indoor) host in `Missions/Locations`.
