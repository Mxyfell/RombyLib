using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace RombyLib
{
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

    public static class Romby
    {
        private static bool _isPatched;
        private static PlayerController _controller;
        private static int _mainThreadId;

        public static SkinController Skin { get; } = new SkinController();
        public static StatsController Stats { get; } = new StatsController();
        public static WeaponsController Weapons { get; } = new WeaponsController();

        static Romby()
        {
            Init();
        }

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
                Debug.LogError("[RombyLib] Patching error: " + ex);
            }
        }

        public static PlayerController Controller
        {
            get
            {
                EnsurePlayer();
                return _controller;
            }
        }
        public static bool IsLoaded => IsValid(Controller);

        public static bool IsValid(Object obj)
        {
            return obj != null && obj.Pointer != IntPtr.Zero && !obj.WasCollected;
        }

        private static void EnsurePlayer()
        {
            if (IsValid(_controller)) return;

            if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != _mainThreadId) return;

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
                if (_controller != null && __instance != null && _controller.Pointer == __instance.Pointer)
                {
                    _controller = null;
                    Skin.Clear();
                }
            }
        }
        #endregion
    }

    public class SkinController
    {
        private PlayerController Player => Romby.Controller;

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

        public GameObject NormalVisual => Romby.IsValid(Player) ? Player.jugadorNormal : null;
        public GameObject DeathVisual => Romby.IsValid(Player) ? Player.jugadorMuerte : null;

        internal SkinController() { }

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

        public void Refresh()
        {
            var plr = Player;
            if (!Romby.IsValid(plr)) return;

            Transform todo = null;
            if (plr.jugadorNormal != null)
                todo = plr.jugadorNormal.transform.Find("Todo");

            if (todo == null && plr.transform != null)
                todo = plr.transform.Find("JugadorNormal/Todo");

            RootTodo = todo;
            if (todo == null) return;

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

        public void SetBodyColor(Color color)
        {
            if (Romby.IsValid(Body)) Body.color = color;
        }
    }

    public class StatsController
    {
        private PlayerController Player => Romby.Controller;

        internal StatsController() { }

        public float MoveSpeed
        {
            get => Romby.IsValid(Player) ? Player.moveSpeed : 0f;
            set { if (Romby.IsValid(Player)) Player.moveSpeed = value; }
        }

        public float BaseMoveSpeed => Romby.IsValid(Player) ? Player.GetBaseMoveSpeed() : 0f;

        public float MaxTotalSpeed
        {
            get => Romby.IsValid(Player) ? Player.maxTotalSpeed : 0f;
            set { if (Romby.IsValid(Player)) Player.maxTotalSpeed = value; }
        }

        public void ApplySpeedBoost(float multiplier, float duration, string boostId = "custom_mod_boost")
        {
            if (Romby.IsValid(Player)) Player.ApplyTemporarySpeedBoost(multiplier, duration, boostId);
        }

        public void RemoveSpeedBoost(string boostId)
        {
            if (Romby.IsValid(Player)) Player.RemoveSpeedBoost(boostId);
        }

        public float CriticalChance
        {
            get => Romby.IsValid(Player) ? Player.criticalChance : 0f;
            set { if (Romby.IsValid(Player)) Player.criticalChance = value; }
        }

        public float CurrentCriticalChance => Romby.IsValid(Player) ? Player.GetCurrentCritChance() : 0f;

        public float CriticalMultiplier
        {
            get => Romby.IsValid(Player) ? Player.criticalMultiplier : 0f;
            set { if (Romby.IsValid(Player)) Player.criticalMultiplier = value; }
        }

        public bool IsDizzy => Romby.IsValid(Player) && Player.IsDizzy;
        public bool IsFrozen => Romby.IsValid(Player) && Player.IsFrozen;
        public bool IsBurning => Romby.IsValid(Player) && Player.IsBurning;
        public bool IsMeditationState => Romby.IsValid(Player) && Player.IsInMeditationState();

        public void Freeze(float duration, Vector2? hitDirection = null)
        {
            if (!Romby.IsValid(Player)) return;

            var d = new Il2CppSystem.Nullable<float>(duration);
            var h = hitDirection.HasValue
                ? new Il2CppSystem.Nullable<Vector2>(hitDirection.Value)
                : default;

            Player.ApplyFreeze(d, h);
        }

        public void CancelFreeze() => Player?.CancelFreeze();

        public void Burn(float duration, float damagePerSecond)
        {
            if (Romby.IsValid(Player)) Player.ApplyBurn(duration, damagePerSecond);
        }
    }

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

                var mChange = AccessTools.Method(typeof(PlayerController), "OnWeaponChanged", new[] { typeof(int) });
                if (mChange != null) FastOnWeaponChanged = AccessTools.MethodDelegate<Action<PlayerController, int>>(mChange);

                var mCycle = AccessTools.Method(typeof(PlayerController), "CycleWeapon", new[] { typeof(int) });
                if (mCycle != null) FastCycleWeapon = AccessTools.MethodDelegate<Action<PlayerController, int>>(mCycle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RombyLib] Fast delegates init fallback: " + ex.Message);
            }
        }

        internal WeaponsController()
        {
            AstroPulse = new AstroPulseHelper(() => Romby.IsValid(Player) ? Player.meleeWeaponController : null);
            SatelliteShield = new SatelliteShieldHelper(() => (Romby.IsValid(Player) && Player.rotatingDaggersObject != null)
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

        public WeaponType CurrentWeapon
        {
            get => (WeaponType)CurrentWeaponId;
            set => SwitchWeapon((int)value);
        }

        public void SwitchWeapon(int weaponId)
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastOnWeaponChanged != null)
                FastOnWeaponChanged(plr, weaponId);
        }

        public void SwitchWeapon(WeaponType weapon) => SwitchWeapon((int)weapon);

        public void EquipSlot(WeaponType weapon) => Player?.OnWeaponEquipped((int)weapon);
        public void EquipSlot(int weaponId) => Player?.OnWeaponEquipped(weaponId);

        public void UnequipSlot(WeaponType weapon) => Player?.OnWeaponUnequipped((int)weapon);
        public void UnequipSlot(int weaponId) => Player?.OnWeaponUnequipped(weaponId);

        public bool ActivateWeapon(WeaponType weapon) => Romby.IsValid(Player) && Player.ActivateWeaponById((int)weapon);
        public void CancelWeapon(WeaponType weapon) => Player?.CancelActiveWeapon((int)weapon);
        public void CancelAllActiveWeapons() => Player?.CancelAllActiveWeapons();

        public Il2CppCollections.List<int> UnlockedWeaponsId => PlayerController.GetAcquiredWeaponIDs();
        public bool IsUnlocked(WeaponType weapon)
        {
            var ids = UnlockedWeaponsId;
            return ids != null && ids.Contains((int)weapon);
        }

        public int TotalAmmo => Romby.IsValid(Player) ? Player.GetTotalWeaponAmmo() : 0;

        public void CycleNext()
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastCycleWeapon != null)
                FastCycleWeapon(plr, 1);
        }

        public void CyclePrevious()
        {
            var plr = Player;
            if (Romby.IsValid(plr) && FastCycleWeapon != null)
                FastCycleWeapon(plr, -1);
        }
        #endregion

        #region Helpers On Player
        public AstroPulseHelper AstroPulse { get; }
        public SatelliteShieldHelper SatelliteShield { get; }
        public PlushinatorHelper Plushinator3000 { get; }
        public GachickponHelper Gachickpon { get; }


        public MoonerangHelper Moonerang { get; }
        public BitBangHelper BitBang { get; }
        public TrebleClefHelper TrebleClef { get; }
        public BigBucksHelper BigBucks { get; }
        public TheScreamerHelper TheScreamer { get; }
        public DivineRelocatorHelper DivineRelocator { get; }
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
        public Il2CppCollections.List<Mine> ActiveSolarMines => Mine.ActiveMines;
        public Il2CppCollections.List<TioPacoController> ActiveUncleFranks => TioPacoController.allActiveTioPacos;
        public Il2CppCollections.Queue<Arma14CajaAtrapaAlmas> ActiveSoulTransmuters => Arma14CajaAtrapaAlmas.ActiveBoxes;
        public int ActiveTeslaPawnsCount => ElectricPoleController.ActivePolesCount;

        public bool IsInsideBlackHole(GameObject obj) => BlackHoleController.IsObjectInAnyBlackHole(obj);
        public void DestroyAllUncleFranks() => TioPacoController.DestroyAllActiveTioPacos();
        public void DestroyAllExistentialVoids() => BlackHoleController.DestroyAllActiveBlackHoles();
        public void DestroyAllTeslaPawns() => ElectricPoleController.DestroyAllActiveElectricPoles();
        #endregion
    }

    #region Specialized Weapon Helpers
    public class AstroPulseHelper : WeaponHelperBase<MeleeWeaponController>
    {
        internal AstroPulseHelper(Func<MeleeWeaponController> getter) : base(getter) { }

        public bool Attack() => IsAvailable && Raw.TryMeleeAttack();
        public bool CanAttack => IsAvailable && Raw.CanAttack();
        public bool IsAttacking => IsAvailable && Raw.IsAttacking();
        public float CooldownRemaining => IsAvailable ? Raw.GetRemainingCooldown() : 0f;
        public float AttackDistance
        {
            get => IsAvailable ? Raw.attackDistance : 0f;
            set { if (IsAvailable) Raw.SetAttackDistance(value); }
        }
    }

    public class PlushinatorHelper : WeaponHelperBase<Arma8Controller>
    {
        internal PlushinatorHelper(Func<Arma8Controller> getter) : base(getter) { }

        public bool Fire() => IsAvailable && Raw.TryFire();
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
        public float FireRate => IsAvailable ? Raw.fireRate : 0f;
        public int BaseDamage => IsAvailable ? Raw.baseDamage : 0;
    }

    public class GachickponHelper : WeaponHelperBase<Arma12Controller>
    {
        internal GachickponHelper(Func<Arma12Controller> getter) : base(getter) { }

        public bool Fire() => IsAvailable && Raw.TryFire();
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
        public (int min, int max) DamageRange => IsAvailable
            ? (Raw.GetChickenDamageRange() is var r ? (r.Item1, r.Item2) : (0, 0))
            : (0, 0);
    }

    public class SatelliteShieldHelper : WeaponHelperBase<RotatingSatelliteController>
    {
        internal SatelliteShieldHelper(Func<RotatingSatelliteController> getter) : base(getter) { }

        public int ActiveCount => IsAvailable ? Raw.GetActiveSatelliteCount() : 0;
        public float ReflectionChance => IsAvailable ? Raw.reflectionChance : 0f;
        public void ForceSync() { if (IsAvailable) Raw.SyncAllActiveSatellites(); }
    }

    public class TrebleClefHelper : WeaponHelperBase<Arma10Controller>
    {
        internal TrebleClefHelper(Func<Arma10Controller> getter) : base(getter) { }

        public bool StartFlame() => IsAvailable && Raw.TryStartFlame();
        public void StopFlame() { if (IsAvailable) Raw.StopFlame(); }
        public bool IsFiring => IsAvailable && Raw.IsFiring;
        public float ThrustForce => IsAvailable ? Raw.CurrentThrustForce : 0f;
        public float AimAngle => (IsAvailable && Raw.TryGetAimAngle(out float a)) ? a : 0f;
        public int CurrentNoteIndex => IsAvailable ? Raw.GetCurrentAngleNoteIndex() : 0;
        public float CurrentPitch => Arma10Controller.GetTrumpetPitch(CurrentNoteIndex);
        public void ClearAllBurns() { if (IsAvailable) Raw.ClearAllBurns(); }
    }

    public class BigBucksHelper : WeaponHelperBase<Arma11Controller>
    {
        internal BigBucksHelper(Func<Arma11Controller> getter) : base(getter) { }

        public void StartCharging() { if (IsAvailable) Raw.StartCharging(); }
        public void ReleaseCharge() { if (IsAvailable) Raw.ReleaseCharge(); }
        public void CancelCharge() { if (IsAvailable) Raw.CancelCharge(); }
        public bool Fire() => IsAvailable && Raw.TryFire();
        public bool IsCharging => IsAvailable && Raw.IsCharging;
        public int ChargedMoney => IsAvailable ? Raw.CurrentChargedMoney : 0;
        public bool HasMoney => IsAvailable && Raw.HasMoneyToFire();
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
    }

    public class BubblewitchHelper : WeaponHelperBase<Arma16Controller>
    {
        internal BubblewitchHelper(Func<Arma16Controller> getter) : base(getter) { }

        public bool Fire() => IsAvailable && Raw.TryFire();
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
        public float GetCaptureChance(EnemyBase enemy) => IsAvailable ? Raw.CalculateCaptureChance(enemy) : 0f;
        public bool TryCapture(EnemyBase enemy) => IsAvailable && Raw.TryCapture(enemy);
        public void SummonMonster(CapturedMonsterData data, Vector3 position) { if (IsAvailable) Raw.SummonMonster(data, position); }
        public Il2CppCollections.List<CapturedMonsterData> Team => IsAvailable ? Raw.GetTeamMonsters() : null;
        public int TeamCount => IsAvailable ? Raw.GetCurrentMonsterCount() : 0;
        public int MaxSlots => IsAvailable ? Raw.GetMaxCaptureSlots() : 0;
        public bool HasFreeSlot => IsAvailable && Raw.HasFreeTeamSlot();
        public void Select(int index) { if (IsAvailable) Raw.SelectMonster(index); }
        public string GenerateRandomName() => IsAvailable ? Raw.GenerateRandomMonsterName() : string.Empty;
    }

    public class MoonerangHelper : WeaponHelperBase<BoomerangController>
    {
        internal MoonerangHelper(Func<BoomerangController> getter) : base(getter) { }

        public bool Launch(Vector3 targetWorldPos) => IsAvailable && Raw.LaunchBoomerang(targetWorldPos);
        public void Recall() { if (IsAvailable) Raw.Recall(); }
        public void Reset() { if (IsAvailable) Raw.ForceReset(); }
        public BoomerangController.BoomerangState State => IsAvailable ? Raw.CurrentState : BoomerangController.BoomerangState.Inactive;
        public bool IsInFlight => State != BoomerangController.BoomerangState.Inactive;
        public bool CanAttack => IsAvailable && Raw.CanAttack();
    }

    public class BitBangHelper : WeaponHelperBase<Arma9Controller>
    {
        internal BitBangHelper(Func<Arma9Controller> getter) : base(getter) { }

        public bool StartPixelizing() => IsAvailable && Raw.TryStartPixelizing();
        public void StopPixelizing() { if (IsAvailable) Raw.StopPixelizing(); }
        public bool IsPixelizing => IsAvailable && Raw.IsPixelizing;
        public EnemyBase Target => IsAvailable ? Raw.CurrentPixelizingTarget : null;
        public float Progress => IsAvailable ? Raw.PixelizationProgress : 0f;
    }

    public class DivineRelocatorHelper : WeaponHelperBase<Arma15Controller>
    {
        internal DivineRelocatorHelper(Func<Arma15Controller> getter) : base(getter) { }

        public void LaunchHandAt(Vector3 targetWorldPos) { if (IsAvailable) Raw.TryLaunchHandAtPosition(targetWorldPos); }
        public int ActiveHandsCount => IsAvailable ? Raw.GetActiveHandCount() : 0;
        public bool IsOnCooldown => IsAvailable && Raw.IsOnCooldown();
        public float CooldownProgress => IsAvailable ? Raw.GetCooldownProgress() : 0f;
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
    }

    public class TheScreamerHelper : WeaponHelperBase<Arma13Controller>
    {
        internal TheScreamerHelper(Func<Arma13Controller> getter) : base(getter) { }

        public void StartRecording() { if (IsAvailable) Raw.StartRecording(); }
        public void StopRecording() { if (IsAvailable) Raw.StopRecordingAndFire(); }
        public int Level => IsAvailable ? Raw.GetCurrentLevel() : 0;
        public float Pitch
        {
            get => IsAvailable ? Raw.projectilePitch : 0f;
            set { if (IsAvailable) Raw.projectilePitch = value; }
        }
    }
    #endregion

    public abstract class WeaponHelperBase<T> where T : UnityEngine.Component
    {
        private readonly Func<T> _getter;

        public T Raw => _getter != null ? _getter() : null;

        public bool IsAvailable => Romby.IsValid(Raw);

        protected WeaponHelperBase(Func<T> getter)
        {
            _getter = getter;
        }
    }
}