// Synthetic startup fault tests. These API substitutes do not prove Harmony or Unity runtime behavior.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BepInPlugin : Attribute
    {
        public BepInPlugin(string id, string name, string version) { }
    }
    public class BaseUnityPlugin { public readonly TestLog Logger = new TestLog(); }
    public sealed class TestLog
    {
        public int Errors;
        public void LogInfo(object message) { }
        public void LogError(object message) { Errors++; }
    }
}

namespace HarmonyLib
{
    public sealed class HarmonyMethod { public HarmonyMethod(MethodInfo method) { } }
    public sealed class Harmony
    {
        public static readonly Dictionary<string, List<string>> Applied = new Dictionary<string, List<string>>();
        public static int FailAt, Calls;
        private readonly string owner;
        public Harmony(string id) { owner = id; }
        public void Patch(MethodInfo target, HarmonyMethod prefix)
        {
            Calls++;
            if (Calls == FailAt) throw new InvalidOperationException("Injected patch failure");
            if (!Applied.ContainsKey(owner)) Applied[owner] = new List<string>();
            Applied[owner].Add(target.Name);
        }
        public void UnpatchSelf() { Applied.Remove(owner); }
    }
}

// The field and method shapes being validated are deliberately represented here.
public sealed class MicManager
{
    public delegate void MicActions();
    private bool checkForMic, listenToPlayer, checkForSubDBLevel;
    private MicActions MicFound;
    public int currentDBLevel;
    public int GetInstanceID() { return 1; }
    private void prepMics() { }
    private void testMic() { }
    private void startRec() { }
    private void playerListen() { }
    private void Update() { }
#if BAD_SIGNATURE
    private void OnAudioFilterRead(double[] samples, int channels) { }
#else
    private void OnAudioFilterRead(float[] samples, int channels) { }
#endif
}

internal static class StartupSafetyTests
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
    }
    private static WTTG2Mods.SilentMic Start(int failAt)
    {
        HarmonyLib.Harmony.Applied.Clear();
        HarmonyLib.Harmony.Applied["other.mod"] = new List<string> { "foreign patch" };
        HarmonyLib.Harmony.Calls = 0;
        HarmonyLib.Harmony.FailAt = failAt;
        WTTG2Mods.SilentMic mod = new WTTG2Mods.SilentMic();
        typeof(WTTG2Mods.SilentMic).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mod, null);
        return mod;
    }
    public static void Main()
    {
#if BAD_SIGNATURE
        WTTG2Mods.SilentMic incompatible = Start(0);
        Check(HarmonyLib.Harmony.Calls == 0, "Incompatible audio signature must be rejected before patching");
        Check(incompatible.Logger.Errors > 0, "Startup rejection must be logged");
#else
        WTTG2Mods.SilentMic compatible = Start(0);
        Check(HarmonyLib.Harmony.Applied[WTTG2Mods.SilentMic.Guid].Count == 6, "Compatible layout installs all six patches");
        Check(compatible.Logger.Errors == 0, "Successful startup must not report isolation failure");
        for (int failAt = 1; failAt <= 6; failAt++)
        {
            WTTG2Mods.SilentMic failed = Start(failAt);
            Check(!HarmonyLib.Harmony.Applied.ContainsKey(WTTG2Mods.SilentMic.Guid), "Partial patches must be rolled back after failure " + failAt);
            Check(HarmonyLib.Harmony.Applied["other.mod"].Count == 1, "Rollback must preserve foreign patches after failure " + failAt);
            Check(failed.Logger.Errors > 0, "Failure must be logged after failure " + failAt);
        }
#endif
        Console.WriteLine("STARTUP SAFETY PASS: " + checks + " assertions");
    }
}
