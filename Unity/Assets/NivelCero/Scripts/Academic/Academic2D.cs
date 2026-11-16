using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using NivelCero.Core;

namespace NivelCero
{
    /// <summary>
    /// AE4 2D scene: Sector de Arranque. Tilemap + Rigidbody2D platforming,
    /// three-layer parallax, Bit as autonomous/interactive NPC (Bit2D), circuit
    /// hack and firewall, and the Guardian boss (Academic2D.Boss.cs). Original art
    /// from Tools/Art/generate_2d_art.py and audio from Tools/Audio/generate_audio.py.
    /// Interface lives in Academic2D.UI.cs; level layout in Academic2DLevel.
    /// </summary>
    public sealed partial class Academic2D : MonoBehaviour
    {
        [Tooltip("Atlas 0.6 conservado como referencia histórica; el arte actual se carga de Resources/Academic2D.")]
        public Texture2D Tilesheet;
        [Header("Tilemaps editables (opcional: se generan al ejecutar si están vacíos)")]
        public Grid LevelGrid;
        public Tilemap GroundMap, PlatformMap, HazardMap, BackMap, DecorMap, CorruptionMap;
        public bool PauseWhenUnfocused = true;

        public bool IsPaused { get { return paused; } }
        public bool GateUnlocked { get { return gateUnlocked; } }
        public bool LevelCompleted { get { return won; } }
        public float CurrentHealth { get { return hp; } }
        public float BossHealth { get { return bossHP; } }
        public int RequiredFragments { get { return (collected.Contains(0) ? 1 : 0) + (collected.Contains(1) ? 1 : 0); } }
        public int FragmentCount { get { return collected.Count; } }
        public Vector2 PlayerPosition { get { return player != null ? player.position : Vector2.zero; } }
        public Vector2 PlayerVelocity { get { return player != null ? player.linearVelocity : Vector2.zero; } }
        public int Facing { get { return facing; } }
        public Camera GameCamera { get { return view; } }
        public Vector2 GuardianPosition { get { return guardian != null ? (Vector2)guardian.transform.position : Vector2.zero; } }
        public Vector2 ActiveCheckpoint { get { return checkpoint; } }
        public int DeathCount { get { return deaths; } }
        public bool IsGrounded { get { return grounded; } }
        public CircuitPuzzle ActiveCircuit { get { return hacking ? circuit : null; } }
        public bool InCinematic { get { return bossState == BossState.Intro; } }
        public Vector2 TerminalPosition { get { return terminal != null ? (Vector2)terminal.transform.position : Vector2.zero; } }
        public Bit2D Companion { get { return bitBrain; } }
        public bool HasFragment(int id) { return collected.Contains(id); }

        // The same commands back the menu and circuit UI; automation can use
        // these without changing health, position, progression or encounter state.
        public void StartGame()
        {
            if (started) return;
            started = true; paused = false;
            Say(Speaker.Bit, "Zero, despertá. GL1TCH está corrompiendo este sector. Seguime: las luces cian marcan lo que podemos usar.");
            music.SetMood(MusicDirector.Mood.Explore);
            SetClock();
        }
        public bool ValidateCircuit() { return CommitHack(); }
        /// <summary>Same command as the defeat screen's "REAPARECER EN CHECKPOINT" button.</summary>
        public void RespawnFromMenu() { if (defeated) Respawn(); }
        public bool IsDefeated { get { return defeated; } }
        public bool IsDying { get { return dying; } }

        enum Speaker { Bit, Glitch, Guardian, System }
        sealed class Shard { public int Id; public Transform Visual; public SpriteAnimator2D Anim; public Vector3 Home; }

        Academic2DArt art;
        MusicDirector music;
        Camera view;
        Rigidbody2D player;
        SpriteRenderer playerSprite, armSprite, bit, terminal, firewall, portal, door, terminalPrompt;
        SpriteAnimator2D playerAnim, terminalAnim, portalAnim;
        AudioSource firewallHum;
        Bit2D bitBrain;
        readonly List<Shard> shards = new List<Shard>();
        readonly List<SpriteRenderer> markers = new List<SpriteRenderer>();
        readonly List<SpriteAnimator2D> markerAnims = new List<SpriteAnimator2D>();
        readonly Vector2[] checkpointPositions = { new Vector2(4, 2), new Vector2(32, 2), new Vector2(73, 2) };
        readonly HashSet<int> collected = new HashSet<int>();
        readonly List<Object> ownedObjects = new List<Object>();
        readonly Dictionary<string, Tile> runtimeTiles = new Dictionary<string, Tile>();
        PhysicsMaterial2D bodyMaterial;
        CircuitPuzzle circuit;
        bool hacking, gateUnlocked;
        float hackTime, terminalCooldown, hackFeedback, hitFeedback;
        int hackAttempts;
        Vector2 checkpoint = new Vector2(4, 2);
        bool started, paused, defeated, won, grounded, airDash, blade, dying, wasGrounded, portalAnnounced;
        float hp = 100, damageGrace, fireDelay, dashDelay, dashTime, jumpBuffer, coyote;
        float subtitleTime, hurtTime, landTime, dyingTime, aimHold, shake, damageFlash, elapsed, fallSpeed, attackPose, doorOffset = 9;
        int facing = 1, deaths, lastStepFrame = -1;
        Vector2 aimDirection = Vector2.right;
        string subtitle = "";
        Speaker speaker = Speaker.Bit;
        const int PlayerLayer = 2;
        const int SolidMask = ~(1 << PlayerLayer);
        readonly Collider2D[] groundHits = new Collider2D[4];
        ContactFilter2D groundFilter;

        void Start()
        {
            if (!Application.isEditor) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
            art = Academic2DArt.Load();
            if (art == null) { enabled = false; return; }
            SoundBank.Ensure();
            music = MusicDirector.Ensure();
            music.SetMood(MusicDirector.Mood.Silent);
            bodyMaterial = new PhysicsMaterial2D("Zero sin fricción") { friction = 0, bounciness = 0 };
            // Ground is solid geometry only: triggers (Bit, Guardian hull) never count as floor.
            groundFilter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = SolidMask };
            BuildLevel();
            BuildCamera();
            BuildPlayer();
            BuildInteractables();
            BuildBit();
            BuildBoss();
            Time.timeScale = 0;
        }

        // ------------------------------------------------------------ construction
        void BuildCamera()
        {
            var cameraObject = new GameObject("Cámara 2D");
            cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            view.orthographic = true; view.orthographicSize = 5.625f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color32(0x07, 0x0C, 0x12, 0xFF);
            view.transform.position = new Vector3(6, 4, -10);
            cameraPosition = view.transform.position;
        }

        void BuildPlayer()
        {
            playerSprite = Visual("Zero 2D", art.Zero["idle"][0], new Vector3(4, 2, 0), 10);
            playerSprite.gameObject.layer = PlayerLayer;
            playerAnim = playerSprite.gameObject.AddComponent<SpriteAnimator2D>();
            player = playerSprite.gameObject.AddComponent<Rigidbody2D>();
            player.gravityScale = 3; player.freezeRotation = true;
            player.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.interpolation = RigidbodyInterpolation2D.Interpolate;
            var body = player.gameObject.AddComponent<CapsuleCollider2D>();
            body.size = new Vector2(.6f, 1.5f);
            body.sharedMaterial = bodyMaterial;
            armSprite = Visual("Brazo y arma de Zero", art.ArmBlaster[4], Vector3.zero, 11);
            armSprite.transform.SetParent(playerSprite.transform, false);
            armSprite.gameObject.layer = PlayerLayer;
            playerAnim.Play("idle", art.Zero["idle"], 6);
        }

        void BuildBit()
        {
            bit = Visual("Bit - NPC autónomo", art.Bit["idle"][0], new Vector3(2.6f, 3.4f, 0), 12);
            bitBrain = bit.gameObject.AddComponent<Bit2D>();
            bitBrain.Init(this, art);
        }

        void BuildInteractables()
        {
            terminal = Visual("Terminal - Firewall de seguridad", art.Fx["terminal"][0], new Vector3(64, 1, 0), 6);
            terminalAnim = terminal.gameObject.AddComponent<SpriteAnimator2D>();
            terminalAnim.Play("idle", new[] { art.Fx["terminal"][0], art.Fx["terminal"][1] }, 2.5f);
            terminalPrompt = Visual("Indicador [E]", art.Fx["keyE"][0], new Vector3(64, 3.1f, 0), 20);
            terminalPrompt.enabled = false;

            // Firewall: physical red laser grid (hostile language) with its collider.
            firewall = Visual("Firewall - circuito obligatorio", art.Barrier[0], new Vector3(68, 5, 0), 7);
            firewall.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1, 9);
            firewall.gameObject.AddComponent<SpriteAnimator2D>().Play("laser", new[] { art.Barrier[0], art.Barrier[1] }, 8);
            for (int y = -4; y <= 4; y++)
            {
                if (y == 0) continue;
                var row = Visual("Láser " + y, art.Barrier[(y + 4) % 2], Vector3.zero, 7);
                row.transform.SetParent(firewall.transform, false); row.transform.localPosition = new Vector3(0, y, 0);
                row.gameObject.AddComponent<SpriteAnimator2D>().Play("laser", (y % 2 == 0) ? new[] { art.Barrier[0], art.Barrier[1] } : new[] { art.Barrier[1], art.Barrier[0] }, 8);
            }
            firewallHum = firewall.gameObject.AddComponent<AudioSource>();
            firewallHum.clip = SoundBank.Clip("firewall_loop"); firewallHum.loop = true; firewallHum.volume = 0; firewallHum.Play();
            var emitter = Visual("Emisor del firewall", art.Barrier[3], Vector3.zero, 8);
            emitter.transform.SetParent(firewall.transform, false); emitter.transform.localPosition = new Vector3(0, 4.6f, 0);
            emitter.flipY = true;

            // Arena shutter: raised until the Guardian wakes, then it seals the fight.
            door = Visual("Compuerta de la arena", art.Barrier[2], new Vector3(Academic2DLevel.ArenaDoorX, 5 + doorOffset, 0), 7);
            var doorCollider = door.gameObject.AddComponent<BoxCollider2D>(); doorCollider.size = new Vector2(1, 9); doorCollider.enabled = false;
            for (int y = -4; y <= 4; y++)
            {
                if (y == 0) continue;
                var row = Visual("Placa " + y, art.Barrier[2], Vector3.zero, 7);
                row.transform.SetParent(door.transform, false); row.transform.localPosition = new Vector3(0, y, 0);
            }

            portal = Visual("Portal de salida", art.Portal[0], new Vector3(102, 1, 0), 6);
            portalAnim = portal.gameObject.AddComponent<SpriteAnimator2D>();

            for (int i = 0; i < checkpointPositions.Length; i++)
            {
                var beacon = Visual("Checkpoint " + i, art.Fx["beacon"][0], new Vector3(checkpointPositions[i].x, 1, 0), 6);
                markers.Add(beacon);
                markerAnims.Add(beacon.gameObject.AddComponent<SpriteAnimator2D>());
            }
            AddShard(0, new Vector2(25, 2)); AddShard(1, new Vector2(59, 2)); AddShard(2, new Vector2(42, 5.3f));
        }

        SpriteRenderer Visual(string label, Sprite sprite, Vector3 position, int order)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform); obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = order;
            return renderer;
        }

        void AddShard(int id, Vector2 position)
        {
            var visual = Visual(id == 2 ? "Fragmento opcional" : "Fragmento " + id, art.Fx["shard"][0], position, 9);
            var anim = visual.gameObject.AddComponent<SpriteAnimator2D>();
            anim.Play("spin", art.Fx["shard"], 10);
            shards.Add(new Shard { Id = id, Visual = visual.transform, Anim = anim, Home = position });
        }

        // ------------------------------------------------------------ frame loop
        void Update()
        {
            if (player == null) return;
            Keyboard k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame && started && !defeated && !won && !dying)
            {
                if (hacking) { hacking = false; SetClock(); SoundBank.Play("ui_back"); Say(Speaker.Bit, "Conexión cancelada. El firewall sigue activo."); }
                else { paused = !paused; menuPage = MenuPage.Main; SoundBank.Play(paused ? "pause_in" : "pause_out"); SetClock(); }
            }
            music.Muffled = paused || hacking || !started;
            if (hacking)
            {
                hackTime -= Time.unscaledDeltaTime;
                hackFeedback = Mathf.Max(0, hackFeedback - Time.unscaledDeltaTime);
                if (hackTime <= 0) FailHack();
                return;
            }
            if (!started || paused || defeated || won) return;
            float dt = Time.deltaTime;
            elapsed += dt;
            terminalCooldown = Mathf.Max(0, terminalCooldown - dt);
            hitFeedback = Mathf.Max(0, hitFeedback - dt);
            jumpBuffer -= dt; coyote -= dt; damageGrace -= dt; fireDelay -= dt; dashDelay -= dt; dashTime -= dt; subtitleTime -= dt;
            hurtTime -= dt; landTime -= dt; aimHold -= dt; attackPose -= dt;
            shake = Mathf.Max(0, shake - dt * 2.5f); damageFlash = Mathf.Max(0, damageFlash - dt * 2);

            if (dying)
            {
                dyingTime -= dt;
                if (dyingTime <= 0) { dying = false; defeated = true; deaths++; music.Stinger("stinger_defeat"); SetClock(); }
                UpdateCamera(dt); UpdateParallax(); UpdateBoss(dt); UpdateHostiles(dt);
                return;
            }

            bool control = !InCinematic;
            if (control && k != null)
            {
                if (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) jumpBuffer = .14f;
                if (k.digit1Key.wasPressedThisFrame && blade) { blade = false; SoundBank.Play("ui_move", 1, .8f); }
                if (k.digit2Key.wasPressedThisFrame && !blade) { blade = true; SoundBank.Play("ui_move", 1, 1.2f); }
                if ((k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame) && dashDelay <= 0 && (!airDash || grounded))
                {
                    dashTime = .16f; dashDelay = .9f;
                    if (!grounded) airDash = true;
                    SoundBank.Play("dash");
                    SpawnFx("dust", player.position + Vector2.down * .75f, 14, false, 8);
                }
                if (k.eKey.wasPressedThisFrame) Interact();
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && view != null)
            {
                Vector2 aim = view.ScreenToWorldPoint(mouse.position.ReadValue());
                Vector2 delta = aim - ShoulderWorld();
                if (delta.sqrMagnitude > .04f) aimDirection = delta.normalized;
                if (control && mouse.leftButton.isPressed && fireDelay <= 0) Attack();
            }
            if (player.position.y < -7) Kill();

            if (firewallHum != null && firewallHum.isActiveAndEnabled)
                firewallHum.volume = GameSettings.Sfx * .6f * Mathf.Clamp01(1 - Mathf.Abs(player.position.x - 68) / 12);
            UpdatePickups(dt);
            UpdateCheckpoints();
            UpdateCamera(dt);
            UpdateParallax();
            UpdateBoss(dt);
            UpdateHostiles(dt);
            UpdateEffects(dt);
            UpdatePlayerVisual();
            UpdatePortal();
        }

        void FixedUpdate()
        {
            if (player == null || !started || paused || defeated || won || hacking) return;
            Keyboard k = Keyboard.current;
            Vector2 velocity = player.linearVelocity;
            // Ground only while not rising: lets the one-way catwalk be crossed from below.
            grounded = velocity.y <= .1f && Physics2D.OverlapCircle(player.position + Vector2.down * .78f, .14f, groundFilter, groundHits) > 0;
            if (grounded) { coyote = .1f; airDash = false; }
            if (grounded && !wasGrounded && fallSpeed < -7)
            {
                landTime = .1f;
                SoundBank.Play("land", Mathf.InverseLerp(-7, -22, fallSpeed) * .6f + .4f);
                SpawnFx("dust", player.position + Vector2.down * .75f, 14, false, 8);
            }
            wasGrounded = grounded;
            fallSpeed = velocity.y;
            bool control = !dying && !InCinematic;
            float axis = !control || k == null ? 0 : ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1 : 0) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1 : 0);
            if (axis != 0 && aimHold <= 0) facing = axis < 0 ? -1 : 1;
            velocity.x = Mathf.MoveTowards(velocity.x, axis * 7, (grounded ? 75 : 55) * Time.fixedDeltaTime);
            if (control && jumpBuffer > 0 && coyote > 0)
            {
                velocity.y = 14; jumpBuffer = coyote = 0; grounded = false;
                SoundBank.Play("jump");
                SpawnFx("dust", player.position + Vector2.down * .75f, 14, false, 8);
            }
            // Variable jump height: releasing early shortens the arc (more control).
            if (control && k != null && velocity.y > 4 && !(k.spaceKey.isPressed || k.wKey.isPressed || k.upArrowKey.isPressed)) velocity.y = Mathf.MoveTowards(velocity.y, 4, 60 * Time.fixedDeltaTime);
            if (dashTime > 0 && control) { velocity.x = (axis == 0 ? facing : axis) * 19; velocity.y = Mathf.Max(0, velocity.y); }
            if (dying) velocity.x = Mathf.MoveTowards(velocity.x, 0, 40 * Time.fixedDeltaTime);
            player.linearVelocity = velocity;
        }

        Vector2 ShoulderWorld()
        {
            string anim = playerAnim != null && playerAnim.Current != null ? playerAnim.Current : "idle";
            Vector2 offset = art.Shoulder(anim, playerAnim != null ? playerAnim.Frame : 0);
            offset.x *= facing;
            return (Vector2)playerSprite.transform.position + offset;
        }

        void Interact()
        {
            if (!gateUnlocked && Vector2.Distance(player.position, terminal.transform.position + Vector3.up) < 3)
            {
                if (terminalCooldown > 0) { SoundBank.Play("hack_error", .6f); Say(Speaker.Bit, "Alarma activa. El terminal reinicia en " + Mathf.CeilToInt(terminalCooldown) + " s."); }
                else
                {
                    circuit = new CircuitPuzzle(413 + hackAttempts, 3); hackTime = 75; hacking = true; hackFeedback = 0;
                    SoundBank.Play("terminal_open"); SetClock();
                }
                return;
            }
            if (bitBrain != null && bitBrain.PlayerInRange) bitBrain.Talk();
        }

        public void BitSays(string text) { Say(Speaker.Bit, text); }

        /// <summary>Context line chosen by Bit when Zero talks to it (E).</summary>
        public string BitAdvice()
        {
            if (bossActive && bossHP > 0)
                return BossVulnerable ? "¡Núcleo expuesto! Ahora: todo el daño entra." : "Su blindaje desvía casi todo. Esquivá y atacá cuando abra el núcleo.";
            if (!gateUnlocked && player.position.x > 54) return "E junto al terminal. Rotá módulos hasta unir ENTRADA y SALIDA, después VALIDAR.";
            if (gateUnlocked && bossHP > 0) return "El Núcleo de Seguridad está pasando la compuerta. El Guardián anuncia cada ataque: leé la señal roja.";
            if (bossHP <= 0) return RequiredFragments < 2 ? "Faltan fragmentos principales para abrir el portal." : "El portal está estable. Salgamos de acá.";
            if (RequiredFragments < 2) return "Dos fragmentos abren la salida. El tercero está en la pasarela de arriba: opcional. Shift = dash, también en el aire.";
            return "Ya tenemos los dos fragmentos. El terminal del firewall está adelante.";
        }

        void Attack()
        {
            fireDelay = blade ? .42f : .23f; aimHold = .45f; attackPose = .12f;
            facing = aimDirection.x < 0 ? -1 : 1;
            Vector2 origin = ShoulderWorld();
            Vector2 direction = aimDirection;
            float range = blade ? 2.8f : 22;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, range, SolidMask);
            Vector2 end = hit.collider != null ? hit.point : origin + direction * range;
            bool onGuardian = hit.collider != null && guardian != null && hit.collider.gameObject == guardian.gameObject;
            if (blade)
            {
                SoundBank.Play("blade");
                var slash = SpawnFx("slash", origin + direction * 1.1f, 20, true, 16);
                slash.flipX = facing < 0;
                slash.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg * facing);
            }
            else
            {
                SoundBank.Play("blaster");
                SpawnBolt(origin + direction * .55f, end);
            }
            if (onGuardian) GuardianHit(blade ? 25 : 9, end);
            else if (hit.collider != null) SpawnFx("hitCyan", end, 16, true, 22);
        }

        void UpdatePickups(float dt)
        {
            foreach (Shard shard in shards)
            {
                if (collected.Contains(shard.Id)) continue;
                shard.Visual.position = shard.Home + Vector3.up * Mathf.Sin(elapsed * 2.4f + shard.Id) * .12f;
                if (Vector2.Distance(player.position, shard.Visual.position) < 1.1f)
                {
                    collected.Add(shard.Id); shard.Visual.gameObject.SetActive(false);
                    SoundBank.Play("fragment");
                    SpawnFx("hitWhite", shard.Visual.position, 18, true, 14, UiTheme.Reward);
                    bitBrain.Celebrate();
                    Say(Speaker.Bit, shard.Id == 2 ? "¡Fragmento opcional! Es un registro viejo: GL1TCH era una herramienta de reparación. ¿Qué lo corrompió?" : RequiredFragments == 2 ? "Segundo fragmento principal. El portal ya puede compilar la salida." : "Fragmento de código recuperado. GL1TCH pierde terreno.");
                }
            }
            if (terminalPrompt != null)
            {
                bool near = !gateUnlocked && Vector2.Distance(player.position, terminal.transform.position + Vector3.up) < 3;
                terminalPrompt.enabled = near;
                if (near) terminalPrompt.transform.position = terminal.transform.position + new Vector3(0, 2.1f + Mathf.Sin(elapsed * 4) * .06f, 0);
            }
        }

        void UpdateCheckpoints()
        {
            for (int i = 0; i < checkpointPositions.Length; i++)
            {
                bool active = checkpoint.x >= checkpointPositions[i].x;
                if (Vector2.Distance(player.position, checkpointPositions[i]) < 1.5f && checkpointPositions[i].x > checkpoint.x)
                {
                    checkpoint = checkpointPositions[i]; hp = 100; active = true;
                    SoundBank.Play("checkpoint");
                    SpawnFx("hitCyan", checkpointPositions[i] + Vector2.up, 14, true, 20);
                    bitBrain.Celebrate();
                    Say(Speaker.Bit, "Checkpoint sincronizado. Integridad restaurada.");
                }
                if (active) markerAnims[i].Play("on", new[] { art.Fx["beacon"][1], art.Fx["beacon"][2], art.Fx["beacon"][3] }, 5);
                else markerAnims[i].Hold("off", art.Fx["beacon"], 0);
            }
        }

        // ------------------------------------------------------------ camera & parallax
        Vector3 cameraPosition;
        void UpdateCamera(float dt)
        {
            float lookAhead = Mathf.Clamp(player.linearVelocity.x * .25f, -1.5f, 1.5f) + facing * .8f;
            float targetX = player.position.x + lookAhead;
            if (bossActive && bossHP > 0) targetX = Mathf.Lerp(player.position.x, 88, .45f);
            if (InCinematic) targetX = Mathf.Lerp(player.position.x, GuardianPosition.x, .55f);
            float halfWidth = view.orthographicSize * view.aspect;
            targetX = Mathf.Clamp(targetX, Academic2DLevel.MinX + halfWidth - .5f, Academic2DLevel.MaxX - halfWidth + .5f);
            float targetY = Mathf.Max(3.6f, player.position.y + 1.1f);
            var goal = new Vector3(targetX, targetY, -10);
            cameraPosition = Vector3.Lerp(cameraPosition, goal, 1 - Mathf.Exp(-dt * 6));
            Vector3 jitter = Vector3.zero;
            if (shake > 0 && !GameSettings.ReduceEffects)
                jitter = new Vector3(Mathf.PerlinNoise(elapsed * 40, 0) - .5f, Mathf.PerlinNoise(0, elapsed * 40) - .5f, 0) * shake * .6f;
            view.transform.position = PixelSnap(cameraPosition + jitter);
        }

        void LateUpdate()
        {
            if (view == null) return;
            // Integer pixel scale: crisp pixel art at 720p/1080p/1440p (3 px per art pixel at 1080p).
            int scale = Mathf.Max(1, Mathf.RoundToInt(Screen.height / (Academic2DArt.PixelsPerUnit * 11.25f)));
            view.orthographicSize = Screen.height / (2f * Academic2DArt.PixelsPerUnit * scale);
            if (!started || paused || hacking) { UpdateParallax(); }
        }

        static Vector3 PixelSnap(Vector3 p)
        {
            float unit = 1f / Academic2DArt.PixelsPerUnit;
            return new Vector3(Mathf.Round(p.x / unit) * unit, Mathf.Round(p.y / unit) * unit, p.z);
        }

        // ------------------------------------------------------------ player visuals
        void UpdatePlayerVisual()
        {
            Vector2 v = player.linearVelocity;
            playerSprite.flipX = facing < 0;
            if (hurtTime > 0) playerAnim.Play("hurt", art.Zero["hurt"], 10, false);
            else if (dashTime > 0) playerAnim.Play("dash", art.Zero["dash"], 16);
            else if (!grounded) playerAnim.Hold(v.y > 0 ? "jump" : "fall", art.Zero[v.y > 0 ? "jump" : "fall"], v.y > 0 ? (v.y > 7 ? 0 : 1) : (v.y > -6 ? 0 : 1));
            else if (landTime > 0) playerAnim.Hold("land", art.Zero["land"], 0);
            else if (Mathf.Abs(v.x) > .6f)
            {
                playerAnim.Play("run", art.Zero["run"], 14);
                playerAnim.Speed = Mathf.Clamp(Mathf.Abs(v.x) / 7, .55f, 1.2f);
                // Footfalls on the two contact frames of the eight-frame cycle.
                int frame = playerAnim.Frame;
                if ((frame == 2 || frame == 6) && frame != lastStepFrame) SoundBank.Play("step", .9f, 1, .06f, .12f);
                lastStepFrame = frame;
            }
            else { playerAnim.Play("idle", art.Zero["idle"], 5); playerAnim.Speed = 1; }
            if (playerAnim.Current != "run") playerAnim.Speed = 1;

            // Aimed front arm: nine pre-rendered angles, mirrored when facing left.
            Vector2 aim = aimDirection;
            float angle = Mathf.Atan2(aim.y, Mathf.Abs(aim.x)) * Mathf.Rad2Deg;
            if (aimHold <= 0 && attackPose <= 0) angle = Mathf.Lerp(-35, 0, Mathf.Clamp01(Mathf.Abs(v.x) / 7)) * (grounded ? 1 : .3f);
            if (Mathf.Sign(aim.x) != facing && aimHold > 0) angle = 0;
            int index = Mathf.Clamp(Mathf.RoundToInt((angle + 90) / 22.5f), 0, 8);
            armSprite.sprite = (blade ? art.ArmBlade : art.ArmBlaster)[index];
            armSprite.flipX = facing < 0;
            Vector2 shoulder = art.Shoulder(playerAnim.Current ?? "idle", playerAnim.Frame);
            armSprite.transform.localPosition = new Vector3(shoulder.x * facing, shoulder.y, 0);
            armSprite.enabled = !dying;
            bool blink = damageGrace > 0 && Mathf.Sin(elapsed * 30) > 0;
            Color tint = blink ? new Color(1, 1, 1, .45f) : Color.white;
            playerSprite.color = tint; armSprite.color = tint;
        }

        // ------------------------------------------------------------ progression & damage
        bool CanExit { get { return gateUnlocked && bossHP <= 0 && collected.Contains(0) && collected.Contains(1); } }

        void UpdatePortal()
        {
            if (CanExit)
            {
                if (!portalAnnounced)
                {
                    portalAnnounced = true;
                    SoundBank.Play("portal_open");
                    Say(Speaker.Bit, "El portal compiló la ruta. ¡Vamos, Zero!");
                }
                portalAnim.Play("open", new[] { art.Portal[1], art.Portal[2], art.Portal[3], art.Portal[4] }, 8);
                if (Vector2.Distance(player.position, (Vector2)portal.transform.position + Vector2.up * 1.6f) < 2)
                {
                    won = true;
                    SoundBank.Play("portal_enter");
                    music.SetMood(MusicDirector.Mood.Victory); music.Stinger("stinger_victory");
                    SetClock();
                }
            }
            else portalAnim.Hold("closed", art.Portal, 0);
        }

        void ReceiveDamage(float amount, Vector2? source = null)
        {
            if (damageGrace > 0 || dying || defeated) return;
            hp = Mathf.Max(0, hp - amount); damageGrace = 1; hurtTime = .3f;
            shake = Mathf.Max(shake, .35f); damageFlash = 1;
            SoundBank.Play("hurt");
            SpawnFx("hitRed", player.position + Vector2.up * .3f, 18, true, 18);
            if (source.HasValue)
            {
                float side = Mathf.Sign(player.position.x - source.Value.x);
                if (side == 0) side = -facing;
                player.linearVelocity = new Vector2(side * 4.5f, Mathf.Max(player.linearVelocity.y, 4));
            }
            if (bitBrain != null) bitBrain.Worry();
            if (hp <= 0) Kill();
        }

        void Kill()
        {
            if (dying || defeated) return;
            hp = 0; dying = true; dyingTime = 1.35f;
            hurtTime = 0; dashTime = 0; damageFlash = 1; shake = .5f;
            SoundBank.Play("death");
            music.SetMood(MusicDirector.Mood.Silent);
            playerAnim.Play("death", art.Zero["death"], 6, false, true);
            playerAnim.Speed = 1;
        }

        void Respawn()
        {
            player.position = checkpoint; player.linearVelocity = Vector2.zero;
            hp = 100; defeated = false; dying = false; damageGrace = 2;
            dashTime = dashDelay = jumpBuffer = coyote = hurtTime = landTime = 0; airDash = false;
            if (bossHP > 0) ResetBoss();
            ClearHostiles();
            SoundBank.Play("respawn");
            SpawnFx("hitCyan", checkpoint, 12, true, 26);
            music.SetMood(MusicDirector.Mood.Explore);
            playerAnim.Play("idle", art.Zero["idle"], 5, true, true);
            cameraPosition = new Vector3(checkpoint.x, Mathf.Max(3.6f, checkpoint.y + 1.1f), -10);
            Say(Speaker.Bit, "Te restauré desde el último checkpoint. Respirá y leé el patrón.");
            SetClock();
        }

        void FailHack()
        {
            hacking = false; hackAttempts++; terminalCooldown = 8; SetClock();
            SoundBank.Play("alarm"); SoundBank.Play("hack_error");
            shake = Mathf.Max(shake, .3f);
            Say(Speaker.Glitch, "Acceso denegado. La alarma te marcó, intruso.");
            if (bitBrain != null) bitBrain.Alert();
            damageGrace = 0; ReceiveDamage(10);
        }

        bool CommitHack()
        {
            if (!hacking || circuit == null || !circuit.IsSolved)
            {
                if (hacking) { hackFeedback = 2; hackTime = Mathf.Max(0, hackTime - 5); SoundBank.Play("hack_error"); }
                return false;
            }
            gateUnlocked = true; hacking = false;
            firewall.gameObject.SetActive(false);
            SpawnFx("hitRed", firewall.transform.position + Vector3.up * 2, 12, true, 30);
            SpawnFx("hitRed", firewall.transform.position + Vector3.down * 2, 12, true, 30);
            terminalAnim.Hold("solved", art.Fx["terminal"], 3);
            SoundBank.Play("hack_success");
            if (bitBrain != null) bitBrain.Celebrate();
            SetClock();
            Say(Speaker.Bit, "Circuito conectado. Firewall desactivado: el Núcleo de Seguridad está abierto.");
            return true;
        }

        void Say(string message) { Say(Speaker.Bit, message); }
        void Say(Speaker who, string message)
        {
            speaker = who; subtitle = message; subtitleTime = Mathf.Clamp(message.Length * .07f, 3.5f, 8);
            if (who == Speaker.Bit) SoundBank.Play("bit_chirp", .8f, 1, .08f, .4f);
            else if (who == Speaker.Glitch) SoundBank.Play("glitch_voice", .8f, 1, .03f, .4f);
        }

        void OnApplicationFocus(bool focus)
        {
            if (PauseWhenUnfocused && !focus && started && !won && !defeated)
            {
                hacking = false; paused = true; menuPage = MenuPage.Main; SetClock();
            }
        }

        void SetClock()
        {
            Time.timeScale = !started || paused || defeated || won || hacking ? 0 : 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            if (bodyMaterial != null) Destroy(bodyMaterial);
            foreach (var tile in runtimeTiles.Values) if (tile != null) Destroy(tile);
            foreach (var item in ownedObjects) if (item != null) Destroy(item);
            if (art != null) art.Dispose();
        }
    }
}
