<div align="center">

# 💎 RombyLib

**A modern, lightweight modding library for [Rombykon](https://store.steampowered.com/app/4238990/Rombykon/)**

[![Steam](https://img.shields.io/badge/Steam-Rombykon-1b2838?style=flat-square&logo=steam)](https://store.steampowered.com/app/4238990/Rombykon/)
[![BepInEx](https://img.shields.io/badge/BepInEx-IL2CPP-orange?style=flat-square)](https://github.com/BepInEx/BepInEx)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE.txt)

*Empowering developers to create native, clean, and intuitive mods for Rombykon.*

</div>

---

## 🚀 Features

- **⚡ Zero Boilerplate:** Access player data anywhere via the global static `Romby` class.
- **🏃 Stats & Mechanics:** Effortlessly tune speed, critical strike formulas, statuses (*Freeze*, *Burn*, *Dizzy*).
- **🎨 Skin Controller:** Direct hooks into body parts, facial sprites, and pivots (`Body`, `Eyes`, `Mouth`, `Pivots`).
- **⚔️ All 16 Weapons:** Full unified API using the game's official English names.
- **🎯 Specialized Helpers:** Custom methods for unique weapon mechanics (*Treble Clef* notes/angles, *Big Bucks* charge, *Bubblewitch* capture system).
- **📡 Event Driven:** React to game states instantly with `WeaponsController.OnEquipped`.

---

## 📦 Quick Start

### 1. Installation
Drop `RombyLib.dll` into your `BepInEx/plugins/` directory.

### 2. Mod Example

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using RombyLib;
using UnityEngine;

namespace MyRombyMod
{
    [BepInPlugin("com.author.myrombymod", "My Romby Mod", "1.0.0")]
    [BepInDependency("ru.mxyfell.rombylib")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            // Listen for weapon changes
            WeaponsController.OnEquipped += weapon =>
            {
                Log.LogInfo($"Switched to: {weapon}");
            };
        }

        // Example interaction
        public void Update()
        {
            if (!Romby.IsLoaded) return;

            // Simple stat tweaks
            Romby.Stats.MoveSpeed = 15f;

            // Change skin appearance
            Romby.Skin.Body.color = Color.cyan;

            // Interact with weapons directly
            if (Romby.Weapons.CurrentWeapon == WeaponType.TrebleClef)
            {
                float angle = Romby.Weapons.TrebleClef.AimAngle;
                Romby.Weapons.TrebleClef.StartFlame();
            }
        }
    }
}
```


### 🗡️ Supported Weapons
| ID | Name | Type | Controller / Access |
|---|---|---|---|
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
| 14 | The Soul Transmuter | World | `Romby.Weapons.ActiveSoulTransmuters` |
| 15 | Divine Relocator | Player | `Romby.Weapons.DivineRelocator` |
| 16 | Bubblewitch | Player | `Romby.Weapons.Bubblewitch` |


### 🤝 Credits & Acknowledgements
Rombykon developed by Guiliam.  
Built with BepInEx and Harmony.

### 📜 License
Licensed under the MIT License.
Licensed under the MIT License.
