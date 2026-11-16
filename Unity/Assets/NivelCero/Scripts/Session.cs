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
        public Material CyanMaterial, RedMaterial;
        public bool InMenu = true, Paused, Defeat, Victory;
        public bool Blocked { get { return InMenu || Paused || Defeat || Victory || Puzzle != null; } }
        public CircuitPuzzle Puzzle { get; private set; }
        public Terminal ActiveTerminal { get; private set; }
        public string Message = "Bit: Zero, despertá. GL1TCH está corrompiendo el sistema.";
        public float MessageUntil;
        float hackRemaining;
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
            if (!WorldIsBaked) new SectorBuilder().Build(this);
            else { SectorBuilder.Cyan = CyanMaterial; SectorBuilder.Red = RedMaterial; }
            SyncCursor();
        }
        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                if (Puzzle != null) CancelHack(true);
                else if (!InMenu && !Defeat && !Victory) Paused = !Paused;
            }
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
        void OnApplicationFocus(bool focus) { if (!focus && !InMenu) Paused = true; }
        void OnDestroy() { Time.timeScale = 1; if (I == this) I = null; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void SyncCursor()
        {
            Time.timeScale = Blocked ? 0 : 1;
            Cursor.lockState = Blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Blocked;
        }
        public void Notify(string text) { Message = text; MessageUntil = Time.unscaledTime + 6; }
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
            Progress.checkpointId = id; PlayerHealth.Restore(); Save(); Notify("Checkpoint guardado. Integridad restaurada.");
        }
        public void PlayerDied() { if (Defeat || Victory || InMenu) return; CancelHack(false); Defeat = true; Notify("La señal de Zero se perdió."); }
        public void Respawn()
        {
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
            hackRemaining = terminal.TimeLimit;
        }
        public void ValidateHack()
        {
            if (Puzzle == null) return;
            if (!Puzzle.IsSolved) { Notify("Circuito abierto: conectá origen y destino antes de validar."); return; }
            var target = ActiveTerminal; Puzzle = null; ActiveTerminal = null;
            target.Succeed(); attempts++; Tone.Play(700, .14f);
        }
        public void CancelHack(bool failure)
        {
            if (Puzzle == null) return;
            if (failure) { ActiveTerminal.LockedUntil = Time.time + 5; Notify("Alarma: terminal bloqueada durante 5 s."); attempts++; }
            Puzzle = null; ActiveTerminal = null;
        }
        public void Complete()
        {
            if (!Progress.CanExit) { Notify("Faltan los dos fragmentos principales o derrotar al Guardián."); return; }
            Victory = true; Save(); Notify("Bit: el Sector de Arranque está libre. Memoria Fragmentada nos espera.");
        }
        public void ReloadLevel() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }
}
