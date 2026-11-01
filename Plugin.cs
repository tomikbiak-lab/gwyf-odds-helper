using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OddsHelper
{
    // Shows the odds of the bet you placed at the casino game you are standing at,
    // in a card in the bottom-left corner. Odds come only from game rules and synced
    // values every client has. Result prediction runs only when you are alone in the lobby.
    [BepInPlugin("takto.gwyf.oddshelper", "Odds Helper", "2.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private ConfigEntry<Key> _toggleKey;
        private ConfigEntry<float> _range;

        private bool _visible = true;
        private GameBase _target;
        private float _nextScan;
        private float _nextRefresh;
        private string _text = "";

        private string _title = "";

        private TextMeshProUGUI _ticketLabel;
        private HudPanel _hud;

        private void Awake()
        {
            Log = Logger;
            _toggleKey = Config.Bind("General", "ToggleKey", Key.F8, "Key that shows/hides the panel");
            _range = Config.Bind("General", "Range", 6f, "How far (m) to look for the nearest game");
            Tracker.Install(new Harmony("takto.gwyf.oddshelper"));
            Logger.LogInfo("Odds Helper 2.2 loaded. Toggle the panel with " + _toggleKey.Value);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey.Value].wasPressedThisFrame)
                _visible = !_visible;

            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.5f;
                _target = _visible ? FindNearestGame() : null;
                EnsureHud();
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.2f;
                try
                {
                    _title = _target != null ? Odds.Title(_target) : "";
                    _text = _target != null ? Odds.Describe(_target) : "";
                }
                catch (Exception e)
                {
                    _text = "Error: " + e.Message;
                    Logger.LogWarning(e);
                }
            }

            UpdateHud();
        }

        // ---------------------------------------------------------------- HUD card, bottom-left

        private void EnsureHud()
        {
            if (_hud == null)
                _hud = new HudPanel(_toggleKey.Value.ToString());
            if (_ticketLabel != null)
                return;

            // The ticket counter gives us the game's own font, and tells us when the HUD is hidden (menus).
            var display = FindFirstObjectByType<MoneyDisplayAndFeedbacks>();
            _ticketLabel = display != null ? Reflect.Get<TextMeshProUGUI>(display, "ticketBalanceLabel", null) : null;
            if (_ticketLabel != null)
                _hud.SetFont(_ticketLabel.font);
        }

        private void UpdateHud()
        {
            if (_hud == null)
                return;

            bool show = _visible && _target != null && !string.IsNullOrEmpty(_text)
                && (_ticketLabel == null || _ticketLabel.isActiveAndEnabled);
            if (show)
                _hud.Show(_title, _text, Predictor.IsSolo);
            else
                _hud.Hide();
        }

        private GameBase FindNearestGame()
        {
            var cam = Camera.main;
            if (cam == null)
                return null;

            Vector3 pos = cam.transform.position;
            float best = _range.Value * _range.Value;
            GameBase nearest = null;
            foreach (var game in FindObjectsByType<GameBase>(FindObjectsSortMode.None))
            {
                if (game == null || !game.isActiveAndEnabled)
                    continue;
                float d = (game.transform.position - pos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = game;
                }
            }
            return nearest;
        }
    }
}
