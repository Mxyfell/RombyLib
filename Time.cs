using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls game timescale, pause state, and active slow-motion status effects.
    /// </summary>
    public class TimeController
    {
        private SlowEffectManager _cachedSlowManager;

        private SlowEffectManager SlowManager
        {
            get
            {
                if (Romby.IsValid(_cachedSlowManager)) return _cachedSlowManager;
                _cachedSlowManager = Object.FindObjectOfType<SlowEffectManager>();
                return Romby.IsValid(_cachedSlowManager) ? _cachedSlowManager : null;
            }
        }

        /// <summary>Checks whether gameplay is currently paused.</summary>
        public bool IsPaused => GameTimeManager.IsGamePaused;

        /// <summary>Effective current timescale applied to the game loop.</summary>
        public float TimeScale
        {
            get => GameTimeManager.CurrentTimeScale;
            set => GameTimeManager.SetBaseTimeScale(value);
        }

        /// <summary>Current movement speed multiplier calculated from active slow effects.</summary>
        public float CurrentSlowMultiplier
        {
            get
            {
                var mgr = SlowManager;
                return mgr != null ? mgr.GetCurrentSpeedMultiplier() : 1f;
            }
        }

        /// <summary>Checks if any slow motion or movement reduction effect is currently active.</summary>
        public bool HasActiveSlowEffects
        {
            get
            {
                var mgr = SlowManager;
                return mgr != null && mgr.HasActiveEffects();
            }
        }

        /// <summary>Pauses gameplay specifying caller identifier.</summary>
        public void Pause(string requester = "RombyLib")
        {
            GameTimeManager.PauseGame(requester);
            Game.Events.TriggerPauseChanged(true);
        }

        /// <summary>Resumes gameplay specifying caller identifier.</summary>
        public void Resume(string requester = "RombyLib")
        {
            GameTimeManager.ResumeGame(requester);
            Game.Events.TriggerPauseChanged(false);
        }

        /// <summary>Toggles pause state between active and resumed.</summary>
        public void TogglePause(string requester = "RombyLib")
        {
            GameTimeManager.TogglePause(requester);
            Game.Events.TriggerPauseChanged(GameTimeManager.IsGamePaused);
        }

        /// <summary>Requests a scoped timescale modification from a named system owner.</summary>
        public void RequestTimeScale(string owner, float scale)
        {
            GameTimeManager.RequestTimeScale(owner, scale);
        }

        /// <summary>Releases a scoped timescale modification requested by an owner.</summary>
        public void ReleaseTimeScale(string owner)
        {
            GameTimeManager.ReleaseTimeScale(owner);
        }

        /// <summary>Applies a timed slow motion status effect by unique ID.</summary>
        public void ApplySlow(string effectId, float slowFactor, float duration)
        {
            var mgr = SlowManager;
            if (mgr != null && !string.IsNullOrEmpty(effectId))
            {
                mgr.ApplySlowEffect(effectId, slowFactor, duration);
            }
        }

        /// <summary>Removes an active slow motion effect by ID.</summary>
        public void RemoveSlow(string effectId)
        {
            var mgr = SlowManager;
            if (mgr != null && !string.IsNullOrEmpty(effectId))
            {
                mgr.RemoveSlowEffect(effectId);
            }
        }

        /// <summary>Clears all active slow motion and timescale reduction effects.</summary>
        public void ClearAllSlows()
        {
            var mgr = SlowManager;
            if (mgr != null) mgr.ClearAllSlowEffects();
        }

        /// <summary>Checks whether a specific slow motion effect is currently active.</summary>
        public bool HasSlow(string effectId)
        {
            var mgr = SlowManager;
            return mgr != null && !string.IsNullOrEmpty(effectId) && mgr.HasActiveEffect(effectId);
        }
    }
}
