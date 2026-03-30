using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Grey Prince Nail Slam Collider Fix
    // when grey prince is staggered, his nail slam collider can remain active
    // because the "Send Event" (stagger) state is missing the ActivateGameObject
    // action that deactivates it. copies from "Slash End" into "Send Event"
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    public static class GreyPrinceNailSlamPatch
    {
        [HarmonyPostfix]
        static void Postfix(PlayMakerFSM __instance)
        {
            if (__instance.gameObject.scene.name != "GG_Grey_Prince_Zote") return;
            if (__instance.gameObject.name != "Grey Prince") return;
            if (__instance.FsmName != "Control") return;

            var sourceAction = FsmUtil.GetAction<HutongGames.PlayMaker.Actions.ActivateGameObject>(
                __instance, "Slash End", 1);
            if (sourceAction == null) return;

            var staggerState = FsmUtil.GetState(__instance, "Send Event");
            if (staggerState == null) return;

            var cloned = FsmUtil.CloneAction(sourceAction);
            FsmUtil.AddAction(staggerState, cloned);

            Plugin.Logger.LogInfo("Grey Prince nail slam collider fixed.");
        }
    }
}
