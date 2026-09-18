using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace RombyLib
{
    [BepInPlugin("ru.mxyfell.rombylib", "RombyLib", "1.0.0")]
    internal class Hook : BasePlugin
    {
        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Romby).Assembly);
            Log.LogInfo("RombyLib v1.0.0 Loaded");
        }
    }
}
