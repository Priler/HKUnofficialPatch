using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### God Tamer Beast Roll Collider Fix
    // the beast's collider isn't restored after rolling because the "Idle" state
    // is missing the ActivateGameObject action that re-enables it.
    // copies the action from "Spit Recover" into "Idle"
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    public static class GodTamerBeastRollPatch
    {
        [HarmonyPostfix]
        static void Postfix(PlayMakerFSM __instance)
        {
            if (__instance.gameObject.scene.name != "GG_God_Tamer") return;
            if (__instance.gameObject.name != "Lobster") return;
            if (__instance.FsmName != "Control") return;

            var sourceAction = FsmUtil.GetAction<HutongGames.PlayMaker.Actions.ActivateGameObject>(
                __instance, "Spit Recover", 2);
            if (sourceAction == null) return;

            var idleState = FsmUtil.GetState(__instance, "Idle");
            if (idleState == null) return;

            var cloned = FsmUtil.CloneAction(sourceAction);
            FsmUtil.AddAction(idleState, cloned);

            Plugin.Logger.LogInfo("God Tamer Beast roll collider fixed.");
        }
    }
}
