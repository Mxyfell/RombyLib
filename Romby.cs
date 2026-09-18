using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

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
        public static SkinController Skin { get; internal set; }
        public static StatsController Stats { get; internal set; }
        public static WeaponsController Weapons { get; internal set; }
        public static PlayerController Controller { get; internal set; }

        public static bool IsLoaded => Controller != null && Skin != null && Stats != null;
        #region Internal Patches
        [HarmonyPatch(typeof(PlayerController))]
        private static class PlrPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void AwakePostfix(PlayerController __instance)
            {
                if (__instance != null && __instance.gameObject != null)
                {
                    Controller = __instance;
                    Skin = new SkinController(Controller);
                    Stats = new StatsController(Controller);
                    Weapons = new WeaponsController(Controller);
                }
            }

            [HarmonyPatch("OnDestroy")]
            [HarmonyPostfix]
            private static void OnDestroyPostfix(PlayerController __instance)
            {
                if (Controller == __instance)
                {
                    Controller = null;
                    Skin = null;
                    Stats = null;
                    Weapons = null;
                }
            }
        }
        #endregion
    }

    public class SkinController
    {
        public Transform RootTodo { get; }

        public SpriteRenderer Body { get; }                 //Cuepro
        public SpriteRenderer EyeLeft { get; }              //OjoL
        public SpriteRenderer EyeRight { get; }             //OjoR
        public SpriteRenderer Mouth { get; }                //Boca
        public SpriteRenderer Extra { get; }                //Extra

        public SpriteRenderer BodyShadow { get; }           //Cuepro Sombra
        public SpriteRenderer ExtraShadow { get; }          //Extra Sombra

        public SpriteRenderer LegLeft { get; }              // PiernaL
        public SpriteRenderer LegRight { get; }             // PiernaR

        public Transform PivotLegLeft { get; }              // PivotPiernaL
        public Transform PivotLegRight { get; }             // PivotPiernaR

        internal SkinController(PlayerController player)
        {
            var todo = player.jugadorNormal != null
                ? player.jugadorNormal.transform.Find("Todo")
                : player.transform.Find("JugadorNormal/Todo");
            RootTodo = todo;

            if (todo == null) return;

            Body = todo.Find("Cuerpo")?.GetComponent<SpriteRenderer>();
            EyeLeft = todo.Find("OjoL")?.GetComponent<SpriteRenderer>();
            EyeRight = todo.Find("OjoR")?.GetComponent<SpriteRenderer>();
            Mouth = todo.Find("Boca")?.GetComponent<SpriteRenderer>();
            Extra = todo.Find("Extra")?.GetComponent<SpriteRenderer>();

            BodyShadow = todo.Find("Cuerpo Sombra")?.GetComponent<SpriteRenderer>();
            ExtraShadow = todo.Find("Extra Sombra")?.GetComponent<SpriteRenderer>();

            PivotLegLeft = todo.Find("PivotPiernaL");
            PivotLegRight = todo.Find("PivotPiernaR");

            LegLeft = PivotLegLeft?.Find("PiernaL")?.GetComponent<SpriteRenderer>();
            LegRight = PivotLegRight?.Find("PiernaR")?.GetComponent<SpriteRenderer>();
        }
    }

    public class StatsController
    {
        private readonly PlayerController _player;

        internal StatsController(PlayerController player)
        {
            _player = player;
        }

        public float MoveSpeed
        {
            get => _player.moveSpeed;
            set => _player.moveSpeed = value;
        }
        public float BaseMoveSpeed => _player.GetBaseMoveSpeed();
        public float MaxTotalSpeed
        {
            get => _player.maxTotalSpeed;
            set => _player.maxTotalSpeed = value;
        }

        public void ApplySpeedBoost(float multiplier, float duration, string boostId = "custom_mod_boost")
        {
            _player.ApplyTemporarySpeedBoost(multiplier, duration, boostId);
        }
        public void RemoveSpeedBoost(string boostId)
        {
            _player.RemoveSpeedBoost(boostId);
        }
        public float CriticalChance
        {
            get => _player.criticalChance;
            set => _player.criticalChance = value;
        }
        public float CurrentCriticalChance => _player.GetCurrentCritChance();

        public float CriticalMultiplier
        {
            get => _player.criticalMultiplier;
            set => _player.criticalMultiplier = value;
        }
        public bool IsDizzy => _player.IsDizzy;
        public bool IsFrozen => _player.IsFrozen;
        public bool IsBurning => _player.IsBurning;
        public bool IsMeditationState => _player.IsInMeditationState();

        public void Freeze(float duration) => _player.ApplyFreeze(new Nullable<float>(duration), default);
        public void Burn(float duration, float damagePerSecond) => _player.ApplyBurn(duration, damagePerSecond);
    }

    public class WeaponsController
    {
        private readonly PlayerController _player;

        private static readonly AccessTools.FieldRef<PlayerController, int> CurrentWeaponRef =
            AccessTools.FieldRefAccess<PlayerController, int>("currentEquippedWeaponID");

        private static readonly System.Action<PlayerController, int> CycleWeaponMethod =
            AccessTools.MethodDelegate<System.Action<PlayerController, int>>(AccessTools.Method(typeof(PlayerController), "CycleWeapon"));

        internal WeaponsController(PlayerController player)
        {
            _player = player;

            Moonerang = new MoonerangHelper(player.boomerangController);
            BitBang = new BitBangHelper(player.bitBangController);
            TrebleClef = new TrebleClefHelper(player.trompetaLlameanteController);
            BigBucks = new BigBucksHelper(player.donBilletonController);
            TheScreamer = new TheScreamerHelper(player.vocalizadorController);
            DivineRelocator = new DivineRelocatorHelper(player.grabbingHandsController);
            Bubblewitch = new BubblewitchHelper(player.cajaController);
        }

        #region Common & State
        public int CurrentWeaponId
        {
            get => CurrentWeaponRef(_player);
            set => _player.OnWeaponEquipped(value);
        }

        public WeaponType CurrentWeapon
        {
            get => (WeaponType)CurrentWeaponId;
            set => CurrentWeaponId = (int)value;
        }

        public void Equip(WeaponType weapon) => _player.OnWeaponEquipped((int)weapon);
        public void Equip(int weaponId) => _player.OnWeaponEquipped(weaponId);

        public void Unequip(WeaponType weapon) => _player.OnWeaponUnequipped((int)weapon);
        public void Unequip(int weaponId) => _player.OnWeaponUnequipped(weaponId);

        public void ActivateWeapon(WeaponType weapon) => _player.ActivateWeaponById((int)weapon);
        public void CancelWeapon(WeaponType weapon) => _player.CancelActiveWeapon((int)weapon);
        public void CancelAllActiveWeapons() => _player.CancelAllActiveWeapons();

        public List<int> UnlockedWeaponsId => PlayerController.GetAcquiredWeaponIDs();
        public bool IsUnlocked(WeaponType weapon) => UnlockedWeaponsId != null && UnlockedWeaponsId.Contains((int)weapon);

        public int TotalAmmo => _player.GetTotalWeaponAmmo();
        public void CycleNext() => CycleWeaponMethod?.Invoke(_player, 1);
        public void CyclePrevious() => CycleWeaponMethod?.Invoke(_player, -1);
        #endregion

        #region Controller On Player
        public MeleeWeaponController AstroPulse => _player.meleeWeaponController;
        public RotatingSatelliteController SatelliteShield =>
            _player.rotatingDaggersObject?.GetComponent<RotatingSatelliteController>();
        public Arma8Controller Plushinator3000 => _player.pelucheadorController;
        public Arma12Controller Gachickpon => _player.huevoPolloController;
        public MoonerangHelper Moonerang { get; }
        public BitBangHelper BitBang { get; }
        public TrebleClefHelper TrebleClef { get; }
        public BigBucksHelper BigBucks { get; }
        public TheScreamerHelper TheScreamer { get; }
        public DivineRelocatorHelper DivineRelocator { get; }
        public BubblewitchHelper Bubblewitch { get; }
        #endregion
        #region Prefabs for Spawned Objects
        public GameObject SolarMinePrefab => _player.minePrefab;
        public GameObject ExistentialVoidPrefab => _player.blackHolePrefab;
        public GameObject UncleFrankPrefab => _player.tioPacoPrefab;
        public GameObject TeslaPawnPrefab => _player.electricPolePrefab;
        public GameObject TheSoulTransmuterPrefab => _player.cajaAtrapaAlmasPrefab;
        #endregion
        #region Spawned World Objects
        public List<Mine> ActiveSolarMines => Mine.ActiveMines;
        public List<TioPacoController> ActiveUncleFranks => TioPacoController.allActiveTioPacos;
        public Queue<Arma14CajaAtrapaAlmas> ActiveSoulTransmuters => Arma14CajaAtrapaAlmas.ActiveBoxes;
        public int ActiveTeslaPawnsCount => ElectricPoleController.ActivePolesCount;

        public bool IsInsideBlackHole(GameObject obj) => BlackHoleController.IsObjectInAnyBlackHole(obj);
        public void DestroyAllUncleFranks() => TioPacoController.DestroyAllActiveTioPacos();
        public void DestroyAllExistentialVoids() => BlackHoleController.DestroyAllActiveBlackHoles();
        public void DestroyAllTeslaPawns() => ElectricPoleController.DestroyAllActiveElectricPoles();
        #endregion
    }

    #region Specialized Weapon Controllers
    public class TrebleClefHelper
    {
        private readonly Arma10Controller _c;
        public Arma10Controller Raw => _c;
        internal TrebleClefHelper(Arma10Controller controller) => _c = controller;

        public bool StartFlame() => _c != null && _c.TryStartFlame();
        public void StopFlame() => _c?.StopFlame();
        public bool IsFiring => _c != null && _c.IsFiring;
        public float ThrustForce => _c != null ? _c.CurrentThrustForce : 0f;
        public float AimAngle => (_c != null && _c.TryGetAimAngle(out float a)) ? a : 0f;
        public int CurrentNoteIndex => _c != null ? _c.GetCurrentAngleNoteIndex() : 0;
        public float CurrentPitch => Arma10Controller.GetTrumpetPitch(CurrentNoteIndex);
        public void ClearAllBurns() => _c?.ClearAllBurns();
    }
    public class BigBucksHelper
    {
        private readonly Arma11Controller _c;
        public Arma11Controller Raw => _c;
        internal BigBucksHelper(Arma11Controller controller) => _c = controller;

        public void StartCharging() => _c?.StartCharging();
        public void ReleaseCharge() => _c?.ReleaseCharge();
        public void CancelCharge() => _c?.CancelCharge();
        public bool Fire() => _c != null && _c.TryFire();
        public bool IsCharging => _c != null && _c.IsCharging;
        public int ChargedMoney => _c != null ? _c.CurrentChargedMoney : 0;
        public bool HasMoney => _c != null && _c.HasMoneyToFire();
    }
    public class BubblewitchHelper
    {
        private readonly Arma16Controller _c;
        public Arma16Controller Raw => _c;
        internal BubblewitchHelper(Arma16Controller controller) => _c = controller;

        public float GetCaptureChance(EnemyBase enemy) => _c != null ? _c.CalculateCaptureChance(enemy) : 0f;
        public bool TryCapture(EnemyBase enemy) => _c != null && _c.TryCapture(enemy);
        public void SummonMonster(CapturedMonsterData data, Vector3 possition) => _c?.SummonMonster(data, possition);
        public List<CapturedMonsterData> Team => _c?.GetTeamMonsters();
        public int TeamCount => _c != null ? _c.GetCurrentMonsterCount() : 0;
        public int MaxSlots => _c != null ? _c.GetMaxCaptureSlots() : 0;
        public bool HasFreeSlot => _c != null && _c.HasFreeTeamSlot();
        public void Select(int index) => _c?.SelectMonster(index);
        public string GenerateRandomName() => _c?.GenerateRandomMonsterName();
    }
    public class MoonerangHelper
    {
        private readonly BoomerangController _c;
        public BoomerangController Raw => _c;
        internal MoonerangHelper(BoomerangController controller) => _c = controller;

        public bool Launch(Vector3 targetWorldPos) => _c != null && _c.LaunchBoomerang(targetWorldPos);
        public void Recall() => _c?.Recall();
        public void Reset() => _c?.ForceReset();
        public BoomerangController.BoomerangState State => _c != null ? 
            _c.CurrentState : BoomerangController.BoomerangState.Inactive;
        public bool IsInFlight => State != BoomerangController.BoomerangState.Inactive;
    }
    public class BitBangHelper
    {
        private readonly Arma9Controller _c;
        public Arma9Controller Raw => _c;
        internal BitBangHelper(Arma9Controller controller) => _c = controller;

        public bool StartPixelizing() => _c != null && _c.TryStartPixelizing();
        public void StopPixelizing() => _c?.StopPixelizing();
        public bool IsPixelizing => _c != null && _c.IsPixelizing;
        public EnemyBase Target => _c?.CurrentPixelizingTarget;
        public float Progress => _c != null ? _c.PixelizationProgress : 0f;
    }
    public class DivineRelocatorHelper
    {
        private readonly Arma15Controller _c;
        public Arma15Controller Raw => _c;
        internal DivineRelocatorHelper(Arma15Controller controller) => _c = controller;

        public void LaunchHandAt(Vector3 targetWorldPos) => _c?.TryLaunchHandAtPosition(targetWorldPos);
        public int ActiveHandsCount => _c != null ? _c.GetActiveHandCount() : 0;
        public bool IsOnCooldown => _c != null && _c.IsOnCooldown();
        public float CooldownProgress => _c != null ? _c.GetCooldownProgress() : 0f;
    }
    public class TheScreamerHelper
    {
        private readonly Arma13Controller _c;
        public Arma13Controller Raw => _c;
        internal TheScreamerHelper(Arma13Controller controller) => _c = controller;

        public void StartRecording() => _c?.StartRecording();
        public void StopRecording() => _c?.StopRecordingAndFire();
        public float Pitch
        {
            get => _c != null ? _c.projectilePitch : 0f;
            set { if (_c != null) _c.projectilePitch = value; }
        }
    }
    #endregion
}