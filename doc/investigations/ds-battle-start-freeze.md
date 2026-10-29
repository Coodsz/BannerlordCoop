# Joining players freeze and disconnect when combat starts on a dedicated server (#4084): step 1 trace

Start sha `bddf7ab4`. The issue was reported on build `0.1.6+5f4d63cc`. The dedicated server runs on the reporter's own
PC (about 8 Mbps upload) at battle size 300. The reporter, on the same machine, is fine. Remote friends lose voice
and damage, freeze, then disconnect. Lowering the battle size only partly helped, and a third player can't join.

## Logs

The client and server logs, the save and the video are Discord CDN attachments, and this session's network policy
refuses `cdn.discordapp.com`. The preserved copies are private, time-limited bearer links, so they were not fetched.
No log line could be read, so the deployment NRE fixed by #3811 (the `SetupTeams` NullReferenceException) can't be
confirmed or ruled out here.

## What the issue text points at

- Only the players remote from the server freeze, and it scales with battle size. That fits the server's upload
  saturating, not a logic error on the battle start handshake.
- Mission traffic is peer to peer. When a direct link fails, `LiteNetP2PClient` falls back to relaying through the
  server (`Missions/Services/Network/LiteNetP2PClient.cs:35-36`, relay send `:1218-1244`, per-route budget
  `GetMaxRelayPayloadBytes`). If the friends couldn't connect directly, every battle packet between them and the
  reporter crosses the server's 8 Mbps uplink on the same connection as the campaign stream. That can starve the
  campaign connection until its disconnect timeout fires.
- `MovementRateController` and `MovementBatchSender` (adaptive send rate and receiver caps) were already in 0.1.6 and
  haven't changed since `v0.1.6`. The relay client's only change since then is test socket binding.

## Needed to confirm

From the friend's `Coop_client.log`:
- whether its peer route is `CampaignRelaySend` (relay) or direct;
- `[BattleTraffic]` oversized or relay budget warnings;
- the disconnect reason (timeout or remote close);
- whether a `SetupTeams` NullReferenceException appears.

From `Coop_server.log`: relay volume and the disconnect reason. A relay route plus a timeout means a throughput
problem, which needs a relay cap or rate cut, not a handshake fix. A `SetupTeams` NRE means #3811 already covers it on
development.
