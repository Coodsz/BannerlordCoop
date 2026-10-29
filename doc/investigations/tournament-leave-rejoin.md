# Tournament leave vs disconnect: what the server keeps and cleans up

Trace for the report "a player who leaves a tournament any way except the exit button can't rejoin"
(client logs `Failed to get MobileParty using network handle 10458`, server logs
`Ignored campaign baseline request ... after 16 baselines`). Start sha `552c3713`.

## What the server keeps for a player in a tournament

All of it lives in `TournamentSessionHandler` and the session registry. None of it is a `MobileParty`.

| State | Where |
| --- | --- |
| Session snapshot: contestants (`ControllerId`, `IsHuman`), spectators, host and successor controllers, choices, bracket | `ITournamentSessionRegistry`, mutated through `TournamentSessionRegistry.TryLeaveActive` (`source/GameInterface/Services/Tournaments/TournamentSessionRegistry.cs:467`) |
| Peer to controller map used when the peer is already gone | `tournamentPeerControllers`, filled in `TryAuthenticate` (`source/GameInterface/Services/Tournaments/Handlers/TournamentSessionHandler.cs:880`) |
| Bet ledger | `SettleBetLedger`, called from `ApplyActiveLeave` (`TournamentSessionHandler.cs:820`) |
| Mission instance membership for the arena mission | `ServerMissionMembershipHandler` (`source/Coop.Core/Server/Services/Instances/Handlers/ServerMissionMembershipHandler.cs:160`, `:184`) |
| The player's own party, parked while offline | `PlayerPartyVisibilityHandler.ParkParty` (`source/Coop.Core/Server/Services/Players/Handlers/PlayerPartyVisibilityHandler.cs:127`) |

No tournament code creates, registers or keeps a `MobileParty`, and none touches `MainParty`
(searched `Services/Tournaments`, `Services/Arenas`, `Missions/Tournaments`). Agents exist only in the
client's local mission.

## Exit button flow (client)

1. `CoopTournamentVM` calls `RequestLeaveActive` (`source/GameInterface/Services/Tournaments/UI/CoopTournamentVM.cs:310`),
   which sends `NetworkRequestLeaveActiveTournament` (`TournamentUIController.cs:313-318`).
2. Leaving the mission, `CoopTournamentController.OnLeaving` (`source/Missions/Tournaments/CoopTournamentController.cs:2167-2176`)
   sends `NetworkRequestLeaveActiveTournament` again if the leave was not requested yet, then `NetworkMissionLeft`.

Server side:

- `Handle_LeaveActive` (`TournamentSessionHandler.cs:436`) runs `LeaveActive` (`:730`), then
  `ApplyActiveLeave` (`:804`): removes the human from the session, settles the bet ledger, migrates the
  host, simulates the match if the ballot says so, and resolves the bracket when the last human left
  (`ResolveAfterLastHumanLeaves`, `:843`). A completed tournament is finalized instead (`:786`).
- `Handle_MissionLeft` (`ServerMissionMembershipHandler.cs:160`) removes the mission membership and
  tells the others `MissionPeerLeft`.

## Disconnect flow (Alt+F4 or crash)

- `Handle_Disconnected` (`TournamentSessionHandler.cs:660`) resolves the controller through the player
  manager or, if another handler already cleared the peer, through `tournamentPeerControllers`
  (`:672-681`), then `ProcessDisconnected`/`ProcessDisconnectedSession` (`:683-705`): preparation
  sessions run `LeavePreparationAfterDisconnect` (`:707`), active sessions run the same `LeaveActive`
  (`:730`) as the exit button, with a null peer.
- `ServerMissionMembershipHandler.Handle_PlayerDisconnected` (`:184`) removes the membership through
  `missionManager.HandleDisconnect` and tells the others `MissionPeerDisconnected` instead of
  `MissionPeerLeft`.
- `PlayerPartyVisibilityHandler.Handle_PlayerDisconnected` (`:108`) clears the peer (`:122`) and parks the
  party (`:127`): inactive and invisible, map figure removed. It is un-parked on
  `PlayerCampaignSynchronized` (`:201`, `:248`), after the join baselines converge.

## What the exit button cleans up that a disconnect does not

Nothing on the server. Both paths reach `LeaveActive` and remove the mission membership. The
differences are which message the other mission members get (`MissionPeerLeft` vs
`MissionPeerDisconnected`) and that a disconnect parks the party, which every disconnect does, in or
out of a tournament.

## Why this does not explain the report

The rejoin fails in the join baseline. `JoinCampaignBaselineSender.Send`
(`source/Coop.Core/Server/Services/MobileParties/JoinCampaignBaselineSender.cs:49-121`) sends every
party that is active on the server. The client maps their handles in `PartyBehaviorWireMapper.TryFromNetwork`
(`source/GameInterface/Services/MobileParties/Data/PartyBehaviorWireMapper.cs:122`), cannot resolve handle
10458, reports the baseline as failed (`JoinCampaignBaselineHandler.cs:59-71`) and asks again until the
server's limit of 16 (`source/Coop.Core/Server/Connections/States/LoadingState.cs:252-266`). Handles come
from the server's id to handle map (`AttachmentIdMapper.BuildServerMap`, `ObjectManager.SetJoinHandleMap`),
so the missing party is an active server party whose registry id the joining client does not register
when it loads the save. The tournament creates no such party, and the report says an older save failed
too. Next step: log the id behind the unresolved handle when the baseline is built, or dump
`objectManager.GetHandleMap()` for handle 10458 on the server at the failed join.
