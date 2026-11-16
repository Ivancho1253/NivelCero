using UnityEngine;
using UnityEngine.SceneManagement;
using NivelCero.Core;

namespace NivelCero
{
    // IMGUI layer of the 2D scene, drawn with the shared UiTheme.
    public sealed partial class Academic2D
    {
        enum MenuPage { Main, Options, Controls }
        MenuPage menuPage = MenuPage.Main;

        void OnGUI()
        {
            if (player == null || art == null) return;
            var saved = UiTheme.Begin();
            bool overlay = !started || paused || defeated || won;
            if (!GameSettings.ReduceEffects) UiTheme.Vignette(UiTheme.Threat, damageFlash);
            else if (damageFlash > .5f) UiTheme.Fill(new Rect(0, 0, 1280, 6), UiTheme.Threat);
            if (InCinematic) DrawIntro();
            else if (started && !hacking) DrawHud();
            if (subtitleTime > 0 && started && !overlay && !hacking) DrawDialogue();
            if (hacking) DrawCircuit();
            else if (overlay) DrawMenu();
            GUI.matrix = saved;
        }

        // ------------------------------------------------------------ HUD
        void DrawHud()
        {
            // Integrity (left): icon, segmented bar, numeric value.
            UiTheme.DrawPanel(new Rect(20, 18, 340, 92), UiTheme.Ally, .92f);
            UiTheme.Label(new Rect(36, 26, 200, 22), "ZERO · INTEGRIDAD", 16, true, UiTheme.Muted);
            UiTheme.Label(new Rect(250, 24, 96, 24), Mathf.CeilToInt(hp) + "<size=14> / 100</size>", 22, true, hp > 35 ? UiTheme.Text : UiTheme.Threat, TextAnchor.UpperRight);
            UiTheme.Bar(new Rect(36, 52, 310, 12), hp / 100, hp > 35 ? UiTheme.Ally : UiTheme.Threat, 10);
            // Weapon slots and dash cooldown.
            DrawSlot(new Rect(36, 74, 98, 26), "1 PULSO", !blade);
            DrawSlot(new Rect(140, 74, 98, 26), "2 HOJA", blade);
            float dashReady = dashDelay > 0 ? 1 - dashDelay / .9f : 1;
            bool dashBlocked = airDash && !grounded;
            UiTheme.Fill(new Rect(246, 74, 100, 26), new Color(.06f, .1f, .14f, 1));
            UiTheme.Fill(new Rect(246, 96, 100 * Mathf.Clamp01(dashReady), 4), dashBlocked ? UiTheme.Muted : UiTheme.Ally);
            UiTheme.Label(new Rect(254, 74, 92, 24), dashBlocked ? "DASH ·  SUELO" : dashReady >= 1 ? "SHIFT DASH" : "DASH ...", 15, true, dashReady >= 1 && !dashBlocked ? UiTheme.Text : UiTheme.Muted, TextAnchor.MiddleLeft);

            if (bossActive && bossHP > 0 && bossState != BossState.Dormant) DrawBossBar();
            else DrawObjectives();

            // Zone name, discreet, under the objectives.
            int zone = Academic2DLevel.Zone(player.position.x);
            UiTheme.Label(new Rect(20, 118, 340, 20), "SECTOR 01 · " + Academic2DLevel.ZoneNames[zone], 14, true, UiTheme.Muted);

            if (!gateUnlocked && Mathf.Abs(player.position.x - 64) < 3)
                Prompt(terminalCooldown > 0 ? "TERMINAL BLOQUEADO · " + Mathf.CeilToInt(terminalCooldown) + " s" : "HACKEAR FIREWALL", terminalCooldown <= 0);
            else if (bitBrain != null && bitBrain.PlayerInRange && !bossActive && subtitleTime <= 0)
                Prompt("HABLAR CON BIT", true);

            if (phaseBanner > 0)
            {
                float a = Mathf.Clamp01(phaseBanner);
                UiTheme.Label(new Rect(340, 210, 600, 50), "FASE " + shownPhase, 44, true, new Color(UiTheme.Threat.r, UiTheme.Threat.g, UiTheme.Threat.b, a), TextAnchor.MiddleCenter);
                UiTheme.Label(new Rect(340, 258, 600, 30), shownPhase == 2 ? "Nuevo ataque: ONDA DE CHOQUE — saltala" : "Nuevo ataque: LLUVIA CORRUPTA — buscá el hueco", 20, false, new Color(1, 1, 1, a), TextAnchor.MiddleCenter);
            }
            UiTheme.Label(new Rect(20, 690, 1240, 22), "A/D o flechas mover · Espacio saltar · Shift dash · Mouse apuntar · Clic atacar · 1/2 arma · E interactuar · Esc pausa", 14, false, new Color(.62f, .7f, .76f, .75f), TextAnchor.MiddleCenter);
        }

        void DrawSlot(Rect r, string text, bool active)
        {
            UiTheme.Fill(r, active ? new Color(.07f, .2f, .24f, 1) : new Color(.06f, .1f, .14f, 1));
            if (active) UiTheme.Fill(new Rect(r.x, r.yMax - 3, r.width, 3), UiTheme.Ally);
            UiTheme.Label(new Rect(r.x + 8, r.y, r.width - 8, r.height), text, 15, true, active ? UiTheme.Text : UiTheme.Muted, TextAnchor.MiddleLeft);
        }

        void DrawObjectives()
        {
            Rect r = new Rect(860, 18, 400, 92);
            UiTheme.DrawPanel(r, UiTheme.Reward, .92f);
            UiTheme.Label(new Rect(r.x + 16, r.y + 8, 200, 20), "OBJETIVO", 16, true, UiTheme.Muted);
            for (int i = 0; i < 3; i++)
            {
                bool have = collected.Contains(i);
                Rect slot = new Rect(r.xMax - 118 + i * 34, r.y + 6, 28, 28);
                UiTheme.DrawSprite(slot, art.Fx["shard"][0], have ? Color.white : new Color(.25f, .3f, .36f, i == 2 ? .5f : 1));
            }
            UiTheme.Label(new Rect(r.xMax - 120, r.y + 34, 104, 16), "2 + 1 OPCIONAL", 12, true, UiTheme.Muted, TextAnchor.UpperRight);
            string objective = RequiredFragments < 2 ? "Recuperá los dos fragmentos principales (" + RequiredFragments + "/2)."
                : !gateUnlocked ? "Hackeá el terminal y atravesá el firewall."
                : bossHP > 0 ? "Entrá al Núcleo de Seguridad y desactivá al Guardián."
                : "Entrá al portal de salida.";
            UiTheme.Label(new Rect(r.x + 16, r.y + 52, r.width - 32, 40), objective, 18, false, UiTheme.Text);
        }

        void DrawBossBar()
        {
            Rect r = new Rect(384, 16, 556, 84);
            UiTheme.DrawPanel(r, UiTheme.Threat, .94f);
            UiTheme.Label(new Rect(r.x + 16, r.y + 8, 380, 22), "GUARDIÁN DEL SISTEMA", 18, true, UiTheme.Text);
            for (int i = 0; i < 3; i++)
            {
                bool done = BossPhase > i + 1, current = BossPhase == i + 1;
                UiTheme.Fill(new Rect(r.xMax - 110 + i * 32, r.y + 12, 24, 10), done ? UiTheme.ThreatDark : current ? UiTheme.Threat : new Color(.2f, .24f, .3f, 1));
            }
            UiTheme.Bar(new Rect(r.x + 16, r.y + 36, r.width - 32, 14), bossHP / BossMaxHealth, BossVulnerable ? UiTheme.Reward : UiTheme.Threat, 3);
            string state; Color stateColor;
            if (bossState == BossState.PhaseShift) { state = "REINICIANDO DEFENSAS · INMUNE"; stateColor = UiTheme.Corrupt; }
            else if (BossVulnerable) { state = "NÚCLEO EXPUESTO · DAÑO COMPLETO"; stateColor = UiTheme.Reward; }
            else if (bossState == BossState.Charge) { state = "CARGANDO DESCARGA · SALÍ DE LA MARCA"; stateColor = UiTheme.Threat; }
            else if (bossState == BossState.Raise) { state = "BRAZOS ARRIBA · PREPARÁ EL SALTO"; stateColor = UiTheme.Threat; }
            else if (bossState == BossState.Rain) { state = "COLUMNAS MARCADAS · BUSCÁ EL HUECO"; stateColor = UiTheme.Threat; }
            else if (bossState == BossState.Dying) { state = "SISTEMA COLAPSANDO"; stateColor = UiTheme.Ally; }
            else { state = "BLINDADO · ESQUIVÁ Y ESPERÁ LA APERTURA"; stateColor = UiTheme.Muted; }
            Sprite icon = BossVulnerable ? art.Fx["reticle"][0] : bossState == BossState.Charge || bossState == BossState.Raise || bossState == BossState.Rain ? art.Fx["warn"][0] : null;
            if (icon != null) UiTheme.DrawSprite(new Rect(r.x + 12, r.y + 54, 24, 24), icon, BossVulnerable ? UiTheme.Reward : Color.white);
            UiTheme.Label(new Rect(r.x + (icon != null ? 42 : 16), r.y + 56, r.width - 58, 22), state, 16, true, stateColor);
        }

        void Prompt(string text, bool enabled)
        {
            Rect r = new Rect(490, 560, 300, 40);
            UiTheme.DrawPanel(r, enabled ? UiTheme.Ally : UiTheme.Muted, .95f);
            UiTheme.KeyCap(new Rect(r.x + 10, r.y + 7, 26, 26), "E");
            UiTheme.Label(new Rect(r.x + 46, r.y, r.width - 50, r.height), text, 17, true, enabled ? UiTheme.Text : UiTheme.Muted, TextAnchor.MiddleLeft);
        }

        void DrawDialogue()
        {
            Rect r = new Rect(230, 606, 820, 78);
            Color accent = speaker == Speaker.Glitch ? UiTheme.Corrupt : speaker == Speaker.Guardian ? UiTheme.Threat : UiTheme.Ally;
            UiTheme.DrawPanel(r, accent, .95f);
            int portrait = speaker == Speaker.Glitch ? 4 : speaker == Speaker.Guardian ? 5 : bitBrain != null ? bitBrain.PortraitMood : 0;
            if (art.Portraits != null) UiTheme.DrawSprite(new Rect(r.x + 12, r.y + 11, 56, 56), art.Portraits[portrait]);
            string name = speaker == Speaker.Glitch ? "GL1TCH" : speaker == Speaker.Guardian ? "GUARDIÁN" : speaker == Speaker.System ? "SISTEMA" : "BIT";
            float jitter = speaker == Speaker.Glitch && !GameSettings.ReduceEffects && Mathf.Repeat(Time.unscaledTime, .9f) < .08f ? 3 : 0;
            UiTheme.Label(new Rect(r.x + 82 + jitter, r.y + 8, 300, 20), name, 15, true, accent);
            UiTheme.Label(new Rect(r.x + 82, r.y + 28, r.width - 98, 48), subtitle, 18, false, UiTheme.Text);
        }

        void DrawIntro()
        {
            Rect f = UiTheme.FullScreen;
            float t = Mathf.Clamp01(introClock / .5f);
            UiTheme.Fill(new Rect(f.x, f.y, f.width, 70 * t), Color.black);
            UiTheme.Fill(new Rect(f.x, f.yMax - 70 * t, f.width, 70 * t), Color.black);
            if (introClock > 1.1f)
            {
                float a = Mathf.Clamp01((introClock - 1.1f) / .4f);
                UiTheme.Label(new Rect(240, 470, 800, 56), "GUARDIÁN DEL SISTEMA", 46, true, new Color(1, 1, 1, a), TextAnchor.MiddleCenter);
                UiTheme.Fill(new Rect(540, 528, 200 * a, 2), UiTheme.Threat);
                UiTheme.Label(new Rect(240, 536, 800, 28), "Protocolo de Seguridad 01 · Sector de Arranque", 20, false, new Color(UiTheme.Muted.r, UiTheme.Muted.g, UiTheme.Muted.b, a), TextAnchor.MiddleCenter);
            }
        }

        // ------------------------------------------------------------ menus
        void DrawMenu()
        {
            Rect f = UiTheme.FullScreen;
            UiTheme.Fill(f, new Color(.02f, .035f, .05f, won || defeated ? .82f : .72f));
            float height = menuPage == MenuPage.Options ? 420 : menuPage == MenuPage.Controls ? 420 : won ? 432 : 402;
            Rect panel = new Rect(360, 360 - height * .5f, 560, height);
            Color accent = won ? UiTheme.Ally : defeated ? UiTheme.Threat : UiTheme.Ally;
            UiTheme.DrawPanel(panel, accent);
            float x = panel.x + 40, w = panel.width - 80, y = panel.y + 30;
            if (menuPage == MenuPage.Options) { DrawOptions(x, y, w); return; }
            if (menuPage == MenuPage.Controls) { DrawControls(x, y, w); return; }

            string heading = won ? "SECTOR RESTAURADO" : defeated ? "SEÑAL PERDIDA" : paused ? "PAUSA" : "NIVEL CERO";
            UiTheme.Label(new Rect(x, y, w, 52), heading, 42, true, won ? UiTheme.Ally : defeated ? UiTheme.Threat : UiTheme.Text);
            y += 54;
            string body;
            if (won)
                body = "Bit: recuperamos el Sector de Arranque. Memoria Fragmentada, Torre Firewall y el Núcleo Corrupto todavía nos esperan.";
            else if (defeated)
                body = "La integridad de Zero llegó a cero. Bit puede restaurarte desde el último checkpoint; el progreso de fragmentos y firewall se conserva.";
            else if (paused)
                body = "Escape del Glitch · Sector 01. El sistema queda congelado mientras decidís.";
            else
                body = "ESCAPE DEL GLITCH · Escena académica 2D\nDos fragmentos, un circuito y un Guardián. La pasarela superior guarda un tercer fragmento opcional.";
            UiTheme.Label(new Rect(x, y, w, 80), body, 18, false, UiTheme.Muted);
            y += 92;
            if (won)
            {
                int minutes = Mathf.FloorToInt(elapsed / 60), seconds = Mathf.FloorToInt(elapsed % 60);
                DrawStat(new Rect(x, y, w / 3 - 8, 64), "TIEMPO", minutes + ":" + seconds.ToString("00"));
                DrawStat(new Rect(x + w / 3, y, w / 3 - 8, 64), "FRAGMENTOS", collected.Count + "/3");
                DrawStat(new Rect(x + 2 * w / 3, y, w / 3 - 8, 64), "CAÍDAS", deaths.ToString());
                y += 84;
            }
            if (!won && UiTheme.Button(new Rect(x, y, w, 46), defeated ? "REAPARECER EN CHECKPOINT" : started ? "CONTINUAR" : "INICIAR SECTOR 2D", true, true))
            {
                if (defeated) Respawn();
                else if (!started) StartGame();
                else { paused = false; SoundBank.Play("pause_out"); SetClock(); }
            }
            if (!won) y += 54;
            if (UiTheme.Button(new Rect(x, y, w / 2 - 6, 40), "OPCIONES")) menuPage = MenuPage.Options;
            if (UiTheme.Button(new Rect(x + w / 2 + 6, y, w / 2 - 6, 40), "CONTROLES")) menuPage = MenuPage.Controls;
            y += 48;
            if (UiTheme.Button(new Rect(x, y, w, 40), "REINICIAR ESCENA")) { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            y += 48;
            if (UiTheme.Button(new Rect(x, y, w, 40), "VOLVER AL MENÚ PRINCIPAL")) { Time.timeScale = 1; SceneManager.LoadScene("SectorDeArranque3D"); }
        }

        void DrawStat(Rect r, string label, string value)
        {
            UiTheme.Fill(r, new Color(.06f, .1f, .14f, 1));
            UiTheme.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, 18), label, 13, true, UiTheme.Muted);
            UiTheme.Label(new Rect(r.x + 10, r.y + 24, r.width - 20, 36), value, 30, true, UiTheme.Text);
        }

        void DrawOptions(float x, float y, float w)
        {
            UiTheme.Label(new Rect(x, y, w, 46), "OPCIONES", 36, true, UiTheme.Text);
            y += 64;
            float master = UiTheme.Slider(new Rect(x, y, w, 36), "Volumen general", GameSettings.Master);
            if (!Mathf.Approximately(master, GameSettings.Master)) GameSettings.Master = master;
            y += 44;
            float musicVolume = UiTheme.Slider(new Rect(x, y, w, 36), "Música", GameSettings.Music);
            if (!Mathf.Approximately(musicVolume, GameSettings.Music)) GameSettings.Music = musicVolume;
            y += 44;
            float sfx = UiTheme.Slider(new Rect(x, y, w, 36), "Efectos", GameSettings.Sfx);
            if (!Mathf.Approximately(sfx, GameSettings.Sfx)) GameSettings.Sfx = sfx;
            y += 52;
            bool reduce = UiTheme.Toggle(new Rect(x, y, w, 36), "Reducir efectos (sacudidas, destellos, glitch)", GameSettings.ReduceEffects);
            if (reduce != GameSettings.ReduceEffects) GameSettings.ReduceEffects = reduce;
            y += 52;
            if (UiTheme.Button(new Rect(x, y, w, 40), Screen.fullScreen ? "MODO VENTANA" : "PANTALLA COMPLETA")) Screen.fullScreen = !Screen.fullScreen;
            y += 56;
            if (UiTheme.Button(new Rect(x, y, w, 44), "GUARDAR Y VOLVER", true, true)) { GameSettings.Save(); menuPage = MenuPage.Main; }
        }

        void DrawControls(float x, float y, float w)
        {
            UiTheme.Label(new Rect(x, y, w, 46), "CONTROLES", 36, true, UiTheme.Text);
            y += 62;
            string[,] rows = {
                { "A / D  ·  flechas", "Moverse" }, { "Espacio  ·  W", "Saltar (mantener = más alto)" }, { "Shift", "Dash (una vez en el aire)" },
                { "Mouse + clic izq.", "Apuntar y atacar" }, { "1  /  2", "Pulse Blaster / Energy Blade" }, { "E", "Terminal o hablar con Bit" }, { "Esc", "Pausa · cancelar hack" } };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                UiTheme.Label(new Rect(x, y, w * .48f, 28), rows[i, 0], 18, true, UiTheme.Ally);
                UiTheme.Label(new Rect(x + w * .5f, y, w * .5f, 28), rows[i, 1], 18, false, UiTheme.Text);
                y += 34;
            }
            y += 14;
            if (UiTheme.Button(new Rect(x, y, w, 44), "VOLVER", true, true)) menuPage = MenuPage.Main;
        }

        // ------------------------------------------------------------ circuit hack
        void DrawCircuit()
        {
            Rect f = UiTheme.FullScreen;
            UiTheme.Fill(f, new Color(.01f, .02f, .03f, .78f));
            Rect panel = new Rect(300, 70, 680, 590);
            bool error = hackFeedback > 0;
            float jitter = error && !GameSettings.ReduceEffects ? Mathf.Sin(Time.unscaledTime * 90) * 3 * hackFeedback : 0;
            panel.x += jitter;
            UiTheme.DrawPanel(panel, error ? UiTheme.Threat : UiTheme.Ally);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 22, 620, 40), "HACK · FIREWALL DE SEGURIDAD", 30, true, UiTheme.Text);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 64, 620, 44), "Clic en un módulo para rotarlo. Uní la ENTRADA (izquierda, arriba) con la SALIDA (derecha, abajo). Luego VALIDAR.", 17, false, UiTheme.Muted);
            float remaining = Mathf.Clamp01(hackTime / 75);
            UiTheme.Bar(new Rect(panel.x + 30, panel.y + 116, 620, 8), remaining, remaining > .3f ? UiTheme.Ally : UiTheme.Threat);
            UiTheme.Label(new Rect(panel.x + 30, panel.y + 126, 620, 22), "TIEMPO " + Mathf.CeilToInt(hackTime) + " s · un error resta 5 s; agotar el tiempo activa la alarma", 14, true, remaining > .3f ? UiTheme.Muted : UiTheme.Threat);

            int size = circuit.Size;
            float cell = 104, left = panel.x + panel.width * .5f - size * cell * .5f, top = panel.y + 170;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Rect tile = new Rect(left + x * cell, top + y * cell, cell - 8, cell - 8);
                    bool hover = tile.Contains(Event.current.mousePosition);
                    bool powered = circuit.IsPowered(x, y);
                    UiTheme.Fill(tile, hover ? new Color(.09f, .17f, .22f, 1) : new Color(.05f, .09f, .12f, 1));
                    UiTheme.Fill(new Rect(tile.x, tile.yMax - 2, tile.width, 2), powered ? UiTheme.AllyDark : UiTheme.Line);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tile.Contains(Event.current.mousePosition))
                    {
                        circuit.Rotate(x, y); Event.current.Use(); SoundBank.Play("rotate", 1, 1, .08f, .02f);
                    }
                    Color c = powered ? UiTheme.Ally : new Color(.42f, .52f, .6f);
                    int mask = circuit.MaskAt(x, y);
                    Vector2 center = tile.center;
                    float half = tile.width * .5f, thick = 10;
                    UiTheme.Fill(new Rect(center.x - 9, center.y - 9, 18, 18), c);
                    if ((mask & CircuitPuzzle.North) != 0) UiTheme.Fill(new Rect(center.x - thick / 2, tile.y, thick, half), c);
                    if ((mask & CircuitPuzzle.South) != 0) UiTheme.Fill(new Rect(center.x - thick / 2, center.y, thick, half), c);
                    if ((mask & CircuitPuzzle.East) != 0) UiTheme.Fill(new Rect(center.x, center.y - thick / 2, half, thick), c);
                    if ((mask & CircuitPuzzle.West) != 0) UiTheme.Fill(new Rect(tile.x, center.y - thick / 2, half, thick), c);
                }
            // Source and destination ports, shape-coded (arrow in, square out).
            UiTheme.Fill(new Rect(left - 40, top + 40, 34, 16), UiTheme.Ally);
            UiTheme.Label(new Rect(left - 128, top + 28, 84, 40), "ENTRADA", 15, true, UiTheme.Ally, TextAnchor.MiddleRight);
            float outY = top + (size - 1) * cell + 40;
            UiTheme.Fill(new Rect(left + size * cell - 4, outY, 34, 16), circuit.IsSolved ? UiTheme.Ally : UiTheme.Line);
            UiTheme.Label(new Rect(left + size * cell + 36, outY - 12, 90, 40), "SALIDA", 15, true, circuit.IsSolved ? UiTheme.Ally : UiTheme.Muted, TextAnchor.MiddleLeft);

            float by = panel.yMax - 74;
            if (error) UiTheme.Label(new Rect(panel.x + 30, by - 34, 620, 26), "× SIN CONEXIÓN · -5 s", 18, true, UiTheme.Threat);
            else if (circuit.IsSolved) UiTheme.Label(new Rect(panel.x + 30, by - 34, 620, 26), "CIRCUITO CERRADO · listo para validar", 18, true, UiTheme.Ally);
            if (UiTheme.Button(new Rect(panel.x + 30, by, 250, 46), "VALIDAR CIRCUITO", true, true)) ValidateCircuit();
            if (UiTheme.Button(new Rect(panel.x + 292, by, 170, 46), "RESTABLECER")) circuit.Reset();
            if (UiTheme.Button(new Rect(panel.x + 474, by, 176, 46), "DESCONECTAR")) { hacking = false; SoundBank.Play("ui_back"); SetClock(); }
        }
    }
}
