using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Transition Death Fix
    // prevents from continuing pantheon challenges after death upon scene transitions
    // adds a wait condition to "Hero Death Anim" FSM that pauses until
    // scene transition is complete before proceeding with death sequence
    [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    public static class TransitionDeathPatch
    {
        [HarmonyPostfix]
        static void Postfix(PlayMakerFSM __instance)
        {
            if (__instance.FsmName != "Hero Death Anim") return;

            // verify this is on the Knight's Hero Death child object
            var parent = __instance.transform.parent;
            if (parent == null || parent.gameObject.name != "Knight") return;
            if (__instance.gameObject.name != "Hero Death") return;

            var wpCheckState = FsmUtil.GetState(__instance, "WP Check");
            if (wpCheckState == null) return;

            var waitAction = new WaitUntilNotTransitioning();
            FsmUtil.AddAction(wpCheckState, waitAction);

            Plugin.Logger.LogInfo("Transition death fix applied to Hero Death FSM.");
        }
    }

    // custom FSM action that waits until the game is no longer transitioning
    public class WaitUntilNotTransitioning : FsmStateAction
    {
        public override void OnEnter()
        {
            if (!GameManager.instance.IsInSceneTransition)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            if (!GameManager.instance.IsInSceneTransition)
            {
                Finish();
            }
        }
    }
}
