using System.Collections;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### HUD Display Checker
    // the HUD can disappear during godhome boss transitions because the
    // "Slide Out" FSM doesn't always receive the "IN" event.
    // after BossSceneController starts, waits for gameplay state then forces HUD in
    [HarmonyPatch(typeof(BossSceneController), "Start")]
    public static class HUDDisplayCheckerPatch
    {
        private static readonly string[] sceneExclusions = {
            "GG_Hollow_Knight",
            "GG_Radiance"
        };

        [HarmonyPostfix]
        static void Postfix(BossSceneController __instance)
        {
            var gm = GameManager.instance;
            if (gm == null) return;

            for (int i = 0; i < sceneExclusions.Length; i++)
            {
                if (gm.sceneName == sceneExclusions[i])
                    return;
            }

            __instance.StartCoroutine(ForceHUDIn());
        }

        private static IEnumerator ForceHUDIn()
        {
            yield return new WaitUntil(() => GameManager.instance.gameState == GameState.PLAYING);

            var gc = GameCameras.instance;
            if (gc == null || gc.hudCanvas == null) yield break;

            var slideOut = FSMUtility.LocateFSM(gc.hudCanvas, "Slide Out");
            if (slideOut != null)
                slideOut.SendEvent("IN");
        }
    }
}
