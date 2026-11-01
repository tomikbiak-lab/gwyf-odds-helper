using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Mirror;
using TMPro;
using UnityEngine;

namespace OddsHelper
{
    // Builds the panel text (TextMeshPro rich text) for the game you are standing at.
    internal static class Odds
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly PropertyInfo EstimatedValueProp = typeof(GameBase).GetProperty("EstimatedValue", Reflect.Flags);

        private const string Green = "#5dff7a";
        private const string Yellow = "#ffd75d";
        private const string Red = "#ff6b6b";
        private const string Gray = "#b0b0b0";
        private const string Purple = "#d08cff";

        internal static string Title(GameBase game) =>
            string.IsNullOrEmpty(game.GameName) ? game.GameType.ToString() : game.GameName;

        internal static string Describe(GameBase game)
        {
            var sb = new StringBuilder();
            double ev = EstimatedValue(game);
            bool solo = Predictor.IsSolo;

            switch (game)
            {
                case Roulette r: Roulette(sb, r, solo); break;
                case MoneyWheel w: MoneyWheel(sb, w, ev, solo); break;
                case WheelOfFortune w: WheelOfFortune(sb, w, ev, solo); break;
                case Crash c: Crash(sb, c, solo); break;
                case Minesweeper m: Minesweeper(sb, m, ev, solo); break;
                case DragonTower d: DragonTower(sb, d, ev, solo); break;
                case HiLoGame h: HiLo(sb, h, ev, solo); break;
                case DuckRace d: DuckRace(sb, d, ev); break;
                case Keno k: Keno(sb, k, ev); break;
                case Blackjack b: Blackjack(sb, b, ev, solo); break;
                case CrossyRoad c: CrossyRoad(sb, c); break;
                case Plinko p: Plinko(sb, p, ev); break;
                default:
                    sb.Append(Dim("Table payout factor: " + Num(ev) + "×")).Append('\n');
                    break;
            }

            if (!solo && NetworkServer.active == false && NetworkClient.active)
                sb.Append(Dim("Result prediction only when solo")).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

        // ---------------------------------------------------------------- roulette

        private static readonly Dictionary<string, double> RoulettePayouts = new Dictionary<string, double>
        {
            { "Red", 2 }, { "Black", 2 }, { "Odd", 2 }, { "Even", 2 }, { "Low", 2 }, { "High", 2 },
            { "Dozen1", 3 }, { "Dozen2", 3 }, { "Dozen3", 3 }, { "Column1", 3 }, { "Column2", 3 }, { "Column3", 3 }
        };

        private static readonly Dictionary<string, string> RouletteNames = new Dictionary<string, string>
        {
            { "Red", "red" }, { "Black", "black" }, { "Odd", "odd" }, { "Even", "even" },
            { "Low", "1–18" }, { "High", "19–36" }, { "Dozen1", "1–12" }, { "Dozen2", "13–24" },
            { "Dozen3", "25–36" }, { "Column1", "column 1" }, { "Column2", "column 2" }, { "Column3", "column 3" }
        };

        private static HashSet<int> RouletteNumbers(string option)
        {
            if (int.TryParse(option, out int n))
                return new HashSet<int> { n };

            int[] Static(string field) => Reflect.GetStatic<int[]>(typeof(Roulette), field, new int[0]);
            IEnumerable<int> range(int a, int b) => Enumerable.Range(a, b - a + 1);
            switch (option)
            {
                case "Red": return new HashSet<int>(Static("RedNumbers"));
                case "Black": return new HashSet<int>(Static("BlackNumbers"));
                case "Odd": return new HashSet<int>(range(1, 36).Where(x => x % 2 == 1));
                case "Even": return new HashSet<int>(range(1, 36).Where(x => x % 2 == 0));
                case "Low": return new HashSet<int>(range(1, 18));
                case "High": return new HashSet<int>(range(19, 36));
                case "Dozen1": return new HashSet<int>(range(1, 12));
                case "Dozen2": return new HashSet<int>(range(13, 24));
                case "Dozen3": return new HashSet<int>(range(25, 36));
                case "Column1": return new HashSet<int>(Static("Column1"));
                case "Column2": return new HashSet<int>(Static("Column2"));
                case "Column3": return new HashSet<int>(Static("Column3"));
                default: return new HashSet<int>();
            }
        }

        private static double RoulettePayout(string option) =>
            int.TryParse(option, out _) ? 36 : RoulettePayouts.TryGetValue(option, out double p) ? p : 0;

        private static string RouletteLabel(string option) =>
            int.TryParse(option, out _) ? "number " + option : RouletteNames.TryGetValue(option, out string n) ? n : option;

        private static void Roulette(StringBuilder sb, Roulette game, bool solo)
        {
            Tracker.RouletteBets.TryGetValue(game, out var bets);
            int? predicted = null;
            if (solo && int.TryParse(Predictor.PredictWheel(game), out int p))
                predicted = p;

            if (bets == null || bets.Count == 0)
            {
                sb.Append("Place a bet to see your odds\n");
                sb.Append(Dim("Number 2.7 % (36×) · colour 48.6 % (2×)")).Append('\n');
                AppendPrediction(sb, predicted?.ToString());
                return;
            }

            double total = bets.Values.Sum();
            var winning = new HashSet<int>();
            double expected = 0;
            double predictedReturn = 0;

            foreach (var bet in bets.OrderByDescending(b => b.Value))
            {
                var numbers = RouletteNumbers(bet.Key);
                double payout = RoulettePayout(bet.Key);
                double chance = numbers.Count / 37.0;
                winning.UnionWith(numbers);
                expected += bet.Value * payout * chance;
                if (predicted.HasValue && numbers.Contains(predicted.Value))
                    predictedReturn += bet.Value * payout;

                sb.Append(RouletteLabel(bet.Key)).Append(": <b>").Append(Pct(chance)).Append("</b> → ").Append(X(payout)).Append('\n');
            }

            double anyWin = winning.Count / 37.0;
            sb.Append("Chance to win something: <b>").Append(Colored(Pct(anyWin), anyWin >= 0.5 ? Green : anyWin >= 0.2 ? Yellow : Red)).Append("</b>\n");
            sb.Append(Dim("Return " + Pct(expected / total))).Append('\n');

            if (predicted.HasValue)
            {
                AppendPrediction(sb, predicted.Value.ToString());
                AppendOutcome(sb, predictedReturn, total);
            }
        }

        // ---------------------------------------------------------------- wheels

        private static readonly Dictionary<string, double> MoneyWheelPayouts = new Dictionary<string, double>
        {
            { "Green", 2 }, { "Blue", 3 }, { "Red", 5 }, { "Orange", 10 }
        };

        private static void MoneyWheel(StringBuilder sb, MoneyWheel game, double ev, bool solo)
        {
            var counts = WheelCounts(game, out int total);
            string chosen = ChosenMoneyWheelOption(game);

            if (total > 0 && chosen != null && MoneyWheelPayouts.TryGetValue(chosen, out double payout))
            {
                counts.TryGetValue(chosen, out int n);
                double chance = (double)n / total;
                sb.Append("Bet: ").Append(ColorName(chosen)).Append(" → ").Append(X(payout * ev)).Append('\n');
                sb.Append("Win chance: <b>").Append(Pct(chance)).Append("</b>\n");
                sb.Append(Dim("Return " + Pct(chance * payout * ev))).Append('\n');

                string best = MoneyWheelPayouts.Keys.OrderByDescending(k => (counts.TryGetValue(k, out int c) ? c : 0) * MoneyWheelPayouts[k]).First();
                if (best != chosen)
                    sb.Append(Dim("Best colour: ")).Append(ColorName(best)).Append('\n');
            }

            if (solo)
            {
                string predicted = Predictor.PredictWheel(game);
                AppendPrediction(sb, predicted == null ? null : ColorName(predicted));
                if (predicted != null && chosen != null)
                    AppendOutcome(sb, predicted == chosen ? MoneyWheelPayouts[chosen] * ev : 0, 1);
            }
        }

        private static string ChosenMoneyWheelOption(MoneyWheel game)
        {
            var indicator = Reflect.Get<Transform>(game, "betIndicator", null);
            var buttons = Reflect.Get<MoneyWheelButton[]>(game, "buttons", null);
            if (indicator == null || buttons == null || buttons.Length == 0)
                return null;
            return buttons.OrderBy(b => (b.transform.position - indicator.position).sqrMagnitude).First().betOption;
        }

        private static void WheelOfFortune(StringBuilder sb, WheelOfFortune game, double ev, bool solo)
        {
            var counts = WheelCounts(game, out int total);
            counts.TryGetValue("Spin", out int spins);
            int finals = total - spins;
            if (finals > 0)
            {
                double sum = counts.Where(c => c.Key != "Spin").Sum(c => ParseNum(c.Key) * c.Value);
                double win = counts.Where(c => c.Key != "Spin" && ParseNum(c.Key) * ev > 1).Sum(c => c.Value) / (double)finals;
                sb.Append("Profit chance: <b>").Append(Pct(win)).Append("</b>\n");
                sb.Append(Dim("Return " + Pct(sum / finals * ev))).Append('\n');
            }

            if (solo)
            {
                string predicted = Predictor.PredictWheel(game);
                double value = ParseNum(predicted);
                AppendPrediction(sb, predicted == null ? null : X(value * ev));
                if (predicted != null)
                    AppendOutcome(sb, value * ev, 1);
            }
        }

        private static Dictionary<string, int> WheelCounts(Component game, out int total)
        {
            var counts = new Dictionary<string, int>();
            total = 0;
            foreach (var r in game.GetComponentsInChildren<WheelResult>(true))
            {
                if (r == null || string.IsNullOrEmpty(r.result)) continue;
                counts.TryGetValue(r.result, out int n);
                counts[r.result] = n + 1;
                total++;
            }
            return counts;
        }

        // ---------------------------------------------------------------- crash

        private static void Crash(StringBuilder sb, Crash game, bool solo)
        {
            float instant = Reflect.Get<float>(game, "instantCrashChance", 0.1f);
            float maxPoint = Reflect.Get<float>(game, "maxPoint", 100f);
            float now = Reflect.Get<float>(game, "_multiplier", 1f);
            bool started = Reflect.Get<bool>(game, "_hasStarted", false);
            bool crashed = Reflect.Get<bool>(game, "_hasCrashed", false);
            bool running = started && !crashed && now > 1.0f;

            double Reach(double m) => m >= maxPoint ? 0.0 : Math.Min(1.0 - instant, 1.0 / m);

            if (running)
            {
                double pNow = Reach(now);
                sb.Append("Now <b>").Append(X(now)).Append("</b> – chance to reach:\n");
                foreach (double t in NextTargets(now))
                    sb.Append("  ").Append(X(t)).Append(": <b>").Append(Pct(Reach(t) / pNow)).Append("</b>\n");
            }
            else
            {
                sb.Append("Chance the round reaches:\n");
                foreach (double t in new[] { 1.5, 2, 3, 5, 10 })
                    sb.Append("  ").Append(X(t)).Append(": <b>").Append(Pct(Reach(t))).Append("</b>\n");
                sb.Append(Dim("Cash out at " + X(1.0 / (1.0 - instant)) + " or later = 100 % return")).Append('\n');
            }

            if (solo && Tracker.CrashPoints.TryGetValue(game, out float point) && started)
            {
                sb.Append(Colored("Crashes at: <b>" + X(point) + "</b>", Purple)).Append('\n');
                if (running)
                    sb.Append(point - now > 0.15f ? Colored("Keep holding", Green) : Colored("CASH OUT NOW!", Red)).Append('\n');
            }
        }

        private static IEnumerable<double> NextTargets(double now)
        {
            var steps = new[] { 1.2, 1.5, 2, 3, 5, 10, 25, 50 };
            return steps.Where(s => s > now + 0.05).Take(4);
        }

        // ---------------------------------------------------------------- minesweeper

        private static void Minesweeper(StringBuilder sb, Minesweeper game, double ev, bool solo)
        {
            int tiles = Reflect.Count(Reflect.Get<object>(game, "tiles", null));
            int mines = Reflect.Get<int>(game, "_currentMineCount", 3);
            if (tiles <= 0)
                return;
            int safe = tiles - mines;

            Tracker.MinesMultiplier.TryGetValue(game, out double mult);
            int revealed = RevealedFromMultiplier(mult, tiles, mines, ev);

            sb.Append(mines).Append(" mines · revealed ").Append(revealed).Append('\n');
            if (revealed < safe)
            {
                double next = (double)(safe - revealed) / (tiles - revealed);
                sb.Append("Next tile safe: <b>").Append(Colored(Pct(next), next >= 0.8 ? Green : next >= 0.6 ? Yellow : Red)).Append("</b>\n");
                sb.Append(Dim("Payout after it: " + X(ev / SurviveChance(revealed + 1, tiles, mines)))).Append('\n');
            }

            if (solo)
            {
                var mineIdx = Predictor.MineIndexes(game);
                if (mineIdx.Count > 0)
                {
                    int width = (int)Math.Round(Math.Sqrt(tiles));
                    sb.Append(Colored("Mines:", Purple)).Append('\n');
                    if (width * width == tiles)
                    {
                        for (int row = 0; row < width; row++)
                        {
                            sb.Append("<mspace=0.9em>");
                            for (int col = 0; col < width; col++)
                                sb.Append(mineIdx.Contains(row * width + col) ? Colored("X", Red) : Colored("·", Green));
                            sb.Append("</mspace>\n");
                        }
                    }
                    else
                    {
                        sb.Append(string.Join(", ", mineIdx.Select(i => (i + 1).ToString()))).Append('\n');
                    }
                }
            }
        }

        private static double SurviveChance(int k, int tiles, int mines)
        {
            double p = 1.0;
            for (int i = 0; i < k; i++)
                p *= (double)(tiles - mines - i) / (tiles - i);
            return p;
        }

        private static int RevealedFromMultiplier(double mult, int tiles, int mines, double ev)
        {
            if (mult <= 1.0001)
                return 0;
            int best = 0;
            double bestDiff = double.MaxValue;
            for (int k = 1; k <= tiles - mines; k++)
            {
                double diff = Math.Abs(ev / SurviveChance(k, tiles, mines) - mult);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = k;
                }
            }
            return best;
        }

        // ---------------------------------------------------------------- dragon tower

        private static void DragonTower(StringBuilder sb, DragonTower game, double ev, bool solo)
        {
            Tracker.TowerFloor.TryGetValue(game, out int floor);
            int floors = Reflect.Count(Reflect.Get<object>(game, "floors", null));

            if (floor >= 0 && floor < floors)
            {
                sb.Append("Floor ").Append(floor + 1).Append(" – pass chance: <b>75 %</b>\n");
                sb.Append(Dim("Payout after it: " + X(ev / Math.Pow(0.75, floor + 1)))).Append('\n');
            }
            else
            {
                sb.Append("Each floor: <b>75 %</b>\n");
            }

            if (solo)
            {
                var eggs = Predictor.TowerEggs(game);
                if (eggs.Count > 0 && eggs.Any(e => e.egg >= 0))
                {
                    sb.Append(Colored("Eggs (from bottom):", Purple)).Append('\n');
                    for (int i = 0; i < eggs.Count; i++)
                    {
                        if (floor >= 0 && i < floor) continue;
                        sb.Append("<mspace=0.9em>").Append(i + 1).Append(": ");
                        for (int b = 0; b < eggs[i].buttons; b++)
                            sb.Append(b == eggs[i].egg ? Colored("X", Red) : Colored("·", Green));
                        sb.Append("</mspace>\n");
                    }
                }
            }
        }

        // ---------------------------------------------------------------- hi-lo

        private static readonly Regex MultiplierText = new Regex(@"([0-9]+(?:[.,][0-9]+)?)\s*x", RegexOptions.IgnoreCase);

        private static void HiLo(StringBuilder sb, HiLoGame game, double ev, bool solo)
        {
            var text = Reflect.Get<TextMeshPro>(game, "multiplierText", null);
            var match = text != null ? MultiplierText.Match(text.text ?? "") : Match.Empty;
            if (match.Success)
            {
                double mult = ParseNum(match.Groups[1].Value.Replace(',', '.'));
                if (mult > 0)
                {
                    double chance = Math.Min(1.0, ev / mult);
                    sb.Append("Bet ").Append(X(mult)).Append(" – chance: <b>").Append(Colored(Pct(chance), chance >= 0.5 ? Green : chance >= 0.25 ? Yellow : Red)).Append("</b>\n");
                }
            }
            else
            {
                sb.Append("Chance = ").Append(Num(ev)).Append(" ÷ multiplier\n");
            }

            if (solo)
            {
                float roll = Predictor.PredictHiLoRoll(game);
                var slider = Reflect.Get<object>(game, "hiLoSlider", null);
                float value = Reflect.Get<float>(slider, "currentValue", 0.5f);
                bool isOver = Reflect.Get<bool>(game, "_isOver", false);
                bool win = isOver ? roll >= value : roll <= value;
                sb.Append(Colored("Result: <b>" + (roll * 100).ToString("0.0", Inv) + "</b>", Purple)).Append('\n');
                sb.Append(win ? Colored("→ YOU WIN", Green) : Colored("→ you lose, " + (isOver ? "pick Under" : "pick Over") + " or move the slider", Red)).Append('\n');
            }
        }

        // ---------------------------------------------------------------- duck race, keno, crossy road, plinko

        private static void DuckRace(StringBuilder sb, DuckRace game, double ev)
        {
            int ducks = Reflect.Count(Reflect.Get<object>(game, "ducks", null));
            if (ducks <= 0)
                return;
            double chance = 1.0 / ducks;
            if (Tracker.DuckBets.TryGetValue(game, out int duck))
                sb.Append("Your duck #").Append(duck + 1).Append('\n');
            sb.Append("Win chance: <b>").Append(Pct(chance)).Append("</b> → ").Append(X(4 * ev)).Append('\n');
            sb.Append(Dim("Return " + Pct(chance * 4 * ev))).Append('\n');
        }

        private static void Keno(StringBuilder sb, Keno game, double ev)
        {
            int n = (Reflect.Prop(game, "Buttons") as Array)?.Length ?? 0;
            int m = Reflect.Get<int>(game, "diamondCount", 10);
            Tracker.KenoPicks.TryGetValue(game, out var picks);
            int k = picks?.Count ?? 0;
            if (n <= 0 || k == 0)
            {
                sb.Append("Pick numbers to see your odds\n");
                return;
            }

            var mults = Reflect.Call(game, "GetBalancedMultipliers", n, k, m) as Dictionary<int, double>;
            double anyHit = 1 - Hyper(0, n, k, m);
            double profit = 0;
            for (int h = 0; h <= k; h++)
                if (mults != null && mults.TryGetValue(h, out double x) && x * ev > 1)
                    profit += Hyper(h, n, k, m);

            sb.Append(k).Append(" numbers – at least 1 hit: <b>").Append(Pct(anyHit)).Append("</b>\n");
            sb.Append("Profit chance: <b>").Append(Colored(Pct(profit), profit >= 0.4 ? Green : profit >= 0.2 ? Yellow : Red)).Append("</b>\n");
        }

        private static double Hyper(int h, int n, int k, int m) => Choose(k, h) * Choose(n - k, m - h) / Choose(n, m);

        private static double Choose(int n, int r)
        {
            if (r < 0 || r > n) return 0;
            double result = 1;
            for (int i = 1; i <= r; i++)
                result = result * (n - r + i) / i;
            return result;
        }

        private static void CrossyRoad(StringBuilder sb, CrossyRoad game)
        {
            var chanceFn = typeof(CrossyRoad).GetMethod("GetCrashChanceForStep", Reflect.Flags);
            double current = Reflect.Get<double>(game, "currentMultiplier", 0);
            int step = Reflect.Get<int>(game, "_currentStep", 0);
            if (current > 0)
                sb.Append("Multiplier: <b>").Append(X(current)).Append("</b>\n");
            if (chanceFn != null)
            {
                float risk = (float)chanceFn.Invoke(game, new object[] { step });
                sb.Append("Next step safe: <b>").Append(Colored(Pct(1 - risk), risk <= 0.2f ? Green : risk <= 0.4f ? Yellow : Red)).Append("</b>\n");
            }
        }

        private static void Plinko(StringBuilder sb, Plinko game, double ev)
        {
            var mults = Reflect.Get<double[]>(game, "slotMultipliers", null);
            if (mults != null)
                sb.Append(string.Join(" ", mults.Select(m => X(m * ev)))).Append('\n');
            sb.Append(Dim("Physics – exact odds can't be computed")).Append('\n');
        }

        // ---------------------------------------------------------------- blackjack

        private static void Blackjack(StringBuilder sb, Blackjack game, double ev, bool solo)
        {
            var hand = Reflect.Get<IList<CardData>>(game, "playerHand", null);
            var dealer = Reflect.Get<IList<CardData>>(game, "dealerHand", null);
            int decks = Math.Max(1, Reflect.Get<int>(game, "numberOfDecks", 1));

            if (hand != null && hand.Count > 0)
            {
                int total = HandValue(hand, out bool soft);
                sb.Append("You have <b>").Append(soft ? "soft " : "").Append(total).Append("</b>");
                if (dealer != null && dealer.Count > 0)
                    sb.Append(" · dealer shows <b>").Append(CardName(dealer[0])).Append("</b>");
                sb.Append('\n');

                if (total < 21)
                {
                    // Shoe minus the cards you can see (your hand + dealer's face-up card).
                    var seen = new List<CardData>(hand);
                    if (dealer != null && dealer.Count > 0) seen.Add(dealer[0]);
                    double bust = BustChance(hand, seen, decks);
                    sb.Append("If you hit, bust chance: <b>").Append(Colored(Pct(bust), bust <= 0.3 ? Green : bust <= 0.55 ? Yellow : Red)).Append("</b>\n");
                }
            }
            else
            {
                sb.Append("Win ").Append(X(2 * ev)).Append(" · blackjack ").Append(X(3 * ev)).Append('\n');
            }

            if (solo)
            {
                var next = Predictor.NextCards(game, 3);
                if (next.Count > 0)
                    sb.Append(Colored("Next cards: <b>" + string.Join(", ", next.Select(CardName)) + "</b>", Purple)).Append('\n');
            }
        }

        private static int HandValue(IList<CardData> hand, out bool soft)
        {
            int total = 0, aces = 0;
            foreach (var c in hand)
            {
                total += c.GetBlackjackValue();
                if (c.Rank == Rank.Ace) aces++;
            }
            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }
            soft = aces > 0;
            return total;
        }

        private static double BustChance(IList<CardData> hand, List<CardData> seen, int decks)
        {
            var remaining = new Dictionary<Rank, int>();
            foreach (Rank r in Enum.GetValues(typeof(Rank)))
                remaining[r] = 4 * decks;
            foreach (var c in seen)
                remaining[c.Rank] = Math.Max(0, remaining[c.Rank] - 1);

            int left = remaining.Values.Sum();
            if (left == 0) return 0;

            int busting = 0;
            foreach (var pair in remaining)
            {
                var test = new List<CardData>(hand) { new CardData(default, pair.Key) };
                if (HandValue(test, out _) > 21)
                    busting += pair.Value;
            }
            return (double)busting / left;
        }

        private static string CardName(CardData c)
        {
            switch (c.Rank)
            {
                case Rank.Ace: return "A";
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                default: return ((int)c.Rank).ToString();
            }
        }

        // ---------------------------------------------------------------- formatting

        private static void AppendPrediction(StringBuilder sb, string result)
        {
            if (result != null)
                sb.Append(Colored("Result: <b>" + result + "</b>", Purple)).Append('\n');
        }

        private static void AppendOutcome(StringBuilder sb, double returned, double staked)
        {
            if (returned > staked)
                sb.Append(Colored("→ YOU WIN", Green)).Append('\n');
            else if (returned > 0)
                sb.Append(Colored("→ partial return, you still lose", Yellow)).Append('\n');
            else
                sb.Append(Colored("→ you lose, bet differently", Red)).Append('\n');
        }

        private static double EstimatedValue(GameBase game)
        {
            try { return EstimatedValueProp != null ? (double)EstimatedValueProp.GetValue(game) : 1.0; }
            catch { return 1.0; }
        }

        private static string ColorName(string key)
        {
            switch (key)
            {
                case "Green": return Colored("green", Green);
                case "Blue": return Colored("blue", "#6bb8ff");
                case "Red": return Colored("red", Red);
                case "Orange": return Colored("orange", "#ffae42");
                default: return key;
            }
        }

        private static string Colored(string text, string color) => "<color=" + color + ">" + text + "</color>";
        private static string Dim(string text) => "<size=85%>" + Colored(text, Gray) + "</size>";
        private static double ParseNum(string s) => double.TryParse(s, NumberStyles.Float, Inv, out double v) ? v : 0;
        private static string Pct(double p) => (p * 100).ToString(p < 0.01 ? "0.##" : "0.#", Inv) + " %";
        private static string X(double m) => m.ToString("0.##", Inv) + "×";
        private static string Num(double v) => v.ToString("0.###", Inv);
    }
}
