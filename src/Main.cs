using System;
using HarmonyLib;
using UnityModManagerNet;

namespace AdofaiPro
{
    /// <summary>
    /// Mod 入口。UMM 读取 Info.json 的 EntryMethod 调用 <see cref="Load"/>。
    /// </summary>
    public static class Main
    {
        internal const string HarmonyId = "argvchs.AdofaiPro";

        internal static UnityModManager.ModEntry Mod { get; private set; }

        private static Harmony harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            modEntry.OnToggle = OnToggle;

            // 尽早尝试注册事件；若此时 GCS.levelEventsInfo 还没建好，
            // SetupLevelEventsInfo 的后置补丁会补一次。
            CustomEvent.EnsureRegistered();

            Log("AdofaiPro loaded.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                if (harmony == null)
                {
                    harmony = new Harmony(HarmonyId);
                    harmony.PatchAll(typeof(Main).Assembly);
                }

                CustomEvent.EnsureRegistered();
                Log("AdofaiPro enabled.");
            }
            else
            {
                try
                {
                    harmony?.UnpatchAll(HarmonyId);
                }
                catch (Exception e)
                {
                    Log("Unpatch failed: " + e);
                }

                CustomEvent.Unregister();
                Log("AdofaiPro disabled.");
            }

            return true;
        }

        internal static void Log(string message)
        {
            Mod?.Logger.Log(message);
        }

        internal static void Error(string message)
        {
            if (Mod != null)
            {
                Mod.Logger.Error(message);
            }
            else
            {
                Console.WriteLine("[AdofaiPro] " + message);
            }
        }
    }
}
