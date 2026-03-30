using System;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;

namespace UnofficialPatch
{
    public static class FsmUtil
    {
        public static FsmState GetState(PlayMakerFSM fsm, string stateName)
        {
            if (fsm?.Fsm?.States == null) return null;
            var states = fsm.Fsm.States;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i]?.Name == stateName)
                    return states[i];
            }
            return null;
        }

        public static T GetAction<T>(PlayMakerFSM fsm, string stateName, int actionIndex) where T : FsmStateAction
        {
            var state = GetState(fsm, stateName);
            if (state?.Actions == null || actionIndex < 0 || actionIndex >= state.Actions.Length)
                return null;
            return state.Actions[actionIndex] as T;
        }

        // clone an FSM action via MemberwiseClone (protected, so we use reflection)
        public static FsmStateAction CloneAction(FsmStateAction source)
        {
            if (source == null) return null;
            var cloneMethod = typeof(object).GetMethod("MemberwiseClone",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (FsmStateAction)cloneMethod.Invoke(source, null);
        }

        public static void AddAction(FsmState state, FsmStateAction action)
        {
            if (state == null || action == null) return;
            var actions = state.Actions != null
                ? state.Actions.ToList()
                : new System.Collections.Generic.List<FsmStateAction>();
            actions.Add(action);
            state.Actions = actions.ToArray();
        }
    }
}
