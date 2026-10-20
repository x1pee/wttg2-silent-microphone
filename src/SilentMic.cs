using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace WTTG2Mods
{
    [BepInPlugin(Guid, "WTTG2 Silent Microphone", "1.0.1")]
    public sealed class SilentMic : BaseUnityPlugin
    {
        public const string Guid = "pmxp.wttg2.silent-microphone";
        private static readonly HashSet<int> Ready = new HashSet<int>();

        private void Awake()
        {
            Harmony harmony = null;
            try
            {
                // Deliberately no Options.Mic writes and no Windows device/permission changes.
                // Validate the exact installed layout before applying any patches.
                Type type = typeof(MicManager);
                string[] flags = { "checkForMic", "listenToPlayer", "checkForSubDBLevel" };
                foreach (string name in flags)
                {
                    FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null || field.FieldType != typeof(bool)) throw new MissingFieldException(type.FullName, name);
                }
                FieldInfo found = type.GetField("MicFound", BindingFlags.Instance | BindingFlags.NonPublic);
                if (found == null || found.FieldType != typeof(MicManager.MicActions)) throw new MissingFieldException("MicFound");
                string[] starts = { "prepMics", "testMic", "startRec", "playerListen" };
                List<MethodInfo> targets = new List<MethodInfo>();
                foreach (string name in starts)
                {
                    MethodInfo target = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (target == null || target.IsStatic || target.ReturnType != typeof(void)) throw new MissingMethodException(type.FullName, name);
                    targets.Add(target);
                }
                MethodInfo update = type.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                MethodInfo filter = type.GetMethod("OnAudioFilterRead", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(float[]), typeof(int) }, null);
                if (update == null || filter == null || update.ReturnType != typeof(void) || filter.ReturnType != typeof(void))
                    throw new MissingMethodException("MicManager update/filter signature");
                harmony = new Harmony(Guid);
                HarmonyMethod prepare = new HarmonyMethod(typeof(SilentMic).GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic));
                foreach (MethodInfo target in targets) harmony.Patch(target, prefix: prepare);
                harmony.Patch(update, prefix: new HarmonyMethod(typeof(SilentMic).GetMethod("ZeroLevel", BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(filter, prefix: new HarmonyMethod(typeof(SilentMic).GetMethod("SilenceFilter", BindingFlags.Static | BindingFlags.NonPublic)));
                Logger.LogInfo("Silent microphone installed: all four game capture paths blocked; synthetic MicFound and zero input level. Options.Mic unchanged. Restart without this DLL to restore capture.");
            }
            catch (Exception error)
            {
                // A failed later patch must not leave a partially modified MicManager.
                // Restrict rollback to our owner ID; never remove another mod's patches.
                if (harmony != null)
                {
                    try { harmony.UnpatchSelf(); }
                    catch (Exception rollbackError)
                    {
                        Logger.LogError("Silent microphone rollback failed; restart the game: " + rollbackError);
                    }
                }
                Logger.LogError("Silent microphone installation failed; microphone isolation is NOT active: " + error);
            }
        }

        private static bool Prepare(MicManager __instance, ref bool ___checkForMic,
            ref bool ___listenToPlayer, ref bool ___checkForSubDBLevel, MicManager.MicActions ___MicFound)
        {
            ___checkForMic = false;
            ___checkForSubDBLevel = false;
            ___listenToPlayer = true;
            __instance.currentDBLevel = 0;
            // The real device is never opened or enumerated. Report a ready, silent input
            // through the normal game's event, avoiding the NoMic fallback.
            if (___MicFound != null && Ready.Add(__instance.GetInstanceID())) ___MicFound();
            return false;
        }

        private static void ZeroLevel(MicManager __instance, ref bool ___checkForMic)
        {
            __instance.currentDBLevel = 0;
            ___checkForMic = false;
        }

        private static bool SilenceFilter(MicManager __instance, float[] __0)
        {
            if (__0 != null) Array.Clear(__0, 0, __0.Length);
            __instance.currentDBLevel = 0;
            return false;
        }
    }
}
