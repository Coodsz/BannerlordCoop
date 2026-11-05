# Branch notes: fix/disband-army-cost-clan (brief 59)

Base: `base/upstream-215f01db9` (upstream development at 215f01db9). No PR opened.

## The bug
A player's disband request never disbands the army on the dedicated server.

- `DisbandArmyHandler.HandleNetworkDisbandArmyApplyInternal` (`DisbandArmyHandler.cs:50-66` at base) calls `DisbandArmyActionPatches.DisbandArmyApplyInternal`, which charges `clientParty.LeaderHero.Clan` (the right clan) with vanilla `DefaultDiplomacyModel.GetInfluenceCostOfDisbandingArmy()` (`DisbandArmyActionPatches.cs:75`).
- That vanilla cost reads `Clan.PlayerClan`. On the server, `ClanPlayerClanPatch` only substitutes the player's clan on a client (`ClanPlayerClanPatch.cs:19`) and otherwise falls through to vanilla (`:25`), which returns `Campaign.PlayerDefaultFaction`.
- Nothing on the server sets `PlayerDefaultFaction` outside a `MainHeroSubstitutionScope` or `BarterPlayerContext`: `HeroInterface.SwitchToPlayer` sets it (`HeroInterface.cs:185`) but is only called from the client's `LoadingState` (`LoadingState.cs:76`). The code itself says the server has none (`PerkResetCampaignBehaviorPatches.cs:30` "Server has no main hero/player clan", `DisableCompanionsCampaignBehavior.cs:152` "Hero.MainHero which is null on the headless server").
- So `Clan.PlayerClan` is null on the server, the cost throws a `NullReferenceException`, `GameThread.RunSafe` logs it, and the army stays. The handler only set `ResolvedMainHeroContext`, which is read by a few coop patches but not by `Clan.PlayerClan`.
- Not checked on a live server; the evidence is the code above plus the E2E reproduction below, where the server also has `Clan.PlayerClan == null`.

## The fix
`DisbandArmyHandler` runs the disband inside `MainHeroSubstitutionScope(clientParty.LeaderHero, clientParty)`, the scope other server handlers use for vanilla code that reads the player (for example `BanditInteractionsHandler`). It sets `PlayerDefaultFaction` to the disbanding player's clan for the call and restores it after, and also sets `ResolvedMainHeroContext` as before. The influence is still charged to `clientParty.LeaderHero.Clan`, now computed for that clan.

## Tests
- New E2E `Services/Armies/ArmyDisbandCostTests.DisbandRequest_DisbandsTheArmyAndChargesTheDisbandingClan`: the server has no player clan, the member's client sends the disband, the army is gone and the member's clan lost influence, and `Clan.PlayerClan` is null again afterwards. Fails on base (army not disbanded, log shows the NRE in `GetInfluenceCostOfDisbandingArmy`), passes with the fix.
- E2E Services.Armies: 30/30 with the fix.
- Unit: GameInterface.Tests 2270, Coop.Tests 1454, all passed.
- `fix/battle-request-sender-checks` works around this in its disband sender test by setting `PlayerDefaultFaction` in the test; once this fix is in, that line can go.
