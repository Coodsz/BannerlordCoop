# Dedicated server state divergence (#4141): step 1 trace

Start sha `bddf7ab4`. The issue was reported on build `0.1.6+5f4d63cc` with no steps and no quoted log lines.
It points at `coop-server-20261024-222756.log` around 1:28.

## Logs

The four server logs and two client zips are Discord CDN attachments. This session's network policy refuses
`cdn.discordapp.com`, through both the shell proxy and the page fetcher. The preserved copies are private bearer
links. No log line could be read, so no symptom can be picked by log evidence yet.

## The three symptoms and where each one lives

- **14 million denar debt.** Vanilla grows `Clan.DebtToKingdom` in
  `DefaultClanFinanceModel.ApplyShareForExpenses` and `AddExpensesForCallToWarAgreements`, when a clan's share of a
  negative `Kingdom.TributeWallet` or `CallToWarWallet` is more than its gold. It then credits the wallet back.
  - Both wallets and `Clan._clanDebtToKingdom` are server-authoritative AutoSync fields
    (`KingdomSync.cs:31,40`, `ClanSync.cs:21,23`).
  - Coop's player-clan expense path is `DefaultClanFinanceModelInterface.CalculateClanExpensesForPlayerClan`,
    and member transfers are `ClanFinance.ApplyDailyTransfers`.
  - Whether the debt is `DebtToKingdom` or negative hero gold, and on which side, needs the log.
- **AI kingdom made peace during an ongoing battle.** This is the stance change, `MakePeaceAction` with the kingdom
  decision, while a coop battle mission is running. The log should show the decision and the map event state on each
  side.
- **Field battle outcome diverged (a player captured despite a client-side win).** #4122 (merged in the start sha,
  `c07a62612`/`048ef71e3`) only stops the client `SetOverrideWinner` write from logging a managed-setter error. It
  does not change who wins on the server. The server log should show which side the server resolved and why.

## Next step

Once the logs are readable, pick the symptom whose server and client lines disagree most directly, check its area for
changes since `v0.1.6`, and write the failing E2E test in a new file.
