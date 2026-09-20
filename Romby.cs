using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

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

        internal StatsController() { }

        /// <summary>Current movement speed modifier of the player.</summary>
        public float MoveSpeed
        {
            get => Romby.IsValid(Player) ? Player.moveSpeed : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.moveSpeed = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set MoveSpeed: Player is not loaded.");
            }
        }

        /// <summary>Base movement speed of the player before any buffs or modifiers.</summary>
        public float BaseMoveSpeed => Romby.IsValid(Player) ? Player.GetBaseMoveSpeed() : 0f;

        /// <summary>Maximum allowed movement speed cap.</summary>
        public float MaxTotalSpeed
        {
            get => Romby.IsValid(Player) ? Player.maxTotalSpeed : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.maxTotalSpeed = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set MaxTotalSpeed: Player is not loaded.");
            }
        }

        /// <summary>
        /// Applies a temporary speed boost multiplier for a specified duration.
        /// </summary>
        /// <param name="multiplier">Speed multiplier (e.g. 1.5 for +50%).</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="boostId">Unique identifier used to track or cancel this boost.</param>
        public void ApplySpeedBoost(float multiplier, float duration, string boostId = "custom_mod_boost")
        {
            if (Romby.IsValid(Player))
            {
                Player.ApplyTemporarySpeedBoost(multiplier, duration, boostId);
            }
            else
            {
                Debug.LogWarning("[RombyLib.Stats] Cannot apply speed boost: Player is not loaded.");
            }
        }

        /// <summary>
        /// Removes an active speed boost identified by its ID.
        /// </summary>
        public void RemoveSpeedBoost(string boostId)
        {
            if (Romby.IsValid(Player))
            {
                Player.RemoveSpeedBoost(boostId);
            }
            else
            {
                Debug.LogWarning("[RombyLib.Stats] Cannot remove speed boost: Player is not loaded.");
            }
        }

        /// <summary>Base critical strike chance.</summary>
        public float CriticalChance
        {
            get => Romby.IsValid(Player) ? Player.criticalChance : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.criticalChance = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set CriticalChance: Player is not loaded.");
            }
        }

        /// <summary>Total calculated critical hit chance including bonuses.</summary>
        public float CurrentCriticalChance => Romby.IsValid(Player) ? Player.GetCurrentCritChance() : 0f;

        /// <summary>Critical hit damage multiplier.</summary>
        public float CriticalMultiplier
        {
            get => Romby.IsValid(Player) ? Player.criticalMultiplier : 0f;
            set
            {
                if (Romby.IsValid(Player)) Player.criticalMultiplier = value;
                else Debug.LogWarning("[RombyLib.Stats] Failed to set CriticalMultiplier: Player is not loaded.");
            }
        }

        /// <summary>Indicates whether the player is currently dizzy.</summary>
        public bool IsDizzy => Romby.IsValid(Player) && Player.IsDizzy;

        /// <summary>Indicates whether the player is currently frozen.</summary>
        public bool IsFrozen => Romby.IsValid(Player) && Player.IsFrozen;

        /// <summary>Indicates whether the player is currently taking burn damage.</summary>
        public bool IsBurning => Romby.IsValid(Player) && Player.IsBurning;

        /// <summary>Indicates whether the player is in meditation state.</summary>
        public bool IsMeditationState => Romby.IsValid(Player) && Player.IsInMeditationState();

        /// <summary>
        /// Freezes the player for a given duration, optionally applying directional knockback.
        /// </summary>
        public void Freeze(float duration, Vector2? hitDirection = null)
        {
            if (!Romby.IsValid(Player))
            {
                Debug.LogWarning("[RombyLib.Stats] Cannot freeze player: Player is not loaded.");
                return;
            }

            var d = new Il2CppSystem.Nullable<float>(duration);
            var h = hitDirection.HasValue
                ? new Il2CppSystem.Nullable<Vector2>(hitDirection.Value)
                : new Il2CppSystem.Nullable<Vector2>();

            Player.ApplyFreeze(d, h);
        }

        /// <summary>
        /// Immediately cancels the freeze status on the player.
        /// </summary>
        public void CancelFreeze()
        {
            if (Romby.IsValid(Player)) Player.CancelFreeze();
            else Debug.LogWarning("[RombyLib.Stats] Cannot cancel freeze: Player is not loaded.");
        }

        /// <summary>
        /// Applies burn status effect for a set duration with damage over time.
        /// </summary>
        public void Burn(float duration, float damagePerSecond)
        {
            if (Romby.IsValid(Player))
            {
                Player.ApplyBurn(duration, damagePerSecond);
            }
            else
            {
                Debug.LogWarning("[RombyLib.Stats] Cannot burn player: Player is not loaded.");
            }
        }
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
                FastOnWeaponChanged(plr, weaponId);
            else
                Debug.LogWarning("[RombyLib.Weapons] FastOnWeaponChanged delegate is unavailable.");
        }

        /// <summary>Switches active weapon by <see cref="WeaponType"/>.</summary>
        public void SwitchWeapon(WeaponType weapon) => SwitchWeapon((int)weapon);

        /// <summary>Equips weapon into player slots.</summary>
        public void EquipSlot(WeaponType weapon) => EquipSlot((int)weapon);

        /// <summary>Equips weapon into player slots by ID.</summary>
        public void EquipSlot(int weaponId)
        {
            if (Romby.IsValid(Player)) Player.OnWeaponEquipped(weaponId);
            else Debug.LogWarning($"[RombyLib.Weapons] Cannot equip weapon {weaponId}: Player is not loaded.");
        }

        /// <summary>Unequips weapon from player slots.</summary>
        public void UnequipSlot(WeaponType weapon) => UnequipSlot((int)weapon);

        /// <summary>Unequips weapon from player slots by ID.</summary>
        public void UnequipSlot(int weaponId)
        {
            if (Romby.IsValid(Player)) Player.OnWeaponUnequipped(weaponId);
            else Debug.LogWarning($"[RombyLib.Weapons] Cannot unequip weapon {weaponId}: Player is not loaded.");
        }

        /// <summary>Triggers the active attack/firing behavior for a weapon.</summary>
        public bool ActivateWeapon(WeaponType weapon)
        {
            if (Romby.IsValid(Player)) return Player.ActivateWeaponById((int)weapon);
            Debug.LogWarning($"[RombyLib.Weapons] Cannot activate weapon {weapon}: Player is not loaded.");
            return false;
        }

        /// <summary>Cancels active action for a specific weapon.</summary>
        public void CancelWeapon(WeaponType weapon)
        {
            if (Romby.IsValid(Player)) Player.CancelActiveWeapon((int)weapon);
            else Debug.LogWarning($"[RombyLib.Weapons] Cannot cancel weapon {weapon}: Player is not loaded.");
        }

        /// <summary>Cancels active action for all currently firing weapons.</summary>
        public void CancelAllActiveWeapons()
        {
            if (Romby.IsValid(Player)) Player.CancelAllActiveWeapons();
            else Debug.LogWarning("[RombyLib.Weapons] Cannot cancel all active weapons: Player is not loaded.");
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
                FastCycleWeapon(plr, 1);
        }

        /// <summary>Cycles to the previous weapon in inventory.</summary>
        public void CyclePrevious()
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastCycleWeapon != null)
                FastCycleWeapon(plr, -1);
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
        public void DestroyAllUncleFranks() => TioPacoController.DestroyAllActiveTioPacos();

        /// <summary>Immediately collapses and destroys all active black holes.</summary>
        public void DestroyAllExistentialVoids() => BlackHoleController.DestroyAllActiveBlackHoles();

        /// <summary>Immediately destroys all active Tesla pawns.</summary>
        public void DestroyAllTeslaPawns() => ElectricPoleController.DestroyAllActiveElectricPoles();
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
        public void ForceSync() { if (IsAvailable) Raw.SyncAllActiveSatellites(); }
    }

    /// <summary>Helper for Weapon 10 (Treble Clef / Flaming Trumpet).</summary>
    public class TrebleClefHelper : WeaponHelperBase<Arma10Controller>
    {
        internal TrebleClefHelper(Func<Arma10Controller> getter) : base(getter) { }

        /// <summary>Begins continuous flamethrower emission.</summary>
        public bool StartFlame() => IsAvailable && Raw.TryStartFlame();

        /// <summary>Stops flamethrower stream.</summary>
        public void StopFlame() { if (IsAvailable) Raw.StopFlame(); }

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
        public void ClearAllBurns() { if (IsAvailable) Raw.ClearAllBurns(); }
    }

    /// <summary>Helper for Weapon 11 (Big Bucks).</summary>
    public class BigBucksHelper : WeaponHelperBase<Arma11Controller>
    {
        internal BigBucksHelper(Func<Arma11Controller> getter) : base(getter) { }

        /// <summary>Begins charging the weapon attack.</summary>
        public void StartCharging() { if (IsAvailable) Raw.StartCharging(); }

        /// <summary>Releases charge and fires attack.</summary>
        public void ReleaseCharge() { if (IsAvailable) Raw.ReleaseCharge(); }

        /// <summary>Cancels current charge without firing.</summary>
        public void CancelCharge() { if (IsAvailable) Raw.CancelCharge(); }

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
        public bool TryCapture(EnemyBase enemy) => IsAvailable && Raw.TryCapture(enemy);

        /// <summary>Summons a previously captured monster at a world position.</summary>
        public void SummonMonster(CapturedMonsterData data, Vector3 position) { if (IsAvailable) Raw.SummonMonster(data, position); }

        /// <summary>List of captured monsters currently in team.</summary>
        public Il2CppCollections.List<CapturedMonsterData> Team => IsAvailable ? Raw.GetTeamMonsters() : null;

        /// <summary>Current count of monsters in team.</summary>
        public int TeamCount => IsAvailable ? Raw.GetCurrentMonsterCount() : 0;

        /// <summary>Max monsters allowed in team.</summary>
        public int MaxSlots => IsAvailable ? Raw.GetMaxCaptureSlots() : 0;

        /// <summary>Checks if team has available slot for another capture.</summary>
        public bool HasFreeSlot => IsAvailable && Raw.HasFreeTeamSlot();

        /// <summary>Selects active monster slot index.</summary>
        public void Select(int index) { if (IsAvailable) Raw.SelectMonster(index); }

        /// <summary>Generates a procedural name for captured monster.</summary>
        public string GenerateRandomName() => IsAvailable ? Raw.GenerateRandomMonsterName() : string.Empty;
    }

    /// <summary>Helper for Weapon 4 (Moonerang).</summary>
    public class MoonerangHelper : WeaponHelperBase<BoomerangController>
    {
        internal MoonerangHelper(Func<BoomerangController> getter) : base(getter) { }

        /// <summary>Throws boomerang toward target position.</summary>
        public bool Launch(Vector3 targetWorldPos) => IsAvailable && Raw.LaunchBoomerang(targetWorldPos);

        /// <summary>Recalls boomerang back to player.</summary>
        public void Recall() { if (IsAvailable) Raw.Recall(); }

        /// <summary>Immediately forces boomerang back into hand.</summary>
        public void Reset() { if (IsAvailable) Raw.ForceReset(); }

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
        public void StopPixelizing() { if (IsAvailable) Raw.StopPixelizing(); }

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
        public void LaunchHandAt(Vector3 targetWorldPos) { if (IsAvailable) Raw.TryLaunchHandAtPosition(targetWorldPos); }

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
        public void StartRecording() { if (IsAvailable) Raw.StartRecording(); }

        /// <summary>Stops recording and fires acoustic shockwave based on sound input.</summary>
        public void StopRecording() { if (IsAvailable) Raw.StopRecordingAndFire(); }

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