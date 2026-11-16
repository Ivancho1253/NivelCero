using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NivelCero
{
    /// <summary>
    /// 3D menus, HUD, dialogue and hack screen, drawn with the shared UiTheme so
    /// both scenes speak the same visual language (Docs/Arte/DIRECCION_ARTISTICA.md).
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        Session S { get { return Session.I; } }
        enum Page { Main, Options, Controls }
        Page page = Page.Main;
        Texture2D portraitSheet, shardIcon;

        void Start()
        {
            var v = S.Player.View;
            v.Sensitivity = PlayerPrefs.GetFloat("sensitivity", .12f);
            v.Fov = PlayerPrefs.GetFloat("fov", 78);
            v.InvertY = PlayerPrefs.GetInt("invertY", 0) == 1;
            GameSettings.Apply();
            if (!Application.isEditor && PlayerPrefs.HasKey("screenWidth"))
                Screen.SetResolution(PlayerPrefs.GetInt("screenWidth"), PlayerPrefs.GetInt("screenHeight"), PlayerPrefs.GetInt("fullscreen", 1) == 1);
            if (PlayerPrefs.HasKey("quality")) QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt("quality"), 0, QualitySettings.names.Length - 1));
            portraitSheet = Resources.Load<Texture2D>("Academic2D/portraits");
            shardIcon = Resources.Load<Texture2D>("Academic2D/fx");
        }

        void OnGUI()
        {
            if (S == null || S.Player == null) return;
            var saved = UiTheme.Begin();
            if (!GameSettings.ReduceEffects) UiTheme.Vignette(UiTheme.Threat, S.HurtFlash);
            if (S.InMenu || S.Paused || S.Defeat || S.Victory) Menu();
            else if (S.Puzzle != null) Hack();
            else Hud();
            GUI.matrix = saved;
        }

        // ------------------------------------------------------------ HUD
        void Hud()
        {
            float hp = S.PlayerHealth.Current;
            UiTheme.DrawPanel(new Rect(20, 18, 340, 96), UiTheme.Ally, .92f);
            UiTheme.Label(new Rect(36, 26, 200, 22), "ZERO · INTEGRIDAD", 16, true, UiTheme.Muted);
            UiTheme.Label(new Rect(250, 24, 96, 24), Mathf.CeilToInt(hp) + "<size=14> / 100</size>", 22, true, hp > 35 ? UiTheme.Text : UiTheme.Threat, TextAnchor.UpperRight);
            UiTheme.Bar(new Rect(36, 52, 310, 12), hp / 100, hp > 35 ? UiTheme.Ally : UiTheme.Threat, 10);
            bool blade = S.Combat.IsBlade;
            string weapon = blade ? "2 HOJA" : S.Combat.Reloading ? "1 PULSO · RECARGA" : "1 PULSO · " + S.Combat.Ammo + "/" + S.Combat.Magazine;
            UiTheme.Fill(new Rect(36, 74, 200, 30), new Color(.07f, .2f, .24f, 1));
            UiTheme.Fill(new Rect(36, 101, 200, 3), S.Combat.Reloading || (!blade && S.Combat.Ammo == 0) ? UiTheme.Threat : UiTheme.Ally);
            UiTheme.Label(new Rect(44, 74, 192, 28), weapon, 16, true, UiTheme.Text, TextAnchor.MiddleLeft);
            UiTheme.Fill(new Rect(244, 74, 102, 30), new Color(.06f, .1f, .14f, 1));
            UiTheme.Fill(new Rect(244, 101, S.Player.DashReady ? 102 : 30, 3), S.Player.DashReady ? UiTheme.Ally : UiTheme.Muted);
            UiTheme.Label(new Rect(252, 74, 94, 28), S.Player.DashReady ? "SHIFT DASH" : "DASH ...", 15, true, S.Player.DashReady ? UiTheme.Text : UiTheme.Muted, TextAnchor.MiddleLeft);

            bool bossFight = S.Boss != null && S.Boss.Active && !S.Progress.bossDefeated;
            if (bossFight && S.Boss.IntroTime > 0)
            {
                float t = S.Boss.IntroTime, a = Mathf.Clamp01((2.8f - t) / .5f) * Mathf.Clamp01(t / .4f);
                UiTheme.Label(new Rect(240, 440, 800, 56), "GUARDIÁN DEL SISTEMA", 46, true, new Color(1, 1, 1, a), TextAnchor.MiddleCenter);
                UiTheme.Fill(new Rect(540, 498, 200 * a, 2), UiTheme.Threat);
                UiTheme.Label(new Rect(240, 506, 800, 28), "Protocolo de Seguridad 01 · el escudo sólo cae hackeando ALFA o BETA", 20, false, new Color(UiTheme.Muted.r, UiTheme.Muted.g, UiTheme.Muted.b, a), TextAnchor.MiddleCenter);
            }
            if (bossFight) BossBar(); else Objectives();

            // Crosshair with hit confirmation (shape changes, not just colour).
            Rect c = new Rect(640 - 2, 360 - 2, 4, 4);
            Color cross = S.Combat.HitConfirm > 0 ? UiTheme.Reward : new Color(1, 1, 1, .85f);
            UiTheme.Fill(c, cross);
            if (S.Combat.HitConfirm > 0)
            {
                UiTheme.Fill(new Rect(628, 348, 6, 2), cross); UiTheme.Fill(new Rect(646, 348, 6, 2), cross);
                UiTheme.Fill(new Rect(628, 370, 6, 2), cross); UiTheme.Fill(new Rect(646, 370, 6, 2), cross);
            }

            var scan = S.Player.GetComponent<InteractionScanner>();
            // Terminal prompts always; the Bit prompt only early on or while Zero stands still out of combat.
            bool bitPrompt = scan != null && scan.Prompt != null && scan.Prompt.Contains("BIT");
            bool showBit = !bossFight && Time.unscaledTime >= S.MessageUntil && (S.Progress.elapsedSeconds < 25 || S.Player.Velocity.sqrMagnitude < .05f);
            if (scan != null && !string.IsNullOrEmpty(scan.Prompt) && (!bitPrompt || showBit))
            {
                Rect r = new Rect(460, 560, 360, 40);
                UiTheme.DrawPanel(r, UiTheme.Ally, .92f);
                UiTheme.KeyCap(new Rect(r.x + 10, r.y + 7, 26, 26), "E");
                UiTheme.Label(new Rect(r.x + 46, r.y, r.width - 50, r.height), scan.Prompt.Replace("E · ", ""), 17, true, UiTheme.Text, TextAnchor.MiddleLeft);
            }
            if (Time.unscaledTime < S.MessageUntil) Dialogue(S.Message);
        }

        void Objectives()
        {
            Rect r = new Rect(860, 18, 400, 92);
            UiTheme.DrawPanel(r, UiTheme.Reward, .92f);
            UiTheme.Label(new Rect(r.x + 16, r.y + 8, 200, 20), "OBJETIVO", 16, true, UiTheme.Muted);
            string[] ids = { "fragment-main-1", "fragment-main-2", "fragment-optional" };
            for (int i = 0; i < 3; i++)
            {
                bool have = S.Progress.HasFragment(ids[i]);
                Rect slot = new Rect(r.xMax - 118 + i * 34, r.y + 6, 28, 28);
                if (shardIcon != null)
                {
                    var old = GUI.color; GUI.color = have ? Color.white : new Color(.25f, .3f, .36f, i == 2 ? .5f : 1);
                    // Fragment icon = first frame of the "shard" row in the shared 2D FX sheet.
                    GUI.DrawTextureWithTexCoords(slot, shardIcon, new Rect(0, 1 - 12 * 48f / shardIcon.height, 48f / shardIcon.width, 48f / shardIcon.height));
                    GUI.color = old;
                }
            }
            UiTheme.Label(new Rect(r.xMax - 120, r.y + 34, 104, 16), "2 + 1 OPCIONAL", 12, true, UiTheme.Muted, TextAnchor.UpperRight);
            string goal = !S.Progress.RequiredFragmentsRecovered ? "Recuperá los dos fragmentos principales."
                : !S.Progress.accessUnlocked ? "Hackeá el acceso restringido."
                : !S.Progress.bossDefeated ? "Hackeá el escudo y derrotá al Guardián."
                : "Entrá al portal de salida.";
            UiTheme.Label(new Rect(r.x + 16, r.y + 52, r.width - 32, 40), goal, 18, false, UiTheme.Text);
        }

        void BossBar()
        {
            var b = S.Boss;
            Rect r = new Rect(384, 16, 556, 84);
            UiTheme.DrawPanel(r, UiTheme.Threat, .94f);
            UiTheme.Label(new Rect(r.x + 16, r.y + 8, 380, 22), "GUARDIÁN DEL SISTEMA", 18, true, UiTheme.Text);
            for (int i = 0; i < 3; i++)
                UiTheme.Fill(new Rect(r.xMax - 110 + i * 32, r.y + 12, 24, 10), b.Phase > i + 1 ? UiTheme.ThreatDark : b.Phase == i + 1 ? UiTheme.Threat : new Color(.2f, .24f, .3f, 1));
            UiTheme.Bar(new Rect(r.x + 16, r.y + 36, r.width - 32, 14), b.Health.Current / b.Health.Maximum, b.Shielded ? UiTheme.Threat : UiTheme.Reward, 3);
            if (b.HitFlash > 0) UiTheme.Fill(new Rect(r.x + 16, r.y + 36, r.width - 32, 14), new Color(1, 1, 1, .35f));
            string state = b.Telegraphing ? "DESCARGA MARCADA · MOVETE" : b.Shielded ? "ESCUDO ACTIVO · HACKEÁ ALFA O BETA" : "NÚCLEO EXPUESTO · " + Mathf.CeilToInt(b.VulnerableRemaining) + " s";
            Color sc = b.Telegraphing ? UiTheme.Threat : b.Shielded ? UiTheme.Muted : UiTheme.Reward;
            UiTheme.Label(new Rect(r.x + 16, r.y + 56, r.width - 32, 22), state, 16, true, sc);
            if (b.PhaseBanner > 0)
            {
                float a = Mathf.Clamp01(b.PhaseBanner);
                UiTheme.Label(new Rect(340, 200, 600, 50), "FASE " + b.Phase, 44, true, new Color(UiTheme.Threat.r, UiTheme.Threat.g, UiTheme.Threat.b, a), TextAnchor.MiddleCenter);
                UiTheme.Label(new Rect(340, 248, 600, 30), "El escudo se restauró: reconectá un nodo", 20, false, new Color(1, 1, 1, a), TextAnchor.MiddleCenter);
            }
        }

        void Dialogue(string message)
        {
            bool bit = message.StartsWith("Bit:"), glitch = message.StartsWith("GL1TCH"), guardian = message.StartsWith("Guardián");
            Color accent = glitch ? UiTheme.Corrupt : guardian ? UiTheme.Threat : bit ? UiTheme.Ally : UiTheme.Muted;
            Rect r = new Rect(230, 610, 820, 72);
            UiTheme.DrawPanel(r, accent, .94f);
            float textX = r.x + 18;
            if ((bit || glitch || guardian) && portraitSheet != null)
            {
                int index = glitch ? 4 : guardian ? 5 : 0;
                GUI.DrawTextureWithTexCoords(new Rect(r.x + 10, r.y + 8, 56, 56), portraitSheet, new Rect(index / 6f, 0, 1 / 6f, 1));
                textX = r.x + 80;
            }
            int colon = message.IndexOf(':');
            string speaker = colon > 0 && colon < 12 ? message.Substring(0, colon).ToUpperInvariant() : "SISTEMA";
            string body = colon > 0 && colon < 12 ? message.Substring(colon + 1).Trim() : message;
            UiTheme.Label(new Rect(textX, r.y + 6, 300, 20), speaker, 15, true, accent);
            UiTheme.Label(new Rect(textX, r.y + 26, r.xMax - textX - 16, 44), body, 18, false, UiTheme.Text);
        }

        // ------------------------------------------------------------ menus
        void Menu()
        {
            Rect f = UiTheme.FullScreen;
            UiTheme.Fill(f, new Color(.02f, .035f, .05f, S.InMenu ? .55f : .75f));
            Rect panel = new Rect(360, 70, 560, 590);
            Color accent = S.Defeat ? UiTheme.Threat : UiTheme.Ally;
            UiTheme.DrawPanel(panel, accent);
            float x = panel.x + 40, w = panel.width - 80, y = panel.y + 28;
            if (page == Page.Options) { Options(x, y, w); return; }
            if (page == Page.Controls) { Controls(x, y, w); return; }
            string title = S.Victory ? "SECTOR RESTAURADO" : S.Defeat ? "SEÑAL PERDIDA" : S.Paused ? "PAUSA" : "NIVEL CERO";
            UiTheme.Label(new Rect(x, y, w, 52), title, 44, true, S.Defeat ? UiTheme.Threat : S.Victory ? UiTheme.Ally : UiTheme.Text);
            y += 54;
            string sub = S.InMenu ? "ESCAPE DEL GLITCH · Sector de Arranque\nUn sistema corrupto. Tu código es la salida."
                : S.Defeat ? "La integridad de Zero llegó a cero. Bit puede restaurarte desde el último checkpoint."
                : S.Victory ? "Bit: el camino a Memoria Fragmentada está abierto."
                : "El sistema queda congelado mientras decidís.";
            UiTheme.Label(new Rect(x, y, w, 60), sub, 18, false, UiTheme.Muted);
            y += 74;
            if (S.Victory)
            {
                int seconds = Mathf.FloorToInt(S.Progress.elapsedSeconds);
                Stat(new Rect(x, y, w / 2 - 6, 64), "TIEMPO", (seconds / 60) + ":" + (seconds % 60).ToString("00"));
                Stat(new Rect(x + w / 2 + 6, y, w / 2 - 6, 64), "FRAGMENTOS", S.Progress.FragmentCount + "/3");
                y += 84;
                if (UiTheme.Button(new Rect(x, y, w, 46), "VOLVER AL MENÚ", true, true)) S.ReloadLevel();
                return;
            }
            if (S.Defeat)
            {
                if (UiTheme.Button(new Rect(x, y, w, 46), "REAPARECER EN CHECKPOINT", true, true)) S.Respawn();
                y += 54;
                if (UiTheme.Button(new Rect(x, y, w, 40), "MENÚ PRINCIPAL")) S.ReloadLevel();
                return;
            }
            if (S.InMenu)
            {
                if (UiTheme.Button(new Rect(x, y, w, 46), "NUEVA PARTIDA", true, true)) S.StartGame(false);
                y += 52;
                if (UiTheme.Button(new Rect(x, y, w, 40), "CONTINUAR", File.Exists(S.SavePath))) S.StartGame(true);
                y += 48;
                if (UiTheme.Button(new Rect(x, y, w, 40), "ESCENA ACADÉMICA 2D")) SceneManager.LoadScene("AE4_2D_SectorDeArranque");
                y += 48;
            }
            else
            {
                if (UiTheme.Button(new Rect(x, y, w, 46), "CONTINUAR", true, true)) { S.Paused = false; SoundBank.Play("pause_out"); }
                y += 52;
                if (UiTheme.Button(new Rect(x, y, w, 40), "GUARDAR PARTIDA")) { S.Save(); S.Notify("Progreso guardado."); }
                y += 48;
                if (UiTheme.Button(new Rect(x, y, w, 40), "MENÚ PRINCIPAL")) { S.Save(); S.ReloadLevel(); }
                y += 48;
            }
            if (UiTheme.Button(new Rect(x, y, w / 2 - 6, 40), "OPCIONES")) page = Page.Options;
            if (UiTheme.Button(new Rect(x + w / 2 + 6, y, w / 2 - 6, 40), "CONTROLES")) page = Page.Controls;
            y += 48;
            if (UiTheme.Button(new Rect(x, y, w, 40), "SALIR")) Application.Quit();
            if (S.InMenu && Time.unscaledTime < S.MessageUntil + 30) UiTheme.Label(new Rect(x, panel.yMax - 40, w, 30), S.Message, 15, false, UiTheme.Muted);
        }

        void Stat(Rect r, string label, string value)
        {
            UiTheme.Fill(r, new Color(.06f, .1f, .14f, 1));
            UiTheme.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, 18), label, 13, true, UiTheme.Muted);
            UiTheme.Label(new Rect(r.x + 10, r.y + 24, r.width - 20, 36), value, 30, true, UiTheme.Text);
        }

        void Options(float x, float y, float w)
        {
            var v = S.Player.View;
            UiTheme.Label(new Rect(x, y, w, 46), "OPCIONES", 36, true, UiTheme.Text);
            y += 56;
            v.Sensitivity = Mathf.Lerp(.02f, .5f, UiTheme.Slider(new Rect(x, y, w, 32), "Sensibilidad", Mathf.InverseLerp(.02f, .5f, v.Sensitivity), v.Sensitivity.ToString("0.00")));
            y += 38;
            v.Fov = Mathf.Lerp(60, 110, UiTheme.Slider(new Rect(x, y, w, 32), "Campo de visión", Mathf.InverseLerp(60, 110, v.Fov), Mathf.RoundToInt(v.Fov).ToString()));
            y += 38;
            float master = UiTheme.Slider(new Rect(x, y, w, 32), "Volumen general", GameSettings.Master);
            if (!Mathf.Approximately(master, GameSettings.Master)) GameSettings.Master = master;
            y += 38;
            float music = UiTheme.Slider(new Rect(x, y, w, 32), "Música", GameSettings.Music);
            if (!Mathf.Approximately(music, GameSettings.Music)) GameSettings.Music = music;
            y += 38;
            float sfx = UiTheme.Slider(new Rect(x, y, w, 32), "Efectos", GameSettings.Sfx);
            if (!Mathf.Approximately(sfx, GameSettings.Sfx)) GameSettings.Sfx = sfx;
            y += 42;
            v.InvertY = UiTheme.Toggle(new Rect(x, y, w, 32), "Invertir mouse vertical", v.InvertY);
            y += 38;
            bool reduce = UiTheme.Toggle(new Rect(x, y, w, 32), "Reducir efectos (destellos, viñeta)", GameSettings.ReduceEffects);
            if (reduce != GameSettings.ReduceEffects) GameSettings.ReduceEffects = reduce;
            y += 42;
            if (UiTheme.Button(new Rect(x, y, w / 2 - 6, 36), Screen.fullScreen ? "VENTANA" : "PANTALLA COMPLETA", true, false, 17)) Screen.fullScreen = !Screen.fullScreen;
            if (UiTheme.Button(new Rect(x + w / 2 + 6, y, w / 2 - 6, 36), "CALIDAD: " + QualitySettings.names[QualitySettings.GetQualityLevel()], true, false, 17))
                QualitySettings.SetQualityLevel((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
            y += 42;
            if (UiTheme.Button(new Rect(x, y, w / 2 - 6, 36), "1280 × 720", true, false, 17)) Screen.SetResolution(1280, 720, Screen.fullScreen);
            if (UiTheme.Button(new Rect(x + w / 2 + 6, y, w / 2 - 6, 36), "1920 × 1080", true, false, 17)) Screen.SetResolution(1920, 1080, Screen.fullScreen);
            y += 46;
            if (UiTheme.Button(new Rect(x, y, w, 42), "GUARDAR Y VOLVER", true, true))
            {
                PlayerPrefs.SetFloat("sensitivity", v.Sensitivity); PlayerPrefs.SetFloat("fov", v.Fov);
                PlayerPrefs.SetInt("invertY", v.InvertY ? 1 : 0);
                PlayerPrefs.SetInt("screenWidth", Screen.width); PlayerPrefs.SetInt("screenHeight", Screen.height);
                PlayerPrefs.SetInt("fullscreen", Screen.fullScreen ? 1 : 0); PlayerPrefs.SetInt("quality", QualitySettings.GetQualityLevel());
                GameSettings.Save(); page = Page.Main;
            }
        }

        void Controls(float x, float y, float w)
        {
            UiTheme.Label(new Rect(x, y, w, 46), "CONTROLES", 36, true, UiTheme.Text);
            y += 58;
            string[,] rows = {
                { "W A S D", "Moverse (relativo a la cámara)" }, { "Mouse", "Mirar" }, { "Espacio", "Saltar" }, { "Shift", "Dash" },
                { "Ctrl", "Agacharse" }, { "V  ·  C", "Primera/tercera persona · hombro" }, { "1  /  2", "Pulse Blaster / Energy Blade" },
                { "Clic izq. / der.", "Atacar · apuntar o bloquear" }, { "R", "Recargar" }, { "E", "Interactuar · hablar con Bit" }, { "Esc", "Pausa · cancelar hack" } };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                UiTheme.Label(new Rect(x, y, w * .4f, 26), rows[i, 0], 17, true, UiTheme.Ally);
                UiTheme.Label(new Rect(x + w * .42f, y, w * .58f, 26), rows[i, 1], 17, false, UiTheme.Text);
                y += 31;
            }
            y += 10;
            if (UiTheme.Button(new Rect(x, y, w, 42), "VOLVER", true, true)) page = Page.Main;
        }

        // ------------------------------------------------------------ hack
        void Hack()
        {
            Rect f = UiTheme.FullScreen;
            UiTheme.Fill(f, new Color(.01f, .02f, .03f, .78f));
            Rect panel = new Rect(290, 40, 700, 640);
            UiTheme.DrawPanel(panel, UiTheme.Ally);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 20, 640, 40), "HACK · " + S.ActiveTerminal.Label, 28, true, UiTheme.Text);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 60, 640, 44), "Clic en un módulo para rotarlo. Uní ORIGEN (oeste de la celda superior izquierda) con DESTINO (este de la inferior derecha).", 16, false, UiTheme.Muted);
            float remaining = Mathf.Clamp01(S.HackRemaining / Mathf.Max(1, S.ActiveTerminal.TimeLimit));
            UiTheme.Bar(new Rect(panel.x + 30, panel.y + 108, 640, 8), remaining, remaining > .3f ? UiTheme.Ally : UiTheme.Threat);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 118, 640, 22), "TIEMPO " + Mathf.CeilToInt(S.HackRemaining) + " s", 14, true, remaining > .3f ? UiTheme.Muted : UiTheme.Threat);
            int size = S.Puzzle.Size;
            float cell = Mathf.Min(96, 400f / size), left = 640 - size * cell / 2, top = panel.y + 156;
            for (int yy = 0; yy < size; yy++)
                for (int xx = 0; xx < size; xx++)
                {
                    Rect r = new Rect(left + xx * cell, top + yy * cell, cell - 6, cell - 6);
                    bool hover = r.Contains(Event.current.mousePosition);
                    bool powered = S.Puzzle.IsPowered(xx, yy);
                    UiTheme.Fill(r, hover ? new Color(.09f, .17f, .22f, 1) : new Color(.05f, .09f, .12f, 1));
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(Event.current.mousePosition))
                    { S.Puzzle.Rotate(xx, yy); Event.current.Use(); SoundBank.Play("rotate", 1, 1, .08f, .02f); }
                    Color col = powered ? UiTheme.Ally : new Color(.42f, .52f, .6f);
                    int mask = S.Puzzle.MaskAt(xx, yy); float cx = r.center.x, cy = r.center.y, half = r.width / 2, t = 8;
                    UiTheme.Fill(new Rect(cx - 7, cy - 7, 14, 14), col);
                    if ((mask & 1) != 0) UiTheme.Fill(new Rect(cx - t / 2, r.y, t, half), col);
                    if ((mask & 2) != 0) UiTheme.Fill(new Rect(cx, cy - t / 2, half, t), col);
                    if ((mask & 4) != 0) UiTheme.Fill(new Rect(cx - t / 2, cy, t, half), col);
                    if ((mask & 8) != 0) UiTheme.Fill(new Rect(r.x, cy - t / 2, half, t), col);
                }
            UiTheme.Fill(new Rect(left - 34, top + cell / 2 - 10, 30, 14), UiTheme.Ally);
            UiTheme.Label(new Rect(left - 120, top + cell / 2 - 24, 82, 40), "ORIGEN", 14, true, UiTheme.Ally, TextAnchor.MiddleRight);
            float oy = top + (size - 1) * cell + cell / 2 - 10;
            UiTheme.Fill(new Rect(left + size * cell - 2, oy, 30, 14), S.Puzzle.IsSolved ? UiTheme.Ally : UiTheme.Line);
            UiTheme.Label(new Rect(left + size * cell + 32, oy - 14, 90, 40), "DESTINO", 14, true, S.Puzzle.IsSolved ? UiTheme.Ally : UiTheme.Muted, TextAnchor.MiddleLeft);
            float by = panel.yMax - 66;
            if (S.Puzzle.IsSolved) UiTheme.Label(new Rect(panel.x + 30, by - 30, 640, 24), "CIRCUITO CERRADO · listo para validar", 17, true, UiTheme.Ally);
            if (UiTheme.Button(new Rect(panel.x + 30, by, 250, 44), "VALIDAR CIRCUITO", true, true)) S.ValidateHack();
            if (UiTheme.Button(new Rect(panel.x + 292, by, 170, 44), "REINICIAR")) S.Puzzle.Reset();
            if (UiTheme.Button(new Rect(panel.x + 474, by, 196, 44), "CANCELAR · ESC")) S.CancelHack(true);
        }
    }
}
