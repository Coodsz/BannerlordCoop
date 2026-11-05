# Branch notes: fix/map-tracks-sender-check (audit row 7)

Base: `base/upstream-215f01db9` (upstream development at 215f01db9). The fork's own `development` is far behind upstream. No PR opened.

## What changed
`MapTracksHandler` `NetworkInitializePlayerTracksKeys`: on the server, a request from a `NetPeer` is ignored unless `PlayerPartyId` is the sender's party (`IsSenderControllingParty`, which logs the refusal). The client sends this on `PlayerHeroChanged`, so after a hero switch a late request can still name the old party, which may now be another player's; before, the server set up track keys (and the scouting snapshot) for that party. Server-local publishes are not checked.

## Tests
- New E2E `Services/MapTracks/MapTracksKeySenderTests` (owner and outsider). On base the outsider case fails and the owner case passes; with the fix both pass.
- E2E Services.MapTracks: 6/6.
- Unit: GameInterface.Tests 2270, Coop.Tests 1454, all passed.

## Left out (rule unclear)
- `TimeHandler` `NetworkRequestTimeSpeedChange`: time control is global and any player may change it by design as far as the code shows, so there's no owner to check against.
- Crafted weapon rename (`CraftingCampaignBehaviorWeaponNameHandler`, `SetCraftedWeaponNameHandler`): the server doesn't record who crafted an item, and "the item is in the sender's party roster" breaks once it's equipped, sold or given away.
- Hit, surgery, upgrade tracking and troop score rewards (`HitRewardHandler`, `MapEventPartyHandler` `NetworkTroopScoreHit`): the battle host reports these for NPC parties and a player's troops can still score just before the player leaves, so neither "the sender's party" nor "the sender is in the battle now" is clearly right.

## Note on running Coop.Tests
`dotnet test Coop.Tests/bin/Release/net6.0/Coop.Tests.dll` reported 838 and then 168 passing tests on two runs here (no failures), while `dotnet test Coop.Tests/Coop.Tests.csproj -c Release --no-build` runs all 1454. Use the project form.
