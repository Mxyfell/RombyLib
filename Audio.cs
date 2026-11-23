using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls sound effects, audio buses, and positional sound playback.
    /// </summary>
    public class AudioController
    {
        private AudioManager Manager
        {
            get
            {
                var inst = AudioManager.Instance;
                return Romby.IsValid(inst) ? inst : null;
            }
        }

        /// <summary>Sets or gets global master audio volume (0.0 to 1.0).</summary>
        public float MasterVolume
        {
            get => Game.Settings.MasterVolume;
            set
            {
                var mgr = Manager;
                if (mgr != null) mgr.SetMasterVolume(value);
                Game.Settings.MasterVolume = value;
            }
        }

        /// <summary>Sets or gets sound effects volume (0.0 to 1.0).</summary>
        public float SFXVolume
        {
            get => Game.Settings.SFXVolume;
            set
            {
                var mgr = Manager;
                if (mgr != null) mgr.SetSFXVolume(value);
                Game.Settings.SFXVolume = value;
            }
        }

        /// <summary>Sets or gets background music volume (0.0 to 1.0).</summary>
        public float MusicVolume
        {
            get => Game.Settings.MusicVolume;
            set
            {
                var mgr = Manager;
                if (mgr != null) mgr.SetMusicVolume(value);
                Game.Settings.MusicVolume = value;
            }
        }

        /// <summary>Plays a sound effect by registered clip name.</summary>
        public void PlaySound(string clipName, float volumeMultiplier = 1f, float pitch = 1f)
        {
            var mgr = Manager;
            if (mgr != null && !string.IsNullOrEmpty(clipName))
            {
                mgr.PlaySoundWithPitch(clipName, volumeMultiplier, pitch);
            }
        }

        /// <summary>Plays an audio clip asset directly through the SFX channel.</summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            var mgr = Manager;
            if (mgr != null && clip != null)
            {
                mgr.PlaySFX2D(clip, volumeMultiplier);
            }
        }

        /// <summary>Plays a sound effect with randomized pitch variation.</summary>
        public void PlaySFXWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f, float volumeMultiplier = 1f)
        {
            var mgr = Manager;
            if (mgr != null && clip != null)
            {
                mgr.PlaySFXWithRandomPitch(clip, minPitch, maxPitch, volumeMultiplier);
            }
        }

        /// <summary>Plays a random clip from an array at a specific world position.</summary>
        public void PlayRandomSFX(AudioClip[] clips, Vector3 position, float volumeMultiplier = 1f, float pitchVariation = 0.1f)
        {
            var mgr = Manager;
            if (mgr != null && clips != null && clips.Length > 0)
            {
                mgr.PlayRandomSFX(clips, position, volumeMultiplier, pitchVariation);
            }
        }

        /// <summary>Pauses all active sound effects.</summary>
        public void PauseAllSFX()
        {
            var mgr = Manager;
            if (mgr != null) mgr.PauseAllSFX();
        }

        /// <summary>Resumes playback for previously paused sound effects.</summary>
        public void ResumeAllSFX()
        {
            var mgr = Manager;
            if (mgr != null) mgr.ResumeAllSFX();
        }

        /// <summary>Immediately stops all playing sound effects.</summary>
        public void StopAllSounds()
        {
            var mgr = Manager;
            if (mgr != null) mgr.StopAllSounds();
        }

        /// <summary>Fades out all active sound effects over a duration.</summary>
        public void FadeOutAllSFX(float fadeDuration = 0.5f)
        {
            var mgr = Manager;
            if (mgr != null) mgr.FadeOutAllSFX(fadeDuration);
        }
    }

    /// <summary>
    /// Controls background soundtrack playback, playlist contexts, and dynamic music transitions.
    /// </summary>
    public class MusicController
    {
        private MusicManager Manager
        {
            get
            {
                var inst = MusicManager.Instance;
                return Romby.IsValid(inst) ? inst : null;
            }
        }

        /// <summary>Current active music context playlist name.</summary>
        public string CurrentContext => Manager != null ? Manager.GetCurrentContextName() : string.Empty;

        /// <summary>Currently playing track filename or identifier.</summary>
        public string CurrentTrack => Manager != null ? Manager.GetCurrentTrackName() : string.Empty;

        /// <summary>Checks whether a background soundtrack is currently playing.</summary>
        public bool IsPlaying => Manager != null && Manager.IsPlaying();

        /// <summary>Sets or gets direct volume for background music playback.</summary>
        public float Volume
        {
            set
            {
                var mgr = Manager;
                if (mgr != null) mgr.SetVolume(value);
            }
        }

        /// <summary>Switches active background music to a named context playlist.</summary>
        public void PlayContext(string contextName, bool forceOverride = false)
        {
            var mgr = Manager;
            if (mgr != null && !string.IsNullOrEmpty(contextName))
            {
                mgr.PlayContext(contextName, forceOverride);
            }
        }

        /// <summary>Pushes a new music context onto the stack and begins playback.</summary>
        public void PushContext(string contextName)
        {
            var mgr = Manager;
            if (mgr != null && !string.IsNullOrEmpty(contextName))
            {
                mgr.PushContextAndPlay(contextName);
            }
        }

        /// <summary>Pops top music context and restores previous playlist.</summary>
        public void PopContext()
        {
            var mgr = Manager;
            if (mgr != null) mgr.PopContext();
        }

        /// <summary>Immediately stops music playback.</summary>
        public void Stop()
        {
            var mgr = Manager;
            if (mgr != null) mgr.Stop();
        }

        /// <summary>Fades out active music volume smoothly.</summary>
        public void FadeOut(float fadeDuration = 0.5f)
        {
            var mgr = Manager;
            if (mgr != null) mgr.FadeOutVolume(fadeDuration);
        }

        /// <summary>Fades in background music to normal volume.</summary>
        public void FadeIn()
        {
            var mgr = Manager;
            if (mgr != null) mgr.FadeInVolume();
        }
    }
}
