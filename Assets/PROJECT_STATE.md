# Consequences — Project State
> Last updated: 2026-03-15
> Author: Ivan Murtov | UCL CS | Supervised by Prof. Dean Mohamedally | Client: MotionInput Games Ltd

---

## What the Game Is

**Consequences** is a 2D financial literacy game for students of all kinds, targeting mobile, tablet, and PC. It is also designed to be accessible for students with disabilities (e.g. autism, ADHD, dyslexia) via Calm Mode and other accessibility features. Players experience the real-world impact of financial decisions through two mini-games.

**Mini-game 1 — Spending Game (Shopping List)**
- 3 rounds, weekly grocery budget (Round 1: £8.50, Round 2: £7.50 tight, Round 3: £12.00 payday)
- 8 base items + 5 extras in round 3; player toggles what to buy (needs vs wants)
- End-of-round: feedback panel → consequence panel (1-month / 1-year projection) → star rating
- 3 stars = saved money + no debt; 2 = no debt; 1 = any savings; 0 = otherwise

**Mini-game 2 — Emergency Fund (6 Weeks)**
- Each week: £100 income − £40 essentials = £60 available; player picks saving tier (£20 / £30 / £40)
- Random JSON event each week (type driven by week number): normal, choice, bonus, emergency, crisis, lucky
- Emergency/crisis costs: fund covers first, bank covers remainder, debt if insufficient
- End: 4-panel consequence screen + star rating (same logic as above)
- Week progression: 1=normal, 2=choice, 3=emergency, 4=bonus, 5=emergency, 6=crisis

---

## Scenes

| Scene | Build Index | Status |
|---|---|---|
| Home.unity | 0 | ✅ In build |
| GameChoice.unity | 1 | ✅ In build |
| Spending.unity | 2 | ✅ In build |
| Progress.unity | 4 | ✅ In build |
| Settings.unity | 5 | ✅ In build |
| EmergencyFund.unity | 3 | ✅ In build |
| Progress.unity | 4 | ✅ In build |
| Settings.unity | 5 | ✅ In build |
| BankScene.unity | 6 | ✅ In build |

---

## Script Map

### Core Services (Singletons, DontDestroyOnLoad)

| Script | Role |
|---|---|
| `Core/BankAccountService.cs` | Global bank account. Balance starts £500, saved to PlayerPrefs. API: `GetBalance()`, `Spend()`, `Earn()`, `GetRecentTransactions()` |
| `Core/PlayerModelService.cs` | Tracks player engagement (OK / Frustrated / Bored). Records spending/fund rounds, inactivity, streaks, overspend count, treat ratio. API: `RecordSpendingRound()`, `RecordEmergencyFundRound()`, `GetEngagementState()`, `ResetAll()` |
| `Data/EventLoader.cs` | Loads all JSON events from `Resources/Events/`. Groups by type. Smart selection: no-repeat, difficulty-filtered, shuffled pools. API: `GetEvent(type)`, `GetEasyEvent()`, `GetEventByDifficulty()` |
| `Settings/SettingsManager.cs` | Loads/saves settings JSON to PlayerPrefs. Broadcasts `OnSettingsChanged`. API: `GetSettings()`, `UpdateSettings()` |
| `Settings/ThemeManager.cs` | Applies theme (Standard / Calm / HighContrast) to all `IThemedElement` components on settings change |
| `GameSettings.cs` | Static class, just holds `CalmMode` bool. Synced from SettingsManager |

### Game Controllers

| Script | Role |
|---|---|
| `SpendingGameController.cs` (~500 lines) | Manages 3-round shopping loop. Talks to BankAccountService, PlayerModelService, DuckReaction, ConsequencePanel, StarRating |
| `EmergencyFundController.cs` (~705 lines) | Manages 6-week fund loop. Uses EmergencyFundUIFlow for panels, EventLoader for events, DuckReaction. Has 60s inactivity timer → re-engagement popup |

### UI Components

| Script | Role |
|---|---|
| `UI/EmergencyFundUIFlow.cs` | State machine for Emergency Fund panels: Tutorial → SavingTier → Event → Feedback → Final. Wires callbacks to controller |
| `UI/SpendingGameUI.cs` | Manages Shopping List panels (shopping / feedback / consequence) |
| `UI/MoneyCounter.cs` | Budget bar + spend display. Colors: green (safe) / yellow (70%) / red (90%). Calm mode: no animation |
| `UI/ConsequencePanel.cs` | Projects 1-month and 1-year futures from current savings/debt rate |
| `EmergencyFundConsequencePanel.cs` | 4-panel end-screen: fund total → goal explanation → 8-month simulation → lesson |
| `UI/DuckReaction.cs` | Rubber duck mascot. 8 emotions (Happy, Sad, Excited, Worried, Thinking, Celebrating, Shocked, Neutral), each with color tint + bounce. Calm mode: minimal bounce, negative emotions → Neutral |
| `DuckReactionBackgroundChanger.cs` | Swaps background based on duck trigger string (payday/emergency/bonus/etc). Disabled in Calm mode |
| `UI/StarRating.cs` | 0–3 star display with punch animation. Calm mode: gentle reveal |
| `UI/BankHud.cs` | Shows current bank balance |
| `UI/BankTransactionList.cs` | Scrollable list of last 10 transactions |
| `UI/FloatingMoneyText.cs` | Animated floating £ text on money change. Disabled in Calm mode |

### Settings & Theming

| Script | Role |
|---|---|
| `Settings/SettingsPanel.cs` | Settings screen UI. Calm mode toggle applies full preset (0.5x speed, no particles, soft colours, longer timers, lower audio) |
| `Settings/ThemeConfig.cs` | ScriptableObject with 3 palettes: Standard (vibrant), Calm (muted), HighContrast |
| `Settings/ThemedButton/Panel/Text.cs` | Implement `IThemedElement`; auto-update on theme change |
| `Settings/ThemeCreator.cs` | Editor tool for theme asset creation |

### Data

| Script | Role |
|---|---|
| `Data/EmergencyFundEvent.cs` | Serializable event data: id, type, title, description, duckEmotion, duckLine, costPounds, bonusPounds, choices[], tags[] |

### Navigation & Misc

| Script | Role |
|---|---|
| `SceneLoader.cs` / `LoadScene.cs` | Generic scene load helpers |
| `LoadEmergencyFundGame.cs` | Loads EmergencyFund scene |
| `CredentialsPanel.cs` | Credits overlay |
| `DebugOverlay.cs` | Debug info panel (press D to toggle) |
| `ShoppingListTutorial.cs` / `EmergencyFundTutorial.cs` | Tutorial panels (latter may be legacy) |
| `Editor/ApplyRoundedCorners.cs` | Editor tool: applies RoundedRect sprite to UI Images |

---

## Key Data Flow

```
App start
  └─ PlayerPrefs → BankAccountService, SettingsManager, PlayerModelService singletons init

Home → GameChoice → [Spending OR Emergency Fund]

SPENDING GAME:
  SpendingGameController
    ├─ SpendingGameUI (panel management)
    ├─ BankAccountService.Spend() per item toggle
    ├─ ConsequencePanel (end of round projection)
    ├─ PlayerModelService.RecordSpendingRound()
    ├─ DuckReaction (emotional feedback)
    └─ StarRating (final display)

EMERGENCY FUND:
  EmergencyFundController
    ├─ EmergencyFundUIFlow (state machine)
    ├─ EventLoader.GetEvent(weekType)
    ├─ BankAccountService.Spend() / Earn()
    ├─ PlayerModelService.RecordEmergencyFundRound()
    ├─ DuckReaction
    ├─ EmergencyFundConsequencePanel (end screen)
    └─ StarRating

SETTINGS:
  SettingsPanel → SettingsManager.UpdateSettings()
    ├─ GameSettings.CalmMode updated
    └─ ThemeManager.ApplyTheme() → all IThemedElement components
```

---

## Calm Mode — What It Affects

Calm Mode is checked throughout. When ON:
- Animation speed: 0.5x, minimal duck bounce (3px, 0.3s)
- DuckReaction: negative emotions (Sad, Shocked, Worried) → Neutral
- DuckReactionBackgroundChanger: disabled (stable background)
- FloatingMoneyText: disabled
- MoneyCounter: no bar animation
- ConsequencePanel: encouraging, non-judgmental language
- StarRating: gentle reveal (no punch scale)
- Inactivity popup cooldown: 120s (vs 60s normal)
- Audio: lower volume preset
- Particles: disabled

---

## Accessibility Features

- Calm Mode (full preset above)
- High Contrast theme (ThemeConfig)
- Dyslexia-friendly font toggle
- Adjustable text size (Small / Medium / Large / XL)
- Unlimited time mode
- Hints toggle
- Large touch targets

---

## PlayerPrefs Keys

| Key | Type | Notes |
|---|---|---|
| `BankBalance` | float | Current bank balance (default 500) |
| `EmergencyFundBalance` | float | Current fund balance |
| `Settings` | string (JSON) | Full SettingsData serialized |
| `ShoppingTutorialShown` | int | Tutorial flag |
| `EmergencyFundTutorialShown` | int | Tutorial flag |

---

## Prefabs

- `DuckCharacter.prefab` — duck sprite/animation container
- `BankHUD.prefab` — bank balance HUD
- `TransactionRow.prefab` — single transaction list item

---

## Known Issues / To-Do

| Priority | Issue |
|---|---|
| 🟡 Verify | JSON event files exist in `Resources/Events/` |
| 🟡 Verify | ThemeConfig ScriptableObject assets exist (StandardTheme.asset, CalmTheme.asset, HighContrastTheme.asset) |
| 🟡 Verify | Audio assets wired up (settings framework exists, assets unclear) |
| 🟠 Test | Touch input (mobile/tablet) not confirmed tested |
| 🟠 Test | Progress scene functionality unclear |

---

## Project Meta

- **Engine:** Unity (recent LTS), 2D
- **UI:** TextMeshPro
- **Input:** Old + New Input Systems both enabled
- **Events:** JSON files in `Resources/Events/`
- **Persistence:** PlayerPrefs
- **Total Scripts:** ~40 C# + editor tools
- **Version:** v1.0
