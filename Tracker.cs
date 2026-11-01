using System;
using System.Collections.Generic;
using HarmonyLib;

namespace OddsHelper
{
    // Remembers what you bet on, from the RPCs the host sends to every client.
    internal static class Tracker
    {
        internal static readonly Dictionary<Roulette, Dictionary<string, double>> RouletteBets = new Dictionary<Roulette, Dictionary<string, double>>();
        internal static readonly Dictionary<DuckRace, int> DuckBets = new Dictionary<DuckRace, int>();
        internal static readonly Dictionary<Keno, HashSet<KenoButton>> KenoPicks = new Dictionary<Keno, HashSet<KenoButton>>();
        internal static readonly Dictionary<Minesweeper, double> MinesMultiplier = new Dictionary<Minesweeper, double>();
        internal static readonly Dictionary<DragonTower, int> TowerFloor = new Dictionary<DragonTower, int>();

        // Host-only knowledge, used by the solo predictor.
        internal static readonly Dictionary<Crash, float> CrashPoints = new Dictionary<Crash, float>();
        internal static readonly Dictionary<Wheel, string> WheelPredictions = new Dictionary<Wheel, string>();

        internal static void Install(Harmony harmony)
        {
            Patch(harmony, typeof(RouletteButton), "UserCode_RpcSetBets__Double__Int64__Double__Int64", nameof(OnRouletteBet));
            Patch(harmony, typeof(DuckRace), "UserCode_RpcPlaceBetFeedback__Int32", nameof(OnDuckBet));
            Patch(harmony, typeof(KenoButton), "UserCode_RpcChangeMaterial__Boolean", nameof(OnKenoPick));
            Patch(harmony, typeof(Minesweeper), "UserCode_RpcSetMultiplierText__Double", nameof(OnMinesMultiplier));
            Patch(harmony, typeof(DragonTower), "UserCode_RpcSetInteractableButtons__Int32", nameof(OnTowerFloor));
            Patch(harmony, typeof(Crash), "RaiseRoutine", nameof(OnCrashStart), prefix: true);
            Patch(harmony, typeof(Wheel), "FindResult", nameof(OnWheelResult));
        }

        private static void Patch(Harmony harmony, Type type, string method, string hook, bool prefix = false)
        {
            try
            {
                var original = AccessTools.Method(type, method);
                if (original == null)
                {
                    Plugin.Log.LogWarning($"Not found {type.Name}.{method} – part of the mod will not work");
                    return;
                }
                var patch = new HarmonyMethod(typeof(Tracker), hook);
                harmony.Patch(original, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Patch {type.Name}.{method} failed: {e.Message}");
            }
        }

        private static void OnRouletteBet(RouletteButton __instance, double betM, long betE)
        {
            var roulette = Reflect.Get<Roulette>(__instance, "roulette", null);
            if (roulette == null)
                return;
            if (!RouletteBets.TryGetValue(roulette, out var bets))
                RouletteBets[roulette] = bets = new Dictionary<string, double>();

            double amount = betM * Math.Pow(10, betE);
            if (amount > 0)
                bets[__instance.name] = amount;
            else
                bets.Remove(__instance.name);
        }

        private static void OnDuckBet(DuckRace __instance, int duckIndex) => DuckBets[__instance] = duckIndex;

        private static void OnKenoPick(KenoButton __instance, bool isSelected)
        {
            var keno = Reflect.Get<Keno>(__instance, "keno", null);
            if (keno == null)
                return;
            if (!KenoPicks.TryGetValue(keno, out var picks))
                KenoPicks[keno] = picks = new HashSet<KenoButton>();
            if (isSelected)
                picks.Add(__instance);
            else
                picks.Remove(__instance);
        }

        private static void OnMinesMultiplier(Minesweeper __instance, double multiplier) => MinesMultiplier[__instance] = multiplier;

        private static void OnTowerFloor(DragonTower __instance, int floorIndex) => TowerFloor[__instance] = floorIndex;

        private static void OnCrashStart(Crash __instance, float crashPoint) => CrashPoints[__instance] = crashPoint;

        private static void OnWheelResult(Wheel __instance, string __result)
        {
            if (WheelPredictions.TryGetValue(__instance, out var predicted))
            {
                string verdict = predicted == __result ? "OK" : "MISS";
                Plugin.Log.LogInfo($"[Prediction] {__instance.name}: predicted {predicted}, landed {__result} → {verdict}");
                WheelPredictions.Remove(__instance);
            }
        }
    }
}
