using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Basin Fountain Fix
    // accepts partial geo donations at the ancient basin fountain
    // also takes shade geo on donation
    [HarmonyPatch(typeof(PlayMakerFSM), "OnEnable")]
    public static class BasinFountainPatch
    {
        private static bool isPartialDonationAccepted = false;

        [HarmonyPostfix]
        static void Postfix(PlayMakerFSM __instance)
        {
            if (!__instance.gameObject.name.Contains("Fountain Donation") ||
                __instance.FsmName != "Conversation Control")
                return;

            InsertFsmState(__instance, "Partial Donate", new AcceptPartialDonation(__instance));
            InsertFsmState(__instance, "Full Donation", new TakeAllGeo(__instance));
        }

        private static void InsertFsmState(PlayMakerFSM fsm, string stateName, FsmStateAction actionToInsert)
        {
            var state = GetStateByName(fsm, stateName);
            if (state == null)
            {
                Plugin.Logger.LogInfo($"'{stateName}' state not found.");
                return;
            }

            // avoid injecting multiple times
            if (state.Actions != null && state.Actions.Any(a => a.GetType() == actionToInsert.GetType()))
                return;

            var actions = (state.Actions != null)
                ? state.Actions.ToList()
                : new List<FsmStateAction>();

            actions.Insert(0, actionToInsert);
            state.Actions = actions.ToArray();

            Plugin.Logger.LogInfo($"Injected {actionToInsert.GetType().Name} into '{stateName}'.");
        }

        private static FsmState GetStateByName(PlayMakerFSM fsm, string stateName)
        {
            if (fsm?.Fsm == null) return null;
            var states = fsm.Fsm.States;
            if (states == null) return null;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i]?.Name == stateName)
                    return states[i];
            }
            return null;
        }

        // ### FSM Actions

        private class AcceptPartialDonation : FsmStateAction
        {
            private PlayMakerFSM self;
            public AcceptPartialDonation(PlayMakerFSM self) { this.self = self; }

            public override void OnEnter()
            {
                Plugin.Logger.LogInfo("Partial Donation: accepting as full donation.");
                isPartialDonationAccepted = true;
                PlayerData.instance.geoPool = 0;
                self.SetState("Full Donation");
            }
        }

        private class TakeAllGeo : FsmStateAction
        {
            private PlayMakerFSM self;
            public TakeAllGeo(PlayMakerFSM self) { this.self = self; }

            public override void OnEnter()
            {
                if (isPartialDonationAccepted)
                {
                    isPartialDonationAccepted = false;
                    self.FsmVariables.GetFsmInt("Amount to Donate").Value = 0;
                    return;
                }

                Plugin.Logger.LogInfo("Full Donation: taking all geo.");
                PlayerData.instance.geoPool = 0;
                self.FsmVariables.GetFsmInt("Amount to Donate").Value = PlayerData.instance.geo;
            }
        }
    }
}
