# Consequences — Project State
> Last updated: 2026-03-16
> Author: Ivan Murtov | UCL CS | Supervised by Prof. Dean Mohamedally | Client: MotionInput Games Ltd

---

## What the Game Is

**Consequences** is a 2D financial literacy game for students of all kinds, targeting mobile, tablet, and PC. It is also designed to be accessible for students with disabilities (e.g. autism, ADHD, dyslexia) via Calm Mode and other accessibility features. Players experience the real-world impact of financial decisions through two mini-games.

**Mini-game 1 — Spending Game (Shopping List)**
- 3 rounds, weekly grocery budget (Round 1: £8.50, Round 2: £7.50 tight, Round 3: £12.00 payday)
- 8 base items + 5 extras in round 3; player toggles what to buy (needs vs wants)
- End-of-round: feedback panel → star rating
- Final: ConsequencePanel shows 3-week scorecard (essentials / treats / budget) with narrative
- 3 stars = saved money + all essentials; 2 = no debt; 1 = any savings; 0 = otherwise
- Records to PlayerModelService each round

**Mini-game 2 — Emergency Fund (6 Weeks)**
- Each week: £100 income − £40 essentials = £60 available; player picks saving tier (£20 / £30 / £40)
- Random JSON event each week (type driven by week number): normal, choice, bonus, emergency, crisis, lucky
- Emergency/crisis costs: fund covers first, bank covers remainder, debt if insufficient
- End: consequence screen + star rating
- Week progression: 1=normal, 2=choice, 3=emergency, 4=bonus, 5=emergency, 6=crisis
- Records to PlayerModelService each week + on final

---

## Scenes

| Scene | Build Index | Status |
|---|---|---|
| Home.unity | 0 | ✅ In build |
| GameChoice.unity | 1 | ✅ In build |
| Spending.unity | 2 | ✅ In build |
| EmergencyFund.unity | 3 | ✅ In build |
| Progress.unity | 4 | ✅ In build |
| Settings.unity | 5 | ✅ In build |
| BankScene.unity | 6 | ✅ In build |

---

## Script Map

### Core Services (Singletons, DontDestroyOnLoad)

| Script | Role |
|---|---|
| `Core/BankAccountService.cs` | Two separate balances: `balancePounds` (Spending game, default £500) and `emergencyBalancePounds` (Emergency Fund, default £500). Each has its own PlayerPrefs key and API: `GetBalance/Spend/Earn` for spending; `GetEmergencyBalance/SpendEmergency/EarnEmergency/ResetEmergencyBalance` for EF |
| `Core/PlayerModelService.cs` | Tracks player engagement (OK / Frustrated / Bored). Records spending/fund rounds, inactivity, streaks, overspend count, treat ratio. Persisted via PlayerPrefs. API: `RecordSpendingRound()`, `RecordEmergencyFundRound()`, `RecordInactivity()`, `GetEngagementState()`, `ResetAll()` |
| `Data/EventLoader.cs` | Loads all JSON events from `Resources/Events/`. Groups by type. Smart selection: no-repeat, difficulty-filtered, shuffled pools. API: `GetEvent(type)`, `GetEasyEvent()`, `GetEventByDifficulty()` |
| `Settings/SettingsManager.cs` | Loads/saves settings JSON to PlayerPrefs. Broadcasts `OnSettingsChanged`. API: `GetSettings()`, `UpdateSettings()` |
| `Settings/ThemeManager.cs` | Applies theme (Standard / Calm / HighContrast) to all `IThemedElement` components on settings change |
| `GameSettings.cs` | Static class, just holds `CalmMode` bool. Synced from SettingsManager |

### Game Controllers

| Script | Role |
|---|---|
| `SpendingGameController.cs` | Manages 3-round shopping loop. `OnDestroy` resets round counters. Calls `BankAccountService.Spend()` (spending balance), `PlayerModelService.RecordSpendingRound()`, `ConsequencePanel.ShowFinalConsequences()` |
| `EmergencyFundController.cs` | Manages 6-week fund loop. `OnDestroy` calls `ResetState()` which resets week counter and resets emergency balance to £500. Calls `BankAccountService` emergency variants only. Calls `PlayerModelService.RecordEmergencyFundRound()` per week + final, `RecordInactivity()` on idle |

### UI Components

| Script | Role |
|---|---|
| `UI/EmergencyFundUIFlow.cs` | State machine for Emergency Fund panels: Tutorial → SavingTier → Event → Feedback → Final. `progressPanel` is inactive by default (activated by `UpdateHUD` when game runs). Wires callbacks to controller |
| `UI/SpendingGameUI.cs` | Manages Shopping List panels (shopping / feedback / consequence) |
| `UI/MoneyCounter.cs` | Budget bar + spend display. Colors: green (safe) / yellow (70%) / red (90%). Calm mode: no animation |
| `UI/ConsequencePanel.cs` | Final consequences screen. `ShowFinalConsequences()` evaluates 3 criteria: essentials coverage (fedAllRounds), treat count, and budget. Title prioritises essentials first. 7-case narrative covers all combinations. Calm mode: encouraging language |
| `EmergencyFundConsequencePanel.cs` | 4-panel end-screen: fund total → goal explanation → 8-month simulation → lesson |
| `UI/DuckReaction.cs` | Rubber duck mascot. 8 emotions (Happy, Sad, Excited, Worried, Thinking, Celebrating, Shocked, Neutral), each with color tint + bounce. Calm mode: minimal bounce, negative emotions → Neutral |
| `DuckReactionBackgroundChanger.cs` | Swaps background based on duck trigger string (payday/emergency/bonus/etc). Disabled in Calm mode |
| `UI/StarRating.cs` | 0–3 star display. Uses ★/☆ TMP text fallback if no sprites assigned. Calm mode: gentle reveal, no punch scale |
| `UI/BankHud.cs` | Shows current spending balance (uses `GetBalance()`) |
| `UI/BankTransactionList.cs` | Scrollable list of last 10 transactions |
| `UI/FloatingMoneyText.cs` | Animated floating £ text on money change. Disabled in Calm mode |

### Debug

| Script | Role |
|---|---|
| `Debug/DebugOverlay.cs` | Press **D** in play mode to toggle. Shows: engagement state, overspend count, treat avg, streaks, idle count, spending balance, emergency balance, current scene. Present in Home.unity (persists via DontDestroyOnLoad) and EmergencyFund.unity |

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
| `SceneLoader.cs` / `LoadScene.cs` | Generic scene load helpers (LoadHome, LoadSpending, LoadGameChoice, etc.) |
| `LoadEmergencyFundGame.cs` | Loads EmergencyFund scene |
| `CredentialsPanel.cs` | Credits overlay |
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
    ├─ BankAccountService.Spend() — spending balance only
    ├─ ConsequencePanel.ShowFinalConsequences(totalSaved, totalOverspent, stars, roundsAllEssentials, totalTreats)
    ├─ PlayerModelService.RecordSpendingRound() — per round
    ├─ DuckReaction (emotional feedback)
    └─ StarRating (per round + final)

EMERGENCY FUND:
  EmergencyFundController
    ├─ EmergencyFundUIFlow (state machine)
    ├─ EventLoader.GetEvent(weekType)
    ├─ BankAccountService.SpendEmergency() / EarnEmergency() — emergency balance only
    ├─ PlayerModelService.RecordEmergencyFundRound() — per week + final
    ├─ PlayerModelService.RecordInactivity() — on 60s idle
    ├─ DuckReaction
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
| `BankBalance` | float | Spending game balance (default 500) |
| `EmergencyBankBalance` | float | Emergency Fund bank balance (default 500, reset on EF scene unload) |
| `EmergencyFundBalance` | int | Emergency Fund pot balance (reset on EF scene unload) |
| `Settings` | string (JSON) | Full SettingsData serialized |
| `ShoppingTutorialShown` | int | Tutorial flag |
| `EmergencyTutorialSeen` | int | EF tutorial flag (key used in EmergencyFundUIFlow) |
| `PM_OverspendCount` | int | PlayerModel: overspend count |
| `PM_TreatRatioAvg` | float | PlayerModel: rolling average treat ratio |
| `PM_InactivityCount` | int | PlayerModel: idle trigger count |
| `PM_FailedStreak` | int | PlayerModel: consecutive failed rounds |
| `PM_SuccessStreak` | int | PlayerModel: consecutive successful rounds |
| `PM_TreatRoundCount` | int | PlayerModel: rounds used in treat avg calculation |

---

## Prefabs

- `DuckCharacter.prefab` — duck sprite/animation container
- `BankHUD.prefab` — bank balance HUD
- `TransactionRow.prefab` — single transaction list item

---

## Known Bugs (2026-03-16)

| # | Bug | Notes |
|---|---|---|
| 1 | Duck mascot grows unboundedly on rapid button clicks | Pop animation stacks when button is clicked fast — duck scales out of scene |
| 2 | Back button navigates incorrectly | E.g. Settings → Back goes to GameChoice instead of Home. Back behavior inconsistent across scenes |
| 3 | No option to go home or replay after finishing a game | Game ends on consequence screen with no navigation |
| 4 | Progress page is completely missing | Scene exists in build (index 4) but has no content/functionality |
| 5 | Calm Mode doesn't work | Toggle exists but effects not applying |
| 6 | Inactivity detection not triggering hints | Feature was built (PlayerModelService.RecordInactivity) but hints/prompts not firing |

## Other To-Do

| Priority | Issue |
|---|---|
| 🟡 Verify | JSON event files exist in `Resources/Events/` |
| 🟡 Verify | ThemeConfig ScriptableObject assets exist (StandardTheme.asset, CalmTheme.asset, HighContrastTheme.asset) |
| 🟡 Verify | Audio assets wired up (settings framework exists, assets unclear) |
| 🟠 Test | Touch input (mobile/tablet) not confirmed tested |
| 🔴 Feature | Speech synthesis / TTS integration |
| 🔴 Feature | Internationalization (move strings to JSON) |

---

## Project Meta

- **Engine:** Unity (recent LTS), 2D
- **UI:** TextMeshPro
- **Input:** Old + New Input Systems both enabled (`activeInputHandler: 2`)
- **Events:** JSON files in `Resources/Events/`
- **Persistence:** PlayerPrefs
- **Total Scripts:** ~40 C# + editor tools
- **Version:** v1.0
