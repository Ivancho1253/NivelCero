using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NivelCero
{
    // Functional Spanish UI for integration testing; production UI/art remains pending.
    public sealed class GameHud : MonoBehaviour
    {
        Session S { get { return Session.I; } }
        bool options, controls;
        GUIStyle heading, body;
        void Start()
        {
            var v = S.Player.View;
            v.Sensitivity = PlayerPrefs.GetFloat("sensitivity", .12f);
            v.Fov = PlayerPrefs.GetFloat("fov", 78);
            v.InvertY = PlayerPrefs.GetInt("invertY", 0) == 1;
            AudioListener.volume = PlayerPrefs.GetFloat("volume", .7f);
        }
        void OnGUI()
        {
            if (S == null || S.Player == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            }
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.color = Color.white;
            if (S.InMenu || S.Paused || S.Defeat || S.Victory)
            {
                GUI.Box(new Rect(0, 0, 1280, 720), "");
                GUILayout.BeginArea(new Rect(340, 65, 600, 610));
                GUILayout.Label("NIVEL CERO\nESCAPE DEL GLITCH", heading);
                GUILayout.Label("Sector de Arranque · reconstrucción 0.6-dev\nArte provisional · ejecución Unity pendiente de verificar", body);
                GUILayout.Space(22);
                if (options) Options();
                else if (controls)
                {
                    GUILayout.Label("WASD · mover | Mouse · mirar\nEspacio · saltar | Shift · dash | Ctrl · agacharse\nV · primera/tercera persona | C · hombro\n1 · Pulse Blaster | 2 · Energy Blade\nClic izquierdo · atacar | Clic derecho · apuntar/bloquear\nR · recargar | E · interactuar/Bit | Esc · pausa\nHack: clic para rotar; conectar oeste del origen con este del destino.", body);
                    if (Button("VOLVER")) controls = false;
                }
                else if (S.Defeat)
                {
                    GUILayout.Label("SEÑAL PERDIDA", heading);
                    if (Button("REAPARECER EN CHECKPOINT")) S.Respawn();
                    if (Button("MENÚ PRINCIPAL")) S.ReloadLevel();
                }
                else if (S.Victory)
                {
                    GUILayout.Label("SECTOR RESTAURADO", heading);
                    GUILayout.Label("Bit: el camino a Memoria Fragmentada está abierto.\nTiempo: " + Mathf.FloorToInt(S.Progress.elapsedSeconds) + " s · Fragmentos: " + S.Progress.FragmentCount + "/3", body);
                    if (Button("VOLVER AL MENÚ")) S.ReloadLevel();
                }
                else
                {
                    if (S.InMenu)
                    {
                        if (Button("NUEVA PARTIDA")) S.StartGame(false);
                        GUI.enabled = File.Exists(S.SavePath);
                        if (Button("CONTINUAR")) S.StartGame(true);
                        GUI.enabled = true;
                        if (Button("ESCENA ACADÉMICA 2D")) SceneManager.LoadScene("AE4_2D_SectorDeArranque");
                    }
                    else
                    {
                        if (Button("CONTINUAR")) S.Paused = false;
                        if (Button("GUARDAR PARTIDA")) { S.Save(); S.Notify("Progreso guardado."); }
                        if (Button("MENÚ PRINCIPAL")) { S.Save(); S.ReloadLevel(); }
                    }
                    if (Button("OPCIONES")) options = true;
                    if (Button("CONTROLES")) controls = true;
                    if (Button("SALIR")) Application.Quit();
                }
                GUILayout.EndArea();
            }
            else if (S.Puzzle != null) Hack();
            else
            {
                GUI.Box(new Rect(25, 22, 325, 105), "");
                GUI.Label(new Rect(40, 32, 300, 25), "ZERO · INTEGRIDAD " + Mathf.CeilToInt(S.PlayerHealth.Current) + " / 100", body);
                GUI.Label(new Rect(40, 60, 300, 25), "FRAGMENTOS " + S.Progress.FragmentCount + "/3 · DASH " + (S.Player.DashReady ? "LISTO" : "RECARGANDO"), body);
                GUI.Label(new Rect(40, 88, 300, 25), S.Combat.WeaponName + " · " + (S.Combat.Reloading ? "RECARGANDO" : S.Combat.Ammo + "/18"), body);
                string goal = !S.Progress.RequiredFragmentsRecovered ? "Recuperá los dos fragmentos principales" : !S.Progress.bossDefeated ? "Hackeá el escudo y derrotá al Guardián" : "Entrá al portal de salida";
                GUI.Label(new Rect(850, 28, 400, 60), "OBJETIVO\n" + goal, body);
                GUI.Label(new Rect(635, 350, 30, 30), "+", body);
                var scan = S.Player.GetComponent<InteractionScanner>();
                GUI.Label(new Rect(470, 580, 430, 35), scan.Prompt, body);
                if (S.Boss.Active && !S.Progress.bossDefeated)
                {
                    GUI.Box(new Rect(360, 30, 480, 65), "GUARDIÁN · FASE " + S.Boss.Phase + " · " + Mathf.CeilToInt(S.Boss.Health.Current));
                    GUI.Label(new Rect(380, 58, 450, 30), S.Boss.Telegraphing ? "¡DESCARGA MARCADA!" : S.Boss.Shielded ? "ESCUDO ACTIVO · HACKEÁ UN NODO" : "NÚCLEO EXPUESTO · " + Mathf.CeilToInt(S.Boss.VulnerableRemaining) + " s", body);
                }
            }
            if (Time.unscaledTime < S.MessageUntil || S.InMenu)
                GUI.Label(new Rect(230, 650, 850, 65), S.Message, body);
            GUI.matrix = saved;
        }
        void Options()
        {
            var v = S.Player.View;
            GUILayout.Label("Sensibilidad " + v.Sensitivity.ToString("F2"), body);
            v.Sensitivity = GUILayout.HorizontalSlider(v.Sensitivity, .02f, .5f);
            GUILayout.Label("Campo de visión " + Mathf.RoundToInt(v.Fov), body);
            v.Fov = GUILayout.HorizontalSlider(v.Fov, 60, 110);
            v.InvertY = GUILayout.Toggle(v.InvertY, "Invertir mouse vertical");
            GUILayout.Label("Volumen", body); AudioListener.volume = GUILayout.HorizontalSlider(AudioListener.volume, 0, 1);
            if (Button(Screen.fullScreen ? "MODO VENTANA" : "PANTALLA COMPLETA")) Screen.fullScreen = !Screen.fullScreen;
            GUILayout.BeginHorizontal();
            if (Button("1280 × 720")) Screen.SetResolution(1280, 720, Screen.fullScreen);
            if (Button("1920 × 1080")) Screen.SetResolution(1920, 1080, Screen.fullScreen);
            GUILayout.EndHorizontal();
            if (Button("CALIDAD: " + QualitySettings.names[QualitySettings.GetQualityLevel()])) QualitySettings.SetQualityLevel((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
            if (Button("GUARDAR Y VOLVER"))
            {
                PlayerPrefs.SetFloat("sensitivity", v.Sensitivity); PlayerPrefs.SetFloat("fov", v.Fov);
                PlayerPrefs.SetInt("invertY", v.InvertY ? 1 : 0); PlayerPrefs.SetFloat("volume", AudioListener.volume); PlayerPrefs.Save(); options = false;
            }
        }
        void Hack()
        {
            GUI.Box(new Rect(290, 40, 700, 610), "");
            GUI.Label(new Rect(330, 55, 620, 40), "HACK // " + S.ActiveTerminal.Label, heading);
            GUI.Label(new Rect(330, 100, 620, 55), "Conectá ORIGEN (oeste) con DESTINO (este).\nClic para rotar · " + Mathf.CeilToInt(S.HackRemaining) + " s", body);
            int size = S.Puzzle.Size; float cell = 75, left = 640 - size * cell / 2;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var r = new Rect(left + x * cell, 175 + y * cell, cell - 4, cell - 4);
                GUI.backgroundColor = S.Puzzle.IsPowered(x, y) ? Color.cyan : new Color(.45f, .5f, .6f);
                if (GUI.Button(r, "")) S.Puzzle.Rotate(x, y);
                GUI.backgroundColor = Color.white;
                int mask = S.Puzzle.MaskAt(x, y); float cx = r.center.x, cy = r.center.y;
                var prev = GUI.color; GUI.color = S.Puzzle.IsPowered(x, y) ? Color.cyan : Color.white;
                if ((mask & 1) != 0) GUI.DrawTexture(new Rect(cx - 3, r.y + 5, 6, r.height / 2), Texture2D.whiteTexture);
                if ((mask & 2) != 0) GUI.DrawTexture(new Rect(cx, cy - 3, r.width / 2 - 5, 6), Texture2D.whiteTexture);
                if ((mask & 4) != 0) GUI.DrawTexture(new Rect(cx - 3, cy, 6, r.height / 2 - 5), Texture2D.whiteTexture);
                if ((mask & 8) != 0) GUI.DrawTexture(new Rect(r.x + 5, cy - 3, r.width / 2, 6), Texture2D.whiteTexture);
                GUI.color = prev;
            }
            if (GUI.Button(new Rect(340, 505, 180, 45), "VALIDAR CIRCUITO")) S.ValidateHack();
            if (GUI.Button(new Rect(550, 505, 170, 45), "REINICIAR")) S.Puzzle.Reset();
            if (GUI.Button(new Rect(750, 505, 180, 45), "CANCELAR / ESC")) S.CancelHack(true);
            GUI.Label(new Rect(330, 570, 620, 55), "ORIGEN: primera celda superior izquierda\nDESTINO: última celda inferior derecha", body);
        }
        bool Button(string label) { return GUILayout.Button(label, GUILayout.MinHeight(36)); }
    }
}
