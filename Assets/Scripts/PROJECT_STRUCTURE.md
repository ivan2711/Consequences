# Project Structure Documentation

## 📁 Folder Organization

### Core/
**Main game controllers and logic**
- `EmergencyFundController.cs` - Emergency fund saving game
- `SpendingGameController.cs` - Weekly budgeting game  
- `ScenarioSpendingController.cs` - Alternative spending scenarios
- `EmergencyFundConsequencePanel.cs` - End game results

### UI/
**User interface components**
- `DuckReaction.cs` - Duck mascot reactions
- `DuckReactionBackgroundChanger.cs` - Dynamic background system
- `MoneyCounter.cs` - Budget display
- `SceneLoader.cs` - Scene navigation
- `ProgressTracker.cs` - Save/load progress

### Tutorials/
**Tutorial and onboarding**
- `EmergencyFundTutorial.cs` - Emergency fund tutorial
- `ShoppingListTutorial.cs` - Shopping tutorial
- `TutorialManager.cs` - Tutorial flow control

### Utils/
**Helper scripts and utilities**
- `MilestoneBackground.cs` - Special achievement backgrounds
- `CloseFeedbackButton.cs` - UI helpers
- `ToggleFixer.cs` - Toggle state management

### Editor/Active/
**Active editor tools**
- `QuickSetupBackgrounds.cs` - Background setup
- `DiagnoseDynamicBackgrounds.cs` - System diagnostics

### _Archive/
**Historical scripts (no longer needed)**
- `OneTimeSetup/` - Setup scripts already used
- `Diagnostics/` - Old diagnostic tools
- `OldBuilders/` - Deprecated builder scripts

---

## 🎮 Core Game Flow

### Emergency Fund Game
1. Start with £0 savings goal: £600
2. 10 weeks of decisions
3. Each week: payday + choices
4. Special events: bonus weeks, emergencies
5. End: consequence screen based on savings

### Spending Game  
1. Budget: £120
2. Shopping list with essentials + treats
3. Choose items to buy
4. Feedback on choices
5. Star rating system

---

## 🦆 Duck System

### DuckReaction Component
- 6 emotions: Happy, Worried, Thinking, Celebrating, Excited, Neutral
- Shows messages with appropriate expressions
- Animated transitions

### DuckReactionBackgroundChanger
- 8 trigger backgrounds based on messages
- Backgrounds persist until next trigger
- Automatic detection from duck messages

---

## 🎨 Background System

### Trigger Messages → Backgrounds
1. "Pay day!" → duck_pay_day.png
2. "Decide wisely..." → duck_decide_wisely.png
3. "Bonus week!" → duck_bonus_week.png
4. "Use your fund!" → duck_use_your_fund.png
5. "Lucky you!" → duck_lucky_you.png
6. "two things" / "decide" → duck_two_things.png
7. "game over" / "try again" → duck_game_over.png
8. "PERFECT" → duck_perfect.png

---

## 🛠 Maintenance Guide

### Adding New Features
1. Keep core game logic in `Core/`
2. UI components go in `UI/`
3. Utilities in `Utils/`
4. Editor tools in `Editor/Active/`

### Debugging
- Use `DiagnoseDynamicBackgrounds` for background issues
- Check Console for "Background changed to..." messages
- Verify components in Inspector

### Best Practices
- Always comment public fields with [Tooltip]
- Use /// <summary> for class documentation
- Keep scripts single-purpose when possible
- Archive old/unused scripts rather than deleting

---

## 📝 Change Log

### 2026-02-03
- Added dynamic background system
- Implemented DuckReactionBackgroundChanger
- Made UI panels semi-transparent (78-90%)
- Cleaned and organized project structure
- Archived 50+ one-time setup scripts

### Previous
- Initial game implementation
- Duck reaction system
- Tutorial system
- Progress tracking
