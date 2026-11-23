using System;
using UnityEngine;
using Cinemachine;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls camera framing, zoom, follow targets, and screen shake effects.
    /// </summary>
    public class GameCameraController
    {
        private CinemachineVirtualCamera _cachedVirtualCamera;

        /// <summary>Gets the active Cinemachine virtual camera assigned to the player.</summary>
        public CinemachineVirtualCamera VirtualCamera
        {
            get
            {
                if (Romby.IsValid(_cachedVirtualCamera)) return _cachedVirtualCamera;
                var plr = Romby.Controller;
                if (Romby.IsValid(plr) && Romby.IsValid(plr.virtualCamera))
                {
                    _cachedVirtualCamera = plr.virtualCamera;
                    return _cachedVirtualCamera;
                }
                _cachedVirtualCamera = Object.FindObjectOfType<CinemachineVirtualCamera>();
                return Romby.IsValid(_cachedVirtualCamera) ? _cachedVirtualCamera : null;
            }
        }

        /// <summary>Gets the main scene camera component.</summary>
        public Camera MainCamera => Camera.main;

        /// <summary>World position of the main camera.</summary>
        public Vector3 Position
        {
            get
            {
                var cam = MainCamera;
                return cam != null ? cam.transform.position : Vector3.zero;
            }
        }

        /// <summary>Current orthographic camera zoom size.</summary>
        public float OrthographicSize
        {
            get
            {
                var vcam = VirtualCamera;
                if (vcam != null) return vcam.m_Lens.OrthographicSize;
                var cam = MainCamera;
                return cam != null ? cam.orthographicSize : 7f;
            }
            set => SetZoom(value);
        }

        /// <summary>Current target transform followed by the camera.</summary>
        public Transform FollowTarget
        {
            get
            {
                var vcam = VirtualCamera;
                return vcam != null ? vcam.Follow : null;
            }
            set
            {
                var vcam = VirtualCamera;
                if (vcam != null)
                {
                    vcam.Follow = value;
                    vcam.LookAt = value;
                }
            }
        }

        /// <summary>Adjusts camera orthographic size.</summary>
        public void SetZoom(float targetSize)
        {
            if (targetSize <= 0.1f) targetSize = 0.1f;
            var vcam = VirtualCamera;
            if (vcam != null)
            {
                var lens = vcam.m_Lens;
                lens.OrthographicSize = targetSize;
                vcam.m_Lens = lens;
            }
            var cam = MainCamera;
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = targetSize;
            }
        }

        /// <summary>Triggers screen shake on the virtual camera noise profile if available.</summary>
        public void Shake(float amplitude = 1.2f, float frequency = 2f)
        {
            var vcam = VirtualCamera;
            if (vcam != null)
            {
                var perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    perlin.m_AmplitudeGain = amplitude;
                    perlin.m_FrequencyGain = frequency;
                }
            }
        }

        /// <summary>Resets screen shake noise gains to zero.</summary>
        public void StopShake()
        {
            var vcam = VirtualCamera;
            if (vcam != null)
            {
                var perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    perlin.m_AmplitudeGain = 0f;
                    perlin.m_FrequencyGain = 0f;
                }
            }
        }
    }
}
