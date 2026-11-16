# Donut Devour — HFGE1181 SA1 Casual Top-Down
**Neo Nti · 33HA2608708 · Unity 6000.0.63f1 (URP 2D)**

Eat-and-grow casual top-down game. You are a pink donut: eat crumbs and smaller donuts to grow, avoid bigger donuts, and **survive for 10 minutes**.

- **Android build:** [`APK/Neo_Nti_33HA2608708_HFGE1181_SA1.apk`](APK/) (IL2CPP, ARM64, Android 7.0 / API 24+)
- **Scenes:** `Assets/Scenes/MainMenu.unity` → `Assets/Scenes/Game.unity`
- **Submission cover page:** [`Submission/`](Submission/)

### Installing the APK on a phone
1. Download `APK/Neo_Nti_33HA2608708_HFGE1181_SA1.apk` on the phone (or copy it over USB).
2. Open it and allow "Install unknown apps" for your browser/file manager when Android asks.
3. Launch **Donut Devour**. The game plays in landscape.

## Controls
| Platform | Move | Steer | Pause |
|---|---|---|---|
| Phone (touch) | Drag the on-screen joystick (further = faster) | Donut turns to face the joystick | **II** button |
| PC | W / Up arrow | Mouse | Esc / P / **II** button |

## How the brief is met
**Section A – Level design**
- One level (72×48 tiles) with six enclosed buildings, painted with Unity's Tilemap system from the provided `Donut Level Assets.png`.
- Tileset sliced into 128 px tiles (128 PPU = 1 unit per tile); character/food art is 512 px at 512 PPU so everything shares one world scale.
- Floor and Walls are separate Tilemap layers. Floor tiles are chosen per cell so the black outline matches the neighbouring walls (visual/collision parity).
- Walls: `TilemapCollider2D` merged into a `CompositeCollider2D`; a 2-tile outer wall keeps player and enemies inside. Enemy destinations are also clamped to the play area.
- Doorways are 1, 2, 3, 4, 5 and 6 tiles wide, so small donuts can hide in buildings that big enemies can't enter.
- A Tile Palette (`Assets/Tiles/Palette`) is included for hand-editing.

**Section B – Mechanics**
- Player starts at size 1; every 4 things eaten (+25 satiation each, threshold 100) grows the player by 0.25 (capped at 5 so the larger doors still fit).
- Food and enemies spawn at runtime relative to the player's size (food 0.2–1.15×, enemies 0.25–1.5×). Enemies under 25% of the player's size are removed.
- Food overlapping a bigger unit is pulled to its centre and eaten there; the player can't eat food larger than itself.
- Bigger enemies pull the player to their centre (death); smaller enemies are pulled into the player and eaten.
- Enemies wander/patrol randomly with physics so walls stop them.
- All provided managers are used: GameStateManager, SpawnManager, SoundManager, UIManager, SceneNavigation, CameraHandler, FollowTarget, TargetIndicator.

**Feedback:** eat sound + crumb particles (animation events), grow flash animation + sound + particle ring (state machine behaviours), hit sound + glaze splat + colour flash on enemy contact, enemy spawn-pop / idle / bite animations with sound and particles, death burst, background music (stops when you die), click sound on every button, button press animation, HUD satiation bar, "GROW!" pop-up, red/green enemy arrows (danger/prey).

**UI & scenes:** HUD (size, timer counting up from 00:00, survive countdown, enemy arrows), Main Menu (Play / Exit), Pause Menu (Resume / Restart / Quit), Win and Game Over screens, fade transitions between scenes.

## Changes to the provided scripts
Every change is marked with an `// SA1:` comment. New scripts are in `Assets/Scripts/Added/`.

| Script | Change | Why |
|---|---|---|
| `SizeHandler` | Null checks; brackets fixed in the pull formula (`/(distance*distance)`); `pullStrength`; `GetSatiation()` | Walls have no SizeHandler (NullReference); original formula cancelled itself out so the pull never strengthened near the centre |
| `UnitSizeHandler`, `PlayerSizeHandler` | Eat only when the other unit is smaller; player size cap | Shared `isSmaller` flag is reset by *any* trigger exit, letting a small player eat a big enemy |
| `EnemySizeHandler` | Calls `GameFlowManager.PlayerDied` instead of an instant reload | Death feedback + Game Over screen |
| `PlayerController` | Touch-joystick steering; perspective-correct mouse aim; stops when paused; null-safe mouse | `Mouse.current` is null on phones; perspective camera needs a Z distance for `ScreenToWorldPoint` |
| `EnemyAI` | Moves with Rigidbody2D velocity, clamps targets to the play area, re-targets when blocked, faces travel direction | Transform movement walked through walls |
| `SpawnManager` | Enemy/food caps, recursion guard, no spawning on top of the player, stops after Game Over | Unbounded spawning over 10 minutes; possible stack overflow |
| `GameStateManager` | `TogglePause()` for the on-screen button; `Time.timeScale` follows pause | Mobile pause; freezes physics while paused |
| `UIManager` | Separate Game Over state; number formatting | End screens |
| `SceneNavigation` | Uses fade transition | Smooth scene changes, click sound finishes |
| `SpawnParticleOnEnter/Exit` | Falls back to the animated object's position | State machine behaviours can't reference scene Transforms |
| `SoundManager` | `Stop()` | Music stops when the player dies |
| `UnitSizeHandler` | "Reached the centre" uses a small size-relative tolerance (8% of size) instead of a fixed 0.015 units | Found in playtesting: food snaps to the centre in its own trigger callback, but the moving eater's callback often runs first each physics step, so with 0.015 the player could only eat while standing still |
| `EnemySizeHandler` | Rebinds an uninitialised Animator before `SetTrigger("Bite")` | Found in playtesting: "Animator is not playing an AnimatorController" warnings when off-screen enemies ate |

## Playtest log (16 Nov 2026)
| Test | Result |
|---|---|
| Main menu → Play, Exit | ✅ fade transition, click sound |
| Movement (PC W + mouse) and wall collision | ✅ player stops on walls, can't leave the map |
| Touch joystick (Device Simulator, landscape) | ✅ knob steers and moves the donut; PC hint hidden |
| Food pulled to centre and eaten, satiation bar fills | ✅ (after the `UnitSizeHandler` fix above) |
| Enemy arrows | ✅ red = bigger (danger), green = smaller (prey); hidden when the enemy is on screen |
| Pause (Esc/P/II) → Resume / Restart / Quit | ✅ time freezes while paused |
| Eaten by bigger donut → Game Over → Try Again | ✅ death burst + sound, music stops, stats shown |
| Console | ✅ no errors; only unused-variable warnings from the provided scripts |
| Android APK build | ✅ 39.5 MB, built with Unity 6000.0.63f1 |

## Rebuilding
`SA1 > Build Everything` regenerates import settings, tiles, animations, particles, prefabs and both scenes. `SA1 > Build Android APK` builds the APK into `/APK`.
