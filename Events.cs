using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace RombyLib
{
    /// <summary>
    /// Global C# event dispatch hub for game and player lifecycle notifications.
    /// Event invocations are isolated so subscriber exceptions do not break the game loop.
    /// </summary>
    public class GameEventsController
    {
        internal GameEventsController() { }

        /// <summary>Triggered whenever player money balance changes (newTotal, delta).</summary>
        public event Action<int, int> OnMoneyChanged;

        /// <summary>Triggered whenever player gains experience points (newXP, addedAmount).</summary>
        public event Action<int, int> OnXPChanged;

        /// <summary>Triggered whenever player increases level (newLevel).</summary>
        public event Action<int> OnLevelUp;

        /// <summary>Triggered whenever player life fragment count changes (currentLives).</summary>
        public event Action<int> OnLivesChanged;

        /// <summary>Triggered when a game save operation completes (slotIndex).</summary>
        public event Action<int> OnGameSaved;

        /// <summary>Triggered when a game slot finishes loading (slotIndex).</summary>
        public event Action<int> OnGameLoaded;

        /// <summary>Triggered whenever the in-game day advances or changes (newDay).</summary>
        public event Action<int> OnDayChanged;

        /// <summary>Triggered whenever day/night cycle state transitions (isNight).</summary>
        public event Action<bool> OnNightStateChanged;

        /// <summary>Triggered whenever the player switches active weapon (weaponId).</summary>
        public event Action<int> OnWeaponSwitched;

        /// <summary>Triggered whenever game pause state transitions (isPaused).</summary>
        public event Action<bool> OnPauseChanged;

        /// <summary>Triggered when a Tarot card power is activated (powerType).</summary>
        public event Action<int> OnTarotCardActivated;

        /// <summary>Triggered when a story invasion day of Ykon occurs (day).</summary>
        public event Action<int> OnYkonDayTriggered;

        /// <summary>Triggered when the Possessed Merchant event sequence begins.</summary>
        public event Action OnPossessedMerchantTriggered;

        internal void TriggerMoneyChanged(int newTotal, int delta) => SafeInvoke(OnMoneyChanged, newTotal, delta);
        internal void TriggerXPChanged(int newXP, int delta) => SafeInvoke(OnXPChanged, newXP, delta);
        internal void TriggerLevelUp(int level) => SafeInvoke(OnLevelUp, level);
        internal void TriggerLivesChanged(int lives) => SafeInvoke(OnLivesChanged, lives);
        internal void TriggerGameSaved(int slot) => SafeInvoke(OnGameSaved, slot);
        internal void TriggerGameLoaded(int slot) => SafeInvoke(OnGameLoaded, slot);
        internal void TriggerDayChanged(int day) => SafeInvoke(OnDayChanged, day);
        internal void TriggerNightStateChanged(bool isNight) => SafeInvoke(OnNightStateChanged, isNight);
        internal void TriggerWeaponSwitched(int weaponId) => SafeInvoke(OnWeaponSwitched, weaponId);
        internal void TriggerPauseChanged(bool isPaused) => SafeInvoke(OnPauseChanged, isPaused);
        internal void TriggerTarotCardActivated(int powerType) => SafeInvoke(OnTarotCardActivated, powerType);
        internal void TriggerYkonDay(int day) => SafeInvoke(OnYkonDayTriggered, day);
        internal void TriggerPossessedMerchant() => SafeInvoke(OnPossessedMerchantTriggered);

        private static void SafeInvoke(Action action)
        {
            if (action == null) return;
            var invocationList = action.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action)invocationList[i])();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RombyLib.Events] Exception in event handler: {ex}");
                }
            }
        }

        private static void SafeInvoke<T>(Action<T> action, T arg)
        {
            if (action == null) return;
            var invocationList = action.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T>)invocationList[i])(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RombyLib.Events] Exception in event handler: {ex}");
                }
            }
        }

        private static void SafeInvoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null) return;
            var invocationList = action.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T1, T2>)invocationList[i])(arg1, arg2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RombyLib.Events] Exception in event handler: {ex}");
                }
            }
        }
    }

    /// <summary>
    /// Persistent custom key-value storage for mods, automatically persisted to disk.
    /// </summary>
    public class ModDataStore
    {
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _filePath;

        internal ModDataStore()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(appData, "RombyLib");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                _filePath = Path.Combine(dir, "mod_data.cfg");
                Load();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModData] Failed to initialize ModDataStore directory: {ex.Message}");
            }
        }

        private void Load()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath)) return;
            try
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    int idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        string k = line.Substring(0, idx).Trim();
                        string v = line.Substring(idx + 1).Trim();
                        _entries[k] = v;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModData] Failed to load data: {ex.Message}");
            }
        }

        /// <summary>Flushes all stored entries to disk immediately.</summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(_filePath)) return;
            try
            {
                using var writer = new StreamWriter(_filePath, false);
                foreach (var kvp in _entries)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RombyLib.ModData] Failed to save data: {ex.Message}");
            }
        }

        /// <summary>Stores a string value under a mod key.</summary>
        public void SetString(string modId, string key, string value)
        {
            _entries[$"{modId}:{key}"] = value ?? string.Empty;
            Save();
        }

        /// <summary>Retrieves a string value by mod key.</summary>
        public string GetString(string modId, string key, string defaultValue = "")
        {
            return _entries.TryGetValue($"{modId}:{key}", out var val) ? val : defaultValue;
        }

        /// <summary>Stores an integer value under a mod key.</summary>
        public void SetInt(string modId, string key, int value) => SetString(modId, key, value.ToString());

        /// <summary>Retrieves an integer value by mod key.</summary>
        public int GetInt(string modId, string key, int defaultValue = 0)
        {
            string raw = GetString(modId, key);
            return int.TryParse(raw, out int res) ? res : defaultValue;
        }

        /// <summary>Stores a boolean value under a mod key.</summary>
        public void SetBool(string modId, string key, bool value) => SetString(modId, key, value.ToString());

        /// <summary>Retrieves a boolean value by mod key.</summary>
        public bool GetBool(string modId, string key, bool defaultValue = false)
        {
            string raw = GetString(modId, key);
            return bool.TryParse(raw, out bool res) ? res : defaultValue;
        }

        /// <summary>Stores a floating-point value under a mod key.</summary>
        public void SetFloat(string modId, string key, float value) => SetString(modId, key, value.ToString(CultureInfo.InvariantCulture));

        /// <summary>Retrieves a floating-point value by mod key.</summary>
        public float GetFloat(string modId, string key, float defaultValue = 0f)
        {
            string raw = GetString(modId, key);
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float res) ? res : defaultValue;
        }

        /// <summary>Checks whether a given key exists in storage.</summary>
        public bool HasKey(string modId, string key) => _entries.ContainsKey($"{modId}:{key}");

        /// <summary>Removes a key from storage.</summary>
        public bool DeleteKey(string modId, string key)
        {
            bool removed = _entries.Remove($"{modId}:{key}");
            if (removed) Save();
            return removed;
        }
    }
}
