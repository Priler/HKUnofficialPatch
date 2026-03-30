using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Wallslide Speed Clamp Fix
    // fixes a copy-paste bug where the inner clamp in the second wallslide
    // convergence block uses < instead of >, causing one-frame velocity oscillation
    [HarmonyPatch(typeof(HeroController), "FixedUpdate")]
    public static class WallslideClampPatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var wallslideSpeed = AccessTools.Field(typeof(HeroController), "WALLSLIDE_SPEED");
            var wallslideDecel = AccessTools.Field(typeof(HeroController), "WALLSLIDE_DECEL");

            if (wallslideSpeed == null || wallslideDecel == null)
            {
                Debug.LogError("[UnofficialPatch] WallslideClampFix: could not resolve fields");
                return instructions;
            }

            // find the 2nd WALLSLIDE_DECEL load (puts us inside block 2)
            int decelCount = 0;
            int searchFrom = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(wallslideDecel))
                {
                    decelCount++;
                    if (decelCount == 2)
                    {
                        searchFrom = i;
                        break;
                    }
                }
            }

            if (searchFrom < 0)
            {
                Debug.LogError("[UnofficialPatch] WallslideClampFix: could not find 2nd WALLSLIDE_DECEL");
                return instructions;
            }

            // find the next WALLSLIDE_SPEED load after that (the buggy comparison)
            for (int i = searchFrom + 1; i < list.Count; i++)
            {
                if (!list[i].LoadsField(wallslideSpeed))
                    continue;

                // find the branch instruction within the next few opcodes and flip it
                for (int j = i + 1; j < i + 6 && j < list.Count; j++)
                {
                    var op = list[j].opcode;
                    bool flipped = false;

                    // bge/bge.s -> ble/ble.s (flip < to >)
                    if (op == OpCodes.Bge_S) { list[j].opcode = OpCodes.Ble_S; flipped = true; }
                    else if (op == OpCodes.Bge) { list[j].opcode = OpCodes.Ble; flipped = true; }
                    else if (op == OpCodes.Bge_Un_S) { list[j].opcode = OpCodes.Ble_Un_S; flipped = true; }
                    else if (op == OpCodes.Bge_Un) { list[j].opcode = OpCodes.Ble_Un; flipped = true; }

                    if (flipped)
                    {
                        Debug.Log($"[UnofficialPatch] WallslideClampFix: flipped {op} to {list[j].opcode}");
                        return list;
                    }
                }

                Debug.LogError("[UnofficialPatch] WallslideClampFix: no branch found after WALLSLIDE_SPEED");
                return instructions;
            }

            Debug.LogError("[UnofficialPatch] WallslideClampFix: could not find WALLSLIDE_SPEED after 2nd DECEL");
            return instructions;
        }
    }
}
