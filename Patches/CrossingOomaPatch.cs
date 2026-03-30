using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UnofficialPatch.Patches
{
    // ### Crossing Ooma Fix
    // prevents ooma from spawning explosive jellies that persist across scenes
    // by tracking and destroying them when leaving uumuu boss scenes
    public static class CrossingOomaPatch
    {
        private static HashSet<OomaJellyTracker> tracked = new HashSet<OomaJellyTracker>();
        private static string lastScene = "";

        private static bool IsUumuuScene(string name)
        {
            return name == "GG_Uumuu" || name == "GG_Uumuu_V";
        }

        // detect scene transitions via HeroController.SceneInit
        [HarmonyPatch(typeof(HeroController), "SceneInit")]
        public static class SceneChangeDetector
        {
            [HarmonyPostfix]
            static void Postfix()
            {
                var gm = GameManager.instance;
                string currentScene = gm != null ? gm.GetSceneNameString() : "";

                if (IsUumuuScene(lastScene) && !IsUumuuScene(currentScene))
                    DestroyTrackedJellies();

                lastScene = currentScene;
            }
        }

        // mark jelly-related objects in uumuu scenes
        [HarmonyPatch(typeof(PlayMakerFSM), "OnEnable")]
        public static class JellyMarker
        {
            [HarmonyPostfix]
            static void Postfix(PlayMakerFSM __instance)
            {
                if (!IsUumuuScene(__instance.gameObject.scene.name))
                    return;

                // look for jellyfish spawner pools
                var pool = __instance.gameObject.GetComponent<PersonalObjectPool>();
                if (pool == null || pool.startupPool == null) return;

                foreach (var entry in pool.startupPool)
                {
                    if (entry.prefab == null) continue;
                    AddTrackerIfMissing(entry.prefab);
                }
            }
        }

        private static void AddTrackerIfMissing(GameObject go)
        {
            if (go.GetComponent<OomaJellyTracker>() == null)
                go.AddComponent<OomaJellyTracker>();
        }

        private static void DestroyTrackedJellies()
        {
            if (tracked.Count == 0) return;

            var snapshot = new List<OomaJellyTracker>(tracked);
            int destroyed = 0;
            foreach (var tracker in snapshot)
            {
                if (tracker != null && tracker.gameObject != null)
                {
                    Object.Destroy(tracker.gameObject);
                    destroyed++;
                }
            }

            if (destroyed > 0)
                Plugin.Logger.LogInfo($"Destroyed {destroyed} cross-scene ooma jelly(s).");

            tracked.Clear();
        }

        public class OomaJellyTracker : MonoBehaviour
        {
            private void Awake() { tracked.Add(this); }
            private void OnDestroy() { tracked.Remove(this); }
        }
    }
}
