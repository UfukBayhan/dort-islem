# Four Operations

A Unity and C# math game with seven modes built around finding the missing number. Players complete arithmetic expressions by choosing the correct answer and progressing through levels.

**Game design and development: [Ufuk Bayhan](https://github.com/UfukBayhan)**

The in-game interface is in Turkish.

![Four Operations — mode selection](Docs/mode-selection.png)

## Gameplay

Each question hides an operand or the result of an arithmetic expression. Choose the correct number from four options. Correct answers advance the level goal; incorrect answers increase the mistake counter and let you try the same question again. The first level requires three correct answers. Number ranges and goals change as you progress.

- A starting screen with seven selectable modes.
- Generated arithmetic questions and distractor answers.
- Level progression, elapsed time, and mistake tracking.
- Visual and audio feedback for correct and incorrect answers.
- Restart controls and navigation back to mode selection.
- A Unity UI layout that adapts to different window sizes.

## Game modes

| Mode | Operations |
| --- | --- |
| Addition | + |
| Subtraction | − |
| Multiplication | × |
| Division | ÷ |
| Addition + Subtraction | +, − |
| Multiplication + Division | ×, ÷ |
| Four Operations | +, −, ×, ÷ |

## Screenshots

Captured from the running Windows build.

### Addition

![Addition gameplay](Docs/Toplama.png)

### Division

![Division gameplay](Docs/Bolme.png)

### Four Operations

![Four Operations mixed mode](Docs/DortIslem.png)

## Getting started

1. Clone the repository or download it as a ZIP archive.
2. Add the project folder through Unity Hub.
3. Open it with **Unity 6000.3.9f1** and wait for package imports to finish.
4. In the Project panel, double-click `Assets/Game/Scenes/ModeSelect.unity`.
5. Press **Play** and select a game mode.

If Unity opens an empty `Untitled` scene, open the starting scene in step 4. If the Game tab is missing, use **Window → General → Game**.

Use the mouse to select answers. The **Modlar** (Modes) button or **Escape** returns to mode selection. **Yeniden başla** (Restart) restarts the current mode from its first level. The timer and mistake counter display values for the current level.

## Project structure

| File / folder | Purpose |
| --- | --- |
| `Assets/Game/Scenes` | Starting screen and seven game scenes |
| `MathGame.cs` | Question generation, answer options, and difficulty progression |
| `StemGameManager.cs` | Shared game flow and session management |
| `ModeNavigation.cs` | Scene navigation between modes |
| `SessionHud.cs` | Level, timer, mistake counter, and restart controls |
| `FeedbackEffect.cs` | Answer feedback |
| `Assets/Resources/ModeConfiguration.json` | Initial settings for each mode |

The interface uses Unity UI and TextMesh Pro. The game runs locally without an account or server connection. This portfolio edition uses simple shapes, text, and arithmetic symbols for its interface.

## Building and validation

Select the Windows target through **File → Build Profiles** to create a build. `ModeSelect` should be the starting scene. Build outputs and Unity-generated cache folders are excluded from the repository.

`PreviewBuilder.Build` in `Assets/Editor/PreviewBuilder.cs` regenerates the scenes and creates a Windows development build. **This tool overwrites generated scenes**; commit or back up any manual scene edits before running it.

Running a development build with `--preview-checks` checks question and answer consistency, duplicate-answer protection, level transitions, restart behavior, and menu navigation across all seven modes. These checks do not run during normal gameplay.

Validation completed: the Windows build succeeded, and 35 question/answer checks plus game-flow checks passed across all seven modes. Mode selection and advancing to the next question after a correct answer were also tested in the Unity Editor. Mobile device testing has not been performed.

## Third-party components

Liberation Sans is distributed under the SIL Open Font License. Its license text is included at `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`. Existing Unity and TextMesh Pro notices are preserved.

This repository is shared for portfolio presentation and review. No separate open-source license has been granted.
