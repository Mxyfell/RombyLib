using System;
using System.Collections.Generic;
using TwoDefender.SaveSystem;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace RombyLib
{
    /// <summary>
    /// Main entry point for global game systems, world state, saves, and progression.
    /// </summary>
    public static class Game
    {
        /// <summary>Player progression and stats controller (money, XP, level, lives, skill points).</summary>
        public static PlayerStateController Player { get; } = new PlayerStateController();

        /// <summary>Save system controller for loading, saving, and slot management.</summary>
        public static SaveSystemController Saves { get; } = new SaveSystemController();

        /// <summary>World state controller for day/night cycle, waves, and crystal health.</summary>
        public static WorldStateController World { get; } = new WorldStateController();

        /// <summary>Progress controller for bosses, completed areas, and teleports.</summary>
        public static ProgressController Progress { get; } = new ProgressController();

        /// <summary>Lifetime gameplay statistics controller.</summary>
        public static StatisticsController Statistics { get; } = new StatisticsController();

        /// <summary>Plants and garden elements controller.</summary>
        public static PlantsController Plants { get; } = new PlantsController();

        /// <summary>Wardrobe and cosmetic parts customization controller.</summary>
        public static WardrobeController Wardrobe { get; } = new WardrobeController();

        /// <summary>Audio, video, and gameplay settings controller.</summary>
        public static SettingsController Settings { get; } = new SettingsController();

        /// <summary>Game mode controller (standard, roguelike, challenges).</summary>
        public static GameModeController Mode { get; } = new GameModeController();

        /// <summary>Access to loaded constellation areas and saved constellation/challenge data.</summary>
        public static ConstellationController Constellations { get; } = new ConstellationController();

        /// <summary>Controller for active enemies, nearby queries, and mass combat actions.</summary>
        public static EnemiesController Enemies { get; } = new EnemiesController();

        /// <summary>Persistent custom key-value storage for mods.</summary>
        public static ModDataStore ModData { get; } = new ModDataStore();

        /// <summary>Global game and lifecycle event hub for mod callbacks.</summary>
        public static GameEventsController Events { get; } = new GameEventsController();

        /// <summary>Hub and exploration NPC discovery, dialogue, and interaction controller.</summary>
        public static NPCController NPC { get; } = new NPCController();

        /// <summary>Weapon shops, ammo dispensers, and traveling merchant controller.</summary>
        public static ShopsController Shops { get; } = new ShopsController();

        /// <summary>Rhythm minigame controller (radio, songs, scoring, and auto-assist).</summary>
        public static RhythmController Rhythm { get; } = new RhythmController();

        /// <summary>Colosseum arena controller (Boss Rush, Battle Royale, betting, and combat controls).</summary>
        public static ColosseumController Colosseum { get; } = new ColosseumController();

        /// <summary>Monster farm controller (captured monster storage and wandering pets).</summary>
        public static FarmController Farm { get; } = new FarmController();

        /// <summary>Native cyberpunk UI framework for creating windows, HUDs, buttons, toggles, and sliders.</summary>
        public static UIController UI { get; } = new UIController();

        /// <summary>Mod Manager controller ("УЛЬТРАМОД") for registering, configuring, and persisting mod settings in the Settings menu.</summary>
        public static ModManagerController Mods { get; } = new ModManagerController();

        /// <summary>Tarot card discovery, selection, active effects, and multipliers controller.</summary>
        public static TarotController Tarot { get; } = new TarotController();

        /// <summary>Roguelike game mode progression, seed, and runs controller.</summary>
        public static RoguelikeController Roguelike { get; } = new RoguelikeController();

        /// <summary>New Game Plus mode, final phase, and difficulty multiplier controller.</summary>
        public static NewGamePlusController NewGamePlus { get; } = new NewGamePlusController();

        /// <summary>Cooperative multiplayer, player 2 state, and gamepad management controller.</summary>
        public static CoopController Coop { get; } = new CoopController();

        /// <summary>Global audio effects, SFX pools, and volume channel controller.</summary>
        public static AudioController Audio { get; } = new AudioController();

        /// <summary>Background music, context playlists, and dynamic soundtracks controller.</summary>
        public static MusicController Music { get; } = new MusicController();

        /// <summary>Game camera, virtual camera framing, zoom, and screen shake controller.</summary>
        public static GameCameraController Camera { get; } = new GameCameraController();

        /// <summary>Time scale manipulation, slow motion effects, and pause management controller.</summary>
        public static TimeController Time { get; } = new TimeController();

        /// <summary>Gets the current active game save data safely.</summary>
        public static GameSaveData CurrentData
        {
            get
            {
                var sm = SaveManager.Instance;
                return Romby.IsValid(sm) ? sm.GetCurrentSaveData() : null;
            }
        }

        /// <summary>Collects and captures the current live state of the game.</summary>
        public static GameSaveData CaptureLiveState()
        {
            try
            {
                return SaveDataCollector.CollectAllData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Game] Failed to capture live state: {ex.Message}");
                return null;
            }
        }
    }

    [Flags]
    public enum ConstellationAreaType
    {
        None = 0,
        Battle = 1 << 0,
        Boss = 1 << 1,
        Puzzle = 1 << 2,
        Survival = 1 << 3,
        Bonus = 1 << 4,
        DamageChallenge = 1 << 5,
        Race = 1 << 6,
        Pelfa = 1 << 7,
        ProtectFlower = 1 << 8
    }

    /// <summary>Read-only snapshot of a loaded constellation area.</summary>
    public sealed class ConstellationAreaInfo
    {
        internal ConstellationAreaInfo(ForceFieldCircle source, ConstellationAreaType types)
        {
            Source = source;
            Types = types;
            IsActive = source.IsBossZoneActive || source.IsBattleZoneActive || source.IsSurvivalZoneActive ||
                       source.IsPuzzleZoneActive || source.IsBonusZoneActive || source.IsDamageChallengeZoneActive ||
                       source.IsRaceZoneActive || source.IsProtegerFlorZoneActive;
            IsCompleted = source.IsAreaCompleted();
        }

        /// <summary>Underlying game component for advanced access.</summary>
        public ForceFieldCircle Source { get; }

        /// <summary>Configured area modes; multiple flags may be set.</summary>
        public ConstellationAreaType Types { get; }

        /// <summary>Whether any supported challenge mode is currently active.</summary>
        public bool IsActive { get; }

        /// <summary>Whether the game component reports the area as completed.</summary>
        public bool IsCompleted { get; }
    }

    /// <summary>Provides a unified read-only view of loaded areas and existing constellation/challenge state.</summary>
    public class ConstellationController
    {
        internal ConstellationController() { }

        /// <summary>Gets a snapshot of currently loaded constellation areas.</summary>
        public IReadOnlyList<ConstellationAreaInfo> LoadedAreas
        {
            get
            {
                var circles = ForceFieldCircle.ActiveCircles;
                if (circles == null || circles.Count == 0)
                    return Array.Empty<ConstellationAreaInfo>();

                var areas = new List<ConstellationAreaInfo>(circles.Count);
                foreach (var circle in circles)
                {
                    if (!Romby.IsValid(circle)) continue;
                    areas.Add(new ConstellationAreaInfo(circle, GetAreaTypes(circle)));
                }

                return areas.AsReadOnly();
            }
        }

        /// <summary>Gets the saved area IDs assigned to a constellation.</summary>
        public Il2CppCollections.List<int> GetAreaIds(string constellationName) =>
            Game.Progress.GetConstellationAreas(constellationName);

        /// <summary>Sets the saved area IDs assigned to a constellation.</summary>
        public void SetAreaIds(string constellationName, Il2CppCollections.List<int> areaIds) =>
            Game.Progress.SetConstellationAreas(constellationName, areaIds);

        /// <summary>Gets the active challenge IDs from the current save.</summary>
        public int[] ActiveChallengeIds
        {
            get
            {
                var ids = Game.Mode.ActiveChallengeIds;
                if (ids == null || ids.Length == 0)
                    return Array.Empty<int>();

                var copy = new int[ids.Length];
                Array.Copy(ids, copy, ids.Length);
                return copy;
            }
        }

        /// <summary>Checks whether a challenge is active in the current save.</summary>
        public bool IsChallengeActive(int challengeId) => Game.Mode.IsChallengeActive(challengeId);

        private static ConstellationAreaType GetAreaTypes(ForceFieldCircle area)
        {
            var types = ConstellationAreaType.None;
            if (area.isBattleZone) types |= ConstellationAreaType.Battle;
            if (area.isBossZone) types |= ConstellationAreaType.Boss;
            if (area.isPuzzleZone) types |= ConstellationAreaType.Puzzle;
            if (area.isSurvivalMode) types |= ConstellationAreaType.Survival;
            if (area.ZonaBonus) types |= ConstellationAreaType.Bonus;
            if (area.isDamageChallengeZone) types |= ConstellationAreaType.DamageChallenge;
            if (area.isRaceArea) types |= ConstellationAreaType.Race;
            if (area.isZonaPelfa) types |= ConstellationAreaType.Pelfa;
            if (area.isProtegerFlor) types |= ConstellationAreaType.ProtectFlower;
            return types;
        }
    }

    /// <summary>
    /// Handles save slots, autosaves, manual saving, and loading safely.
    /// </summary>
    public class SaveSystemController
    {
        private SaveManager Manager => Romby.IsValid(SaveManager.Instance) ? SaveManager.Instance : null;

        internal SaveSystemController() { }

        /// <summary>Checks whether a save operation is currently in progress.</summary>
        public bool IsSaving => Manager != null && Manager.IsSaving;

        /// <summary>Checks whether autosaving is currently suspended.</summary>
        public bool IsAutoSaveSuspended => Manager != null && Manager.IsAutoSaveSuspended;

        /// <summary>Gets or sets the current active save slot index.</summary>
        public int CurrentSlot
        {
            get => Manager != null ? Manager.GetCurrentSaveSlot() : 1;
            set
            {
                if (Manager != null)
                {
                    Manager.SetCurrentSaveSlot(value);
                    Debug.Log($"[RombyLib.Save] Set current save slot to {value}.");
                }
            }
        }

        /// <summary>Checks if an active save file exists in the current slot.</summary>
        public bool HasActiveSave
        {
            get
            {
                try
                {
                    return GameManagerSaveHelper.HasActiveSave();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Saves current game progression into the active slot.</summary>
        public bool SaveCurrent()
        {
            try
            {
                Game.Player.SyncToLive();
                GameManagerSaveHelper.SaveCurrentGame();
                Debug.Log("[RombyLib.Save] Saved current game.");
                Game.Events.TriggerGameSaved(CurrentSlot);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to save current game: {ex.Message}");
                return false;
            }
        }

        /// <summary>Saves current game progression into a specific slot.</summary>
        public bool SaveToSlot(int slotIndex)
        {
            try
            {
                Game.Player.SyncToLive();
                if (Manager != null && Manager.SaveGame(slotIndex))
                {
                    Debug.Log($"[RombyLib.Save] Saved game to slot {slotIndex}.");
                    Game.Events.TriggerGameSaved(slotIndex);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Exception while saving to slot {slotIndex}: {ex.Message}");
            }
            return false;
        }

        /// <summary>Loads game state from a specific slot.</summary>
        public bool LoadSlot(int slotIndex)
        {
            try
            {
                bool success = GameManagerSaveHelper.LoadGame(slotIndex);
                Debug.Log($"[RombyLib.Save] Loaded save slot {slotIndex}: {(success ? "Success" : "Failed")}.");
                if (success) Game.Events.TriggerGameLoaded(slotIndex);
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to load save slot {slotIndex}: {ex.Message}");
                return false;
            }
        }

        /// <summary>Triggers an immediate quick save with an optional reason.</summary>
        public void QuickSave(string reason = "Manual")
        {
            try
            {
                Game.Player.SyncToLive();
                if (Manager != null)
                {
                    Manager.QuickSave(reason);
                    Debug.Log($"[RombyLib.Save] Quick-saved ({reason}).");
                    Game.Events.TriggerGameSaved(CurrentSlot);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to quick save: {ex.Message}");
            }
        }

        /// <summary>Triggers an immediate auto-save.</summary>
        public void AutoSave()
        {
            try
            {
                if (Manager != null)
                {
                    Manager.AutoSave();
                    Debug.Log("[RombyLib.Save] Auto-saved game.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to auto save: {ex.Message}");
            }
        }

        /// <summary>Deletes saved game data in a specific slot.</summary>
        public bool DeleteSlot(int slotIndex)
        {
            try
            {
                if (Manager != null && Manager.DeleteSave(slotIndex))
                {
                    Debug.Log($"[RombyLib.Save] Deleted save slot {slotIndex}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to delete save slot {slotIndex}: {ex.Message}");
            }
            return false;
        }

        /// <summary>Checks if save data exists in a specific slot.</summary>
        public bool Exists(int slotIndex) => Manager != null && Manager.DoesSaveExist(slotIndex);

        /// <summary>Gets metadata and details for a specific save slot.</summary>
        public SaveSlotInfo GetSlotInfo(int slotIndex) => Manager != null ? Manager.GetSaveSlotInfo(slotIndex) : null;

        /// <summary>Gets a list of metadata for all save slots.</summary>
        public Il2CppCollections.List<SaveSlotInfo> GetAllSlots() => Manager != null ? Manager.GetAllSaveSlotsInfo() : null;

        /// <summary>Starts a brand new game in the specified slot.</summary>
        public void StartNewGame(int slotIndex)
        {
            try
            {
                GameManagerSaveHelper.StartNewGameInSlot(slotIndex);
                Debug.Log($"[RombyLib.Save] Started new game in slot {slotIndex}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Save] Failed to start new game in slot {slotIndex}: {ex.Message}");
            }
        }

        /// <summary>Suspends automatic saves.</summary>
        public void SuspendAutoSave()
        {
            if (Manager != null)
            {
                Manager.SuspendAutoSave();
                Debug.Log("[RombyLib.Save] Suspended auto-save.");
            }
        }

        /// <summary>Resumes automatic saves.</summary>
        public void ResumeAutoSave()
        {
            if (Manager != null)
            {
                Manager.ResumeAutoSave();
                Debug.Log("[RombyLib.Save] Resumed auto-save.");
            }
        }

        /// <summary>Configures periodic background autosave settings.</summary>
        public void SetPeriodicAutosave(bool enabled, float intervalSeconds = 0f)
        {
            if (Manager == null) return;
            Manager.SetPeriodicAutosave(enabled);
            if (intervalSeconds > 0f)
                Manager.SetAutosaveInterval(intervalSeconds);

            Debug.Log($"[RombyLib.Save] Periodic autosave set: enabled={enabled}, interval={intervalSeconds}s.");
        }
    }

    /// <summary>
    /// Controls world cycle, days, crystal durability, waves, and enemy counters.
    /// </summary>
    public class WorldStateController
    {
        private GameStateSaveData State => Game.CurrentData?.gameState;
        private GameManager GM => GameManager.Instance;
        private CrystalHealth _cachedCrystal;

        internal WorldStateController() { }

        private CrystalHealth GetCrystal()
        {
            if (!Romby.IsValid(_cachedCrystal))
            {
                try
                {
                    var found = UnityEngine.Object.FindObjectOfType<CrystalHealth>();
                    _cachedCrystal = Romby.IsValid(found) ? found : null;
                }
                catch
                {
                    _cachedCrystal = null;
                }
            }
            return Romby.IsValid(_cachedCrystal) ? _cachedCrystal : null;
        }

        /// <summary>Gets or sets the current in-game day.</summary>
        public int CurrentDay
        {
            get
            {
                if (Romby.IsValid(GM)) return GM.currentDay;
                return State?.currentDay ?? 1;
            }
            set
            {
                int old = CurrentDay;
                if (State != null) State.currentDay = value;
                if (Romby.IsValid(GM))
                {
                    GM.SetCurrentDay(value);
                    GM.UpdateDayNumberUI();
                }
                if (value != old) Game.Events.TriggerDayChanged(value);
            }
        }

        /// <summary>Gets or sets whether it is currently nighttime.</summary>
        public bool IsNight
        {
            get => State?.isNight ?? false;
            set
            {
                bool old = IsNight;
                if (State != null) State.isNight = value;
                if (value != old) Game.Events.TriggerNightStateChanged(value);
            }
        }

        /// <summary>Gets or sets time remaining in the current day/night cycle.</summary>
        public float CycleTimeRemaining
        {
            get => State?.cycleTimeRemaining ?? 0f;
            set
            {
                if (State != null) State.cycleTimeRemaining = value;
            }
        }

        /// <summary>Gets or sets the defensive crystal's current health.</summary>
        public float CrystalHealth
        {
            get
            {
                var crystal = GetCrystal();
                if (Romby.IsValid(crystal)) return crystal.currentHealth;
                return State?.crystalCurrentHealth ?? 0f;
            }
            set
            {
                float clamped = Mathf.Clamp(value, 0f, CrystalMaxHealth);
                if (State != null) State.crystalCurrentHealth = clamped;
                var crystal = GetCrystal();
                if (Romby.IsValid(crystal)) crystal.currentHealth = (int)clamped;
            }
        }

        /// <summary>Gets or sets the crystal's maximum health.</summary>
        public float CrystalMaxHealth
        {
            get
            {
                var crystal = GetCrystal();
                if (Romby.IsValid(crystal)) return crystal.maxHealth;
                return State?.crystalMaxHealth ?? 100f;
            }
            set
            {
                float val = Mathf.Max(1f, value);
                if (State != null) State.crystalMaxHealth = val;
                var crystal = GetCrystal();
                if (Romby.IsValid(crystal)) crystal.maxHealth = (int)val;
            }
        }

        /// <summary>Gets or sets the current combat wave index.</summary>
        public int CurrentWave
        {
            get
            {
                if (Romby.IsValid(GM)) return GM.currentWaveIndex;
                return State?.currentWaveIndex ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (State != null) State.currentWaveIndex = val;
                if (Romby.IsValid(GM)) GM.currentWaveIndex = val;
            }
        }

        /// <summary>Gets or sets the number of active enemies remaining.</summary>
        public int EnemiesRemaining
        {
            get
            {
                if (Romby.IsValid(GM)) return GM.enemiesRemaining;
                return State?.enemiesRemaining ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (State != null) State.enemiesRemaining = val;
                if (Romby.IsValid(GM)) GM.enemiesRemaining = val;
            }
        }

        /// <summary>Gets or sets defeated bosses counter.</summary>
        public int DefeatedBossesCount
        {
            get => State?.bossesDefeatedCount ?? 0;
            set { if (State != null) State.bossesDefeatedCount = value; }
        }

        /// <summary>Gets or sets whether New Game Plus mode is active.</summary>
        public bool IsNewGamePlus
        {
            get => State?.newGamePlusActivo ?? false;
            set { if (State != null) State.newGamePlusActivo = value; }
        }

        /// <summary>Gets or sets New Game Plus money multiplier.</summary>
        public int NGPlusMultiplier
        {
            get => State?.ngPlusMoneyMultiplier ?? 1;
            set { if (State != null) State.ngPlusMoneyMultiplier = value; }
        }

        /// <summary>Checks whether Ykon boss was defeated.</summary>
        public bool IsYkonDefeated
        {
            get => State?.ykonDerrotado ?? false;
            set { if (State != null) State.ykonDerrotado = value; }
        }

        /// <summary>List of active tarot cards and their parameters.</summary>
        public Il2CppCollections.List<ActiveTarotCardData> ActiveTarotCards => State?.activeTarotCards;

        /// <summary>Immediately transitions world time to night and triggers combat wave.</summary>
        public void SkipToNight()
        {
            if (Romby.IsValid(GM))
            {
                GM.SkipToNight();
                Debug.Log("[RombyLib.World] Skipped to night cycle.");
            }
            if (State != null) State.isNight = true;
        }

        /// <summary>Sets current day in both running game and save state.</summary>
        public void SetDay(int day)
        {
            CurrentDay = day;
            Debug.Log($"[RombyLib.World] Set current day to {day}.");
        }

        /// <summary>Resets the world cycle back to day phase with target day counter.</summary>
        public void ResetToDay(int targetDay)
        {
            CurrentDay = targetDay;
            if (Romby.IsValid(GM))
            {
                GM.ResetToDay(targetDay);
                Debug.Log($"[RombyLib.World] Reset world to day {targetDay}.");
            }
        }

        /// <summary>Adds or subtracts remaining seconds from current day/night cycle.</summary>
        public void AddCycleTime(float seconds)
        {
            if (Romby.IsValid(GM))
                GM.AddDayTimeRemaining(seconds);
            else
                CycleTimeRemaining += seconds;
        }

        /// <summary>Heals defensive crystal by a specified amount.</summary>
        public void HealCrystal(int amount)
        {
            var crystal = GetCrystal();
            if (Romby.IsValid(crystal))
                crystal.Heal(amount);
            else
                CrystalHealth += amount;
            Debug.Log($"[RombyLib.World] Healed crystal by {amount}. Health: {CrystalHealth}/{CrystalMaxHealth}.");
        }

        /// <summary>Deals damage directly to the defensive crystal.</summary>
        public void DamageCrystal(int damage)
        {
            var crystal = GetCrystal();
            if (Romby.IsValid(crystal))
                crystal.TakeDamage(damage);
            else
                CrystalHealth = Mathf.Max(0, CrystalHealth - damage);
            Debug.Log($"[RombyLib.World] Damaged crystal by {damage}. Health: {CrystalHealth}/{CrystalMaxHealth}.");
        }

        /// <summary>Restores defensive crystal to full maximum durability.</summary>
        public void RestoreCrystalFullHealth()
        {
            var crystal = GetCrystal();
            if (Romby.IsValid(crystal))
                crystal.RestoreFullHealth();
            else
                CrystalHealth = CrystalMaxHealth;
            Debug.Log("[RombyLib.World] Restored crystal to full health.");
        }

        /// <summary>Enables or disables defensive crystal permanent invulnerability.</summary>
        public void SetCrystalInvulnerable(bool invulnerable)
        {
            var crystal = GetCrystal();
            if (Romby.IsValid(crystal))
                crystal.SetInvulnerable(invulnerable);
            else if (Romby.IsValid(GM))
                GM.SetCrystalInvulnerable(invulnerable);
            Debug.Log($"[RombyLib.World] Crystal invulnerability set: {invulnerable}.");
        }

        /// <summary>Grants temporary invulnerability to the crystal for a duration in seconds.</summary>
        public void SetCrystalTemporaryInvulnerability(float durationSeconds)
        {
            var crystal = GetCrystal();
            if (Romby.IsValid(crystal))
                crystal.SetTemporaryInvulnerability(durationSeconds);
            else if (Romby.IsValid(GM))
                GM.SetCrystalTemporaryInvulnerability(durationSeconds);
            Debug.Log($"[RombyLib.World] Crystal temporary invulnerability set for {durationSeconds}s.");
        }

        /// <summary>Amount of Special Stones (rare crafting currency) held in save data.</summary>
        public int SpecialStones
        {
            get => State?.piedrasEspeciales ?? 0;
            set
            {
                if (State != null) State.piedrasEspeciales = value;
            }
        }

        /// <summary>List of unlocked decorative and functional stone sculptures.</summary>
        public IReadOnlyList<string> UnlockedSculptures
        {
            get
            {
                var list = State?.esculturasDesbloqueadas;
                if (list != null)
                {
                    var result = new List<string>(list.Count);
                    foreach (var s in list) result.Add(s);
                    return result;
                }
                return Array.Empty<string>();
            }
        }

        /// <summary>Cumulative currency donated to the wishing fountain.</summary>
        public int FountainTotalDonated
        {
            get => State?.fuenteDineroTotal ?? 0;
            set
            {
                if (State != null) State.fuenteDineroTotal = value;
            }
        }

        /// <summary>List of reward tier indices claimed from the wishing fountain.</summary>
        public IReadOnlyList<int> FountainCollectedRewards
        {
            get
            {
                var list = State?.fuentePremiosRecogidos;
                if (list != null)
                {
                    var result = new List<int>(list.Count);
                    foreach (var id in list) result.Add(id);
                    return result;
                }
                return Array.Empty<int>();
            }
        }

        /// <summary>List of boss jewel trophy IDs unlocked by defeating major bosses.</summary>
        public IReadOnlyList<int> BossJewels
        {
            get
            {
                var list = State?.joyasBossDesbloqueadas;
                if (list != null)
                {
                    var result = new List<int>(list.Count);
                    foreach (var id in list) result.Add(id);
                    return result;
                }
                return Array.Empty<int>();
            }
        }

        /// <summary>List of collectible message bottles obtained across all biomes.</summary>
        public IReadOnlyList<int> Bottles
        {
            get
            {
                var list = State?.botellasObtenidas;
                if (list != null)
                {
                    var result = new List<int>(list.Count);
                    foreach (var id in list) result.Add(id);
                    return result;
                }
                return Array.Empty<int>();
            }
        }

        /// <summary>Checks whether the Possessed Merchant event sequence is currently active.</summary>
        public bool IsPossessedMerchantActive
        {
            get
            {
                var mgr = VendedorPoseidoEventManager.Instance;
                return Romby.IsValid(mgr) && mgr.CinematicaActiva;
            }
        }

        /// <summary>Checks whether the Possessed Merchant has already been rescued in save state.</summary>
        public bool IsPossessedMerchantRescued
        {
            get => State?.vendedorRescatadoMostrado ?? false;
            set
            {
                if (State != null) State.vendedorRescatadoMostrado = value;
            }
        }

        /// <summary>Triggers the Possessed Merchant cutscene and combat event sequence.</summary>
        public void TriggerPossessedMerchantSequence()
        {
            var mgr = VendedorPoseidoEventManager.Instance;
            if (Romby.IsValid(mgr))
            {
                mgr.IniciarSecuenciaPosesion();
                Game.Events.TriggerPossessedMerchant();
            }
        }

        /// <summary>Checks if Ykon story invasion event has occurred for a specific day index.</summary>
        public bool HasYkonAppeared(int day)
        {
            if (State == null) return false;
            return day switch
            {
                4 => State.ykonDia4Mostrado,
                9 => State.ykonDia9Mostrado,
                15 => State.ykonDia15Mostrado,
                23 => State.ykonDia23Mostrado,
                32 => State.ykonDia32Mostrado,
                38 => State.ykonDia38Mostrado,
                47 => State.ykonDia47Mostrado,
                51 => State.ykonDia51Mostrado,
                56 => State.ykonDia56Mostrado,
                60 => State.ykonDia60Mostrado,
                _ => false
            };
        }
    }

    /// <summary>
    /// Controls overall game completion, defeated bosses, areas, and discovered teleports.
    /// </summary>
    public class ProgressController
    {
        private ScenarioSaveData Scenario => Game.CurrentData?.scenario;

        internal ProgressController() { }

        /// <summary>Calculates total game completion ratio as a percentage.</summary>
        public float CompletionPercentage
        {
            get
            {
                try
                {
                    return SaveDataCollector.CalculateCurrentGameCompletionPercentage();
                }
                catch
                {
                    return 0f;
                }
            }
        }

        /// <summary>List of names of all defeated bosses.</summary>
        public Il2CppCollections.List<string> DefeatedBosses => Scenario?.defeatedBosses;

        /// <summary>List of IDs of completed battle areas.</summary>
        public Il2CppCollections.List<int> CompletedBattleAreas => Scenario?.completedBattleAreas;

        /// <summary>List of IDs of discovered teleport waypoints.</summary>
        public Il2CppCollections.List<string> DiscoveredTeleports => Scenario?.discoveredTeleports;

        /// <summary>Checks whether a boss has been defeated by name.</summary>
        public bool HasBossBeenDefeated(string bossName) => DefeatedBosses != null && DefeatedBosses.Contains(bossName);

        /// <summary>Checks whether a battle area was completed by ID.</summary>
        public bool IsBattleAreaCompleted(int areaId) => CompletedBattleAreas != null && CompletedBattleAreas.Contains(areaId);

        /// <summary>Checks whether a teleport waypoint was discovered.</summary>
        public bool IsTeleportDiscovered(string teleportId) => DiscoveredTeleports != null && DiscoveredTeleports.Contains(teleportId);

        /// <summary>Marks a battle area as completed.</summary>
        public void CompleteBattleArea(int areaId)
        {
            if (Scenario?.completedBattleAreas != null && !Scenario.completedBattleAreas.Contains(areaId))
            {
                Scenario.completedBattleAreas.Add(areaId);
                Debug.Log($"[RombyLib.Progress] Completed battle area {areaId}.");
            }
        }

        /// <summary>Marks a teleport waypoint as discovered.</summary>
        public void DiscoverTeleport(string teleportId)
        {
            if (Scenario?.discoveredTeleports != null && !Scenario.discoveredTeleports.Contains(teleportId))
            {
                Scenario.discoveredTeleports.Add(teleportId);
                Debug.Log($"[RombyLib.Progress] Discovered teleport: {teleportId}.");
            }
        }

        /// <summary>Gets area IDs belonging to a constellation.</summary>
        public Il2CppCollections.List<int> GetConstellationAreas(string constellationName) => Scenario?.GetConstellationAreas(constellationName);

        /// <summary>Sets area IDs for a constellation.</summary>
        public void SetConstellationAreas(string constellationName, Il2CppCollections.List<int> areas)
        {
            Scenario?.SetConstellationAreas(constellationName, areas);
            Debug.Log($"[RombyLib.Progress] Updated constellation areas for '{constellationName}'.");
        }

        /// <summary>Gets or sets whether there is a new unlock notification pending.</summary>
        public bool HasNewUnlock
        {
            get => Scenario?.hasNewUnlock ?? false;
            set { if (Scenario != null) Scenario.hasNewUnlock = value; }
        }
    }

    /// <summary>
    /// Finds and inspects defensive plants and flowers in the scene safely.
    /// </summary>
    public class PlantsController
    {
        internal PlantsController() { }

        /// <summary>Finds all active Flower instances in the scene.</summary>
        public Flower[] ActiveFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<Flower>() ?? Array.Empty<Flower>(); }
                catch { return Array.Empty<Flower>(); }
            }
        }

        /// <summary>Finds all active ShootingFlower instances.</summary>
        public ShootingFlower[] ShootingFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<ShootingFlower>() ?? Array.Empty<ShootingFlower>(); }
                catch { return Array.Empty<ShootingFlower>(); }
            }
        }

        /// <summary>Finds all active BarrierFlower instances.</summary>
        public BarrierFlower[] BarrierFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<BarrierFlower>() ?? Array.Empty<BarrierFlower>(); }
                catch { return Array.Empty<BarrierFlower>(); }
            }
        }

        /// <summary>Finds all active SpeedBoostFlower instances.</summary>
        public SpeedBoostFlower[] SpeedBoostFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<SpeedBoostFlower>() ?? Array.Empty<SpeedBoostFlower>(); }
                catch { return Array.Empty<SpeedBoostFlower>(); }
            }
        }

        /// <summary>Finds all active ThrowRocksFlower instances.</summary>
        public ThrowRocksFlower[] ThrowRocksFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<ThrowRocksFlower>() ?? Array.Empty<ThrowRocksFlower>(); }
                catch { return Array.Empty<ThrowRocksFlower>(); }
            }
        }

        /// <summary>Finds all active CorruptedCrystalFlower instances.</summary>
        public CorruptedCrystalFlower[] CorruptedCrystalFlowers
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<CorruptedCrystalFlower>() ?? Array.Empty<CorruptedCrystalFlower>(); }
                catch { return Array.Empty<CorruptedCrystalFlower>(); }
            }
        }

        /// <summary>Gets the total count of flowers active in the scene.</summary>
        public int TotalActiveFlowersCount => ActiveFlowers.Length;

        /// <summary>List of saved plant instances from save data.</summary>
        public Il2CppCollections.List<PlantData> SavedPlants => Game.CurrentData?.plants?.activePlants;

        /// <summary>Clears saveable object registry.</summary>
        public void ClearRegistry()
        {
            try
            {
                SaveableRegistry.Clear();
                Debug.Log("[RombyLib.Plants] Cleared SaveableRegistry.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.Plants] Failed to clear SaveableRegistry: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tracks lifetime player stats including kills, playtime, and deaths.
    /// Synchronizes with both live game counters (EstadisticasPartida) and save file data.
    /// </summary>
    public class StatisticsController
    {
        private StatisticsSaveData Stats => Game.CurrentData?.statistics;

        internal StatisticsController() { }

        /// <summary>
        /// Reads a named statistic counter from live game memory or the save file cache.
        /// Searches case-insensitively across live dictionary and save data keys.
        /// </summary>
        public long GetCounter(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;

            // 1. Check live GameManager.Instance.Estadisticas
            if (Romby.IsValid(GameManager.Instance) && GameManager.Instance.Estadisticas != null)
            {
                try
                {
                    var est = GameManager.Instance.Estadisticas;
                    if (est.contadores != null)
                    {
                        foreach (var entry in est.contadores)
                        {
                            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                                return entry.Value;
                        }
                    }

                    long direct = est.Obtener(key);
                    if (direct > 0) return direct;

                    long directLower = est.Obtener(key.ToLowerInvariant());
                    if (directLower > 0) return directLower;
                }
                catch { }
            }

            // 2. Check save cache (estadisticasClaves / estadisticasValores)
            var data = Game.CurrentData;
            if (data?.estadisticasClaves != null && data?.estadisticasValores != null)
            {
                int count = Math.Min(data.estadisticasClaves.Count, data.estadisticasValores.Count);
                for (int i = 0; i < count; i++)
                {
                    if (string.Equals(data.estadisticasClaves[i], key, StringComparison.OrdinalIgnoreCase))
                        return data.estadisticasValores[i];
                }
            }

            return 0;
        }

        /// <summary>Total playtime in seconds.</summary>
        public float TotalPlayTime
        {
            get
            {
                if (Romby.IsValid(GameManager.Instance) && GameManager.Instance.TotalPlayTime > 0)
                    return GameManager.Instance.TotalPlayTime;

                if (Game.CurrentData != null && Game.CurrentData.playTime > 0)
                    return Game.CurrentData.playTime;

                return Stats?.totalPlayTime ?? 0f;
            }
        }

        /// <summary>Total money accumulated over the run.</summary>
        public long TotalMoneyEarned
        {
            get
            {
                long val = GetCounter("dineroGanado");
                if (val > 0) return val;

                if (Game.CurrentData != null && Game.CurrentData.roguelikeTotalMoneyEarned > 0)
                    return Game.CurrentData.roguelikeTotalMoneyEarned;

                return Stats?.totalMoneyEarned ?? 0;
            }
        }

        /// <summary>Total money spent in shops and fountains.</summary>
        public long TotalMoneySpent => GetCounter("dineroGastado");

        /// <summary>Total experience points collected.</summary>
        public long TotalXPEarned
        {
            get
            {
                if (Game.CurrentData != null && Game.CurrentData.roguelikeTotalXPEarned > 0)
                    return Game.CurrentData.roguelikeTotalXPEarned;

                return Stats?.totalXPEarned ?? 0;
            }
        }

        /// <summary>Total standard and inverted enemies defeated across the game.</summary>
        public long TotalEnemiesKilled
        {
            get
            {
                long total = 0;

                // 1. Live GameManager instance
                if (Romby.IsValid(GameManager.Instance))
                {
                    try
                    {
                        var enemyStats = GameManager.Instance.GetEnemyKillStatistics();
                        if (enemyStats != null)
                        {
                            foreach (var kvp in enemyStats)
                                total += kvp.Value;
                        }

                        var invertedStats = GameManager.Instance.GetEnemyInvertedKillStatistics();
                        if (invertedStats != null)
                        {
                            foreach (var kvp in invertedStats)
                                total += kvp.Value;
                        }

                        if (total > 0) return total;
                    }
                    catch { }
                }

                // 2. Save file GameStateSaveData
                var state = Game.CurrentData?.gameState;
                if (state != null)
                {
                    if (state.enemyKillStatistics != null)
                    {
                        foreach (var kvp in state.enemyKillStatistics)
                            total += kvp.Value;
                    }
                    if (state.enemyInvertedKillStatistics != null)
                    {
                        foreach (var kvp in state.enemyInvertedKillStatistics)
                            total += kvp.Value;
                    }

                    if (total > 0) return total;
                }

                // 3. Fallbacks
                long val = GetCounter("EnemigosEliminados");
                if (val > 0) return val;

                if (Game.CurrentData != null && Game.CurrentData.roguelikeTotalEnemiesKilled > 0)
                    return Game.CurrentData.roguelikeTotalEnemiesKilled;

                return Stats?.totalEnemiesKilled ?? 0;
            }
        }

        /// <summary>Total bosses defeated across the game.</summary>
        public int TotalBossesKilled
        {
            get
            {
                int total = 0;

                // 1. Live GameManager instance
                if (Romby.IsValid(GameManager.Instance))
                {
                    try
                    {
                        var bossStats = GameManager.Instance.GetBossKillStatistics();
                        if (bossStats != null)
                        {
                            foreach (var kvp in bossStats)
                                total += kvp.Value;
                        }

                        var bossInverted = GameManager.Instance.GetBossInvertedKillStatistics();
                        if (bossInverted != null)
                        {
                            foreach (var kvp in bossInverted)
                                total += kvp.Value;
                        }

                        if (total > 0) return total;
                    }
                    catch { }
                }

                // 2. Save file GameStateSaveData
                var state = Game.CurrentData?.gameState;
                if (state != null)
                {
                    if (state.bossKillStatistics != null)
                    {
                        foreach (var kvp in state.bossKillStatistics)
                            total += kvp.Value;
                    }
                    if (state.bossInvertedKillStatistics != null)
                    {
                        foreach (var kvp in state.bossInvertedKillStatistics)
                            total += kvp.Value;
                    }

                    if (total > 0) return total;
                }

                // 3. Defeated bosses count from World
                if (Game.World.DefeatedBossesCount > 0)
                    return Game.World.DefeatedBossesCount;

                long val = GetCounter("JefesDerrotados");
                if (val > 0) return (int)val;

                return Stats?.totalBossesKilled ?? 0;
            }
        }

        /// <summary>Total player deaths.</summary>
        public long TotalDeaths
        {
            get
            {
                long val = GetCounter("derrotas");
                if (val > 0) return val;

                return Stats?.totalDeaths ?? 0;
            }
        }

        /// <summary>Total player revives triggered.</summary>
        public long TotalRevives => GetCounter("reanimaciones");

        /// <summary>Total rocks broken by player.</summary>
        public long TotalRocksBroken
        {
            get
            {
                if (Romby.IsValid(SteamAchievementManager.Instance) && SteamAchievementManager.Instance.rocasRotas > 0)
                    return SteamAchievementManager.Instance.rocasRotas;

                long val = GetCounter("rocasRotas");
                if (val > 0) return val;

                return 0;
            }
        }

        /// <summary>Total tarot cards chosen across the game.</summary>
        public long TotalTarotCardsChosen
        {
            get
            {
                long val = GetCounter("cartasTarot");
                if (val > 0) return val;
                return GetCounter("CartasTarotEscogidas");
            }
        }

        /// <summary>Highest wave reached.</summary>
        public int HighestWaveReached
        {
            get
            {
                if (Stats != null && Stats.highestWaveReached > 0)
                    return Stats.highestWaveReached;

                return Game.World.CurrentWave;
            }
        }

        /// <summary>Maximum player level achieved.</summary>
        public int MaxPlayerLevel
        {
            get
            {
                if (Stats != null && Stats.maxPlayerLevel > 0)
                    return Stats.maxPlayerLevel;

                return Game.Player.Level;
            }
        }

        /// <summary>Formats playtime as HH:MM:SS string.</summary>
        public string FormattedPlayTime
        {
            get
            {
                var time = TotalPlayTime;
                var hours = (int)(time / 3600);
                var minutes = (int)((time % 3600) / 60);
                var seconds = (int)(time % 60);
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
        }
    }

    /// <summary>
    /// Controls character wardrobe cosmetics and equipped body parts.
    /// </summary>
    public class WardrobeController
    {
        private WardrobeSaveData Wardrobe => Game.CurrentData?.wardrobe;

        internal WardrobeController() { }

        /// <summary>Gets or sets equipped body cosmetic ID.</summary>
        public int EquippedBodyId
        {
            get => Wardrobe?.equippedBodyId ?? 0;
            set { if (Wardrobe != null) Wardrobe.equippedBodyId = value; }
        }

        /// <summary>Gets or sets equipped mouth cosmetic ID.</summary>
        public int EquippedMouthId
        {
            get => Wardrobe?.equippedMouthId ?? 0;
            set { if (Wardrobe != null) Wardrobe.equippedMouthId = value; }
        }

        /// <summary>Gets or sets equipped extra/accessory cosmetic ID.</summary>
        public int EquippedExtraId
        {
            get => Wardrobe?.equippedExtraId ?? 0;
            set { if (Wardrobe != null) Wardrobe.equippedExtraId = value; }
        }

        /// <summary>Gets or sets equipped left eye cosmetic ID.</summary>
        public int EquippedEyeLeftId
        {
            get => Wardrobe?.equippedEyeLId ?? 0;
            set { if (Wardrobe != null) Wardrobe.equippedEyeLId = value; }
        }

        /// <summary>Gets or sets equipped right eye cosmetic ID.</summary>
        public int EquippedEyeRightId
        {
            get => Wardrobe?.equippedEyeRId ?? 0;
            set { if (Wardrobe != null) Wardrobe.equippedEyeRId = value; }
        }

        /// <summary>List of unlocked body cosmetic IDs.</summary>
        public Il2CppCollections.List<int> UnlockedBodies => Wardrobe?.unlockedBodyIds;

        /// <summary>List of unlocked mouth cosmetic IDs.</summary>
        public Il2CppCollections.List<int> UnlockedMouths => Wardrobe?.unlockedMouthIds;

        /// <summary>List of unlocked extra cosmetic IDs.</summary>
        public Il2CppCollections.List<int> UnlockedExtras => Wardrobe?.unlockedExtraIds;

        /// <summary>Unlocks a body cosmetic part by ID.</summary>
        public bool UnlockBody(int id)
        {
            if (Wardrobe?.unlockedBodyIds != null && !Wardrobe.unlockedBodyIds.Contains(id))
            {
                Wardrobe.unlockedBodyIds.Add(id);
                Debug.Log($"[RombyLib.Wardrobe] Unlocked body ID: {id}.");
                return true;
            }
            return false;
        }

        /// <summary>Clamps all cosmetic part transforms to valid safe bounds.</summary>
        public void ClampTransforms()
        {
            Wardrobe?.ClampPartTransformsToLimits();
            Debug.Log("[RombyLib.Wardrobe] Clamped cosmetic part transforms.");
        }
    }

    /// <summary>
    /// Manages audio volume levels, graphics quality, fullscreen, and language.
    /// </summary>
    public class SettingsController
    {
        private SettingsSaveData Data => Game.CurrentData?.settings;

        internal SettingsController() { }

        /// <summary>Master volume level (0.0 to 1.0).</summary>
        public float MasterVolume
        {
            get => Data?.masterVolume ?? 1f;
            set
            {
                if (Data != null)
                {
                    Data.masterVolume = Mathf.Clamp01(value);
                    Debug.Log($"[RombyLib.Settings] Master volume set to {Data.masterVolume}.");
                }
            }
        }

        /// <summary>Music volume level (0.0 to 1.0).</summary>
        public float MusicVolume
        {
            get => Data?.musicVolume ?? 1f;
            set
            {
                if (Data != null)
                {
                    Data.musicVolume = Mathf.Clamp01(value);
                    Debug.Log($"[RombyLib.Settings] Music volume set to {Data.musicVolume}.");
                }
            }
        }

        /// <summary>Sound effects volume level (0.0 to 1.0).</summary>
        public float SFXVolume
        {
            get => Data?.sfxVolume ?? 1f;
            set
            {
                if (Data != null)
                {
                    Data.sfxVolume = Mathf.Clamp01(value);
                    Debug.Log($"[RombyLib.Settings] SFX volume set to {Data.sfxVolume}.");
                }
            }
        }

        /// <summary>Unity graphics quality level index.</summary>
        public int QualityLevel
        {
            get => Data?.qualityLevel ?? QualitySettings.GetQualityLevel();
            set
            {
                if (Data != null) Data.qualityLevel = value;
                QualitySettings.SetQualityLevel(value, true);
                Debug.Log($"[RombyLib.Settings] Quality level set to {value}.");
            }
        }

        /// <summary>Enables or disables camera post-processing effects.</summary>
        public bool PostProcessingEnabled
        {
            get => Data?.postProcessingEnabled ?? true;
            set
            {
                if (Data != null)
                {
                    Data.postProcessingEnabled = value;
                    Debug.Log($"[RombyLib.Settings] Post-processing enabled: {value}.");
                }
            }
        }

        /// <summary>Enables or disables fullscreen mode.</summary>
        public bool Fullscreen
        {
            get => Data?.fullscreen ?? Screen.fullScreen;
            set
            {
                if (Data != null) Data.fullscreen = value;
                Screen.fullScreen = value;
                Debug.Log($"[RombyLib.Settings] Fullscreen set: {value}.");
            }
        }

        /// <summary>Gets or sets game localization language code.</summary>
        public string Language
        {
            get
            {
                try
                {
                    if (SmartLocalization.LanguageManager.HasInstance && Romby.IsValid(SmartLocalization.LanguageManager.Instance))
                    {
                        string loaded = SmartLocalization.LanguageManager.Instance.LoadedLanguage;
                        if (!string.IsNullOrEmpty(loaded)) return loaded;
                    }
                }
                catch { }

                return !string.IsNullOrEmpty(Data?.language) ? Data.language : "en";
            }
            set
            {
                if (Data != null)
                {
                    Data.language = value;
                    Debug.Log($"[RombyLib.Settings] Language set to '{value}'.");
                }
            }
        }
    }

    /// <summary>
    /// Manages active game mode (campaign, roguelike), multipliers, and challenges.
    /// </summary>
    public class GameModeController
    {
        private GameSaveData Data => Game.CurrentData;

        internal GameModeController() { }

        /// <summary>Raw game mode type identifier.</summary>
        public int ModeType
        {
            get => Data?.gameModeType ?? 0;
            set { if (Data != null) Data.gameModeType = value; }
        }

        /// <summary>Checks whether roguelike mode is currently active.</summary>
        public bool IsRoguelike => ModeType != 0;

        /// <summary>Elapsed run time in roguelike mode.</summary>
        public float RoguelikeRunTime
        {
            get => Data?.roguelikeRunElapsedTime ?? 0f;
            set { if (Data != null) Data.roguelikeRunElapsedTime = value; }
        }

        /// <summary>Money multiplier applied in roguelike mode.</summary>
        public int RoguelikeMoneyMultiplier
        {
            get => Data?.roguelikeMoneyMultiplier ?? 1;
            set { if (Data != null) Data.roguelikeMoneyMultiplier = value; }
        }

        /// <summary>Array of active challenge IDs.</summary>
        public int[] ActiveChallengeIds => Data?.activeChallengeIds ?? Array.Empty<int>();

        /// <summary>Checks whether a challenge is currently active by ID.</summary>
        public bool IsChallengeActive(int challengeId)
        {
            var arr = ActiveChallengeIds;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == challengeId) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Controls player progression, money, XP, level, lives, and skill points,
    /// ensuring continuous two-way synchronization between the active save data and live GameManager.
    /// </summary>
    public class PlayerStateController
    {
        private PlayerSaveData Data => Game.CurrentData?.player;
        private GameManager GM => GameManager.Instance;

        internal PlayerStateController() { }

        /// <summary>
        /// Gets or sets the player's currency/money. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int Money
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerMoney;
                return Data?.money ?? 0;
            }
            set
            {
                int oldVal = Money;
                int val = Mathf.Max(0, value);
                if (Data != null)
                    Data.money = val;

                if (Romby.IsValid(GM))
                {
                    GM.playerMoney = val;
                    GM.UpdateMoneyUI();
                }

                if (val != oldVal)
                    Game.Events.TriggerMoneyChanged(val, val - oldVal);
            }
        }

        /// <summary>
        /// Gets or sets current player experience points (XP). Synchronizes both live GameManager and active save data.
        /// </summary>
        public int XP
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerCurrentXP;
                return Data?.currentXP ?? 0;
            }
            set
            {
                int oldVal = XP;
                int val = Mathf.Max(0, value);
                if (Data != null)
                    Data.currentXP = val;

                if (Romby.IsValid(GM))
                    GM.playerCurrentXP = val;

                if (val != oldVal)
                    Game.Events.TriggerXPChanged(val, val - oldVal);
            }
        }

        /// <summary>
        /// Gets or sets player current level. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int Level
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerLevel;
                return Data?.level ?? 1;
            }
            set
            {
                int oldVal = Level;
                int val = Mathf.Max(1, value);
                if (Data != null)
                    Data.level = val;

                if (Romby.IsValid(GM))
                    GM.playerLevel = val;

                if (val != oldVal)
                    Game.Events.TriggerLevelUp(val);
            }
        }

        /// <summary>
        /// Gets or sets player current lives/health fragments. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int Lives
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerCurrentLives;
                return Data?.currentLives ?? 0;
            }
            set
            {
                int oldVal = Lives;
                int clamped = Mathf.Clamp(value, 0, MaxLives);
                if (Data != null)
                    Data.currentLives = clamped;

                if (Romby.IsValid(GM))
                {
                    int current = GM.playerCurrentLives;
                    int diff = clamped - current;
                    if (diff > 0)
                        GM.PlayerGainLives(diff);
                    else if (diff < 0)
                        GM.PlayerLoseLives(-diff);
                }

                if (clamped != oldVal)
                    Game.Events.TriggerLivesChanged(clamped);
            }
        }

        /// <summary>
        /// Gets or sets player maximum lives capacity. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int MaxLives
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerMaxLives;
                return Data?.maxLives ?? 1;
            }
            set
            {
                int val = Mathf.Max(1, value);
                if (Data != null)
                    Data.maxLives = val;

                if (Romby.IsValid(GM))
                    GM.playerMaxLives = val;
            }
        }

        /// <summary>
        /// Gets or sets available skill points. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int SkillPoints
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerSkillPoints;
                return Data?.skillPoints ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (Data != null)
                    Data.skillPoints = val;

                if (Romby.IsValid(GM))
                {
                    GM.playerSkillPoints = val;
                    GM.UpdateSkillPointsIcon();
                }

                var tree = SkillTreeManager.Instance;
                if (Romby.IsValid(tree))
                    tree.RefreshUI();
            }
        }

        /// <summary>
        /// Gets or sets total skill points earned across the run. Synchronizes both live GameManager and active save data.
        /// </summary>
        public int TotalSkillPointsEarned
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerSkillPointsTotalEarned;
                return Data?.skillPointsTotalEarned ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (Data != null)
                    Data.skillPointsTotalEarned = val;

                if (Romby.IsValid(GM))
                    GM.playerSkillPointsTotalEarned = val;
            }
        }

        /// <summary>
        /// Safely spends money with game UI and audio reaction.
        /// </summary>
        public bool SpendMoney(int cost)
        {
            int old = Money;
            bool success = false;
            if (Romby.IsValid(GM))
            {
                if (GM.SpendMoney(cost))
                {
                    if (Data != null)
                        Data.money = GM.playerMoney;
                    success = true;
                }
            }
            else if (Money >= cost)
            {
                Money -= cost;
                success = true;
            }

            if (success)
                Game.Events.TriggerMoneyChanged(Money, Money - old);

            return success;
        }

        /// <summary>
        /// Adds money to the player triggering animations and sound.
        /// </summary>
        public void AddMoney(int amount)
        {
            int old = Money;
            if (Romby.IsValid(GM))
            {
                GM.AddMoney(amount);
                if (Data != null)
                    Data.money = GM.playerMoney;
                Game.Events.TriggerMoneyChanged(GM.playerMoney, GM.playerMoney - old);
            }
            else
            {
                Money += amount;
            }
        }

        /// <summary>
        /// Adds experience points to the player.
        /// </summary>
        public void AddXP(int amount)
        {
            int old = XP;
            if (Romby.IsValid(GM))
            {
                GM.AddXP(amount);
                if (Data != null)
                    Data.currentXP = GM.playerCurrentXP;
                Game.Events.TriggerXPChanged(GM.playerCurrentXP, GM.playerCurrentXP - old);
            }
            else
            {
                XP += amount;
            }
        }

        /// <summary>
        /// Forces an immediate player level up.
        /// </summary>
        public void ForceLevelUp()
        {
            if (Romby.IsValid(GM))
            {
                GM.ForceLevelUp();
                if (Data != null)
                {
                    Data.level = GM.playerLevel;
                    Data.currentXP = GM.playerCurrentXP;
                    Data.skillPoints = GM.playerSkillPoints;
                }
                Game.Events.TriggerLevelUp(GM.playerLevel);
            }
            else
            {
                Level++;
            }
        }

        /// <summary>
        /// Pushes all current player save data into the active live game state via GameStateLoader.
        /// </summary>
        public void SyncToLive()
        {
            if (Data != null)
            {
                try
                {
                    GameStateLoader.LoadPlayerData(Data);
                    Debug.Log("[RombyLib.Player] Synchronized save data to live game via GameStateLoader.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Player] Failed to sync save data to live game: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Pulls current live player state into the active save data via SaveDataCollector.
        /// </summary>
        public void SyncToSave()
        {
            if (Data != null)
            {
                try
                {
                    SaveDataCollector.CollectPlayerData(Data);
                    Debug.Log("[RombyLib.Player] Synchronized live game to save data via SaveDataCollector.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Player] Failed to sync live game to save data: {ex.Message}");
                }
            }
        }

        /// <summary>Snapshot list of captured monsters held in player's Pandora's Box inventory.</summary>
        public IReadOnlyList<CapturedMonsterSaveData> CapturedMonsters
        {
            get
            {
                var list = Data?.capturedMonsters;
                if (list != null)
                {
                    var result = new List<CapturedMonsterSaveData>(list.Count);
                    foreach (var m in list) result.Add(m);
                    return result;
                }
                return Array.Empty<CapturedMonsterSaveData>();
            }
        }

        /// <summary>Maximum number of monster soul slots available in Pandora's Box.</summary>
        public int MaxPandoraSlots
        {
            get => Data?.cajaPandoraMaxSlots ?? 3;
            set
            {
                if (Data != null) Data.cajaPandoraMaxSlots = Mathf.Max(1, value);
            }
        }

        /// <summary>Snapshot list of captured monsters currently stored on the ranch/farm.</summary>
        public IReadOnlyList<CapturedMonsterSaveData> FarmStoredMonsters
        {
            get
            {
                var list = Data?.granjaAlmacenados;
                if (list != null)
                {
                    var result = new List<CapturedMonsterSaveData>(list.Count);
                    foreach (var m in list) result.Add(m);
                    return result;
                }
                return Array.Empty<CapturedMonsterSaveData>();
            }
        }

        /// <summary>Adds a captured monster to player's active box inventory.</summary>
        public void AddCapturedMonster(CapturedMonsterSaveData monster)
        {
            if (monster == null || Data == null) return;
            if (Data.capturedMonsters == null)
                Data.capturedMonsters = new Il2CppCollections.List<CapturedMonsterSaveData>();
            Data.capturedMonsters.Add(monster);
        }

        /// <summary>Removes a captured monster by index from Pandora's Box.</summary>
        public bool RemoveCapturedMonster(int index)
        {
            if (Data?.capturedMonsters == null || index < 0 || index >= Data.capturedMonsters.Count)
                return false;
            Data.capturedMonsters.RemoveAt(index);
            return true;
        }

        /// <summary>Clears all captured monsters currently stored in Pandora's Box.</summary>
        public void ClearCapturedMonsters()
        {
            if (Data?.capturedMonsters != null)
                Data.capturedMonsters.Clear();
        }
    }

    /// <summary>
    /// Controller for the embedded Rhythm minigame (radio, songs, scoring, and auto-assist).
    /// </summary>
    public class RhythmController
    {
        private RhythmMinigameController Ctrl => Romby.IsValid(RhythmMinigameController.Instance) ? RhythmMinigameController.Instance : null;
        private RhythmScoreTracker Tracker => Ctrl != null ? Ctrl.Marcador : null;

        internal RhythmController() { }

        /// <summary>Raw native RhythmMinigameController singleton instance.</summary>
        public RhythmMinigameController RawInstance => Ctrl;

        /// <summary>Whether the rhythm minigame is currently open and active.</summary>
        public bool IsActive => Ctrl != null && RhythmMinigameController.IsActive;

        /// <summary>Current state of the rhythm minigame (Idle, Seleccion, Countdown, Playing, Results, Stopping).</summary>
        public RhythmMinigameState State => Ctrl != null ? Ctrl.Estado : RhythmMinigameState.Idle;

        /// <summary>Whether a song is actively playing right now.</summary>
        public bool IsPlaying => State == RhythmMinigameState.Playing;

        /// <summary>Currently active song chart (or null if no song is loaded).</summary>
        public RhythmChart CurrentChart => Ctrl?.ChartActual;

        /// <summary>Song identifier or title of the currently loaded chart.</summary>
        public string CurrentSongId => CurrentChart?.SongId ?? string.Empty;

        /// <summary>Alternative or display title of the currently loaded chart.</summary>
        public string CurrentSongTitle => CurrentChart?.tituloAlternativo ?? CurrentChart?.nombreClip ?? string.Empty;

        /// <summary>Current score achieved in the song.</summary>
        public int Score
        {
            get => Tracker != null ? Tracker.Puntuacion : 0;
            set { if (Tracker != null) Tracker.Puntuacion = value; }
        }

        /// <summary>Current active combo streak.</summary>
        public int Combo
        {
            get => Tracker != null ? Tracker.Combo : 0;
            set { if (Tracker != null) Tracker.Combo = value; }
        }

        /// <summary>Maximum combo streak reached in the current session.</summary>
        public int MaxCombo
        {
            get => Tracker != null ? Tracker.ComboMaximo : 0;
            set { if (Tracker != null) Tracker.ComboMaximo = value; }
        }

        /// <summary>Current note accuracy ratio (0.0 to 1.0, where 1.0 = 100%).</summary>
        public float Accuracy => Tracker != null ? Tracker.Accuracy : 0f;

        /// <summary>Count of Perfect note judgements in the current run.</summary>
        public int PerfectHits
        {
            get => Tracker != null ? Tracker.Perfect : 0;
            set { if (Tracker != null) Tracker.Perfect = value; }
        }

        /// <summary>Count of Great note judgements in the current run.</summary>
        public int GreatHits
        {
            get => Tracker != null ? Tracker.Great : 0;
            set { if (Tracker != null) Tracker.Great = value; }
        }

        /// <summary>Count of Good note judgements in the current run.</summary>
        public int GoodHits
        {
            get => Tracker != null ? Tracker.Good : 0;
            set { if (Tracker != null) Tracker.Good = value; }
        }

        /// <summary>Count of Missed notes in the current run.</summary>
        public int Misses
        {
            get => Tracker != null ? Tracker.Miss : 0;
            set { if (Tracker != null) Tracker.Miss = value; }
        }

        /// <summary>Total note count in the current chart.</summary>
        public int TotalNotes => Tracker != null ? Tracker.NotasTotales : (CurrentChart?.TotalNotas ?? 0);

        /// <summary>Current hyperspeed setting multiplier index.</summary>
        public int Hyperspeed => Ctrl != null ? Ctrl.Hipervelocidad : 0;

        /// <summary>Stops the current song playback.</summary>
        public void Stop()
        {
            if (Ctrl != null)
            {
                Ctrl.DetenerCancion();
                Debug.Log("[RombyLib.Rhythm] Song stopped.");
            }
        }

        /// <summary>Closes and powers off the radio minigame interface.</summary>
        public void TurnOffRadio()
        {
            if (Ctrl != null)
            {
                Ctrl.ApagarRadio();
                Debug.Log("[RombyLib.Rhythm] Radio powered off.");
            }
        }

        /// <summary>Restarts the current song from the beginning.</summary>
        public void RestartSong()
        {
            if (Ctrl != null)
            {
                Ctrl.Repetir();
                Debug.Log("[RombyLib.Rhythm] Song restarted.");
            }
        }

        /// <summary>
        /// Automatically completes the current song with perfect accuracy and maximum score (S Rank).
        /// </summary>
        public void CompleteWithMaxScore()
        {
            if (Tracker != null)
            {
                Tracker.Puntuacion = Tracker.PuntuacionMaximaPosible > 0 ? Tracker.PuntuacionMaximaPosible : 1000000;
                Tracker.Perfect = Tracker.NotasTotales;
                Tracker.Great = 0;
                Tracker.Good = 0;
                Tracker.Miss = 0;
                Tracker.Combo = Tracker.NotasTotales;
                Tracker.ComboMaximo = Tracker.NotasTotales;
                Debug.Log($"[RombyLib.Rhythm] Auto-completed song with maximum score ({Tracker.Puntuacion}).");
            }
        }
    }

    /// <summary>
    /// Controller for the Colosseum arena (Boss Rush, Battle Royale, betting, and combat controls).
    /// </summary>
    public class ColosseumController
    {
        private ColiseoManager Ctrl => Romby.IsValid(ColiseoManager.Instance) ? ColiseoManager.Instance : null;

        internal ColosseumController() { }

        /// <summary>Raw native ColiseoManager singleton instance.</summary>
        public ColiseoManager RawInstance => Ctrl;

        /// <summary>Whether players are currently inside the Colosseum arena.</summary>
        public bool IsInside => Ctrl != null && Ctrl.JugadoresEnColiseo;

        /// <summary>Whether a combat activity (Boss Rush / Battle Royale) is actively in progress.</summary>
        public bool IsActivityInProgress => Ctrl != null && Ctrl.ActividadEnCurso;

        /// <summary>Whether spectator camera mode is currently active.</summary>
        public bool IsSpectatorActive => Ctrl != null && Ctrl.EspectadorActivo;

        /// <summary>Whether combat fast-forward (speedup) mode is enabled.</summary>
        public bool IsFastForwardActive => Ctrl != null && Ctrl.CombateAceleradoActivo;

        /// <summary>Checks whether Boss Rush mode has been completed in this save slot.</summary>
        public bool IsBossRushCompleted => Ctrl != null && Ctrl.GetBossRushCompletado();

        /// <summary>Gets the highest Battle Royale wave cleared in this save slot.</summary>
        public int BattleRoyaleWavesCleared => Ctrl != null ? Ctrl.GetOleadasBRSuperadas() : 0;

        /// <summary>Teleports the player directly inside the Colosseum arena.</summary>
        public void Enter()
        {
            if (Ctrl != null)
            {
                Ctrl.EntrarAlColiseo();
                Debug.Log("[RombyLib.Colosseum] Entered Colosseum arena.");
            }
        }

        /// <summary>Teleports the player back out to the main world map.</summary>
        public void Exit()
        {
            if (Ctrl != null)
            {
                Ctrl.SalirDelColiseo();
                Debug.Log("[RombyLib.Colosseum] Exited Colosseum arena.");
            }
        }

        /// <summary>Toggles combat speedup / fast-forward mode on and off.</summary>
        public void ToggleFastForward()
        {
            if (Ctrl != null)
            {
                Ctrl.ToggleAceleracionCombate();
                Debug.Log($"[RombyLib.Colosseum] Fast forward toggled (now {Ctrl.CombateAceleradoActivo}).");
            }
        }

        /// <summary>Requests early exit / forfeiture from an active combat activity.</summary>
        public void RequestExitCombat()
        {
            if (Ctrl != null)
            {
                Ctrl.SolicitarSalidaCombate();
                Debug.Log("[RombyLib.Colosseum] Exit combat requested.");
            }
        }
    }

    /// <summary>
    /// Controller for the Monster Farm (captured monster storage and wandering pets).
    /// </summary>
    public class FarmController
    {
        private GranjaManager Ctrl => Romby.IsValid(GranjaManager.Instance) ? GranjaManager.Instance : null;

        internal FarmController() { }

        /// <summary>Raw native GranjaManager singleton instance.</summary>
        public GranjaManager RawInstance => Ctrl;

        /// <summary>Whether players are currently inside the Monster Farm.</summary>
        public bool IsInside => Ctrl != null && Ctrl.JugadoresEnGranja;

        /// <summary>Total number of captured monsters stored in the farm.</summary>
        public int StoredMonstersCount => Ctrl != null ? Ctrl.GetAlmacenadosCount() : 0;

        /// <summary>Total number of active wandering monsters in the farm pasture.</summary>
        public int WanderingMonstersCount => Ctrl != null && Ctrl.paseantesActivos != null ? Ctrl.paseantesActivos.Count : 0;

        /// <summary>Teleports the player directly into the Monster Farm.</summary>
        public void Enter()
        {
            if (Ctrl != null)
            {
                Ctrl.EntrarALaGranja();
                Debug.Log("[RombyLib.Farm] Entered Monster Farm.");
            }
        }

        /// <summary>Teleports the player back to the main world map.</summary>
        public void Exit()
        {
            if (Ctrl != null)
            {
                Ctrl.SalirDeLaGranja();
                Debug.Log("[RombyLib.Farm] Exited Monster Farm.");
            }
        }
    }

    /// <summary>
    /// Facade controller providing high-level access to RombyLib's native cyberpunk UI system.
    /// </summary>
    public class UIController
    {
        /// <summary>Gets the singleton screen-space overlay Canvas.</summary>
        public UnityEngine.Canvas Canvas => RombyLib.UI.Canvas;

        /// <summary>Gets the root RectTransform of the singleton UI Canvas.</summary>
        public UnityEngine.RectTransform CanvasRect => RombyLib.UI.CanvasRect;

        /// <summary>Gets Rombykon's native Cactron SDF font asset.</summary>
        public TMPro.TMP_FontAsset GameFont => RombyLib.UI.GameFont;

        /// <summary>
        /// Creates a styled, draggable window panel matching Rombykon's native aesthetic.
        /// </summary>
        public UIPanel CreatePanel(string title, float width = 420f, float height = 500f, UIAnchor anchor = UIAnchor.Center, UnityEngine.Vector2? offset = null)
        {
            return RombyLib.UI.CreatePanel(title, width, height, anchor, offset);
        }

        /// <summary>
        /// Creates a styled panel with exact screen pixel coordinates.
        /// </summary>
        public UIPanel CreatePanel(string title, float width, float height, float x, float y)
        {
            return RombyLib.UI.CreatePanel(title, width, height, x, y);
        }

        /// <summary>
        /// Displays an authentic in-game announcement notification with smooth floating animations,
        /// matching Rombykon's native announcement aesthetics.
        /// </summary>
        public void ShowAnnouncement(
            string title,
            string subtitle = null,
            float duration = 3.5f,
            UnityEngine.Color? titleColor = null,
            NotificationPosition position = NotificationPosition.Bottom,
            NotificationSound sound = NotificationSound.None,
            string customSoundName = null)
        {
            RombyLib.UI.ShowAnnouncement(title, subtitle, duration, titleColor, position, sound, customSoundName);
        }

        /// <summary>
        /// Displays an in-game notification on screen matching Rombykon's native style.
        /// </summary>
        public void ShowNotification(
            string message,
            float duration = 3.5f,
            UnityEngine.Color? accentColor = null,
            NotificationPosition position = NotificationPosition.Bottom,
            NotificationSound sound = NotificationSound.None,
            string customSoundName = null)
        {
            RombyLib.UI.ShowNotification(message, duration, accentColor, position, sound, customSoundName);
        }

        /// <summary>
        /// Plays an in-game sound effect from native game assets by preset or clip name.
        /// </summary>
        public void PlaySound(NotificationSound sound, float volume = 1f) => RombyLib.UI.PlaySound(sound, volume);

        /// <summary>
        /// Plays an in-game sound effect from native game assets by exact or partial name.
        /// </summary>
        public void PlaySound(string clipName, float volume = 1f) => RombyLib.UI.PlaySound(clipName, volume);
    }

    /// <summary>
    /// High-level controller for mod configuration, reflection registration, and in-game Settings menu integration ("УЛЬТРАМОД").
    /// </summary>
    public class ModManagerController
    {
        /// <summary>All currently registered mod definitions.</summary>
        public IReadOnlyList<ModDefinition> AllMods => ModManager.AllMods;

        /// <summary>Triggered whenever any mod setting changes.</summary>
        public event Action<ModDefinition, ModSettingEntry> OnSettingChanged
        {
            add => ModManager.OnSettingChanged += value;
            remove => ModManager.OnSettingChanged -= value;
        }

        /// <summary>
        /// Registers a mod settings container class.
        /// </summary>
        public ModDefinition Register<T>(string modId = null, string modName = null, string version = null, string author = null, string description = null)
        {
            return ModManager.Register<T>(modId, modName, version, author, description);
        }

        /// <summary>
        /// Registers a mod settings container class by <see cref="Type"/>.
        /// </summary>
        public ModDefinition Register(Type targetType, string modId = null, string modName = null, string version = null, string author = null, string description = null)
        {
            return ModManager.Register(targetType, modId, modName, version, author, description);
        }

        /// <summary>Gets a registered mod by identifier, or null if not found.</summary>
        public ModDefinition GetMod(string modId) => ModManager.GetMod(modId);

        /// <summary>Forces saving of all mod configs to disk immediately.</summary>
        public void SaveAll() => ModManager.SaveAll();

        /// <summary>Scans all loaded assemblies for classes declaring [Manage] or [ModSetting] fields.</summary>
        public void ScanAssemblies() => ModManager.ScanLoadedAssemblies();
    }
}
