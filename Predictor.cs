using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace OddsHelper
{
    // Solo-only: replays the host's own seeded RNG to say what will happen.
    // Never runs while anyone else is connected to the lobby.
    internal static class Predictor
    {
        internal static bool IsSolo => NetworkServer.active && NetworkServer.connections.Count <= 1;

        private static System.Random SeededRandom(GameBase game) => (System.Random)Reflect.Call(game, "GetSeededRandom", 0);

        // ---------------------------------------------------------------- wheels (roulette, money wheel, wheel of fortune)

        internal static string PredictWheel(GameBase game)
        {
            var wheel = Reflect.Get<Wheel>(game, "wheel", null);
            if (wheel == null)
                return null;

            string result = SimulateSpin(wheel, SeededRandom(game));

            // Wheel of Fortune re-spins on "Spin" with the next game turn.
            if (game is WheelOfFortune && result == "Spin")
            {
                int turn = Reflect.Get<int>(game, "gameTurn", 0);
                try
                {
                    for (int i = 1; i <= 5 && result == "Spin"; i++)
                    {
                        Reflect.Set(game, "gameTurn", turn + i);
                        result = SimulateSpin(wheel, SeededRandom(game));
                    }
                }
                finally
                {
                    Reflect.Set(game, "gameTurn", turn);
                }
            }

            if (result != null)
                Tracker.WheelPredictions[wheel] = result;
            return result;
        }

        private static string SimulateSpin(Wheel wheel, System.Random rng)
        {
            int minTurns = Reflect.Get<int>(wheel, "minTurnAmount", 3);
            bool reverse = Reflect.Get<bool>(wheel, "spinDirection", false);
            var wheelTransform = Reflect.Get<Transform>(wheel, "wheelTransform", null);
            var selector = Reflect.Get<Transform>(wheel, "resultSelector", null);
            var resultsParent = Reflect.Get<Transform>(wheel, "resultsParent", null);
            if (wheelTransform == null || selector == null || resultsParent == null)
                return null;

            var rotations = new Dictionary<Transform, Quaternion>();
            var positions = new Dictionary<Transform, Vector3>();

            if (wheel is RouletteWheel)
            {
                // Same draw order as RouletteWheel.SpinTheWheel: wheel first, then ball.
                const float slot = 9.72973f;
                float wheelAngle = minTurns * 360f + rng.Next(0, 37) * slot;
                float ballAngle = minTurns * 360f + rng.Next(0, 37) * slot;
                if (reverse)
                    wheelAngle *= -1f;

                rotations[wheelTransform] = Quaternion.Euler(0f, 0f, -wheelAngle);
                var ballWheel = Reflect.Get<Transform>(wheel, "ballWheel", null);
                if (ballWheel != null)
                    rotations[ballWheel] = Quaternion.Euler(0f, 0f, -ballAngle);
                var ball = Reflect.Get<Transform>(wheel, "ball", null);
                if (ball != null)
                    positions[ball] = new Vector3(0f, 0.9f, 0f);
            }
            else
            {
                float angle = minTurns * 360f + (float)(rng.NextDouble() * 360.0);
                if (reverse)
                    angle *= -1f;
                rotations[wheelTransform] = Quaternion.Euler(0f, 0f, -angle);
            }

            Vector3 pointer = FinalPosition(selector, rotations, positions);
            string best = null;
            float bestDist = float.MaxValue;
            foreach (var r in resultsParent.GetComponentsInChildren<WheelResult>(true))
            {
                float d = (FinalPosition(r.transform, rotations, positions) - pointer).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = r.result;
                }
            }
            return best;
        }

        // World position of a transform after the spin animation, with some local transforms overridden.
        private static Vector3 FinalPosition(Transform t, Dictionary<Transform, Quaternion> rotations, Dictionary<Transform, Vector3> positions)
        {
            Vector3 p = Vector3.zero;
            for (var cur = t; cur != null; cur = cur.parent)
            {
                Quaternion rot = rotations.TryGetValue(cur, out var r) ? r : cur.localRotation;
                Vector3 pos = positions.TryGetValue(cur, out var lp) ? lp : cur.localPosition;
                p = pos + rot * Vector3.Scale(cur.localScale, p);
            }
            return p;
        }

        // ---------------------------------------------------------------- other games

        internal static float PredictHiLoRoll(HiLoGame game) => (float)SeededRandom(game).NextDouble();

        internal static List<int> MineIndexes(Minesweeper game)
        {
            var set = Reflect.Get<HashSet<int>>(game, "_mineIndexes", null);
            var list = set != null ? new List<int>(set) : new List<int>();
            list.Sort();
            return list;
        }

        internal static List<(int egg, int buttons)> TowerEggs(DragonTower game)
        {
            var result = new List<(int, int)>();
            var floors = Reflect.Get<System.Collections.IList>(game, "floors", null);
            if (floors == null)
                return result;
            foreach (var floor in floors)
                result.Add((Reflect.Get<int>(floor, "eggIndex", -1), Reflect.Count(Reflect.Get<object>(floor, "buttons", null))));
            return result;
        }

        internal static List<CardData> NextCards(Blackjack game, int count)
        {
            var deck = Reflect.Get<List<CardData>>(game, "deck", null);
            var result = new List<CardData>();
            if (deck != null)
                for (int i = 0; i < deck.Count && i < count; i++)
                    result.Add(deck[i]);
            return result;
        }
    }
}
