# HIJIN-BATTLE Submission Checklist

## Required Submission Items

### 1. GitHub Repository

Submit the full Unity project repository, not only loose scripts.

Include:

- `Assets`
- `Packages`
- `ProjectSettings`
- `README.md`
- `CONTRIBUTION.md`
- Any development log or evidence folder

Do not include:

- `Library`
- `Temp`
- `Obj`
- `Logs`
- `UserSettings`
- Windows build output unless specifically requested

### 2. Windows Executable Build

Build through Unity:

1. Open `File > Build Settings`.
2. Select `PC, Mac & Linux Standalone`.
3. Target Platform: `Windows`.
4. Architecture: `x86_64`.
5. Make sure required scenes are in build settings.
6. Click `Build`.
7. Zip the entire generated build folder.

The submitted build should include:

- The generated `.exe`
- The `_Data` folder
- `UnityPlayer.dll`
- Any other files Unity generates beside the executable

Do not submit only the `.exe`.

### 3. Documentation / Evidence

Submit:

- `README.md`
- `CONTRIBUTION.md`
- Screenshots or short video showing:
  - Main menu
  - Settings panel
  - Practice mode
  - Main game combat
  - Guard bar / HP UI
  - Enemy behavior
  - Audio/SFX demonstration if possible
- Development log or commit history

## Changes Since the Previous GitHub Package

The previous GitHub package from the midterm handoff already included practice mode, `maingame`, enemy AI expansion, enemy visual setup, and comments.

The latest submit-ready additions include:

### Audio

- Added scene-based BGM controller.
- Added BGM categories:
  - Main menu
  - Practice mode
  - Story
  - Battle
  - Boss battle
- Added sword swing SFX.
- Added successful guard SFX using `defence.wav`.
- Stored audio under Unity `Assets/Resources` so the build can include it.

### Combat Polish

- Improved enemy close-range behavior so enemies do not stand still beside the player.
- Improved Hard mode pressure so enemies act more aggressively in attack range.
- Improved enemy attack timing with clearer wind-up, swing timing, impact point, and recovery.
- Improved enemy stagger so taking damage interrupts defense, evasion, and active attack routines.
- Improved player hit cleanup so taking damage cancels conflicting defend/roll/charge states.

### UI and UX

- Added opening controls panel behavior in `maingame`; it appears at the start of gameplay and hides after a short time.
- Improved settings panel readability and layout.
- Added opaque themed settings background.
- Fixed title/menu font layout issues.
- Adjusted title scene and main menu scene typography.

### Visual Style

- Applied the custom HIJIN-style font to title/menu UI.
- Adjusted scene rendering and environment presentation toward the night/moon samurai theme.
- Added movement boundaries/air walls to keep the player inside the intended play area.

### Settings

- Added/expanded settings controls:
  - Master volume
  - Mouse sensitivity
  - Fullscreen
  - Quality
  - Reset settings
  - Reset progress

## Latest Files Worth Committing

Scripts:

- `Assets/Scripts/PlayerController.cs`
- `Assets/Scripts/EnemyController.cs`
- `Assets/Scripts/EnemyVisualController.cs`
- `Assets/Scripts/MainGameUIController.cs`
- `Assets/Scripts/MainGameBgmController.cs`
- `Assets/Scripts/MainGameSfxController.cs`
- `Assets/Scripts/GameFontController.cs`
- `Assets/Scripts/MainGameSkyboxController.cs`
- `Assets/Scripts/UI/MenuPanelController.cs`

Scenes:

- `Assets/Scenes/maingame.unity`
- `Assets/Scenes/DockThing.unity`
- `Assets/Scenes/MainMenuScene.unity`
- `Assets/Scenes/TitleScene.unity`

Resources:

- `Assets/Resources/Audio/BGM/BGM_MainMenu.wav`
- `Assets/Resources/Audio/BGM/BGM_PracticeMode.wav`
- `Assets/Resources/Audio/BGM/BGM_Story.wav`
- `Assets/Resources/Audio/BGM/BGM_Battle.wav`
- `Assets/Resources/Audio/BGM/BGM_BossBattle.wav`
- `Assets/Resources/Audio/SFX/SFX_SwordSwing.wav`
- `Assets/Resources/defence.wav`
- `Assets/Resources/Fonts/HIJIN_Style.ttf`
- `Assets/Resources/SettingsPanelBackground.png`
- Matching `.meta` files for all Unity assets

Project settings:

- `ProjectSettings/EditorBuildSettings.asset`
- Any changed settings files required for scenes or input/build configuration

## Evidence Standard From Class Slides

Good evidence should show:

- A Unity scene.
- A playable feature.
- A script or system.
- A level design change.
- A feedback improvement.
- GitHub commits.
- README explanation.
- Reference transformation table.
- Playtest notes.
- Next action.

Weak evidence includes:

- Imported scene with no gameplay.
- Asset pack used unchanged.
- AI code that cannot be explained.
- Tutorial copied exactly with no adaptation.
- GitHub updated only at the end.
- README with no credits.
- A good-looking game where contribution is unclear.

## Playability Check

Before final submission, confirm:

- The player can start the game.
- The player can understand the goal.
- The player can control the character.
- The player can interact with the enemy/world.
- The player can succeed or fail.
- The player can restart or continue.
- The build can run outside Unity.

## Final Reminder

In the submission or presentation, clearly show:

- What I made.
- How it works.
- What is mine.
- What I tested.
- What changed from the original project/assets.
- Where the evidence is.
