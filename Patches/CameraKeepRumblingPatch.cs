using GlobalEnums;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Camera Keep Rumbling Fix
    // resets camera shake after scene transitions in godhome,
    // preventing rumble from persisting across boss fights
    [HarmonyPatch(typeof(HeroController), "SceneInit")]
    public static class CameraKeepRumblingPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            var gm = GameManager.instance;
            if (gm == null) return;

            if (gm.GetCurrentMapZone() != MapZone.GODS_GLORY.ToString())
                return;

            var gc = GameCameras.instance;
            if (gc == null || gc.cameraShakeFSM == null) return;

            gc.cameraShakeFSM.SendEvent("LEVEL LOADED");
        }
    }
}
