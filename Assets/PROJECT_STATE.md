# PROJECT_STATE.md
*Last Updated: 2026-01-30 14:47 GMT*

## 🎮 CURRENT STATE
**Status:** ~85% Complete - Build settings need fixing, testing required
**Target:** UK-based Financial Literacy Game for Autistic Children
**Currency:** £ (British Pounds) - NEEDS VERIFICATION
**Last Scan:** 2026-01-30 14:47 GMT

---

## 📁 ACTIVE SCENES (Build Order)
1. **Home.unity** (Build Index 0) - Title screen with PlayButton, ProgressButton, SettingsButton
2. **GameChoice.unity** (Build Index 1) - Choose between 2 games
3. **Spending.unity** (Build Index 2) - Shopping List game (£8 budget, essentials vs treats)
4. Progress.unity (Build Index 4) - Currently enabled in build
5. Settings.unity (Build Index 5) - Currently enabled in build
6. ScenarioSpending.unity (Build Index 6) - Currently enabled in build

⚠️ **NOTE:** EmergencyFund.unity EXISTS but is NOT in build settings!
⚠️ **Monthly Choices.unity** (Build Index 3) - DISABLED in build

---

## 🔧 KEY SCRIPTS

### Game Controllers
- `EmergencyFundGame.cs` - Manages Emergency Fund game loop & scoring
- `SpendingGameController.cs` - Manages Shopping List game, budget tracking, £8 limit
- `FutureSnapshotPanel.cs` - Shows good/bad consequences at game end (both games)

### UI Components
- `MoneyCounter.cs` - Displays current money with £ symbol
- `StarRating.cs` - Shows 0-3 stars based on performance
- `DuckReaction.cs` - Duck character visual feedback
- `ShoppingListTutorial.cs` - Tutorial panel for Shopping List game
- `ProgressBar.cs` - Visual progress towards £1000 goal

### Scene Navigation (NEEDS VERIFICATION)
- `SceneLoader.cs` - Used on Home scene buttons (PlayButton, ProgressButton, SettingsButton)
- `LoadEmergencyFund.cs` - Loads Emergency Fund scene (may not work - scene not in build!)
- `LoadSpendingGame.cs` - Loads Shopping List scene
- `LoadGameChoice.cs` - Returns to Game Choice screen  
- `LoadHome.cs` - Returns to Home screen

### Helper Scripts
- `ShoppingItem.cs` - Individual item data (name, price, isEssential)
- `MonthlyChoice.cs` - Emergency Fund scenario choices
- 154 total scripts in Assets/Scripts/ (many are editor tools)

---

## 🚧 CURRENT BLOCKERS
1. ❌ **EmergencyFund.unity NOT in build settings** - Scene exists but won't be accessible
2. ❌ No mobile build tested yet
3. ❌ No sound effects or music  
4. ⚠️ Haven't verified what PlayButton, ProgressButton, SettingsButton do
5. ⚠️ Need to verify currency (£ vs $) in all active scenes

---

## ✅ NEXT 3 TASKS
1. **Add EmergencyFund.unity to build settings** - Critical! Game is built but not accessible
2. **Test full flow** - Home → what does each button do? → verify games work
3. **Verify £ currency** - Check all scenes use pounds, not dollars

---

## 💷 CURRENCY REMINDER
**ALWAYS USE £ (British Pounds)**
- Emergency Fund: £1000 goal, £50-150 monthly
- Shopping List: £8 budget
- All UI displays £ symbol

---

## 🎨 DESIGN RULES (Autism-Friendly)
✅ Clear, predictable structure
✅ One choice at a time
✅ Immediate visual feedback
✅ Color coding (green=good, red=bad, orange=neutral)
✅ Simple, large text
❌ NO: confusing navigation, unclear consequences, currency inconsistency

---

## 🏆 QUALITY STATUS (ESTIMATED - NEEDS VERIFICATION)
- Spending (Shopping List): 8/10 (Good - has tutorial, £8 budget)
- EmergencyFund: Status unknown - NOT IN BUILD SETTINGS
- Home Scene: Has 3 buttons (Play/Progress/Settings) - destinations unknown
- Overall: ~85% - Core games exist but build configuration incomplete

---

## 📝 QUICK REFERENCE
- **Project Location:** `/Users/vanko/Desktop/FYP/Consequences/`
- **Unity Version:** Recent LTS
- **UI System:** TextMeshPro (TMP)
- **Input:** Both old & new Input System enabled
- **Platform Target:** Mobile (2D)
- **Total Scripts:** 154 (includes editor tools)

---

## 🎓 EDUCATIONAL VALUE

### Emergency Fund teaches:
- Long-term saving habits
- Delayed gratification
- Emergency preparedness
- Consequences of debt

### Shopping List teaches:
- Needs vs wants distinction
- Budget management
- Prioritization (essentials first)
- Real-world shopping decisions

---

## 🔑 KEY DECISIONS MADE (Historical)
1. **Chose Shopping List over Scenario-based** - More educational, clearer needs vs wants
2. **£8 budget for shopping** - Realistic UK weekly grocery amount
3. **£1000 emergency fund goal** - Standard UK recommendation
4. **Tutorial panels on first load** - Essential for autism-friendly design
5. **Two games total** - Quality over quantity, both teach different lessons

---

## ⚡ CRITICAL ISSUES FOUND (2026-01-30 Scan)
1. **EmergencyFund.unity exists but NOT in build settings** - Scene file exists at Assets/Scenes/EmergencyFund.unity but is not in the build, making it inaccessible!
2. **Home scene has PlayButton not "Start"** - Previous documentation incorrect
3. **Multiple buttons on Home** - PlayButton, ProgressButton, SettingsButton - need to verify destinations
4. **Progress.unity and Settings.unity ARE enabled** in build - previous docs said to ignore them
5. **ScenarioSpending.unity IS enabled** in build - this might conflict with Spending.unity

## ⚡ QUICK START FOR NEW CHAT
1. Read this document first
2. Open Unity project at `/Users/vanko/Desktop/FYP/Consequences/`
3. **FIRST: Add EmergencyFund.unity to build settings!**
4. Load `Home.unity` scene  
5. Press Play and test: What does each button do?
6. Verify currency (£ vs $) in ALL scenes
7. Check if games are still functional

---

## 🔧 QUICK FIX - Add EmergencyFund to Build Settings

**Manual Method:**
1. File → Build Settings
2. Click "Add Open Scenes" while EmergencyFund.unity is open
OR
3. Drag Assets/Scenes/EmergencyFund.unity into the list

**Or ask the AI to create a script to do this automatically**

---

## 📊 PROJECT HEALTH CHECK
- ✅ Spending.unity scene exists and in build
- ❌ EmergencyFund.unity NOT in build settings
- ⚠️ Unknown if Progress/Settings scenes are functional
- ⚠️ Unknown what Home buttons actually do
- ⚠️ Need to verify £ currency throughout
- ⚠️ No mobile build yet

---

**USE THIS DOCUMENT for your next AI chat session!** 
**It reflects the ACTUAL current state of your Unity project as of 2026-01-30.**