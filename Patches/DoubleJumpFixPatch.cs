using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Double Jump Fix
    // prevents double jump from being consumed when releasing jump too early
    // ported from MonoMod.Cil to Harmony transpiler
    [HarmonyPatch(typeof(HeroController), "JumpReleased")]
    public static class DoubleJumpFixPatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = new List<CodeInstruction>(instructions);

            // find blt and create a label for the instruction right after it
            var jumpLabel = generator.DefineLabel();
            bool labelPlaced = false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Blt || list[i].opcode == OpCodes.Blt_S)
                {
                    if (i + 1 < list.Count)
                    {
                        list[i + 1].labels.Add(jumpLabel);
                        labelPlaced = true;
                    }
                    break;
                }
            }

            if (!labelPlaced)
            {
                Debug.LogError("[UnofficialPatch] DoubleJumpFix: blt instruction not found");
                return instructions;
            }

            // find ble.un and insert doubleJumping check after it
            var cStateField = AccessTools.Field(typeof(HeroController), nameof(HeroController.cState));
            var doubleJumpingField = AccessTools.Field(typeof(HeroControllerStates), nameof(HeroControllerStates.doubleJumping));

            if (cStateField == null || doubleJumpingField == null)
            {
                Debug.LogError("[UnofficialPatch] DoubleJumpFix: could not resolve cState/doubleJumping fields");
                return instructions;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ble_Un || list[i].opcode == OpCodes.Ble_Un_S)
                {
                    var patch = new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, cStateField),
                        new CodeInstruction(OpCodes.Ldfld, doubleJumpingField),
                        new CodeInstruction(OpCodes.Brtrue_S, jumpLabel),
                    };

                    list.InsertRange(i + 1, patch);

                    Debug.Log("[UnofficialPatch] DoubleJumpFix transpiler applied");
                    return list;
                }
            }

            Debug.LogError("[UnofficialPatch] DoubleJumpFix: ble.un instruction not found");
            return instructions;
        }
    }
}
