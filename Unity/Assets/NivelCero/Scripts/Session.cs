using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using NivelCero.Core;

namespace NivelCero
{
    // Coordinates state only; movement, combat, circuits and world behaviour live separately.
    public sealed class Session : MonoBehaviour
    {
        public static Session I { get; private set; }
        public ProgressData Progress = new ProgressData();
        public PlayerMotor Player;
        public PlayerCombat Combat;
        public Health PlayerHealth;
        public Guardian Boss;
        public GameObject AccessDoor;
        public bool WorldIsBaked;
        public Material CyanMaterial, RedMaterial, ExposedMaterial;
        public bool InMenu = true, Paused, Defeat, Victory;
        public bool Blocked { get { return InMenu || Paused || Defeat || Victory || Puzzle != null; } }
        public CircuitPuzzle Puzzle { get; private set; }
        public Terminal ActiveTerminal { get; private set; }
        public string Message = "Bit: Zero, despertá. GL1TCH está corrompiendo el sistema.";
        public float MessageUntil;
        float hackRemaining;
        bool hurtHooked;
        /// <summary>1 right after Zero takes damage, fading to 0 (HUD vignette).</summary>
        public float HurtFlash;
        int attempts;
        public float HackRemaining { get { return hackRemaining; } }
        public int Attempts { get { return attempts; } }
        [NonSerialized] public string SavePathOverride;
        public string SavePath { get { return string.IsNullOrEmpty(SavePathOverride) ? Path.Combine(Application.persistentDataPath, "nivel-cero-v1.json") : SavePathOverride; } }
        public readonly Vector3[] SpawnPoints = {new Vector3(0, 1, 2), new Vector3(0, 1, 44), new Vector3(0, 1, 83)};

        void Awake()
        {
            I = this;
            if (!Application.isEditor) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
        }
        void Start()
        {
            SoundBank.Ensure(); GameSettings.Apply();
            if (!WorldIsBaked) new SectorBuilder().Build(this);
            else { SectorBuilder.Cyan = CyanMaterial; SectorBuilder.Red = RedMaterial; SectorBuilder.Exposed = ExposedMaterial; }
            SyncCursor();
        }
        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                if (Puzzle != null) CancelHack(true);
                else if (!InMenu && !Defeat && !Victory) { Paused = !Paused; SoundBank.Play(Paused ? "pause_in" : "pause_out"); }
            }
            if (PlayerHealth != null && !hurtHooked) { PlayerHealth.Hurt += (amount, origin) => { SoundBank.Play("hurt", .9f); HurtFlash = 1; }; hurtHooked = true; }
            HurtFlash = Mathf.Max(0, HurtFlash - Time.unscaledDeltaTime * 2);
            if (!Blocked)
            {
                Progress.elapsedSeconds += Time.deltaTime;
                if (Player.transform.position.y < -12) PlayerDied();
            }
            if (Puzzle != null)
            {
                hackRemaining -= Time.unscaledDeltaTime;
                if (hackRemaining <= 0) CancelHack(true);
            }
            SyncCursor();
        }
        /// <summary>Players get an automatic pause on focus loss; automation can opt out.</summary>
        [NonSerialized] public bool PauseWhenUnfocused = true;
        void OnApplicationFocus(bool focus) { if (PauseWhenUnfocused && !focus && !InMenu) Paused = true; }
        void OnDestroy() { Time.timeScale = 1; if (I == this) I = null; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void SyncCursor()
        {
            Time.timeScale = Blocked ? 0 : 1;
            Cursor.lockState = Blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Blocked;
        }
        public void Notify(string text)
        {
            Message = text; MessageUntil = Time.unscaledTime + Mathf.Clamp(text.Length * .07f, 4, 8);
            if (text.StartsWith("Bit:")) BitFollower.React();
            else if (text.StartsWith("GL1TCH")) SoundBank.Play("glitch_voice", .8f, 1, .03f, .4f);
        }
        public void StartGame(bool load)
        {
            Progress = new ProgressData();
            if (load)
            {
                try
                {
                    var loaded = JsonUtility.FromJson<ProgressData>(File.ReadAllText(SavePath));
                    if (loaded == null || !loaded.IsValid()) throw new InvalidDataException("Formato de progreso inválido.");
                    Progress = loaded;
                }
                catch (Exception e) { Progress = new ProgressData(); Notify("No se pudo cargar la partida: " + e.Message); }
            }
            Progress.checkpointId = Mathf.Clamp(Progress.checkpointId, 0, SpawnPoints.Length - 1);
            InMenu = Paused = Defeat = Victory = false;
            ApplyProgress(); Respawn();
            if (!load) Notify("Bit: Zero, despertaste en el Sector de Arranque. GL1TCH está corrompiendo el sistema. Reuní dos fragmentos y abrí la salida.");
        }
        public void ApplyProgress()
        {
            AccessDoor.SetActive(!Progress.accessUnlocked);
            foreach (var shard in FindObjectsByType<CodeShard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                shard.gameObject.SetActive(!Progress.HasFragment(shard.Id));
            Boss.gameObject.SetActive(!Progress.bossDefeated);
        }
        public void Save()
        {
            try
            {
                var temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Progress, true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
            }
            catch (Exception e) { Notify("Error de guardado: " + e.Message); Debug.LogException(e); }
        }
        public void Checkpoint(int id)
        {
            if (id <= Progress.checkpointId) return;
            Progress.checkpointId = id; PlayerHealth.Restore(); Save(); Notify("Bit: checkpoint sincronizado. Integridad restaurada.");
            SoundBank.Play("checkpoint");
        }
        public void PlayerDied() { if (Defeat || Victory || InMenu) return; CancelHack(false); Defeat = true; Notify("La señal de Zero se perdió."); SoundBank.Play("death"); }
        public void Respawn()
        {
            if (Defeat) SoundBank.Play("respawn");
            Defeat = false; Paused = false;
            Player.Respawn(SpawnPoints[Progress.checkpointId]); PlayerHealth.Restore();
            foreach (var pulse in FindObjectsByType<HostilePulse>(FindObjectsSortMode.None)) Destroy(pulse.gameObject);
            foreach (var enemy in FindObjectsByType<SecurityAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { enemy.gameObject.SetActive(true); enemy.ResetEncounter(); }
            if (!Progress.bossDefeated) Boss.ResetEncounter();
        }
        public void BeginHack(Terminal terminal)
        {
            if (terminal == null || Blocked || Time.time < terminal.LockedUntil) return;
            if (terminal.BossTerminal && (Progress.bossDefeated || !Progress.RequiredFragmentsRecovered))
            { Notify("Bit: recuperá los dos fragmentos principales antes de intervenir el escudo."); return; }
            ActiveTerminal = terminal; Puzzle = new CircuitPuzzle(terminal.Seed + attempts, terminal.Size);
            SoundBank.Play("terminal_open");
            hackRemaining = terminal.TimeLimit;
        }
        public void ValidateHack()
        {
            if (Puzzle == null) return;
            if (!Puzzle.IsSolved) { Notify("Circuito abierto: conectá origen y destino antes de validar."); SoundBank.Play("hack_error"); return; }
            var target = ActiveTerminal; Puzzle = null; ActiveTerminal = null;
            target.Succeed(); attempts++; SoundBank.Play("hack_success");
        }
        public void CancelHack(bool failure)
        {
            if (Puzzle == null) return;
            if (failure) { ActiveTerminal.LockedUntil = Time.time + 5; Notify("GL1TCH: alarma activada. Terminal bloqueada durante 5 s."); attempts++; SoundBank.Play("alarm"); SoundBank.Play("hack_error"); }
            else SoundBank.Play("ui_back");
            Puzzle = null; ActiveTerminal = null;
        }
        public void Complete()
        {
            if (!Progress.CanExit) { Notify("Faltan los dos fragmentos principales o derrotar al Guardián."); return; }
            Victory = true; Save(); Notify("Bit: el Sector de Arranque está libre. Memoria Fragmentada nos espera.");
            SoundBank.Play("portal_enter");
        }
        public void ReloadLevel() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }
}
