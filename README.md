# Odds Helper — Gamble With Your Friends

A BepInEx mod for **Gamble With Your Friends** that shows the real odds of the bet you placed, in a small card in the bottom-left corner of the screen.

Walk up to any casino game and the card shows:

- **your chance to win** for the exact bet you placed (e.g. roulette number 36 → 2.7 %, red → 48.6 %)
- the **payout** and the **return** (RTP) of that bet
- live odds while you play: Crash ("chance to reach 2×"), Mines ("next tile safe"), Dragon Tower, Hi-Lo, Blackjack ("bust chance if you hit"), Keno, Duck Race, Crossy Road, Money Wheel, Wheel of Fortune, Plinko

Multiplayer odds use only the game rules and values every player already sees, so the mod never gives you an edge over your friends.

### Solo prediction

When you are **alone in the lobby**, the card also shows the outcome before it happens. This is marked with a purple **PREDICTION** badge:
- the roulette number and wheel results
- the Crash point, with a "CASH OUT NOW!" warning
- the mine layout, Dragon Tower eggs, the next Hi-Lo roll and the next blackjack cards

As soon as another player joins, prediction switches off automatically.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64, 5.4.23 or newer) into the game folder and start the game once.
2. Download `OddsHelper.zip` from [Releases](../../releases) and extract it into the game folder. The DLL ends up in `BepInEx/plugins/OddsHelper/OddsHelper.dll`.
3. Start the game. Press **F8** to show or hide the panel.

## Config

`BepInEx/config/takto.gwyf.oddshelper.cfg`

| Setting | Default | |
|---|---|---|
| `ToggleKey` | `F8` | key that shows/hides the panel |
| `Range` | `6` | how far (m) to look for the nearest game |

## Build

Requires the .NET SDK and the game with BepInEx installed.

```
dotnet build -c Release -p:GameDir="C:\Path\To\Gamble With Your Friends"
```

The build copies the DLL straight into `BepInEx/plugins/OddsHelper`.

## Disclaimer

Fan-made mod, not affiliated with the game's developers. Use at your own risk.
