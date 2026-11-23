using System;
using System.Collections.Generic;
using UnityEngine;

namespace RombyLib
{
    /// <summary>
    /// High-performance spatial cache for active scene enemies.
    /// Eliminates frequent FindObjectsOfType calls and per-frame overhead.
    /// </summary>
    internal static class EnemyTracker
    {
        public struct EnemyEntry
        {
            public EnemyBase Enemy;
            public Transform Transform;
            public Vector3 Position;
            public int InstanceId;
        }

        private static readonly List<EnemyEntry> _cached = new List<EnemyEntry>(64);
        private static readonly List<EnemyBase> _cachedEnemiesOnly = new List<EnemyBase>(64);
        private static float _lastRefreshTime = -1f;
        private const float CacheTtl = 0.10f; // Refresh object cache at most 10 times per second

        public static IReadOnlyList<EnemyEntry> GetActiveEntries()
        {
            RefreshIfNeeded();

            // Refresh positions of alive enemies without re-querying FindObjectsOfType
            for (int i = _cached.Count - 1; i >= 0; i--)
            {
                var entry = _cached[i];
                if (!Romby.IsValid(entry.Enemy) || !Romby.IsValid(entry.Transform) || !entry.Enemy.gameObject.activeInHierarchy)
                {
                    _cached.RemoveAt(i);
                    continue;
                }
                entry.Position = entry.Transform.position;
                _cached[i] = entry;
            }
            return _cached;
        }

        public static IReadOnlyList<EnemyBase> GetAllEnemies()
        {
            var entries = GetActiveEntries();
            _cachedEnemiesOnly.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                _cachedEnemiesOnly.Add(entries[i].Enemy);
            }
            return _cachedEnemiesOnly;
        }

        public static void ForceRefresh()
        {
            _lastRefreshTime = -1f;
            RefreshIfNeeded();
        }

        private static void RefreshIfNeeded()
        {
            float now = Time.time;
            if (now - _lastRefreshTime < CacheTtl)
                return;

            _lastRefreshTime = now;
            _cached.Clear();

            try
            {
                var found = UnityEngine.Object.FindObjectsOfType<EnemyBase>();
                if (found == null) return;

                for (int i = 0; i < found.Length; i++)
                {
                    var e = found[i];
                    if (!Romby.IsValid(e) || !Romby.IsValid(e.gameObject) || !e.gameObject.activeInHierarchy) continue;

                    var tr = e.transform;
                    if (!Romby.IsValid(tr)) continue;

                    _cached.Add(new EnemyEntry
                    {
                        Enemy = e,
                        Transform = tr,
                        Position = tr.position,
                        InstanceId = e.gameObject.GetInstanceID()
                    });
                }
            }
            catch
            {
                // Guard against scene unload or transition states
            }
        }

        public static EnemyBase FindClosest(Vector3 center, float maxRadius, out Vector3 targetPos)
        {
            var entries = GetActiveEntries();
            EnemyBase closest = null;
            targetPos = Vector3.zero;
            float minSqrDist = maxRadius * maxRadius;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                float sqrDist = (e.Position - center).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = e.Enemy;
                    targetPos = e.Position;
                }
            }
            return closest;
        }
    }

    /// <summary>
    /// Controller for discovering, filtering, and executing combat actions on enemies in the scene.
    /// </summary>
    public class EnemiesController
    {
        internal EnemiesController() { }

        /// <summary>
        /// Gets all active, alive <see cref="EnemyBase"/> instances currently spawned in the scene.
        /// </summary>
        public IReadOnlyList<EnemyBase> All => EnemyTracker.GetAllEnemies();

        /// <summary>Total count of active enemies currently spawned.</summary>
        public int Count => All.Count;

        /// <summary>
        /// Gets all active enemies located within a specified radius from the center point.
        /// </summary>
        /// <param name="center">World coordinates center point.</param>
        /// <param name="radius">Search radius in world units.</param>
        public IReadOnlyList<EnemyBase> GetNearby(Vector3 center, float radius)
        {
            var entries = EnemyTracker.GetActiveEntries();
            float sqrRadius = radius * radius;
            var result = new List<EnemyBase>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!Romby.IsValid(entry.Enemy)) continue;
                if ((entry.Position - center).sqrMagnitude <= sqrRadius)
                    result.Add(entry.Enemy);
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// Finds the closest active enemy to a given world position, or null if no enemies exist.
        /// </summary>
        public EnemyBase GetClosest(Vector3 position)
        {
            return EnemyTracker.FindClosest(position, float.MaxValue, out _);
        }

        /// <summary>
        /// Deals damage to all active enemies currently alive in the scene.
        /// </summary>
        /// <param name="damage">Amount of damage to inflict.</param>
        /// <param name="isCritical">Whether the damage is treated as critical.</param>
        /// <returns>Number of enemies successfully hit.</returns>
        public int DamageAll(int damage, bool isCritical = false)
        {
            var all = All;
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                try
                {
                    if (Romby.IsValid(all[i]))
                    {
                        all[i].TakeDamage(damage, isCritical);
                        count++;
                    }
                }
                catch { }
            }
            Debug.Log($"[RombyLib.Enemies] Damaged {count} enemies by {damage}.");
            return count;
        }

        /// <summary>
        /// Deals damage to all enemies within a specified distance from target center.
        /// </summary>
        /// <param name="center">World position center.</param>
        /// <param name="radius">Radius of effect.</param>
        /// <param name="damage">Damage amount.</param>
        /// <param name="isCritical">Whether hit is critical.</param>
        /// <returns>Number of enemies hit.</returns>
        public int DamageInRadius(Vector3 center, float radius, int damage, bool isCritical = false)
        {
            var nearby = GetNearby(center, radius);
            int count = 0;
            for (int i = 0; i < nearby.Count; i++)
            {
                try
                {
                    if (Romby.IsValid(nearby[i]))
                    {
                        nearby[i].TakeDamage(damage, isCritical);
                        count++;
                    }
                }
                catch { }
            }
            Debug.Log($"[RombyLib.Enemies] Damaged {count} enemies in radius {radius}m by {damage}.");
            return count;
        }

        /// <summary>
        /// Instantly eliminates all active enemies on the map.
        /// </summary>
        /// <returns>Number of enemies killed.</returns>
        public int KillAll()
        {
            return DamageAll(999999, true);
        }

        /// <summary>
        /// Restores health to all active enemies by a specified amount.
        /// </summary>
        /// <param name="amount">Health points to restore.</param>
        /// <returns>Number of enemies healed.</returns>
        public int HealAll(int amount)
        {
            var all = All;
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                try
                {
                    if (Romby.IsValid(all[i]))
                    {
                        all[i].RestoreHealth(amount);
                        count++;
                    }
                }
                catch { }
            }
            Debug.Log($"[RombyLib.Enemies] Healed {count} enemies by {amount}.");
            return count;
        }

        /// <summary>
        /// Modifies properties of all active enemies, making them inverted/elite with custom multipliers.
        /// </summary>
        public int InvertAll(float healthMultiplier = 1f, float damageMultiplier = 1f, float speedMultiplier = 1f, float rewardMultiplier = 2f)
        {
            var all = All;
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                try
                {
                    if (Romby.IsValid(all[i]))
                    {
                        all[i].MakeInverted(healthMultiplier, damageMultiplier, speedMultiplier, rewardMultiplier);
                        count++;
                    }
                }
                catch { }
            }
            Debug.Log($"[RombyLib.Enemies] Inverted {count} enemies (reward multiplier: {rewardMultiplier}x).");
            return count;
        }
    }
}
