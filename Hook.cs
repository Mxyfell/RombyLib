using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace RombyLib
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    internal class Hook : BasePlugin
    {
        public const string ModGUID = "ru.mxyfell.rombylib";
        public const string ModName = "RombyLib";
        public const string ModVersion = "1.0.0";

        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;
            Romby.Init();
            Log.LogInfo(ModName + " v" + ModVersion + " loaded successfully.");
        }
    }
}