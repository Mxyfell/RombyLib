using System;
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

        /// <summary>Gets the current active game save data.</summary>
        public static GameSaveData CurrentData => SaveManager.Instance != null ? SaveManager.Instance.GetCurrentSaveData() : null;

        /// <summary>Collects and captures the current live state of the game.</summary>
        public static GameSaveData CaptureLiveState() => SaveDataCollector.CollectAllData();
    }

    /// <summary>
    /// Handles save slots, autosaves, manual saving, and loading.
    /// </summary>
    public class SaveSystemController
    {
        private SaveManager Manager => SaveManager.Instance;

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
        public bool HasActiveSave => GameManagerSaveHelper.HasActiveSave();

        /// <summary>Saves current game progression into the active slot.</summary>
        public bool SaveCurrent()
        {
            GameManagerSaveHelper.SaveCurrentGame();
            Debug.Log("[RombyLib.Save] Saved current game.");
            return true;
        }

        /// <summary>Saves current game progression into a specific slot.</summary>
        public bool SaveToSlot(int slotIndex)
        {
            if (Manager != null && Manager.SaveGame(slotIndex))
            {
                Debug.Log($"[RombyLib.Save] Saved game to slot {slotIndex}.");
                return true;
            }
            Debug.LogWarning($"[RombyLib.Save] Failed to save game to slot {slotIndex}.");
            return false;
        }

        /// <summary>Loads game state from a specific slot.</summary>
        public bool LoadSlot(int slotIndex)
        {
            bool success = GameManagerSaveHelper.LoadGame(slotIndex);
            Debug.Log($"[RombyLib.Save] Loaded save slot {slotIndex}: {(success ? "Success" : "Failed")}.");
            return success;
        }

        /// <summary>Triggers an immediate quick save with an optional reason.</summary>
        public void QuickSave(string reason = "Manual")
        {
            if (Manager != null)
            {
                Manager.QuickSave(reason);
                Debug.Log($"[RombyLib.Save] Quick-saved ({reason}).");
            }
        }

        /// <summary>Triggers an immediate auto-save.</summary>
        public void AutoSave()
        {
            if (Manager != null)
            {
                Manager.AutoSave();
                Debug.Log("[RombyLib.Save] Auto-saved game.");
            }
        }

        /// <summary>Deletes saved game data in a specific slot.</summary>
        public bool DeleteSlot(int slotIndex)
        {
            if (Manager != null && Manager.DeleteSave(slotIndex))
            {
                Debug.Log($"[RombyLib.Save] Deleted save slot {slotIndex}.");
                return true;
            }
            Debug.LogWarning($"[RombyLib.Save] Failed to delete save slot {slotIndex}.");
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
            GameManagerSaveHelper.StartNewGameInSlot(slotIndex);
            Debug.Log($"[RombyLib.Save] Started new game in slot {slotIndex}.");
        }

        /// <summary>Suspends automatic saves.</summary>
        public void SuspendAutoSave()
        {
            Manager?.SuspendAutoSave();
            Debug.Log("[RombyLib.Save] Suspended auto-save.");
        }

        /// <summary>Resumes automatic saves.</summary>
        public void ResumeAutoSave()
        {
            Manager?.ResumeAutoSave();
            Debug.Log("[RombyLib.Save] Resumed auto-save.");
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

        internal WorldStateController() { }

        /// <summary>Gets or sets the current in-game day.</summary>
        public int CurrentDay
        {
            get => State?.currentDay ?? 1;
            set { if (State != null) State.currentDay = value; }
        }

        /// <summary>Gets or sets whether it is currently nighttime.</summary>
        public bool IsNight
        {
            get => State?.isNight ?? false;
            set { if (State != null) State.isNight = value; }
        }

        /// <summary>Gets or sets time remaining in the current day/night cycle.</summary>
        public float CycleTimeRemaining
        {
            get => State?.cycleTimeRemaining ?? 0f;
            set { if (State != null) State.cycleTimeRemaining = value; }
        }

        /// <summary>Gets or sets the defensive crystal's current health.</summary>
        public float CrystalHealth
        {
            get => State?.crystalCurrentHealth ?? 0f;
            set { if (State != null) State.crystalCurrentHealth = Mathf.Clamp(value, 0f, CrystalMaxHealth); }
        }

        /// <summary>Gets or sets the crystal's maximum health.</summary>
        public float CrystalMaxHealth
        {
            get => State?.crystalMaxHealth ?? 100f;
            set { if (State != null) State.crystalMaxHealth = Mathf.Max(1f, value); }
        }

        /// <summary>Gets or sets the current combat wave index.</summary>
        public int CurrentWave
        {
            get => State?.currentWaveIndex ?? 0;
            set { if (State != null) State.currentWaveIndex = value; }
        }

        /// <summary>Gets or sets the number of active enemies remaining.</summary>
        public int EnemiesRemaining
        {
            get => State?.enemiesRemaining ?? 0;
            set { if (State != null) State.enemiesRemaining = Mathf.Max(0, value); }
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
    }

    /// <summary>
    /// Controls overall game completion, defeated bosses, areas, and discovered teleports.
    /// </summary>
    public class ProgressController
    {
        private ScenarioSaveData Scenario => Game.CurrentData?.scenario;

        internal ProgressController() { }

        /// <summary>Calculates total game completion ratio as a percentage.</summary>
        public float CompletionPercentage => SaveDataCollector.CalculateCurrentGameCompletionPercentage();

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
    /// Finds and inspects defensive plants and flowers in the scene.
    /// </summary>
    public class PlantsController
    {
        internal PlantsController() { }

        /// <summary>Finds all active Flower instances in the scene.</summary>
        public Flower[] ActiveFlowers => UnityEngine.Object.FindObjectsOfType<Flower>();

        /// <summary>Finds all active ShootingFlower instances.</summary>
        public ShootingFlower[] ShootingFlowers => UnityEngine.Object.FindObjectsOfType<ShootingFlower>();

        /// <summary>Finds all active BarrierFlower instances.</summary>
        public BarrierFlower[] BarrierFlowers => UnityEngine.Object.FindObjectsOfType<BarrierFlower>();

        /// <summary>Finds all active SpeedBoostFlower instances.</summary>
        public SpeedBoostFlower[] SpeedBoostFlowers => UnityEngine.Object.FindObjectsOfType<SpeedBoostFlower>();

        /// <summary>Finds all active ThrowRocksFlower instances.</summary>
        public ThrowRocksFlower[] ThrowRocksFlowers => UnityEngine.Object.FindObjectsOfType<ThrowRocksFlower>();

        /// <summary>Finds all active CorruptedCrystalFlower instances.</summary>
        public CorruptedCrystalFlower[] CorruptedCrystalFlowers => UnityEngine.Object.FindObjectsOfType<CorruptedCrystalFlower>();

        /// <summary>Gets the total count of flowers active in the scene.</summary>
        public int TotalActiveFlowersCount => ActiveFlowers != null ? ActiveFlowers.Length : 0;

        /// <summary>List of saved plant instances from save data.</summary>
        public Il2CppCollections.List<PlantData> SavedPlants => Game.CurrentData?.plants?.activePlants;

        /// <summary>Clears saveable object registry.</summary>
        public void ClearRegistry()
        {
            SaveableRegistry.Clear();
            Debug.Log("[RombyLib.Plants] Cleared SaveableRegistry.");
        }
    }

    /// <summary>
    /// Tracks lifetime player stats including kills, playtime, and deaths.
    /// </summary>
    public class StatisticsController
    {
        private StatisticsSaveData Stats => Game.CurrentData?.statistics;

        internal StatisticsController() { }

        /// <summary>Total playtime in seconds.</summary>
        public float TotalPlayTime => Stats?.totalPlayTime ?? 0f;

        /// <summary>Total money accumulated over the run.</summary>
        public int TotalMoneyEarned => Stats?.totalMoneyEarned ?? 0;

        /// <summary>Total experience points collected.</summary>
        public int TotalXPEarned => Stats?.totalXPEarned ?? 0;

        /// <summary>Total standard enemies defeated.</summary>
        public int TotalEnemiesKilled => Stats?.totalEnemiesKilled ?? 0;

        /// <summary>Total bosses defeated.</summary>
        public int TotalBossesKilled => Stats?.totalBossesKilled ?? 0;

        /// <summary>Total player deaths.</summary>
        public int TotalDeaths => Stats?.totalDeaths ?? 0;

        /// <summary>Highest wave reached.</summary>
        public int HighestWaveReached => Stats?.highestWaveReached ?? 0;

        /// <summary>Maximum player level achieved.</summary>
        public int MaxPlayerLevel => Stats?.maxPlayerLevel ?? 0;

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
            get => Data?.language ?? "en";
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
}