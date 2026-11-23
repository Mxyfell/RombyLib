# RombyLib

A core modding framework and API facade for [Rombykon](https://store.steampowered.com/app/4238990/Rombykon/), designed for BepInEx 6 (Unity IL2CPP).

[![Steam](https://img.shields.io/badge/Steam-Rombykon-1b2838?style=flat-square&logo=steam)](https://store.steampowered.com/app/4238990/Rombykon/)
[![BepInEx](https://img.shields.io/badge/BepInEx-IL2CPP-orange?style=flat-square)](https://github.com/BepInEx/BepInEx)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE.txt)

---

## Overview

RombyLib provides a safe managed-to-IL2CPP layer for mod developers, eliminating raw pointer handling, unmanaged collection conversions, and game singleton discovery boilerplate.

All game systems are exposed via two static facade entry points:
- **`Romby`** — Player entity, combat helpers, stats, skin rendering, weapon controllers, and local lifecycle hooks.
- **`Game`** — Global systems including saves, world state, Tarot cards, audio, camera, timescale, Roguelike runs, New Game Plus, and local co-op.

---

## Features

### Core & Player Subsystems (`Romby.*`)
- **Player Lifecycle & Stats:** Read and modify movement speed, critical hits, XP, levels, currency, and status effects (*Freeze*, *Burn*, *Dizzy*).
- **Appearance & Skins:** Direct hooks into body parts, facial features, accessories, and pivot transforms (`Body`, `Eyes`, `Mouth`, `Pivots`).
- **Combat & Weapons:** Native mapping for all 16 weapons with specialized wrappers for unique mechanics (*Astro Pulse*, *Treble Clef*, *Big Bucks*, *Bubblewitch*, *The Soul Transmuter*).
- **Captured Monsters:** Read, store, and manage soul-captured monsters and farm inventory.

### Global Game Systems (`Game.*`)
- **Saves (`Game.Saves`):** Slot management, quicksave, autosave, and safe state synchronization between live memory and save structures.
- **World & Progression (`Game.World`, `Game.Progress`):** Day/night cycle, wave control, crystal durability, bosses, areas, and story event flags.
- **Tarot Cards (`Game.Tarot`):** Offer activations, rerolls, discovered card index, and passive power multipliers.
- **Roguelike Mode (`Game.Roguelike`, `Game.NewGamePlus`):** Seeded runs, run timers, wave rewards, infinite mode, and New Game Plus difficulty scaling.
- **Audio & Music (`Game.Audio`, `Game.Music`):** Master/SFX/Music volume channels, custom sound playback, playlist context switching, and radio tracks.
- **Camera (`Game.Camera`):** Cinemachine virtual camera integration, zoom control, target tracking, and procedural noise shake.
- **Time Scale (`Game.Time`):** Scoped pause requests, timescale locking, and timed slow-motion effects.
- **Cooperative Play (`Game.Coop`):** Player 2 join states, controller assignment, and secondary player references.
- **UI Framework (`Game.UI`):** Cyberpunk-styled floating panels (`UIPanel`), sliders, toggles, buttons, and notifications.
- **Mod Settings (`Game.Mods`):** Declarative mod registration via attributes (`[Manage]`, `[ModAction]`), automatic UI injection into the Settings menu, and JSON configuration storage.

---

## Installation

1. Install **BepInEx 6 (IL2CPP)** for Rombykon.
2. Download `RombyLib.dll` and place it into:
   ```text
   Rombykon/BepInEx/plugins/RombyLib/
   ```

---

## Quickstart

Add `RombyLib.dll` and BepInEx assemblies to your project references, then declare `ru.mxyfell.rombylib` as a dependency:

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using RombyLib;
using UnityEngine;

namespace MyRombyMod
{
    [BepInPlugin("com.example.myrombymod", "My Romby Mod", "1.0.0")]
    [BepInDependency("ru.mxyfell.rombylib")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            Log.LogInfo("Mod loaded!");

            // Subscribe to global game events
            Game.Events.OnDayChanged += day => Log.LogInfo($"Day changed: {day}");
            Game.Events.OnMoneyChanged += (total, delta) => Log.LogInfo($"Money: {total} ({delta})");
        }

        public void Update()
        {
            if (!Romby.IsLoaded) return;

            // Player stat adjustment
            Romby.Stats.MoveSpeed = 16f;

            // Weapon interaction
            if (Romby.Weapons.CurrentWeapon == WeaponType.TrebleClef)
            {
                Romby.Weapons.TrebleClef.StartFlame();
            }

            // Global camera and time manipulation
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Game.Time.ApplySlow("bullet_time", 0.3f, 3.0f);
                Game.Camera.Shake(amplitude: 2.0f, frequency: 3.0f);
            }
        }
    }
}
```

---

## Weapon Mapping

| ID | Weapon Name | Type | Access / Helper |
|:---|:---|:---|:---|
| 1 | Astro Pulse | Player | `Romby.Weapons.AstroPulse` |
| 2 | Satellite Shield | Player | `Romby.Weapons.SatelliteShield` |
| 3 | Solar Mine | World | `Romby.Weapons.ActiveSolarMines` |
| 4 | Moonerang | Player | `Romby.Weapons.Moonerang` |
| 5 | Existential Void | World | `Romby.Weapons.DestroyAllExistentialVoids()` |
| 6 | Uncle Frank | World | `Romby.Weapons.ActiveUncleFranks` |
| 7 | Tesla Pawn | World | `Romby.Weapons.ActiveTeslaPawnsCount` |
| 8 | Plushinator 3000 | Player | `Romby.Weapons.Plushinator3000` |
| 9 | BitBang | Player | `Romby.Weapons.BitBang` |
| 10 | Treble Clef | Player | `Romby.Weapons.TrebleClef` |
| 11 | Big Bucks | Player | `Romby.Weapons.BigBucks` |
| 12 | Gachickpon | Player | `Romby.Weapons.Gachickpon` |
| 13 | The Screamer | Player | `Romby.Weapons.TheScreamer` |
| 14 | The Soul Transmuter | Player/World | `Romby.Weapons.TheSoulTransmuter` |
| 15 | Divine Relocator | Player | `Romby.Weapons.DivineRelocator` |
| 16 | Bubblewitch | Player | `Romby.Weapons.Bubblewitch` |

---

## Credits

- **Rombykon** developed by Guiliam.
- Built with **BepInEx** and **HarmonyX**.

---

## License

Distributed under the MIT License. See [LICENSE.txt](LICENSE.txt) for details.
