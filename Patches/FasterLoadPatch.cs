using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Faster Scene Load
    // replaces specific WaitForSeconds delays in HeroController.EnterScene with null yields
    [HarmonyPatch]
    public static class FasterLoadPatch
    {
        private static readonly float[] SKIP = { 0.4f, 0.165f };

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var enterSceneMethod = typeof(HeroController).GetMethod(nameof(HeroController.EnterScene));
            if (enterSceneMethod == null)
            {
                Debug.LogError("[UnofficialPatch] EnterScene method not found");
                return null;
            }

            var stateMachineAttr = enterSceneMethod.GetCustomAttribute<IteratorStateMachineAttribute>();
            if (stateMachineAttr == null)
            {
                Debug.LogError("[UnofficialPatch] IteratorStateMachineAttribute not found on EnterScene");
                return null;
            }

            var moveNextMethod = stateMachineAttr.StateMachineType.GetMethod("MoveNext",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (moveNextMethod == null)
            {
                Debug.LogError("[UnofficialPatch] MoveNext method not found in state machine");
                return null;
            }

            Debug.Log($"[UnofficialPatch] Targeting {stateMachineAttr.StateMachineType.Name}::MoveNext");
            return moveNextMethod;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var wsCtor = AccessTools.Constructor(typeof(WaitForSeconds), new[] { typeof(float) });

            if (wsCtor == null)
            {
                Debug.LogError("[UnofficialPatch] WaitForSeconds constructor not found");
                return instructions;
            }

            int patchCount = 0;

            // work backwards to avoid index issues when removing
            for (int i = list.Count - 1; i >= 1; i--)
            {
                if (list[i].opcode == OpCodes.Newobj &&
                    list[i].operand != null &&
                    list[i].operand.Equals(wsCtor))
                {
                    int prev = i - 1;
                    if (prev >= 0 &&
                        list[prev].opcode == OpCodes.Ldc_R4 &&
                        list[prev].operand is float f &&
                        SKIP.Contains(f))
                    {
                        list.RemoveAt(i);
                        list.RemoveAt(prev);
                        list.Insert(prev, new CodeInstruction(OpCodes.Ldnull));

                        patchCount++;
                        Debug.Log($"[UnofficialPatch] Replaced WaitForSeconds({f}f) with null");
                    }
                }
            }

            Debug.Log($"[UnofficialPatch] FasterLoad transpiler applied {patchCount} patches");
            return list;
        }
    }
}
