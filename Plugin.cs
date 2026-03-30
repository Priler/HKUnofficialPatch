using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace UnofficialPatch
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
        }
    }

    public static class PluginInfo
    {
        public const string GUID = "com.priler.unofficialpatch";
        public const string Name = "Unofficial Patch";
        public const string Version = "1.0.0";
    }
}
