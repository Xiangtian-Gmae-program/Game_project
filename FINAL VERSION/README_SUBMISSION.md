# HIJIN-BATTLE

## Short Description

`HIJIN-BATTLE` is a Unity 3D samurai action game prototype set in a night-time Japanese duel atmosphere. The project includes a playable main battle mode, a practice mode, enemy AI behavior, guard/roll/jump combat, UI feedback, settings, background music, sound effects, and a styled menu flow.

## Controls

| Input | Action |
| --- | --- |
| W / A / S / D | Move |
| Mouse | Camera / direction control |
| Left Mouse Button | Attack / charge attack |
| Right Mouse Button | Jump |
| E | Defend |
| Space | Roll |
| Esc | Pause |

## How to Run

### Run from Unity

1. Open the project folder in Unity.
2. Use the Unity version listed in the project settings.
3. Open `TitleScene` or `MainMenuScene`.
4. Press Play.

### Run from Windows Build

1. Download or unzip the Windows build folder.
2. Keep the `.exe`, `_Data` folder, `UnityPlayer.dll`, and other generated files together.
3. Run `HIJIN-BATTLE.exe` or the generated executable name.

Do not submit only the `.exe` file. Submit the full zipped Windows build folder.

## Unity Version

Use the Unity version recorded in `ProjectSettings/ProjectVersion.txt`.

## Current Status

The current version is a playable vertical slice with:

- Title/menu entry.
- Main menu.
- Practice mode.
- Main-game battle mode.
- Difficulty selection.
- Records/continue/new game UI.
- Player HP and guard bar.
- Enemy HP/target display.
- Practice statistics panel.
- Pause and result flow.
- Scene-based BGM.
- Sword swing and guard success SFX.
- Custom font and themed UI.
- Improved enemy AI and combat feedback.

## Main Features

### Practice Mode

- Non-lethal training enemy.
- Damage and hit statistics.
- Guard/evade practice feedback.
- Controls panel.
- Pause, continue, and return-to-menu flow.

### Main Game

- Formal battle scene.
- Enemy difficulty and behavior profiles.
- Guard bar visualization.
- Player attack, charged attack, jump, defend, and roll.
- Enemy patrol, alert, chase, strafe, attack, defend, evade, stagger, return, and death logic.
- HUD, target information, objective text, result panel, and opening controls panel.

### Audio

- Main menu BGM.
- Practice mode BGM.
- Story BGM.
- Battle BGM.
- Boss battle BGM.
- Sword swing SFX.
- Guard success SFX.

### Settings

- Master volume.
- Mouse sensitivity.
- Fullscreen toggle.
- Quality level.
- Reset settings.
- Reset progress.

## Known Issues

- Some enemy animations still depend on available third-party animation resources.
- More sound effects should be added for hit, roll, UI click, death, and guard break.
- More content is required for a full final game, including more levels, story scenes, and final balancing.

## Credits

This project uses imported Unity assets for characters, animations, environment, font, music, and sound effects. Imported assets were adapted into a custom playable structure through scene design, code changes, UI work, AI behavior, and audio integration.

## AI / Tutorial / Template Use

AI assistance was used to help  organize documentation, generate photos and support debugging. The final gameplay direction, testing feedback, and design decisions were guided by the project owner.

## Development Evidence

Evidence of contribution can be found in:

- Modified Unity scenes.
- Custom and modified scripts.
- Runtime UI systems.
- Audio resource integration.
- Settings and font controllers.
- Git commit history.
- Contribution document.
- Playtest screenshots or video.
