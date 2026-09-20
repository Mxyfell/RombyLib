<div align="center">

# 💎 RombyLib

**A modern, lightweight modding library for [Rombykon](https://store.steampowered.com/app/4238990/Rombykon/)**

[![Steam](https://img.shields.io/badge/Steam-Rombykon-1b2838?style=flat-square&logo=steam)](https://store.steampowered.com/app/4238990/Rombykon/)
[![BepInEx](https://img.shields.io/badge/BepInEx-IL2CPP-orange?style=flat-square)](https://github.com/BepInEx/BepInEx)
[![Status: WIP](https://img.shields.io/badge/Status-Work_in_Progress-yellow?style=flat-square)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE.txt)

*Empowering developers to create native, clean, and intuitive mods for Rombykon.*

</div>

---

> ⚠️ **Work In Progress (Early Development):**  
> RombyLib is currently in early active development. Public APIs may shift and change between updates until a stable `v1.0.0` is tagged.

---

## 🧩 What's Implemented So Far

- **⚡ Core Access:** Global player lifecycle handling via the static `Romby` class.
- **🏃 Stats & Statuses:** Read/write player speed, crits, and basic status effects (*Freeze*, *Burn*, *Dizzy*).
- **🎨 Skin Structure:** Direct hooks into body parts, facial sprites, and pivots (`Body`, `Eyes`, `Mouth`, `Pivots`).
- **⚔️ Weapon Wrappers:** Unified mapping for all 16 weapons + specialized helpers for unique mechanics (*Treble Clef* notes, *Big Bucks* charge, *Bubblewitch* capture).
- **📦 Steam Workshop:** Experimental background downloader and local skin pack parser.

---

## 🗺️ Roadmap / In Progress

- [ ] Complete event bus (damage hooks, entity interactions, death triggers)
- [ ] UI & Custom Menu registration helpers
- [ ] Enemy & Wave manager utilities
- [ ] Custom audio / sound effect injection
- [ ] Full API documentation & examples

---

## 📚 Documentation & Guides

To keep this README clean, comprehensive tutorials and API references are being moved to dedicated pages:

- 📖 **[Official Wiki / Guides](https://github.com/)** *(Work in progress)*
- 💡 **[API Reference](https://github.com/)** *(Coming soon)*

---

## 📦 Quick Preview

### 1. Installation
Drop `RombyLib.dll` into your `BepInEx/plugins/` directory.

### 2. Basic Example

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
            Log.LogInfo("Romby mod loaded!");
        }

        public void Update()
        {
            // Always ensure the player instance is alive
            if (!Romby.IsLoaded) return;

            // Simple stat manipulation
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
🗡️ Mapped Weapons

| ID | Name                | Type   | Controller / Access                          |
| -- | ------------------- | ------ | -------------------------------------------- |
| 1  | Astro Pulse         | Player | `Romby.Weapons.AstroPulse`                   |
| 2  | Satellite Shield    | Player | `Romby.Weapons.SatelliteShield`              |
| 3  | Solar Mine          | World  | `Romby.Weapons.ActiveSolarMines`             |
| 4  | Moonerang           | Player | `Romby.Weapons.Moonerang`                    |
| 5  | Existential Void    | World  | `Romby.Weapons.DestroyAllExistentialVoids()` |
| 6  | Uncle Frank         | World  | `Romby.Weapons.ActiveUncleFranks`            |
| 7  | Tesla Pawn          | World  | `Romby.Weapons.ActiveTeslaPawnsCount`        |
| 8  | Plushinator 3000    | Player | `Romby.Weapons.Plushinator3000`              |
| 9  | BitBang             | Player | `Romby.Weapons.BitBang`                      |
| 10 | Treble Clef         | Player | `Romby.Weapons.TrebleClef`                   |
| 11 | Big Bucks           | Player | `Romby.Weapons.BigBucks`                     |
| 12 | Gachickpon          | Player | `Romby.Weapons.Gachickpon`                   |
| 13 | The Screamer        | Player | `Romby.Weapons.TheScreamer`                  |
| 14 | The Soul Transmuter | World  | `Romby.Weapons.ActiveSoulTransmuters`        |
| 15 | Divine Relocator    | Player | `Romby.Weapons.DivineRelocator`              |
| 16 | Bubblewitch         | Player | `Romby.Weapons.Bubblewitch`                  |

🤝 Credits & Acknowledgements

  - Rombykon developed by Guiliam.
  - Built with BepInEx and Harmony.

📜 License

Distributed under the MIT License. See LICENSE.txt for more details.
