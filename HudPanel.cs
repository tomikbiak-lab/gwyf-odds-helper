using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OddsHelper
{
    // Rounded card in the bottom-left corner of the screen, on its own overlay canvas.
    internal class HudPanel
    {
        private static readonly Color Background = new Color32(0x14, 0x11, 0x1c, 0xeb);
        private static readonly Color Border = new Color32(0xb0, 0x7c, 0xff, 0x70);
        private static readonly Color Accent = new Color32(0xc4, 0x8b, 0xff, 0xff);
        private static readonly Color Muted = new Color32(0x8a, 0x84, 0x96, 0xff);

        private const float Margin = 28f;
        private const float Width = 390f;

        private readonly GameObject _root;
        private readonly RectTransform _card;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _footer;
        private readonly TextMeshProUGUI _badgeText;
        private readonly GameObject _badge;

        private float _alpha;

        internal HudPanel(string toggleKey)
        {
            _root = new GameObject("OddsHelperCanvas");
            Object.DontDestroyOnLoad(_root);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var sprite = RoundedSprite(64, 18);

            // Card: the border colour is the card itself, the dark fill sits 1.5 px inside it.
            _card = NewRect("Card", _root.transform);
            _card.anchorMin = _card.anchorMax = _card.pivot = Vector2.zero;
            _card.anchoredPosition = new Vector2(Margin, Margin);
            _card.sizeDelta = new Vector2(Width, 0f);
            _group = _card.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            AddImage(_card.gameObject, sprite, Border);

            var fill = NewRect("Fill", _card);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(1.5f, 1.5f);
            fill.offsetMax = new Vector2(-1.5f, -1.5f);
            fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            AddImage(fill.gameObject, sprite, Background);

            var layout = _card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 14);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header: game name + "PREDICTION" pill when solo.
            var header = NewRect("Header", _card);
            var row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            _title = NewText("Title", header, 27f, Color.white);
            _title.fontStyle = FontStyles.Bold;
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var badge = NewRect("Badge", header);
            _badge = badge.gameObject;
            AddImage(_badge, sprite, new Color(Accent.r, Accent.g, Accent.b, 0.22f)).pixelsPerUnitMultiplier = 2.5f;
            var pad = _badge.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(10, 10, 4, 4);
            pad.childControlWidth = pad.childControlHeight = true;
            _badgeText = NewText("Text", badge, 15f, Accent);
            _badgeText.fontStyle = FontStyles.Bold;
            _badgeText.characterSpacing = 4f;
            _badgeText.text = "PREDICTION";
            _badgeText.textWrappingMode = TextWrappingModes.NoWrap;

            var divider = NewRect("Divider", _card);
            divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;
            var line = divider.gameObject.AddComponent<Image>();
            line.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            line.raycastTarget = false;

            _body = NewText("Body", _card, 21f, new Color(0.93f, 0.92f, 0.96f));
            _body.lineSpacing = 12f;

            _footer = NewText("Footer", _card, 14f, Muted);
            _footer.alignment = TextAlignmentOptions.Right;
            _footer.text = toggleKey + " hides panel";

            _card.anchoredPosition = new Vector2(Margin, Margin - 12f);
            _group.alpha = 0f;
            _card.gameObject.SetActive(false);
        }

        internal void SetFont(TMP_FontAsset font)
        {
            if (font == null)
                return;
            foreach (var t in new[] { _title, _body, _footer, _badgeText })
                t.font = font;
        }

        internal void Show(string title, string body, bool solo)
        {
            if (_title.text != title) _title.text = title;
            if (_body.text != body) _body.text = body;
            if (_badge.activeSelf != solo) _badge.SetActive(solo);
            Animate(1f);
        }

        internal void Hide() => Animate(0f);

        // Fade and slide up a little when appearing.
        private void Animate(float target)
        {
            if (target > 0f && !_card.gameObject.activeSelf)
                _card.gameObject.SetActive(true);
            if (!_card.gameObject.activeSelf)
                return;

            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime * 6f);
            float eased = 1f - (1f - _alpha) * (1f - _alpha);
            _group.alpha = eased;
            _card.anchoredPosition = new Vector2(Margin, Margin - 12f * (1f - eased));

            if (_alpha <= 0f)
                _card.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- building blocks

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image AddImage(GameObject go, Sprite sprite, Color color)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, Color color)
        {
            var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.richText = true;
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        // White anti-aliased rounded square, 9-sliced so the corners keep their shape at any size.
        private static Sprite RoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(radius - dist + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
