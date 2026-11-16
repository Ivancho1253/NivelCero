using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    /// <summary>
    /// Shared IMGUI look for menus, HUD, dialogue and hack screens of both scenes
    /// (Docs/Arte/DIRECCION_ARTISTICA.md §6). Layout uses a 1280×720 virtual canvas
    /// scaled uniformly and centred, so it never stretches on other aspect ratios.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Deep = new Color32(0x0B, 0x12, 0x1A, 0xF0);
        public static readonly Color Panel = new Color32(0x10, 0x1A, 0x24, 0xEB);
        public static readonly Color Line = new Color32(0x2E, 0x43, 0x54, 0xFF);
        public static readonly Color Text = new Color32(0xE6, 0xEE, 0xF2, 0xFF);
        public static readonly Color Muted = new Color32(0x9F, 0xB3, 0xC0, 0xFF);
        public static readonly Color Ally = new Color32(0x4F, 0xD8, 0xE0, 0xFF);
        public static readonly Color AllyDark = new Color32(0x1F, 0x7F, 0x8C, 0xFF);
        public static readonly Color Reward = new Color32(0xFF, 0xC8, 0x57, 0xFF);
        public static readonly Color Threat = new Color32(0xE8, 0x43, 0x5A, 0xFF);
        public static readonly Color ThreatDark = new Color32(0x8E, 0x1F, 0x33, 0xFF);
        public static readonly Color Corrupt = new Color32(0x9B, 0x4F, 0xD6, 0xFF);

        public const float Width = 1280, Height = 720;
        static Font bold, regular;
        static readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        static GUIStyle sliderStyle, thumbStyle;
        static Texture2D sliderTexture, thumbTexture;
        static int hovered = -1, hoverFrame;
        public static float Scale { get; private set; }
        public static Vector2 Origin { get; private set; }

        public static Font Bold { get { if (bold == null) bold = Resources.Load<Font>("Fonts/Rajdhani-Bold"); return bold; } }
        public static Font Regular { get { if (regular == null) regular = Resources.Load<Font>("Fonts/Rajdhani-Regular"); return regular; } }

        /// <summary>Uniform scale to the 1280×720 virtual canvas, letterboxed.</summary>
        public static Matrix4x4 Begin()
        {
            var previous = GUI.matrix;
            Scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            Origin = new Vector2((Screen.width - Width * Scale) * .5f, (Screen.height - Height * Scale) * .5f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(Origin.x, Origin.y, 0), Quaternion.identity, Vector3.one * Scale);
            GUI.color = Color.white;
            return previous;
        }

        /// <summary>Virtual-canvas rectangle that covers the whole screen (for overlays).</summary>
        public static Rect FullScreen
        {
            get { return new Rect(-Origin.x / Scale, -Origin.y / Scale, Screen.width / Scale, Screen.height / Scale); }
        }

        public static GUIStyle Style(int size, bool isBold = false, TextAnchor anchor = TextAnchor.UpperLeft, Color? color = null, bool wrap = true)
        {
            Color c = color ?? Text;
            int key = size * 1000003 ^ (isBold ? 7 : 3) ^ ((int)anchor << 20) ^ c.GetHashCode() ^ (wrap ? 11 : 13);
            GUIStyle style;
            if (styles.TryGetValue(key, out style)) return style;
            style = new GUIStyle(GUI.skin.label) { font = isBold ? Bold : Regular, fontSize = size, alignment = anchor, wordWrap = wrap, richText = true };
            style.normal.textColor = c; style.padding = new RectOffset(0, 0, 0, 0); style.margin = new RectOffset(0, 0, 0, 0);
            styles[key] = style;
            return style;
        }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        /// <summary>Dark panel, accent line on top and angled corner brackets.</summary>
        public static void DrawPanel(Rect r, Color accent, float alpha = 1)
        {
            Fill(r, new Color(Panel.r, Panel.g, Panel.b, Panel.a * alpha));
            Fill(new Rect(r.x, r.y, r.width, 2), accent);
            Color bracket = new Color(accent.r, accent.g, accent.b, .9f * alpha);
            float k = 10;
            Fill(new Rect(r.x, r.yMax - 2, k, 2), bracket); Fill(new Rect(r.x, r.yMax - k, 2, k), bracket);
            Fill(new Rect(r.xMax - k, r.yMax - 2, k, 2), bracket); Fill(new Rect(r.xMax - 2, r.yMax - k, 2, k), bracket);
            Fill(new Rect(r.x, r.y, 2, k), bracket); Fill(new Rect(r.xMax - 2, r.y, 2, k), bracket);
        }

        public static void Label(Rect r, string text, int size = 18, bool isBold = false, Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            GUI.Label(r, text, Style(size, isBold, anchor, color));
        }

        /// <summary>Button with explicit hover, pressed, focus and disabled states and UI sounds.</summary>
        public static bool Button(Rect r, string text, bool enabled = true, bool primary = false, int size = 20)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive, r);
            Event e = Event.current;
            bool hover = enabled && r.Contains(e.mousePosition);
            bool pressed = hover && Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (e.type == EventType.Repaint)
            {
                if (hover && hovered != id) { hovered = id; SoundBank.Play("ui_move", .8f, 1, .02f); }
                if (hover) hoverFrame = Time.frameCount;
                else if (hovered == id && Time.frameCount - hoverFrame > 1) hovered = -1;
                Color fill = !enabled ? new Color(.08f, .11f, .14f, .7f) : pressed ? AllyDark : hover ? new Color(.09f, .2f, .25f, .98f) : primary ? new Color(.07f, .16f, .2f, .98f) : new Color(.06f, .1f, .14f, .96f);
                Fill(r, fill);
                Color edge = !enabled ? new Color(.3f, .36f, .4f, .5f) : hover || primary ? Ally : Line;
                Fill(new Rect(r.x, r.y, 3, r.height), edge);
                Fill(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(edge.r, edge.g, edge.b, .6f));
                Color textColor = !enabled ? new Color(.55f, .6f, .65f, .6f) : hover || pressed ? Color.white : Text;
                GUI.Label(new Rect(r.x + 18, r.y, r.width - 24, r.height), text, Style(size, true, TextAnchor.MiddleLeft, textColor, false));
                if (hover && enabled) GUI.Label(new Rect(r.xMax - 30, r.y, 20, r.height), ">", Style(size, true, TextAnchor.MiddleCenter, Ally, false));
            }
            if (enabled && e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use(); SoundBank.Play(primary ? "ui_confirm" : "ui_confirm", .9f, primary ? 1 : .94f, 0);
                return true;
            }
            return false;
        }

        public static float Slider(Rect r, string label, float value, string valueText = null)
        {
            EnsureSliderStyles();
            Label(new Rect(r.x, r.y, r.width * .45f, r.height), label, 18, false, Text, TextAnchor.MiddleLeft);
            Rect track = new Rect(r.x + r.width * .45f, r.y + r.height * .5f - 8, r.width * .42f, 16);
            Fill(new Rect(track.x, track.y + 6, track.width, 4), Line);
            Fill(new Rect(track.x, track.y + 6, track.width * Mathf.Clamp01(value), 4), Ally);
            float result = GUI.HorizontalSlider(track, value, 0, 1, sliderStyle, thumbStyle);
            if (Mathf.Abs(result - value) > .001f && Time.frameCount % 4 == 0) SoundBank.Play("ui_move", .6f, .8f + result * .4f, 0, .06f);
            Label(new Rect(r.xMax - r.width * .12f, r.y, r.width * .12f, r.height), valueText ?? Mathf.RoundToInt(result * 100) + "%", 18, true, Muted, TextAnchor.MiddleRight);
            return result;
        }

        public static bool Toggle(Rect r, string label, bool value)
        {
            Rect box = new Rect(r.xMax - 54, r.y + r.height * .5f - 12, 48, 24);
            Label(new Rect(r.x, r.y, r.width - 60, r.height), label, 18, false, Text, TextAnchor.MiddleLeft);
            Fill(box, value ? AllyDark : new Color(.06f, .1f, .14f, 1));
            Fill(new Rect(value ? box.xMax - 22 : box.x + 2, box.y + 2, 20, 20), value ? Ally : Muted);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use(); SoundBank.Play(value ? "ui_back" : "ui_confirm", .8f, 1, 0); return !value;
            }
            return value;
        }

        /// <summary>Flat segmented bar (integrity, boss, timers).</summary>
        public static void Bar(Rect r, float amount, Color color, int segments = 0, Color? back = null)
        {
            Fill(r, back ?? new Color(.07f, .11f, .15f, .95f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(amount), r.height), color);
            if (segments > 1)
                for (int i = 1; i < segments; i++) Fill(new Rect(r.x + r.width * i / segments - 1, r.y, 2, r.height), new Color(0, 0, 0, .55f));
        }

        public static void DrawSprite(Rect r, Sprite sprite, Color? tint = null)
        {
            if (sprite == null) return;
            Rect uv = sprite.textureRect;
            Texture t = sprite.texture;
            var old = GUI.color; GUI.color = tint ?? Color.white;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(uv.x / t.width, uv.y / t.height, uv.width / t.width, uv.height / t.height));
            GUI.color = old;
        }

        public static void KeyCap(Rect r, string key)
        {
            Fill(r, new Color(.06f, .1f, .14f, 1));
            Fill(new Rect(r.x, r.yMax - 3, r.width, 3), AllyDark);
            Fill(new Rect(r.x, r.y, r.width, 1), Ally);
            GUI.Label(r, key, Style(16, true, TextAnchor.MiddleCenter, Text, false));
        }

        /// <summary>Full-screen colour wash at the screen edges (damage feedback).</summary>
        public static void Vignette(Color color, float strength)
        {
            if (strength <= .01f) return;
            Rect f = FullScreen;
            float t = 60 * strength;
            Color c = new Color(color.r, color.g, color.b, .55f * strength);
            Fill(new Rect(f.x, f.y, f.width, t), c); Fill(new Rect(f.x, f.yMax - t, f.width, t), c);
            Fill(new Rect(f.x, f.y, t, f.height), c); Fill(new Rect(f.xMax - t, f.y, t, f.height), c);
        }

        static void EnsureSliderStyles()
        {
            if (sliderStyle != null) return;
            sliderTexture = new Texture2D(1, 1); sliderTexture.SetPixel(0, 0, new Color(0, 0, 0, 0)); sliderTexture.Apply();
            thumbTexture = new Texture2D(1, 1); thumbTexture.SetPixel(0, 0, Text); thumbTexture.Apply();
            sliderStyle = new GUIStyle(); sliderStyle.normal.background = sliderTexture; sliderStyle.fixedHeight = 16;
            thumbStyle = new GUIStyle(); thumbStyle.normal.background = thumbTexture; thumbStyle.hover.background = thumbTexture;
            thumbStyle.active.background = thumbTexture; thumbStyle.fixedWidth = 10; thumbStyle.fixedHeight = 16;
        }
    }
}
