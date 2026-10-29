# Chat while imprisoned or in a siege: why it was blocked and why development no longer blocks it

Trace for #4037 ("chat cannot be used while the player is imprisoned or participating in a siege").
Start sha `bddf7ab4`. The issue was filed on Oct 21 with no version, steps or logs.

## What a captive or besieging client is showing

- A captive client sits in a vanilla game menu. `PlayerCaptivityClientHandler.cs:128-137` switches to
  or activates the captivity menu (`taken_prisoner`, `prisoner_wait`) with `GameMenu.SwitchToMenu` or
  `GameMenu.ActivateGameMenu`.
- A siege participant on the map also sits in a game menu: `menu_siege_strategies`, or `army_wait`
  for an army member. The siege battle itself is a mission and goes through the `MissionScreen` path.
- Vanilla 1.4.8 `SandBox.GauntletUI.Menu.GauntletMenuBaseView.OnInitialize` renders every game menu,
  wait menus included, in the `MapMenuView` `GauntletLayer` and calls `ScreenManager.TrySetFocus` on it.
- `GauntletMapSiegeOverlayView` loads its movie on the map's nameplate layer and never takes focus.

## The cause, before #3857

Before #3857 ("[3835] Chat movement and settlement fix", merged Oct 26 as `14828f4c`),
`ChatOverlay.UpdateVisibility` made the map openable only when it was not at a menu:

    isGameplayScreen = GameStateManager.Current?.ActiveState is MapState mapState && !mapState.AtMenu;

(`14828f4c^1:source/GameInterface/Services/Chat/ChatOverlay.cs:241`). Every game menu turned off
`allowChatOpen`, so captivity and siege menus could never open chat.

## Development now

- `ChatOverlay.cs:247-293` treats any active `MapState` as openable (`isOpenableScreen = mapState != null`,
  line 266).
- At a menu, opening is allowed while `ScreenManager.FocusedLayer` is the `MapMenuView` layer
  (lines 263-265, passed as `isGameplayLayerFocused || isMenuLayerFocused` at line 293), which is the
  layer vanilla focuses for every game menu.
- Once open, `ChatVM.IsChatInputEnabled` (`ChatVM.cs:136`) depends only on the panel being open and the
  selected tab having a send channel. Nothing reads the hero's party, captivity or siege state.
- `ServerChatHandler` does not check the sender's party, captivity or siege state either.

So the reported block is gone on development, and there is no state left to build a failing test from.

## Why there is no failing test

The removed rule was written inline in `UpdateVisibility`, which needs `ScreenManager`, `MapScreen`
and `MapState`. `ShouldAllowChatOpen` only receives booleans that were already computed, so a test on
it passes on both the old and the current code. A no-game test would first need the `MapState` and
menu-focus check extracted into a helper. That is a refactor with no bug behind it, so it is
proposed here rather than done.

## What only a live game can confirm

- That the reporter's build predated #3857.
- On a current dedicated server, that Enter opens chat and sends while the client is in
  `taken_prisoner` or `prisoner_wait`, and while it is besieging as army leader and as army member.
- That no Coop overlay or inquiry popup takes focus from `MapMenuView` in those states.
- That chat hides only during the capture conversation itself, which is intended:
  `ShouldShowPresentation` hides it during conversations.
