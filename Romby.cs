using System;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using TwoDefender.SaveSystem;

using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Identifiers for all weapons in the game.
    /// </summary>
    public enum WeaponType
    {
        AstroPulse = 1,
        SatelliteShield = 2,
        SolarMine = 3,
        Moonerang = 4,
        ExistentialVoid = 5,
        UncleFrank = 6,
        TeslaPawn = 7,
        Plushinator3000 = 8,
        BitBang = 9,
        TrebleClef = 10,
        BigBucks = 11,
        Gachickpon = 12,
        TheScreamer = 13,
        TheSoulTransmuter = 14,
        DivineRelocator = 15,
        Bubblewitch = 16
    }

    /// <summary>
    /// Main entry point for RombyLib. Provides access to player subsystems, stats, skin, and weapons.
    /// </summary>
    public static class Romby
    {
        private static bool _isPatched;
        private static PlayerController _controller;
        private static int _mainThreadId;

        /// <summary>Gets the skin and visual appearance controller for the player.</summary>
        public static SkinController Skin { get; } = new SkinController();

        /// <summary>Gets the stats, buffs, and status effects controller for the player.</summary>
        public static StatsController Stats { get; } = new StatsController();

        /// <summary>Gets the weapons, combat helpers, and weapon management controller.</summary>
        public static WeaponsController Weapons { get; } = new WeaponsController();

        /// <summary>Gets the skill tree and ability management controller.</summary>
        public static SkillsController Skills { get; } = new SkillsController();

        static Romby()
        {
            Init();
        }

        /// <summary>
        /// Initializes library hooks and patches player lifecycle events.
        /// </summary>
        public static void Init()
        {
            if (_mainThreadId == 0)
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            if (_isPatched) return;

            try
            {
                var harmony = new Harmony(Hook.ModGUID);
                harmony.CreateClassProcessor(typeof(PlrPatches)).Patch();
                _isPatched = true;
                Debug.Log("[RombyLib] Initialized and patched player hooks.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib] Failed to apply Harmony patches: {ex}");
            }
        }

        /// <summary>
        /// Gets the active <see cref="PlayerController"/> instance, or null if not spawned.
        /// </summary>
        public static PlayerController Controller
        {
            get
            {
                EnsurePlayer();
                return _controller;
            }
        }

        /// <summary>
        /// Checks whether the player instance is currently loaded and valid in the scene.
        /// </summary>
        public static bool IsLoaded => IsValid(Controller);

        /// <summary>
        /// Safely checks if an Il2Cpp/Unity object is alive, not destroyed, and not collected by GC.
        /// </summary>
        public static bool IsValid(Object obj)
        {
            return obj != null && obj.Pointer != IntPtr.Zero && !obj.WasCollected;
        }

        private static void EnsurePlayer()
        {
            if (IsValid(_controller)) return;

            if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                Debug.LogWarning($"[RombyLib] EnsurePlayer was invoked from non-main thread (Thread ID: {Thread.CurrentThread.ManagedThreadId}). Skipping lookup.");
                return;
            }

            var found = Object.FindObjectOfType<PlayerController>();
            if (IsValid(found))
            {
                _controller = found;
                Skin.Refresh();
            }
            else
            {
                _controller = null;
            }
        }

        /// <summary>Gets or sets the current world position of the player.</summary>
        public static Vector3 Position
        {
            get => IsValid(Controller) ? Controller.transform.position : Vector3.zero;
            set
            {
                if (IsValid(Controller))
                    Controller.transform.position = value;
            }
        }

        /// <summary>Teleports the player to a target 3D world position.</summary>
        public static void Teleport(Vector3 position)
        {
            if (IsValid(Controller))
            {
                Controller.transform.position = position;
                Debug.Log($"[RombyLib] Teleported player to {position}.");
            }
        }

        /// <summary>Teleports the player to target X and Y coordinates, preserving Z.</summary>
        public static void Teleport(float x, float y)
        {
            Teleport(new Vector3(x, y, Position.z));
        }

        /// <summary>Gets or sets whether the player is facing right.</summary>
        public static bool FacingRight
        {
            get => IsValid(Controller) && Controller.FacingRight;
            set
            {
                if (IsValid(Controller))
                    Controller.SetPlayerFacing(value);
            }
        }

        /// <summary>Gets or sets whether player movement is currently blocked.</summary>
        public static bool MovementBlocked
        {
            get => IsValid(Controller) && Controller.IsMovementBlocked();
            set
            {
                if (IsValid(Controller))
                    Controller.SetMovementBlocked(value);
            }
        }

        /// <summary>Respawns the player at the spawn point.</summary>
        public static void Respawn()
        {
            if (IsValid(Controller))
            {
                Controller.OnPlayerRespawn();
                Debug.Log("[RombyLib] Player respawned.");
            }
        }

        /// <summary>Resets all active states and effects on the player.</summary>
        public static void ResetStates()
        {
            if (IsValid(Controller))
            {
                Controller.ResetAllPlayerStates();
                Debug.Log("[RombyLib] Player states reset.");
            }
        }

        #region Internal Patches
        [HarmonyPatch(typeof(PlayerController))]
        private static class PlrPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void AwakePostfix(PlayerController __instance)
            {
                if (IsValid(__instance))
                {
                    _controller = __instance;
                    Skin.Refresh();
                }
            }

            [HarmonyPatch("OnDestroy")]
            [HarmonyPostfix]
            private static void OnDestroyPostfix(PlayerController __instance)
            {
                if (IsValid(_controller) && IsValid(__instance) && _controller.Pointer == __instance.Pointer)
                {
                    _controller = null;
                    Skin.Clear();
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Controls player skills, skill tree progression, and passive ability toggles.
    /// </summary>
    public class SkillsController
    {
        private SkillSystemManager Manager => SkillSystemManager.Instance;
        private SkillTreeManager TreeManager => SkillTreeManager.Instance;

        internal SkillsController() { }

        /// <summary>Checks whether a skill is currently unlocked and active.</summary>
        public bool Has(SkillType skill) => Romby.IsValid(Manager) && Manager.IsSkillActive(skill);

        /// <summary>Checks whether a skill has been acquired/purchased.</summary>
        public bool IsAcquired(SkillType skill) => Romby.IsValid(Manager) && Manager.IsSkillAcquired(skill);

        /// <summary>Checks whether an acquired skill is temporarily toggled off.</summary>
        public bool IsToggledOff(SkillType skill) => Romby.IsValid(Manager) && Manager.IsSkillToggleDisabled(skill);

        /// <summary>List of all currently active skills.</summary>
        public Il2CppCollections.List<SkillType> ActiveSkills => Romby.IsValid(Manager) ? Manager.GetActiveSkills() : null;

        /// <summary>List of all disabled or toggled-off skills.</summary>
        public Il2CppCollections.List<SkillType> DisabledSkills => Romby.IsValid(Manager) ? Manager.GetDisabledSkills() : null;

        /// <summary>Gets the skill point cost of a skill from the tree UI.</summary>
        public int GetCost(SkillType skill)
        {
            if (!Romby.IsValid(TreeManager) || TreeManager.nodeUIs == null)
                return 0;
            foreach (var node in TreeManager.nodeUIs)
            {
                if (node != null && node.skillType == skill)
                    return node.skillCost;
            }
            return 0;
        }

        /// <summary>Checks if player has enough points to purchase the skill.</summary>
        public bool CanAfford(SkillType skill) => Points >= GetCost(skill);

        /// <summary>Attempts to purchase and unlock the skill, deducting skill points.</summary>
        public bool TryBuy(SkillType skill)
        {
            if (IsAcquired(skill))
            {
                Debug.LogWarning($"[RombyLib.Skills] Cannot buy skill {skill}: already acquired.");
                return false;
            }
            int cost = GetCost(skill);
            if (Points < cost)
            {
                Debug.LogWarning($"[RombyLib.Skills] Cannot buy skill {skill}: requires {cost} points, available {Points}.");
                return false;
            }
            Points -= cost;
            bool result = Unlock(skill, false);
            if (result)
                Debug.Log($"[RombyLib.Skills] Successfully bought skill: {skill} for {cost} points.");
            return result;
        }

        /// <summary>Unlocks a skill. If consumePoints is true, deducts its point cost.</summary>
        public bool Unlock(SkillType skill, bool consumePoints = false)
        {
            if (consumePoints)
                return TryBuy(skill);
            if (Romby.IsValid(Manager))
            {
                Manager.SetSkillActive(skill);
                if (Romby.IsValid(TreeManager))
                    TreeManager.RefreshUI();
                Debug.Log($"[RombyLib.Skills] Unlocked skill: {skill}.");
                return true;
            }
            return false;
        }

        /// <summary>Revokes an unlocked skill.</summary>
        public void Revoke(SkillType skill)
        {
            if (Romby.IsValid(Manager))
            {
                Manager.SetSkillInactive(skill);
                if (Romby.IsValid(TreeManager))
                    TreeManager.RefreshUI();
                Debug.Log($"[RombyLib.Skills] Revoked skill: {skill}.");
            }
        }

        /// <summary>Enables a toggled-off skill.</summary>
        public void Enable(SkillType skill)
        {
            if (Romby.IsValid(Manager))
            {
                Manager.EnableSkill(skill);
                Debug.Log($"[RombyLib.Skills] Enabled skill: {skill}.");
            }
        }

        /// <summary>Disables/toggles off an active skill.</summary>
        public void Disable(SkillType skill)
        {
            if (Romby.IsValid(Manager))
            {
                Manager.DisableSkill(skill);
                Debug.Log($"[RombyLib.Skills] Disabled skill: {skill}.");
            }
        }

        /// <summary>Resets all acquired skills and restores default state.</summary>
        public void ResetAll()
        {
            if (Romby.IsValid(Manager))
            {
                Manager.ResetAllSkills();
                Debug.Log("[RombyLib.Skills] Reset all skills.");
            }
        }

        /// <summary>Gets or sets the current unspent skill points.</summary>
        public int Points
        {
            get
            {
                if (Romby.IsValid(GameManager.Instance))
                    return GameManager.Instance.playerSkillPoints;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.skillPoints ?? 0;
            }
            set
            {
                if (Romby.IsValid(GameManager.Instance))
                {
                    GameManager.Instance.playerSkillPoints = value;
                    GameManager.Instance.UpdateSkillPointsIcon();
                }

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                {
                    save.player.skillPoints = value;
                    if (Romby.IsValid(TreeManager))
                        TreeManager.RefreshUI();
                }
            }
        }

        /// <summary>Gets or sets the lifetime total of skill points earned.</summary>
        public int TotalPointsEarned
        {
            get
            {
                var save = SaveManager.Instance?.GetCurrentSaveData();
                return save?.player != null ? save.player.skillPointsTotalEarned : 0;
            }
            set
            {
                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.skillPointsTotalEarned = value;
            }
        }

        /// <summary>Checks whether auto-revive is currently available.</summary>
        public bool IsAutoReviveAvailable => Romby.IsValid(Manager) && Manager.IsAutoReviveAvailable();

        /// <summary>Recharges auto-revive charge.</summary>
        public void RechargeAutoRevive()
        {
            if (Romby.IsValid(Manager))
            {
                Manager.ReactivateAutoRevive();
                Debug.Log("[RombyLib.Skills] Recharged auto-revive.");
            }
        }

        /// <summary>Attempts to trigger auto-revive.</summary>
        public bool TryAutoRevive()
        {
            if (Romby.IsValid(Manager))
            {
                bool success = Manager.TryAutoRevive();
                Debug.Log($"[RombyLib.Skills] Auto-revive attempt: {(success ? "Success" : "Failed")}.");
                return success;
            }
            return false;
        }

        /// <summary>Checks whether all skills in the skill tree are acquired.</summary>
        public bool AreAllSkillsAcquired => Romby.IsValid(TreeManager) && TreeManager.TodasLasHabilidadesAdquiridas();

        /// <summary>Returns the count of acquired skills and total available skills.</summary>
        public (int acquired, int total) Progress
        {
            get
            {
                if (Romby.IsValid(TreeManager))
                {
                    var p = TreeManager.GetProgresoHabilidades();
                    return (p.Item1, p.Item2);
                }
                return (0, 0);
            }
        }

        /// <summary>Checks if player has Dash skill unlocked.</summary>
        public bool CanDash => Has(SkillType.Dash);

        /// <summary>Checks if radar perk is currently active.</summary>
        public bool HasRadar => Romby.IsValid(Manager) && Manager.HasRadarActive();

        /// <summary>Checks if enemy health bar perk is active.</summary>
        public bool HasEnemyHealthBars => Romby.IsValid(Manager) && Manager.HasAnalistaActive();

        /// <summary>Checks if boss health bar perk is active.</summary>
        public bool HasBossHealthBars => Romby.IsValid(Manager) && Manager.HasRastreadorActive();

        /// <summary>Checks if damage numbers display is active.</summary>
        public bool HasDamageNumbers => Has(SkillType.Matematico);

        /// <summary>Checks if chronostasis ability is active.</summary>
        public bool HasChronostasis => Romby.IsValid(Manager) && Manager.HasChronostasisActive();

        /// <summary>Gets the current store discount multiplier.</summary>
        public float StoreDiscountMultiplier => Romby.IsValid(Manager) ? Manager.GetStoreDiscountMultiplier() : 1f;

        /// <summary>Gets the maximum weapon level limit allowed by active skills.</summary>
        public int MaxWeaponLevel => Romby.IsValid(Manager) ? Manager.GetMaxWeaponLevel() : 5;
    }

    /// <summary>
    /// Manages player visual elements, body parts, and sprite renderers.
    /// </summary>
    public class SkinController
    {
        private PlayerController Player => Romby.Controller;

        /// <summary>Root transform containing all visual body parts.</summary>
        public Transform RootTodo { get; private set; }

        public SpriteRenderer Body { get; private set; }
        public SpriteRenderer EyeLeft { get; private set; }
        public SpriteRenderer EyeRight { get; private set; }
        public SpriteRenderer Mouth { get; private set; }
        public SpriteRenderer Extra { get; private set; }
        public SpriteRenderer BodyShadow { get; private set; }
        public SpriteRenderer ExtraShadow { get; private set; }
        public SpriteRenderer LegLeft { get; private set; }
        public SpriteRenderer LegRight { get; private set; }
        public Transform PivotLegLeft { get; private set; }
        public Transform PivotLegRight { get; private set; }

        /// <summary>Visual GameObject shown when the player is alive.</summary>
        public GameObject NormalVisual => Romby.IsValid(Player) ? Player.jugadorNormal : null;

        /// <summary>Visual GameObject shown when the player dies.</summary>
        public GameObject DeathVisual => Romby.IsValid(Player) ? Player.jugadorMuerte : null;

        internal SkinController() { }

        /// <summary>
        /// Clears all cached SpriteRenderer and Transform references.
        /// </summary>
        internal void Clear()
        {
            RootTodo = null;
            Body = null;
            EyeLeft = null;
            EyeRight = null;
            Mouth = null;
            Extra = null;
            BodyShadow = null;
            ExtraShadow = null;
            PivotLegLeft = null;
            PivotLegRight = null;
            LegLeft = null;
            LegRight = null;
        }

        /// <summary>
        /// Re-scans the player hierarchy and updates internal renderer references.
        /// </summary>
        public void Refresh()
        {
            var plr = Player;
            if (!Romby.IsValid(plr))
            {
                Debug.LogWarning("[RombyLib.Skin] Cannot refresh skin: PlayerController is not loaded.");
                return;
            }

            Transform todo = null;
            if (plr.jugadorNormal != null)
                todo = plr.jugadorNormal.transform.Find("Todo");

            if (todo == null && plr.transform != null)
                todo = plr.transform.Find("JugadorNormal/Todo");

            RootTodo = todo;
            if (todo == null)
            {
                Debug.LogWarning("[RombyLib.Skin] Failed to locate visual root transform ('Todo') on PlayerController.");
                return;
            }

            Body = GetRenderer(todo, "Cuerpo");
            EyeLeft = GetRenderer(todo, "OjoL");
            EyeRight = GetRenderer(todo, "OjoR");
            Mouth = GetRenderer(todo, "Boca");
            Extra = GetRenderer(todo, "Extra");

            BodyShadow = GetRenderer(todo, "Cuerpo Sombra");
            ExtraShadow = GetRenderer(todo, "Extra Sombra");

            PivotLegLeft = todo.Find("PivotPiernaL");
            PivotLegRight = todo.Find("PivotPiernaR");

            LegLeft = GetRenderer(PivotLegLeft, "PiernaL");
            LegRight = GetRenderer(PivotLegRight, "PiernaR");

            Debug.Log("[RombyLib.Skin] Refreshed skin references.");
        }

        private static SpriteRenderer GetRenderer(Transform parent, string childName)
        {
            if (parent == null) return null;
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>
        /// Sets tint color for the main body sprite renderer.
        /// </summary>
        public void SetBodyColor(Color color)
        {
            if (Romby.IsValid(Body))
            {
                Body.color = color;
                Debug.Log($"[RombyLib.Skin] Body color set to {color}.");
            }
            else
            {
                Debug.LogWarning("[RombyLib.Skin] Cannot set body color: Body SpriteRenderer is null or unavailable.");
            }
        }
    }

    /// <summary>
    /// Controls player attributes, movement speed, crit values, and active status effects.
    /// </summary>
    public class StatsController
    {
        private PlayerController Player => Romby.Controller;
        private GameManager GM => GameManager.Instance;

        internal StatsController() { }

        #region Money
        /// <summary>Current money available in real-time.</summary>
        public int Money
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerMoney;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.money ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (Romby.IsValid(GM))
                {
                    GM.playerMoney = val;
                    GM.UpdateMoneyUI();
                }

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.money = val;
            }
        }

        /// <summary>Safely spends money with game UI and audio reaction.</summary>
        public bool SpendMoney(int cost)
        {
            if (Romby.IsValid(GM))
            {
                if (GM.SpendMoney(cost))
                {
                    var save = SaveManager.Instance?.GetCurrentSaveData();
                    if (save?.player != null)
                        save.player.money = GM.playerMoney;
                    Debug.Log($"[RombyLib.Stats] Spent {cost} money. Remaining: {Money}.");
                    return true;
                }
                Debug.LogWarning($"[RombyLib.Stats] Failed to spend {cost} money. Current: {Money}.");
                return false;
            }

            if (Money >= cost)
            {
                Money -= cost;
                Debug.Log($"[RombyLib.Stats] Spent {cost} money. Remaining: {Money}.");
                return true;
            }
            return false;
        }

        /// <summary>Adds money to the player triggering animations and sound.</summary>
        public void AddMoney(int amount)
        {
            if (Romby.IsValid(GM))
            {
                GM.AddMoney(amount);
                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.money = GM.playerMoney;
            }
            else
            {
                Money += amount;
            }
            Debug.Log($"[RombyLib.Stats] Added {amount} money. Total: {Money}.");
        }
        #endregion

        #region Lives / Health
        /// <summary>Gets or sets player current lives/health.</summary>
        public int Lives
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerCurrentLives;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.currentLives ?? 0;
            }
            set
            {
                int clamped = Mathf.Clamp(value, 0, MaxLives);

                if (Romby.IsValid(GM))
                {
                    int current = GM.playerCurrentLives;
                    int diff = clamped - current;

                    if (diff > 0)
                        GM.PlayerGainLives(diff);
                    else if (diff < 0)
                        GM.PlayerLoseLives(-diff);
                }

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.currentLives = clamped;
            }
        }

        /// <summary>Gets or sets player maximum lives capacity.</summary>
        public int MaxLives
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerMaxLives;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.maxLives ?? 1;
            }
            set
            {
                int val = Mathf.Max(1, value);
                if (Romby.IsValid(GM))
                    GM.playerMaxLives = val;

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.maxLives = val;
            }
        }

        /// <summary>Restores player lives by a specified amount.</summary>
        public void Heal(int amount)
        {
            if (Romby.IsValid(GM))
                GM.PlayerHeal(amount);
            else
                Lives += amount;

            Debug.Log($"[RombyLib.Stats] Healed player by {amount}. Current lives: {Lives}/{MaxLives}.");
        }

        /// <summary>Applies damage to the player, reducing lives.</summary>
        public void TakeDamage(int damage)
        {
            if (Romby.IsValid(GM))
                GM.PlayerTakeDamage(damage);
            else
                Lives -= damage;

            Debug.Log($"[RombyLib.Stats] Player took {damage} damage. Current lives: {Lives}/{MaxLives}.");
        }

        /// <summary>Instantly kills the player.</summary>
        public void Kill()
        {
            if (Romby.IsValid(GM))
                GM.PlayerTakeDamage(Lives);
            else
                Lives = 0;

            Debug.Log("[RombyLib.Stats] Player killed.");
        }
        #endregion

        #region Level & XP
        /// <summary>Gets or sets player current level.</summary>
        public int Level
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerLevel;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.level ?? 1;
            }
            set
            {
                int val = Mathf.Max(1, value);
                if (Romby.IsValid(GM))
                    GM.playerLevel = val;

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.level = val;
            }
        }

        /// <summary>Gets or sets player current experience points.</summary>
        public int XP
        {
            get
            {
                if (Romby.IsValid(GM))
                    return GM.playerCurrentXP;

                return SaveManager.Instance?.GetCurrentSaveData()?.player?.currentXP ?? 0;
            }
            set
            {
                int val = Mathf.Max(0, value);
                if (Romby.IsValid(GM))
                    GM.playerCurrentXP = val;

                var save = SaveManager.Instance?.GetCurrentSaveData();
                if (save?.player != null)
                    save.player.currentXP = val;
            }
        }

        /// <summary>Adds experience points to the player.</summary>
        public void AddXP(int amount)
        {
            if (Romby.IsValid(GM))
                GM.AddXP(amount);
            else
                XP += amount;

            Debug.Log($"[RombyLib.Stats] Added {amount} XP. Total: {XP}.");
        }

        /// <summary>Forces an immediate player level up.</summary>
        public void ForceLevelUp()
        {
            if (Romby.IsValid(GM))
            {
                GM.ForceLevelUp();
                Debug.Log($"[RombyLib.Stats] Forced level up. New level: {Level}.");
            }
        }
        #endregion

        #region Speed & Status Modifiers
        /// <summary>Gets or sets the current movement speed.</summary>
        public float MoveSpeed
        {
            get => Romby.IsValid(Player) ? Player.moveSpeed : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.moveSpeed = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set MoveSpeed: Player is not loaded.");
            }
        }

        /// <summary>Gets player base movement speed without buffs.</summary>
        public float BaseMoveSpeed => Romby.IsValid(Player) ? Player.GetBaseMoveSpeed() : 0f;

        /// <summary>Gets or sets maximum speed cap.</summary>
        public float MaxTotalSpeed
        {
            get => Romby.IsValid(Player) ? Player.maxTotalSpeed : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.maxTotalSpeed = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set MaxTotalSpeed: Player is not loaded.");
            }
        }

        /// <summary>Applies a temporary speed boost multiplier.</summary>
        public void ApplySpeedBoost(float multiplier, float duration, string boostId = "custom_mod_boost")
        {
            if (Romby.IsValid(Player))
            {
                Player.ApplyTemporarySpeedBoost(multiplier, duration, boostId);
                Debug.Log($"[RombyLib.Stats] Applied speed boost '{boostId}' (x{multiplier}) for {duration}s.");
            }
        }

        /// <summary>Removes a temporary speed boost by its ID.</summary>
        public void RemoveSpeedBoost(string boostId)
        {
            if (Romby.IsValid(Player))
            {
                Player.RemoveSpeedBoost(boostId);
                Debug.Log($"[RombyLib.Stats] Removed speed boost '{boostId}'.");
            }
        }

        /// <summary>Gets or sets base critical strike chance.</summary>
        public float CriticalChance
        {
            get => Romby.IsValid(Player) ? Player.criticalChance : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.criticalChance = value;
            }
        }

        /// <summary>Gets current critical strike chance including buffs.</summary>
        public float CurrentCriticalChance => Romby.IsValid(Player) ? Player.GetCurrentCritChance() : 0f;

        /// <summary>Gets or sets critical strike damage multiplier.</summary>
        public float CriticalMultiplier
        {
            get => Romby.IsValid(Player) ? Player.criticalMultiplier : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.criticalMultiplier = value;
            }
        }

        /// <summary>Checks whether lapas are attached to the player.</summary>
        public bool HasLapas => Romby.IsValid(Player) && Player.HasLapasAttached;

        /// <summary>Attaches a lapa to the player.</summary>
        public void AddLapa()
        {
            if (Romby.IsValid(Player))
            {
                Player.AddLapa();
                Debug.Log("[RombyLib.Stats] Added lapa to player.");
            }
        }

        /// <summary>Removes an attached lapa from the player.</summary>
        public void RemoveLapa()
        {
            if (Romby.IsValid(Player))
            {
                Player.RemoveLapa();
                Debug.Log("[RombyLib.Stats] Removed lapa from player.");
            }
        }

        /// <summary>Checks whether Tesla pole damage buff is active.</summary>
        public bool HasTeslaBuff => Romby.IsValid(Player) && Player.GetTeslaPoleDamageMultiplier() > 1f;

        /// <summary>Gets Tesla pole damage multiplier.</summary>
        public float TeslaDamageMultiplier => Romby.IsValid(Player) ? Player.GetTeslaPoleDamageMultiplier() : 1f;

        /// <summary>Applies Tesla pole damage buff to the player.</summary>
        public void ApplyTeslaBuff()
        {
            if (Romby.IsValid(Player))
            {
                Player.ApplyTeslaPoleBuff();
                Debug.Log("[RombyLib.Stats] Applied Tesla pole buff.");
            }
        }

        /// <summary>Checks whether Vendaval attack speed boost is active.</summary>
        public bool HasVendavalBoost => Romby.IsValid(Player) && Player.HasVendavalAttackSpeedBoost();

        /// <summary>Gets Vendaval attack speed multiplier.</summary>
        public float VendavalMultiplier => Romby.IsValid(Player) ? Player.GetVendavalAttackSpeedMultiplier() : 1f;

        /// <summary>Checks if player is dizzy.</summary>
        public bool IsDizzy => Romby.IsValid(Player) && Player.IsDizzy;

        /// <summary>Checks if player is frozen.</summary>
        public bool IsFrozen => Romby.IsValid(Player) && Player.IsFrozen;

        /// <summary>Checks if player is burning.</summary>
        public bool IsBurning => Romby.IsValid(Player) && Player.IsBurning;

        /// <summary>Checks if player is in meditation state.</summary>
        public bool IsMeditationState => Romby.IsValid(Player) && Player.IsInMeditationState();

        /// <summary>Applies freeze effect to the player for a duration.</summary>
        public void Freeze(float duration, Vector2? hitDirection = null)
        {
            if (!Romby.IsValid(Player)) return;
            var d = new Il2CppSystem.Nullable<float>(duration);
            var h = hitDirection.HasValue
                ? new Il2CppSystem.Nullable<Vector2>(hitDirection.Value)
                : new Il2CppSystem.Nullable<Vector2>();
            Player.ApplyFreeze(d, h);
            Debug.Log($"[RombyLib.Stats] Froze player for {duration}s.");
        }

        /// <summary>Cancels active freeze effect immediately.</summary>
        public void CancelFreeze()
        {
            if (Romby.IsValid(Player))
            {
                Player.CancelFreeze();
                Debug.Log("[RombyLib.Stats] Canceled freeze.");
            }
        }

        /// <summary>Applies burning damage-over-time effect to the player.</summary>
        public void Burn(float duration, float damagePerSecond)
        {
            if (Romby.IsValid(Player))
            {
                Player.ApplyBurn(duration, damagePerSecond);
                Debug.Log($"[RombyLib.Stats] Applied burn for {duration}s ({damagePerSecond} DPS).");
            }
        }
        #endregion
    }

    /// <summary>
    /// Controls player weapon inventory, switching, helpers, and active weapon entities.
    /// </summary>
    public class WeaponsController
    {
        private PlayerController Player => Romby.Controller;

        private static readonly Func<PlayerController, int> FastGetCurrentWeapon;
        private static readonly Action<PlayerController, int> FastOnWeaponChanged;
        private static readonly Action<PlayerController, int> FastCycleWeapon;

        static WeaponsController()
        {
            try
            {
                var mGet = AccessTools.Method(typeof(PlayerController), "GetCurrentWeaponID");
                if (mGet != null) FastGetCurrentWeapon = AccessTools.MethodDelegate<Func<PlayerController, int>>(mGet);
                else Debug.LogWarning("[RombyLib.Weapons] Method 'GetCurrentWeaponID' not found in PlayerController.");

                var mChange = AccessTools.Method(typeof(PlayerController), "OnWeaponChanged", new[] { typeof(int) });
                if (mChange != null) FastOnWeaponChanged = AccessTools.MethodDelegate<Action<PlayerController, int>>(mChange);
                else Debug.LogWarning("[RombyLib.Weapons] Method 'OnWeaponChanged(int)' not found in PlayerController.");

                var mCycle = AccessTools.Method(typeof(PlayerController), "CycleWeapon", new[] { typeof(int) });
                if (mCycle != null) FastCycleWeapon = AccessTools.MethodDelegate<Action<PlayerController, int>>(mCycle);
                else Debug.LogWarning("[RombyLib.Weapons] Method 'CycleWeapon(int)' not found in PlayerController.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RombyLib.Weapons] Fast delegates initialization failed: {ex}");
            }
        }

        internal WeaponsController()
        {
            AstroPulse = new AstroPulseHelper(() => Romby.IsValid(Player) ? Player.meleeWeaponController : null);
            SatelliteShield = new SatelliteShieldHelper(() => (Romby.IsValid(Player) && Romby.IsValid(Player.rotatingDaggersObject))
                ? Player.rotatingDaggersObject.GetComponent<RotatingSatelliteController>()
                : null);
            Plushinator3000 = new PlushinatorHelper(() => Romby.IsValid(Player) ? Player.pelucheadorController : null);
            Gachickpon = new GachickponHelper(() => Romby.IsValid(Player) ? Player.huevoPolloController : null);

            Moonerang = new MoonerangHelper(() => Romby.IsValid(Player) ? Player.boomerangController : null);
            BitBang = new BitBangHelper(() => Romby.IsValid(Player) ? Player.bitBangController : null);
            TrebleClef = new TrebleClefHelper(() => Romby.IsValid(Player) ? Player.trompetaLlameanteController : null);
            BigBucks = new BigBucksHelper(() => Romby.IsValid(Player) ? Player.donBilletonController : null);
            TheScreamer = new TheScreamerHelper(() => Romby.IsValid(Player) ? Player.vocalizadorController : null);
            DivineRelocator = new DivineRelocatorHelper(() => Romby.IsValid(Player) ? Player.grabbingHandsController : null);
            Bubblewitch = new BubblewitchHelper(() => Romby.IsValid(Player) ? Player.cajaController : null);
        }

        #region Common & State
        /// <summary>Gets or sets the current weapon by its raw ID.</summary>
        public int CurrentWeaponId
        {
            get
            {
                var plr = Player;
                if (!Romby.IsValid(plr)) return 0;
                return FastGetCurrentWeapon != null ? FastGetCurrentWeapon(plr) : 0;
            }
            set => SwitchWeapon(value);
        }

        /// <summary>Gets or sets the current weapon by <see cref="WeaponType"/>.</summary>
        public WeaponType CurrentWeapon
        {
            get => (WeaponType)CurrentWeaponId;
            set => SwitchWeapon((int)value);
        }

        /// <summary>Switches active weapon by ID.</summary>
        public void SwitchWeapon(int weaponId)
        {
            var plr = Player;
            if (!Romby.IsValid(plr))
            {
                Debug.LogWarning($"[RombyLib.Weapons] Cannot switch weapon to ID {weaponId}: Player is not loaded.");
                return;
            }

            if (FastOnWeaponChanged != null)
            {
                FastOnWeaponChanged(plr, weaponId);
                Debug.Log($"[RombyLib.Weapons] Switched weapon to ID {weaponId}.");
            }
            else
            {
                Debug.LogWarning("[RombyLib.Weapons] FastOnWeaponChanged delegate is unavailable.");
            }
        }

        /// <summary>Switches active weapon by <see cref="WeaponType"/>.</summary>
        public void SwitchWeapon(WeaponType weapon) => SwitchWeapon((int)weapon);

        /// <summary>Equips weapon into player slots by type.</summary>
        public void EquipSlot(WeaponType weapon) => EquipSlot((int)weapon);

        /// <summary>Equips weapon into player slots by ID.</summary>
        public void EquipSlot(int weaponId)
        {
            if (Romby.IsValid(Player))
            {
                Player.OnWeaponEquipped(weaponId);
                Debug.Log($"[RombyLib.Weapons] Equipped weapon ID {weaponId}.");
            }
            else
            {
                Debug.LogWarning($"[RombyLib.Weapons] Cannot equip weapon {weaponId}: Player is not loaded.");
            }
        }

        /// <summary>Unequips weapon from player slots by type.</summary>
        public void UnequipSlot(WeaponType weapon) => UnequipSlot((int)weapon);

        /// <summary>Unequips weapon from player slots by ID.</summary>
        public void UnequipSlot(int weaponId)
        {
            if (Romby.IsValid(Player))
            {
                Player.OnWeaponUnequipped(weaponId);
                Debug.Log($"[RombyLib.Weapons] Unequipped weapon ID {weaponId}.");
            }
            else
            {
                Debug.LogWarning($"[RombyLib.Weapons] Cannot unequip weapon {weaponId}: Player is not loaded.");
            }
        }

        /// <summary>Triggers the active attack/firing behavior for a weapon.</summary>
        public bool ActivateWeapon(WeaponType weapon)
        {
            if (Romby.IsValid(Player))
            {
                bool success = Player.ActivateWeaponById((int)weapon);
                Debug.Log($"[RombyLib.Weapons] Activated weapon {weapon}: {(success ? "Success" : "Failed")}.");
                return success;
            }
            Debug.LogWarning($"[RombyLib.Weapons] Cannot activate weapon {weapon}: Player is not loaded.");
            return false;
        }

        /// <summary>Cancels active action for a specific weapon.</summary>
        public void CancelWeapon(WeaponType weapon)
        {
            if (Romby.IsValid(Player))
            {
                Player.CancelActiveWeapon((int)weapon);
                Debug.Log($"[RombyLib.Weapons] Canceled weapon {weapon}.");
            }
            else
            {
                Debug.LogWarning($"[RombyLib.Weapons] Cannot cancel weapon {weapon}: Player is not loaded.");
            }
        }

        /// <summary>Cancels active action for all currently firing weapons.</summary>
        public void CancelAllActiveWeapons()
        {
            if (Romby.IsValid(Player))
            {
                Player.CancelAllActiveWeapons();
                Debug.Log("[RombyLib.Weapons] Canceled all active weapons.");
            }
            else
            {
                Debug.LogWarning("[RombyLib.Weapons] Cannot cancel all active weapons: Player is not loaded.");
            }
        }

        /// <summary>Gets the list of weapon IDs currently unlocked by the player.</summary>
        public Il2CppCollections.List<int> UnlockedWeaponsId => PlayerController.GetAcquiredWeaponIDs();

        /// <summary>Checks whether a weapon is unlocked.</summary>
        public bool IsUnlocked(WeaponType weapon)
        {
            var ids = UnlockedWeaponsId;
            return ids != null && ids.Contains((int)weapon);
        }

        /// <summary>Gets total ammunition available across all equipped weapons.</summary>
        public int TotalAmmo => Romby.IsValid(Player) ? Player.GetTotalWeaponAmmo() : 0;

        /// <summary>Cycles to the next weapon in inventory.</summary>
        public void CycleNext()
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastCycleWeapon != null)
            {
                FastCycleWeapon(plr, 1);
                Debug.Log("[RombyLib.Weapons] Cycled to next weapon.");
            }
        }

        /// <summary>Cycles to the previous weapon in inventory.</summary>
        public void CyclePrevious()
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastCycleWeapon != null)
            {
                FastCycleWeapon(plr, -1);
                Debug.Log("[RombyLib.Weapons] Cycled to previous weapon.");
            }
        }
        #endregion

        #region Helpers On Player
        /// <summary>Helper for Weapon 1 (Astro Pulse melee attack).</summary>
        public AstroPulseHelper AstroPulse { get; }

        /// <summary>Helper for Weapon 2 (Satellite Shield orbital blades).</summary>
        public SatelliteShieldHelper SatelliteShield { get; }

        /// <summary>Helper for Weapon 8 (Plushinator 3000 plush launcher).</summary>
        public PlushinatorHelper Plushinator3000 { get; }

        /// <summary>Helper for Weapon 12 (Gachickpon chicken shooter).</summary>
        public GachickponHelper Gachickpon { get; }

        /// <summary>Helper for Weapon 4 (Moonerang projectile).</summary>
        public MoonerangHelper Moonerang { get; }

        /// <summary>Helper for Weapon 9 (Bit Bang pixelizing ray).</summary>
        public BitBangHelper BitBang { get; }

        /// <summary>Helper for Weapon 10 (Treble Clef flamethrower).</summary>
        public TrebleClefHelper TrebleClef { get; }

        /// <summary>Helper for Weapon 11 (Big Bucks money attack).</summary>
        public BigBucksHelper BigBucks { get; }

        /// <summary>Helper for Weapon 13 (The Screamer acoustic weapon).</summary>
        public TheScreamerHelper TheScreamer { get; }

        /// <summary>Helper for Weapon 15 (Divine Relocator grab hands).</summary>
        public DivineRelocatorHelper DivineRelocator { get; }

        /// <summary>Helper for Weapon 16 (Bubblewitch monster catcher).</summary>
        public BubblewitchHelper Bubblewitch { get; }
        #endregion

        #region Prefabs for Spawned Objects
        public GameObject SolarMinePrefab => Romby.IsValid(Player) ? Player.minePrefab : null;
        public GameObject ExistentialVoidPrefab => Romby.IsValid(Player) ? Player.blackHolePrefab : null;
        public GameObject UncleFrankPrefab => Romby.IsValid(Player) ? Player.tioPacoPrefab : null;
        public GameObject TeslaPawnPrefab => Romby.IsValid(Player) ? Player.electricPolePrefab : null;
        public GameObject TheSoulTransmuterPrefab => Romby.IsValid(Player) ? Player.cajaAtrapaAlmasPrefab : null;
        #endregion

        #region Spawned World Objects
        /// <summary>List of all currently placed and active solar mines.</summary>
        public Il2CppCollections.List<Mine> ActiveSolarMines => Mine.ActiveMines;

        /// <summary>List of all active Uncle Frank entities.</summary>
        public Il2CppCollections.List<TioPacoController> ActiveUncleFranks => TioPacoController.allActiveTioPacos;

        /// <summary>Queue of active soul transmuter boxes.</summary>
        public Il2CppCollections.Queue<Arma14CajaAtrapaAlmas> ActiveSoulTransmuters => Arma14CajaAtrapaAlmas.ActiveBoxes;

        /// <summary>Number of placed Tesla pawns currently in the world.</summary>
        public int ActiveTeslaPawnsCount => ElectricPoleController.ActivePolesCount;

        /// <summary>Checks whether a target object is currently inside any active black hole.</summary>
        public bool IsInsideBlackHole(GameObject obj) => BlackHoleController.IsObjectInAnyBlackHole(obj);

        /// <summary>Immediately destroys all active Uncle Frank minions.</summary>
        public void DestroyAllUncleFranks()
        {
            TioPacoController.DestroyAllActiveTioPacos();
            Debug.Log("[RombyLib.Weapons] Destroyed all active Uncle Franks.");
        }

        /// <summary>Immediately collapses and destroys all active black holes.</summary>
        public void DestroyAllExistentialVoids()
        {
            BlackHoleController.DestroyAllActiveBlackHoles();
            Debug.Log("[RombyLib.Weapons] Destroyed all active Existential Voids.");
        }

        /// <summary>Immediately destroys all active Tesla pawns.</summary>
        public void DestroyAllTeslaPawns()
        {
            ElectricPoleController.DestroyAllActiveElectricPoles();
            Debug.Log("[RombyLib.Weapons] Destroyed all active Tesla Pawns.");
        }
        #endregion
    }

    #region Specialized Weapon Helpers
    /// <summary>Helper for Weapon 1 (Astro Pulse / Melee).</summary>
    public class AstroPulseHelper : WeaponHelperBase<MeleeWeaponController>
    {
        internal AstroPulseHelper(Func<MeleeWeaponController> getter) : base(getter) { }

        /// <summary>Executes melee swing if available.</summary>
        public bool Attack() => IsAvailable && Raw.TryMeleeAttack();

        /// <summary>Checks if melee attack can currently be performed.</summary>
        public bool CanAttack => IsAvailable && Raw.CanAttack();

        /// <summary>Checks whether melee swing animation is active.</summary>
        public bool IsAttacking => IsAvailable && Raw.IsAttacking();

        /// <summary>Cooldown remaining until next melee attack.</summary>
        public float CooldownRemaining => IsAvailable ? Raw.GetRemainingCooldown() : 0f;

        /// <summary>Effective attack reach distance.</summary>
        public float AttackDistance
        {
            get => IsAvailable ? Raw.attackDistance : 0f;
            set { if (IsAvailable) Raw.SetAttackDistance(value); }
        }
    }

    /// <summary>Helper for Weapon 8 (Plushinator 3000).</summary>
    public class PlushinatorHelper : WeaponHelperBase<Arma8Controller>
    {
        internal PlushinatorHelper(Func<Arma8Controller> getter) : base(getter) { }

        /// <summary>Attempts to fire plushie projectile.</summary>
        public bool Fire() => IsAvailable && Raw.TryFire();

        /// <summary>Current upgrade level of the weapon.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;

        /// <summary>Rate of fire in shots per second.</summary>
        public float FireRate => IsAvailable ? Raw.fireRate : 0f;

        /// <summary>Base damage dealt per hit.</summary>
        public int BaseDamage => IsAvailable ? Raw.baseDamage : 0;
    }

    /// <summary>Helper for Weapon 12 (Gachickpon).</summary>
    public class GachickponHelper : WeaponHelperBase<Arma12Controller>
    {
        internal GachickponHelper(Func<Arma12Controller> getter) : base(getter) { }

        /// <summary>Attempts to launch a chicken projectile.</summary>
        public bool Fire() => IsAvailable && Raw.TryFire();

        /// <summary>Current upgrade level.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;

        /// <summary>Calculated min and max damage range.</summary>
        public (int min, int max) DamageRange
        {
            get
            {
                if (!IsAvailable) return (0, 0);
                var range = Raw.GetChickenDamageRange();
                return range != null ? (range.Item1, range.Item2) : (0, 0);
            }
        }
    }

    /// <summary>Helper for Weapon 2 (Satellite Shield).</summary>
    public class SatelliteShieldHelper : WeaponHelperBase<RotatingSatelliteController>
    {
        internal SatelliteShieldHelper(Func<RotatingSatelliteController> getter) : base(getter) { }

        /// <summary>Current number of orbiting satellites.</summary>
        public int ActiveCount => IsAvailable ? Raw.GetActiveSatelliteCount() : 0;

        /// <summary>Chance to reflect incoming hostile projectiles.</summary>
        public float ReflectionChance => IsAvailable ? Raw.reflectionChance : 0f;

        /// <summary>Forces recalculation and repositioning of orbiting satellites.</summary>
        public void ForceSync()
        {
            if (IsAvailable)
            {
                Raw.SyncAllActiveSatellites();
                Debug.Log("[RombyLib.Weapons] Synced Satellite Shield satellites.");
            }
        }
    }

    /// <summary>Helper for Weapon 10 (Treble Clef / Flaming Trumpet).</summary>
    public class TrebleClefHelper : WeaponHelperBase<Arma10Controller>
    {
        internal TrebleClefHelper(Func<Arma10Controller> getter) : base(getter) { }

        /// <summary>Begins continuous flamethrower emission.</summary>
        public bool StartFlame() => IsAvailable && Raw.TryStartFlame();

        /// <summary>Stops flamethrower stream.</summary>
        public void StopFlame()
        {
            if (IsAvailable)
            {
                Raw.StopFlame();
                Debug.Log("[RombyLib.Weapons] Stopped Treble Clef flame.");
            }
        }

        /// <summary>Checks whether flamethrower is firing.</summary>
        public bool IsFiring => IsAvailable && Raw.IsFiring;

        /// <summary>Knockback/propulsion force applied during fire.</summary>
        public float ThrustForce => IsAvailable ? Raw.CurrentThrustForce : 0f;

        /// <summary>Current aiming angle in degrees.</summary>
        public float AimAngle => (IsAvailable && Raw.TryGetAimAngle(out float a)) ? a : 0f;

        /// <summary>Musical note index mapped to current angle.</summary>
        public int CurrentNoteIndex => IsAvailable ? Raw.GetCurrentAngleNoteIndex() : 0;

        /// <summary>Audio pitch corresponding to note index.</summary>
        public float CurrentPitch => Arma10Controller.GetTrumpetPitch(CurrentNoteIndex);

        /// <summary>Removes burn instances created by this weapon.</summary>
        public void ClearAllBurns()
        {
            if (IsAvailable)
            {
                Raw.ClearAllBurns();
                Debug.Log("[RombyLib.Weapons] Cleared Treble Clef burns.");
            }
        }
    }

    /// <summary>Helper for Weapon 11 (Big Bucks).</summary>
    public class BigBucksHelper : WeaponHelperBase<Arma11Controller>
    {
        internal BigBucksHelper(Func<Arma11Controller> getter) : base(getter) { }

        /// <summary>Begins charging the weapon attack.</summary>
        public void StartCharging()
        {
            if (IsAvailable)
            {
                Raw.StartCharging();
                Debug.Log("[RombyLib.Weapons] Big Bucks started charging.");
            }
        }

        /// <summary>Releases charge and fires attack.</summary>
        public void ReleaseCharge()
        {
            if (IsAvailable)
            {
                Raw.ReleaseCharge();
                Debug.Log("[RombyLib.Weapons] Big Bucks released charge.");
            }
        }

        /// <summary>Cancels current charge without firing.</summary>
        public void CancelCharge()
        {
            if (IsAvailable)
            {
                Raw.CancelCharge();
                Debug.Log("[RombyLib.Weapons] Big Bucks canceled charge.");
            }
        }

        /// <summary>Attempts to fire immediately.</summary>
        public bool Fire() => IsAvailable && Raw.TryFire();

        /// <summary>Checks whether weapon is currently charging.</summary>
        public bool IsCharging => IsAvailable && Raw.IsCharging;

        /// <summary>Amount of in-game currency accumulated into current charge.</summary>
        public int ChargedMoney => IsAvailable ? Raw.CurrentChargedMoney : 0;

        /// <summary>Checks if player has enough money to fire.</summary>
        public bool HasMoney => IsAvailable && Raw.HasMoneyToFire();

        /// <summary>Current upgrade level.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
    }

    /// <summary>Helper for Weapon 16 (Bubblewitch).</summary>
    public class BubblewitchHelper : WeaponHelperBase<Arma16Controller>
    {
        internal BubblewitchHelper(Func<Arma16Controller> getter) : base(getter) { }

        /// <summary>Attempts to fire capture bubble.</summary>
        public bool Fire() => IsAvailable && Raw.TryFire();

        /// <summary>Current upgrade level.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;

        /// <summary>Calculates capture probability for a given enemy.</summary>
        public float GetCaptureChance(EnemyBase enemy) => IsAvailable ? Raw.CalculateCaptureChance(enemy) : 0f;

        /// <summary>Attempts to capture an enemy into storage.</summary>
        public bool TryCapture(EnemyBase enemy)
        {
            if (IsAvailable)
            {
                bool success = Raw.TryCapture(enemy);
                Debug.Log($"[RombyLib.Weapons] Bubblewitch capture attempt: {(success ? "Success" : "Failed")}.");
                return success;
            }
            return false;
        }

        /// <summary>Summons a previously captured monster at a world position.</summary>
        public void SummonMonster(CapturedMonsterData data, Vector3 position)
        {
            if (IsAvailable)
            {
                Raw.SummonMonster(data, position);
                Debug.Log($"[RombyLib.Weapons] Summoned monster at {position}.");
            }
        }

        /// <summary>List of captured monsters currently in team.</summary>
        public Il2CppCollections.List<CapturedMonsterData> Team => IsAvailable ? Raw.GetTeamMonsters() : null;

        /// <summary>Current count of monsters in team.</summary>
        public int TeamCount => IsAvailable ? Raw.GetCurrentMonsterCount() : 0;

        /// <summary>Max monsters allowed in team.</summary>
        public int MaxSlots => IsAvailable ? Raw.GetMaxCaptureSlots() : 0;

        /// <summary>Checks if team has available slot for another capture.</summary>
        public bool HasFreeSlot => IsAvailable && Raw.HasFreeTeamSlot();

        /// <summary>Selects active monster slot index.</summary>
        public void Select(int index)
        {
            if (IsAvailable)
            {
                Raw.SelectMonster(index);
                Debug.Log($"[RombyLib.Weapons] Bubblewitch selected slot {index}.");
            }
        }

        /// <summary>Generates a procedural name for captured monster.</summary>
        public string GenerateRandomName() => IsAvailable ? Raw.GenerateRandomMonsterName() : string.Empty;
    }

    /// <summary>Helper for Weapon 4 (Moonerang).</summary>
    public class MoonerangHelper : WeaponHelperBase<BoomerangController>
    {
        internal MoonerangHelper(Func<BoomerangController> getter) : base(getter) { }

        /// <summary>Throws boomerang toward target position.</summary>
        public bool Launch(Vector3 targetWorldPos)
        {
            if (IsAvailable)
            {
                bool success = Raw.LaunchBoomerang(targetWorldPos);
                Debug.Log($"[RombyLib.Weapons] Launched Moonerang toward {targetWorldPos}: {(success ? "Success" : "Failed")}.");
                return success;
            }
            return false;
        }

        /// <summary>Recalls boomerang back to player.</summary>
        public void Recall()
        {
            if (IsAvailable)
            {
                Raw.Recall();
                Debug.Log("[RombyLib.Weapons] Moonerang recalled.");
            }
        }

        /// <summary>Immediately forces boomerang back into hand.</summary>
        public void Reset()
        {
            if (IsAvailable)
            {
                Raw.ForceReset();
                Debug.Log("[RombyLib.Weapons] Moonerang forced reset.");
            }
        }

        /// <summary>Current flight state of boomerang.</summary>
        public BoomerangController.BoomerangState State => IsAvailable ? Raw.CurrentState : BoomerangController.BoomerangState.Inactive;

        /// <summary>Checks whether boomerang is currently flying in world.</summary>
        public bool IsInFlight => State != BoomerangController.BoomerangState.Inactive;

        /// <summary>Checks if boomerang is ready to throw.</summary>
        public bool CanAttack => IsAvailable && Raw.CanAttack();
    }

    /// <summary>Helper for Weapon 9 (Bit Bang).</summary>
    public class BitBangHelper : WeaponHelperBase<Arma9Controller>
    {
        internal BitBangHelper(Func<Arma9Controller> getter) : base(getter) { }

        /// <summary>Begins pixelizing beam attack on nearest/aimed target.</summary>
        public bool StartPixelizing() => IsAvailable && Raw.TryStartPixelizing();

        /// <summary>Stops pixelizing beam.</summary>
        public void StopPixelizing()
        {
            if (IsAvailable)
            {
                Raw.StopPixelizing();
                Debug.Log("[RombyLib.Weapons] Stopped Bit Bang pixelizing.");
            }
        }

        /// <summary>Checks whether pixelizing beam is running.</summary>
        public bool IsPixelizing => IsAvailable && Raw.IsPixelizing;

        /// <summary>Current enemy entity targeted by beam.</summary>
        public EnemyBase Target => IsAvailable ? Raw.CurrentPixelizingTarget : null;

        /// <summary>Pixelization progress percentage (0.0 to 1.0).</summary>
        public float Progress => IsAvailable ? Raw.PixelizationProgress : 0f;
    }

    /// <summary>Helper for Weapon 15 (Divine Relocator).</summary>
    public class DivineRelocatorHelper : WeaponHelperBase<Arma15Controller>
    {
        internal DivineRelocatorHelper(Func<Arma15Controller> getter) : base(getter) { }

        /// <summary>Spawns grab hand aimed toward target world position.</summary>
        public void LaunchHandAt(Vector3 targetWorldPos)
        {
            if (IsAvailable)
            {
                Raw.TryLaunchHandAtPosition(targetWorldPos);
                Debug.Log($"[RombyLib.Weapons] Launched grab hand toward {targetWorldPos}.");
            }
        }

        /// <summary>Number of grab hands currently active in world.</summary>
        public int ActiveHandsCount => IsAvailable ? Raw.GetActiveHandCount() : 0;

        /// <summary>Checks if weapon attack is on cooldown.</summary>
        public bool IsOnCooldown => IsAvailable && Raw.IsOnCooldown();

        /// <summary>Normalized cooldown completion progress (0.0 to 1.0).</summary>
        public float CooldownProgress => IsAvailable ? Raw.GetCooldownProgress() : 0f;

        /// <summary>Current upgrade level.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
    }

    /// <summary>Helper for Weapon 13 (The Screamer).</summary>
    public class TheScreamerHelper : WeaponHelperBase<Arma13Controller>
    {
        internal TheScreamerHelper(Func<Arma13Controller> getter) : base(getter) { }

        /// <summary>Starts microphone audio recording buffer.</summary>
        public void StartRecording()
        {
            if (IsAvailable)
            {
                Raw.StartRecording();
                Debug.Log("[RombyLib.Weapons] The Screamer started recording.");
            }
        }

        /// <summary>Stops recording and fires acoustic shockwave based on sound input.</summary>
        public void StopRecording()
        {
            if (IsAvailable)
            {
                Raw.StopRecordingAndFire();
                Debug.Log("[RombyLib.Weapons] The Screamer stopped recording and fired shockwave.");
            }
        }

        /// <summary>Current upgrade level.</summary>
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;

        /// <summary>Pitch modulation of the screamer projectile.</summary>
        public float Pitch
        {
            get => IsAvailable ? Raw.projectilePitch : 0f;
            set { if (IsAvailable) Raw.projectilePitch = value; }
        }
    }
    #endregion

    /// <summary>
    /// Base helper wrapper around an Il2Cpp Unity component.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public abstract class WeaponHelperBase<T> where T : UnityEngine.Component
    {
        private readonly Func<T> _getter;

        /// <summary>Underlying raw component instance, or null if invalid.</summary>
        public T Raw => _getter != null ? _getter() : null;

        /// <summary>Checks if component exists, is alive, and safe to call.</summary>
        public bool IsAvailable => Romby.IsValid(Raw);

        protected WeaponHelperBase(Func<T> getter)
        {
            _getter = getter;
        }
    }
}