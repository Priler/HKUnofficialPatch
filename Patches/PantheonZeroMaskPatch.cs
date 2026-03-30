using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Pantheon 0-Mask Fix
    // the else-if branch in TakeDamage that lets spike/acid damage bypass iframes
    // is missing a BossSceneController.IsTransitioning check, allowing damage during
    // boss transitions which can result in 0 health without triggering death
    [HarmonyPatch(typeof(HeroController), "TakeDamage")]
    public static class PantheonZeroMaskPatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            var canTakeDamage = AccessTools.Method(typeof(HeroController), "CanTakeDamage");
            var invulnerable = AccessTools.Field(typeof(HeroControllerStates), nameof(HeroControllerStates.invulnerable));
            var hazardDeath = AccessTools.Field(typeof(HeroControllerStates), nameof(HeroControllerStates.hazardDeath));
            var isInvincible = AccessTools.Field(typeof(PlayerData), nameof(PlayerData.isInvincible));
            var isTransitioning = AccessTools.PropertyGetter(typeof(BossSceneController), nameof(BossSceneController.IsTransitioning));

            if (canTakeDamage == null || invulnerable == null || hazardDeath == null ||
                isInvincible == null || isTransitioning == null)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: could not resolve required members");
                return instructions;
            }

            // find CanTakeDamage call
            int canTakeDamageIdx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Calls(canTakeDamage))
                {
                    canTakeDamageIdx = i;
                    break;
                }
            }

            if (canTakeDamageIdx < 0)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: CanTakeDamage call not found");
                return instructions;
            }

            // after CanTakeDamage, find the else-if condition chain:
            // invulnerable -> hazardDeath -> isInvincible
            int invulIdx = -1;
            for (int i = canTakeDamageIdx + 1; i < list.Count; i++)
            {
                if (list[i].LoadsField(invulnerable))
                {
                    invulIdx = i;
                    break;
                }
            }

            if (invulIdx < 0)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: invulnerable field not found after CanTakeDamage");
                return instructions;
            }

            int hazardIdx = -1;
            for (int i = invulIdx + 1; i < list.Count; i++)
            {
                if (list[i].LoadsField(hazardDeath))
                {
                    hazardIdx = i;
                    break;
                }
            }

            if (hazardIdx < 0)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: hazardDeath field not found after invulnerable");
                return instructions;
            }

            int isInvIdx = -1;
            for (int i = hazardIdx + 1; i < list.Count; i++)
            {
                if (list[i].LoadsField(isInvincible))
                {
                    isInvIdx = i;
                    break;
                }
            }

            if (isInvIdx < 0)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: isInvincible field not found after hazardDeath");
                return instructions;
            }

            // find the branch right after isInvincible (brtrue that skips past the else-if block)
            int branchIdx = -1;
            for (int i = isInvIdx + 1; i < isInvIdx + 4 && i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Brtrue || list[i].opcode == OpCodes.Brtrue_S)
                {
                    branchIdx = i;
                    break;
                }
            }

            if (branchIdx < 0)
            {
                Debug.LogError("[UnofficialPatch] PantheonZeroMaskFix: branch after isInvincible not found");
                return instructions;
            }

            // reuse the same skip label
            var skipLabel = list[branchIdx].operand;

            // insert after the isInvincible branch: if BossSceneController.IsTransitioning, skip
            var patch = new[]
            {
                new CodeInstruction(OpCodes.Call, isTransitioning),
                new CodeInstruction(OpCodes.Brtrue_S, skipLabel),
            };

            list.InsertRange(branchIdx + 1, patch);

            Debug.Log("[UnofficialPatch] PantheonZeroMaskFix transpiler applied");
            return list;
        }
    }
}
