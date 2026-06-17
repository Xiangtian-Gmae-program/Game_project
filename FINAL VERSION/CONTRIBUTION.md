# HIJIN-BATTLE Contribution Statement

## Project Overview

`HIJIN-BATTLE` is a short 3D action game prototype built in Unity. The game focuses on a night-time Japanese samurai duel atmosphere, with a playable main mode, a practice mode, enemy difficulty differences, guard/roll/jump combat abilities, UI feedback, music, sound effects, and a complete menu-to-game flow.

The current version is no longer only an imported scene or an unchanged asset pack. It contains custom gameplay logic, scene flow, combat UI, enemy AI tuning, practice-mode systems, audio integration, settings, and documentation.

## What I Personally Designed, Built, Modified, Tested, Improved, and Explained

### Designed

- I designed the overall game direction as a Japanese night/moon samurai duel game.
- I designed the structure of two playable modes:
  - `Practice Mode`, used to test combat actions and enemy response.
  - `Main Game`, used as the formal combat mode.
- I designed the intended main-game loop: menu, difficulty selection, battle, victory/failure result, retry/continue, and progress tracking.
- I designed the enemy role separation: normal enemy, guard enemy, elite enemy, and boss-style behavior.
- I designed a clearer control scheme:
  - WASD: Move
  - LMB: Attack / Charge
  - RMB: Jump
  - E: Defend
  - Space: Roll
  - Esc: Pause

### Built

- I built a formal `maingame` scene based on the original combat prototype.
- I built a runtime main-game HUD with player HP, guard bar, target information, objective text, feedback messages, pause panel, and result panel.
- I built a practice-mode statistics interface that tracks damage, hit count, blocks, evades, and combat testing data.
- I built a settings system for:
  - Master volume
  - Mouse sensitivity
  - Fullscreen toggle
  - Quality level
  - Reset settings
  - Reset progress
- I built scene-based background music routing for menu, practice, story, battle, and boss contexts.
- I built one-shot SFX playback for sword swing and successful guard.
- I built a font controller to apply the custom game-style font to title/menu UI while avoiding unreadable heavy body text in gameplay.

### Modified

- I modified the player controller to support a more complete combat kit:
  - Attack and charged attack
  - Jump
  - Defend with guard value
  - Roll
  - Guard/roll invulnerability or damage avoidance logic
  - Action lock cleanup after taking damage
- I modified the enemy controller to make enemy behavior less mechanical:
  - Patrol
  - Alert
  - Chase
  - Strafe
  - Attack
  - Defend
  - Evade
  - Stagger
  - Return to spawn
  - Dead
- I modified enemy behavior so close-range situations force a response instead of allowing the enemy to stand still.
- I modified enemy attack timing so the wind-up, sword swing sound, damage point, and recovery feel more deliberate.
- I modified enemy hit feedback so damage interrupts defense, evasion, and attack routines more clearly.
- I modified the menu and title scene layout to fit the game title and custom font.
- I modified the settings panel layout and gave it an opaque, game-style background.
- I modified the `maingame` environment presentation through lighting/rendering adjustments and movement boundaries.
- I modified the controls help panel so it appears at the beginning of gameplay and disappears after a short time.

### Tested

- I tested the practice-mode flow from `Practice Mode -> PracticeIntro -> Start Practice`.
- I checked and fixed UI visibility problems caused by runtime font loading and panel creation.
- I tested and adjusted scene routing for the main menu, practice mode, and main-game entry.
- I tested enemy visibility after model replacement and animation controller changes.
- I tested player defense input after changing it from mouse middle button to `E`.
- I tested guard bar recovery and later adjusted recovery speed.
- I checked common Unity runtime issues such as invalid built-in font paths, missing panels, NavMesh-agent warnings, and scene UI overlap.

### Improved

- I improved playability by making the player able to start the game, understand the goal, control the character, succeed/fail, pause, retry, and continue.
- I improved readability by adding a clear HUD and controls panel.
- I improved combat feel through action locks, hit interruption, guard feedback, attack wind-up, and more aggressive enemy behavior in Hard mode.
- I improved project completeness by adding BGM, SFX, font styling, settings, and a more polished menu presentation.
- I improved GitHub evidence by organizing changed scripts, scenes, resources, and documentation into clear categories.

### Explained

- I documented how the game is structured.
- I listed major scripts and their responsibilities.
- I wrote this contribution statement to explain what was changed, why it matters, and where the evidence is.

## Reference Transformation Table

| Original / Imported Element | My Transformation |
| --- | --- |
| Original combat prototype scene | Converted into practice mode and then used as the base for formal `maingame`. |
| Existing player controller | Extended with jump, defend, roll, guard value, action locks, and hit cleanup. |
| Existing enemy model/asset package | Integrated as runtime enemy visual types for normal, guard, elite, and boss-style enemies. |
| Basic enemy chasing/attacking | Expanded into state-based behavior with patrol, alert, chase, strafe, attack, defend, evade, stagger, and return. |
| Basic menu | Expanded with Start, Practice Mode, Settings, Quit, Records, Difficulty, Continue, and New Game. |
| Plain UI panels | Restyled into rectangular dark/gold UI matching the samurai night theme. |
| Silent combat prototype | Added BGM by scene context and SFX for sword swing and successful guard. |
| Default UI font | Replaced with a custom style font and adjusted title/menu layout. |

## Current Playable Features

- Title/menu flow.
- Main menu with difficulty, records, continue, new game, settings, and practice entry.
- Practice mode with combat statistics and non-lethal enemy behavior.
- Main game battle scene with enemy AI and result flow.
- Player attack, charged attack, jump, defend, and roll.
- Guard bar and HP UI.
- Enemy HP/target UI.
- Opening controls panel that disappears after a short time.
- Pause/continue/retry/main-menu flow.
- Scene-based background music.
- Sword swing and guard success sound effects.
- Custom font and themed settings background.

## Evidence Locations

Important evidence files in the Unity project include:

- `Assets/Scenes/maingame.unity`
- `Assets/Scenes/DockThing.unity`
- `Assets/Scenes/MainMenuScene.unity`
- `Assets/Scenes/TitleScene.unity`
- `Assets/Scripts/PlayerController.cs`
- `Assets/Scripts/EnemyController.cs`
- `Assets/Scripts/EnemyVisualController.cs`
- `Assets/Scripts/MainGameUIController.cs`
- `Assets/Scripts/MainGameBgmController.cs`
- `Assets/Scripts/MainGameSfxController.cs`
- `Assets/Scripts/GameFontController.cs`
- `Assets/Scripts/UI/MenuPanelController.cs`
- `Assets/Resources/Audio/BGM`
- `Assets/Resources/Audio/SFX`
- `Assets/Resources/defence.wav`
- `Assets/Resources/Fonts/HIJIN_Style.ttf`
- `Assets/Resources/SettingsPanelBackground.png`
