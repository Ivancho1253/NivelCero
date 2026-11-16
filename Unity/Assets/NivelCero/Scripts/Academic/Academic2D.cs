using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using NivelCero.Core;

namespace NivelCero
{
    // Complementary academic scene. Original pixel sprites remain provisional art.
    public sealed class Academic2D : MonoBehaviour
    {
        public Texture2D Tilesheet;
        public bool PauseWhenUnfocused = true;
        public bool IsPaused { get { return paused; } }
        Camera view;
        Rigidbody2D player;
        SpriteRenderer playerSprite, bit, guardian, portal, terminal, firewall;
        readonly List<Transform> background = new List<Transform>();
        readonly List<float> parallax = new List<float>();
        readonly List<Vector3> backgroundOrigins = new List<Vector3>();
        readonly List<Shard> shards = new List<Shard>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<SpriteRenderer> markers = new List<SpriteRenderer>();
        readonly Vector2[] checkpointPositions = { new Vector2(4, 2), new Vector2(32, 2), new Vector2(73, 2) };
        readonly HashSet<int> collected = new HashSet<int>();
        Sprite[] sprites;
        Material beamMaterial;
        PhysicsMaterial2D bodyMaterial;
        Tile floorTile;
        CircuitPuzzle circuit;
        bool hacking, gateUnlocked;
        float hackTime, terminalCooldown, hackFeedback, hitFeedback;
        int hackAttempts;
        AudioSource audioSource;
        AudioClip pulseSound, collectSound, damageSound, hackSound, jumpSound;
        Vector2 checkpoint = new Vector2(4, 2);
        bool started, paused, defeated, won, grounded, airDash, blade;
        float hp = 100, bossHP = 180, damageGrace, fireDelay, dashDelay, dashTime, jumpBuffer, coyote;
        float bossAttack = 2, telegraph, subtitleTime;
        string subtitle = "Bit: Zero, despertá. GL1TCH está corrompiendo este sector.";
        bool bossActive;
        int deaths;

        public bool GateUnlocked { get { return gateUnlocked; } }
        public bool LevelCompleted { get { return won; } }
        public float CurrentHealth { get { return hp; } }
        public float BossHealth { get { return bossHP; } }
        public int RequiredFragments { get { return (collected.Contains(0) ? 1 : 0) + (collected.Contains(1) ? 1 : 0); } }
        public int FragmentCount { get { return collected.Count; } }
        public Vector2 PlayerPosition { get { return player != null ? player.position : Vector2.zero; } }
        public Camera GameCamera { get { return view; } }
        public Vector2 GuardianPosition { get { return guardian != null ? (Vector2)guardian.transform.position : Vector2.zero; } }
        public Vector2 ActiveCheckpoint { get { return checkpoint; } }
        public int DeathCount { get { return deaths; } }
        public bool IsGrounded { get { return grounded; } }
        public CircuitPuzzle ActiveCircuit { get { return hacking ? circuit : null; } }

        // The same commands back the menu and circuit UI; automation can use
        // these without changing health, position, progression or encounter state.
        public void StartGame()
        {
            if (started) return;
            started = true; paused = false; Say(subtitle); SetClock();
        }
        public bool ValidateCircuit() { return CommitHack(); }

        sealed class Shard { public int Id; public Transform Visual; }
        sealed class Shot { public Transform Visual; public Vector2 Velocity; public float Life = 6; }

        void Start()
        {
            if (!Application.isEditor) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
            if (Tilesheet == null) { Debug.LogError("Academic2D requires the original tiles.png atlas."); enabled = false; return; }
            sprites = new Sprite[8];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = Sprite.Create(Tilesheet, new Rect(i * 32, 0, 32, 32), Vector2.one * .5f, 32);
            beamMaterial = new Material(Shader.Find("Sprites/Default"));
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .22f;
            pulseSound = Tone(850, 150, .09f); collectSound = Tone(550, 1250, .26f);
            damageSound = Tone(180, 55, .16f); hackSound = Tone(350, 1100, .42f); jumpSound = Tone(240, 480, .09f);
            BuildTilemap();
            GameObject cameraObject = new GameObject("Camara 2D");
            cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            view.orthographic = true; view.orthographicSize = 6.3f;
            view.backgroundColor = new Color(.018f, .035f, .065f);
            view.transform.position = new Vector3(5, 4, -10);
            playerSprite = Visual("Zero 2D", 2, new Vector3(4, 2, 0), new Vector2(1.4f, 1.8f));
            playerSprite.gameObject.layer = 2;
            player = playerSprite.gameObject.AddComponent<Rigidbody2D>();
            player.gravityScale = 3; player.freezeRotation = true;
            player.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D body = player.gameObject.AddComponent<CapsuleCollider2D>();
            body.size = new Vector2(.48f, .89f);
            bodyMaterial = new PhysicsMaterial2D("Zero sin friccion") { friction = 0, bounciness = 0 };
            body.sharedMaterial = bodyMaterial;
            bit = Visual("Bit - NPC autonomo", 3, new Vector3(6, 3, 0), Vector2.one * .7f);
            terminal = Visual("Terminal - Firewall de seguridad", 7, new Vector3(64, 2, 0), new Vector2(1.1f, 2));
            terminal.color = Color.cyan;
            firewall = Visual("Firewall - circuito obligatorio", 1, new Vector3(68, 5, 0), new Vector2(1, 9));
            firewall.gameObject.AddComponent<BoxCollider2D>();
            guardian = Visual("Guardian del Sistema", 5, new Vector3(92, 3, 0), Vector2.one * 4);
            guardian.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(.85f, .92f);
            portal = Visual("Portal de salida", 6, new Vector3(102, 2.5f, 0), new Vector2(2.2f, 4));
            for (int i = 0; i < checkpointPositions.Length; i++)
                markers.Add(Visual("Checkpoint " + i, 3, checkpointPositions[i] + Vector2.down * .8f, new Vector2(.6f, .6f)));
            AddShard(0, new Vector2(25, 2)); AddShard(1, new Vector2(59, 2)); AddShard(2, new Vector2(42, 5.3f));
            for (int layer = 0; layer < 3; layer++)
            for (int i = -4; i < 30; i++)
            {
                Vector3 position = new Vector3(i * 7 + layer * 2, 4 + layer, 4 + layer);
                SpriteRenderer slab = Visual("Parallax capa " + layer, 7, position, new Vector2(4 + layer, 9 + layer * 2));
                slab.sortingOrder = -10 - layer;
                slab.color = new Color(.25f + layer * .1f, .4f, .6f, 1);
                background.Add(slab.transform); backgroundOrigins.Add(position); parallax.Add(.15f + layer * .17f);
            }
            Time.timeScale = 0;
        }

        void BuildTilemap()
        {
            GameObject grid = new GameObject("AE4 Grid"); grid.transform.SetParent(transform); grid.AddComponent<Grid>();
            GameObject mapObject = new GameObject("Suelo Tilemap - atlas original"); mapObject.transform.SetParent(grid.transform);
            Tilemap map = mapObject.AddComponent<Tilemap>(); mapObject.AddComponent<TilemapRenderer>();
            Tile metal = ScriptableObject.CreateInstance<Tile>(); metal.sprite = sprites[0]; metal.colliderType = Tile.ColliderType.Grid;
            floorTile = metal;
            for (int x = -2; x <= 108; x++)
            {
                if ((x >= 18 && x <= 21) || (x >= 48 && x <= 51)) continue;
                map.SetTile(new Vector3Int(x, 0, 0), metal); map.SetTile(new Vector3Int(x, -1, 0), metal);
            }
            for (int x = 38; x <= 45; x++) map.SetTile(new Vector3Int(x, 3, 0), metal);
            // The ground corridor must remain open: stairs across its width
            // forced every player onto the supposedly optional fragment route.
            // A deliberate side jump now reaches the upper platform instead.
            for (int y = 1; y <= 8; y++) { map.SetTile(new Vector3Int(-2, y, 0), metal); map.SetTile(new Vector3Int(108, y, 0), metal); }
            mapObject.AddComponent<TilemapCollider2D>();
        }

        SpriteRenderer Visual(string label, int index, Vector3 position, Vector2 scale)
        {
            GameObject obj = new GameObject(label); obj.transform.SetParent(transform); obj.transform.position = position; obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[index]; return renderer;
        }
        void AddShard(int id, Vector2 position) { shards.Add(new Shard { Id = id, Visual = Visual("Fragmento " + id, 4, position, Vector2.one * .8f).transform }); }

        void Update()
        {
            if (player == null) return;
            Keyboard k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame && started && !defeated && !won)
            {
                if (hacking) { hacking = false; SetClock(); Say("Bit: conexión cancelada. El firewall sigue activo."); }
                else { paused = !paused; SetClock(); }
            }
            if (hacking)
            {
                hackTime -= Time.unscaledDeltaTime;
                hackFeedback = Mathf.Max(0, hackFeedback - Time.unscaledDeltaTime);
                if (hackTime <= 0) FailHack();
                return;
            }
            if (!started || paused || defeated || won) return;
            float dt = Time.deltaTime;
            terminalCooldown = Mathf.Max(0, terminalCooldown - dt);
            hitFeedback = Mathf.Max(0, hitFeedback - dt);
            jumpBuffer -= dt; coyote -= dt; damageGrace -= dt; fireDelay -= dt; dashDelay -= dt; dashTime -= dt; subtitleTime -= dt;
            if (k != null && k.spaceKey.wasPressedThisFrame) jumpBuffer = .14f;
            if (k != null && k.digit1Key.wasPressedThisFrame) blade = false;
            if (k != null && k.digit2Key.wasPressedThisFrame) blade = true;
            if (k != null && k.leftShiftKey.wasPressedThisFrame && dashDelay <= 0 && (!airDash || grounded))
            {
                dashTime = .16f; dashDelay = .9f;
                if (!grounded) airDash = true;
            }
            if (k != null && k.eKey.wasPressedThisFrame)
            {
                if (!gateUnlocked && Vector2.Distance(player.position, terminal.transform.position) < 3)
                {
                    if (terminalCooldown > 0) Say("Bit: alarma activa. El terminal reinicia en " + Mathf.CeilToInt(terminalCooldown) + " segundos.");
                    else { circuit = new CircuitPuzzle(413 + hackAttempts, 3); hackTime = 75; hacking = true; SetClock(); }
                }
                else if (Vector2.Distance(player.position, bit.transform.position) < 4)
                    Say(bossActive ? "Bit: el Guardián anuncia cada descarga. Saltá y atacá entre ráfagas." : !gateUnlocked && player.position.x > 54 ? "Bit: E junto al terminal. Rotá los módulos; conectá la entrada izquierda con la salida derecha." : "Bit: dos fragmentos abren la salida; el tercero está en la ruta superior. Espacio salta; Shift hace dash.");
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && fireDelay <= 0) Attack();
            if (player.position.y < -7) ReceiveDamage(100);
            foreach (Shard shard in shards)
            {
                if (collected.Contains(shard.Id)) continue;
                shard.Visual.Rotate(0, 0, dt * 60);
                if (Vector2.Distance(player.position, shard.Visual.position) < 1.1f)
                {
                    collected.Add(shard.Id); shard.Visual.gameObject.SetActive(false);
                    audioSource.PlayOneShot(collectSound);
                    Say(shard.Id == 2 ? "Bit: fragmento opcional recuperado. La exploración tiene recompensa." : "Bit: fragmento de código recuperado. GL1TCH pierde terreno.");
                }
            }
            for (int i = 0; i < checkpointPositions.Length; i++)
            {
                if (Vector2.Distance(player.position, checkpointPositions[i]) < 1.5f && checkpointPositions[i].x > checkpoint.x)
                {
                    checkpoint = checkpointPositions[i]; hp = 100; Say("Bit: checkpoint sincronizado. Integridad restaurada.");
                }
                markers[i].color = checkpoint.x >= checkpointPositions[i].x ? Color.cyan : Color.gray;
            }
            Vector3 bitTarget = player.transform.position + new Vector3(playerSprite.flipX ? -1.8f : 1.8f, 1.5f + Mathf.Sin(Time.time * 3) * .15f, 0);
            bit.transform.position = Vector3.Lerp(bit.transform.position, bitTarget, 1 - Mathf.Exp(-dt * 3));
            Vector3 cam = new Vector3(Mathf.Clamp(player.position.x + 2, 5, 103), Mathf.Max(4, player.position.y + 1), -10);
            view.transform.position = Vector3.Lerp(view.transform.position, cam, 1 - Mathf.Exp(-dt * 7));
            for (int i = 0; i < background.Count; i++) background[i].position = backgroundOrigins[i] + Vector3.right * view.transform.position.x * parallax[i];
            UpdateBoss(dt); UpdateShots(dt);
            playerSprite.color = damageGrace > 0 && Mathf.Sin(Time.time * 25) > 0 ? new Color(1, .35f, .48f) : Color.white;
            portal.color = CanExit ? Color.white : new Color(.25f, .3f, .4f);
            if (CanExit && Vector2.Distance(player.position, portal.transform.position) < 2) { won = true; SetClock(); }
        }

        void FixedUpdate()
        {
            if (player == null || !started || paused || defeated || won || hacking) return;
            Keyboard k = Keyboard.current;
            grounded = Physics2D.OverlapCircle(player.position + Vector2.down * .86f, .12f, ~(1 << 2)) != null;
            if (grounded) { coyote = .1f; airDash = false; }
            float axis = k == null ? 0 : (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
            if (axis != 0) playerSprite.flipX = axis < 0;
            Vector2 velocity = player.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, axis * 7, 75 * Time.fixedDeltaTime);
            if (jumpBuffer > 0 && coyote > 0) { velocity.y = 14; jumpBuffer = coyote = 0; audioSource.PlayOneShot(jumpSound); }
            if (dashTime > 0) { velocity.x = (axis == 0 ? (playerSprite.flipX ? -1 : 1) : axis) * 19; velocity.y = Mathf.Max(0, velocity.y); }
            player.linearVelocity = velocity;
        }

        void Attack()
        {
            fireDelay = blade ? .42f : .23f;
            Vector2 aim = view.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 origin = player.position + Vector2.up * .2f;
            Vector2 direction = (aim - origin).normalized;
            if (direction.sqrMagnitude < .1f) direction = playerSprite.flipX ? Vector2.left : Vector2.right;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, blade ? 2.8f : 22, ~(1 << 2));
            audioSource.PlayOneShot(pulseSound, blade ? .65f : 1);
            if (bossHP > 0 && bossActive && hit.collider != null && hit.collider.gameObject == guardian.gameObject)
            {
                bossHP = Mathf.Max(0, bossHP - (blade ? 25 : 9));
                guardian.color = Color.cyan; hitFeedback = .15f;
                if (bossHP <= 0)
                {
                    guardian.gameObject.SetActive(false); shots.ForEach(s => Destroy(s.Visual.gameObject)); shots.Clear();
                    Say("Bit: Guardián desactivado. Recuperá dos fragmentos para abrir el portal.");
                }
            }
            Vector2 end = hit.collider != null ? hit.point : origin + direction * (blade ? 2.8f : 22);
            GameObject beam = new GameObject("Pulso de Zero"); beam.transform.SetParent(transform); LineRenderer line = beam.AddComponent<LineRenderer>();
            line.sharedMaterial = beamMaterial; line.startColor = line.endColor = Color.cyan;
            line.startWidth = .06f; line.endWidth = .02f; line.positionCount = 2;
            line.sortingOrder = 4;
            line.SetPosition(0, origin); line.SetPosition(1, end);
            Destroy(beam, .07f);
        }

        void UpdateBoss(float dt)
        {
            if (bossHP <= 0) return;
            if (!bossActive && gateUnlocked && player.position.x > 80) { bossActive = true; Say("GL1TCH: esta memoria me pertenece. Guardián, eliminá al intruso."); }
            if (!bossActive) return;
            int phase = bossHP > 120 ? 1 : bossHP > 60 ? 2 : 3;
            bossAttack -= dt;
            guardian.transform.position = new Vector3(92, 3 + Mathf.Sin(Time.time * 1.8f) * .5f, 0);
            guardian.color = Color.Lerp(guardian.color, hitFeedback > 0 ? Color.cyan : telegraph > 0 ? Color.red : Color.white, dt * 12);
            if (bossAttack <= 0 && telegraph <= 0) telegraph = .7f;
            if (telegraph > 0)
            {
                telegraph -= dt;
                if (telegraph <= 0)
                {
                    Vector2 delta = player.position - (Vector2)guardian.transform.position;
                    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < phase; i++)
                    {
                        float radians = (angle + (i - (phase - 1) * .5f) * 13) * Mathf.Deg2Rad;
                        SpriteRenderer shot = Visual("Descarga anunciada", 4, guardian.transform.position, Vector2.one * .38f);
                        shot.color = Color.magenta;
                        shots.Add(new Shot { Visual = shot.transform, Velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (5 + phase) });
                    }
                    bossAttack = 2.4f - phase * .3f;
                }
            }
        }

        void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i]; Vector2 old = shot.Visual.position;
                Vector2 next = old + shot.Velocity * dt;
                shot.Life -= dt;
                // Sweep the entire step, stopping at geometry before testing the player.
                // This also prevents damage behind a wall during a slow frame.
                bool blocked = false;
                RaycastHit2D[] walls = Physics2D.LinecastAll(old, next, ~(1 << 2));
                foreach (RaycastHit2D wall in walls)
                {
                    if (wall.collider.gameObject == guardian.gameObject) continue;
                    next = wall.point; blocked = true; break;
                }
                Vector2 segment = next - old;
                float fraction = segment.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(player.position - old, segment) / segment.sqrMagnitude) : 0;
                bool hit = Vector2.Distance(old + segment * fraction, player.position) < .65f;
                shot.Visual.position = next;
                if (hit) ReceiveDamage(18);
                if (hit || shot.Life <= 0 || blocked)
                { Destroy(shot.Visual.gameObject); shots.RemoveAt(i); }
            }
        }

        bool CanExit { get { return gateUnlocked && bossHP <= 0 && collected.Contains(0) && collected.Contains(1); } }
        void ReceiveDamage(float amount)
        {
            if (damageGrace > 0) return;
            hp = Mathf.Max(0, hp - amount); damageGrace = 1;
            audioSource.PlayOneShot(damageSound);
            if (hp <= 0) { defeated = true; deaths++; SetClock(); }
        }
        void Respawn()
        {
            player.position = checkpoint; player.linearVelocity = Vector2.zero; hp = 100; defeated = false; damageGrace = 2;
            dashTime = dashDelay = jumpBuffer = coyote = 0; airDash = false;
            if (bossHP > 0) { bossHP = 180; bossActive = false; bossAttack = 2; telegraph = 0; }
            foreach (Shot shot in shots) Destroy(shot.Visual.gameObject); shots.Clear(); SetClock();
        }
        void FailHack()
        {
            hacking = false; hackAttempts++; terminalCooldown = 8; SetClock();
            Say("Bit: alarma de seguridad. La conexión falló; esperá ocho segundos para reintentar.");
            damageGrace = 0; ReceiveDamage(10);
        }
        bool CommitHack()
        {
            if (!hacking || circuit == null || !circuit.IsSolved)
            {
                if (hacking) { hackFeedback = 2; hackTime = Mathf.Max(0, hackTime - 5); }
                return false;
            }
            gateUnlocked = true; hacking = false;
            firewall.gameObject.SetActive(false); terminal.color = Color.green;
            audioSource.PlayOneShot(hackSound); SetClock();
            Say("Bit: circuito conectado. Firewall desactivado; el Núcleo de Seguridad está abierto.");
            return true;
        }
        AudioClip Tone(float start, float end, float duration)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.CeilToInt(rate * duration)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += 2 * Mathf.PI * Mathf.Lerp(start, end, t) / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * (1 - t) * .5f;
            }
            AudioClip clip = AudioClip.Create("AE4 pulso original", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        void Say(string message) { subtitle = message; subtitleTime = 7; }
        void OnApplicationFocus(bool focus)
        {
            if (PauseWhenUnfocused && !focus && started && !won && !defeated)
            {
                hacking = false; paused = true; SetClock();
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
            if (beamMaterial != null) Destroy(beamMaterial);
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (floorTile != null) Destroy(floorTile);
            foreach (AudioClip clip in new[] { pulseSound, collectSound, damageSound, hackSound, jumpSound }) if (clip != null) Destroy(clip);
            if (sprites != null) foreach (Sprite sprite in sprites) if (sprite != null) Destroy(sprite);
        }

        void OnGUI()
        {
            if (player == null) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Vector3 origin = new Vector3((Screen.width - 1280 * scale) * .5f, (Screen.height - 720 * scale) * .5f, 0);
            GUI.matrix = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one * scale);
            GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            GUIStyle title = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold };
            GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            Panel(new Rect(20, 20, 400, 125));
            GUI.Label(new Rect(36, 28, 375, 30), "NIVEL CERO // SECTOR 01", label);
            GUI.Label(new Rect(36, 56, 350, 26), "INTEGRIDAD " + Mathf.CeilToInt(hp) + " / 100", label);
            Bar(new Rect(36, 85, 360, 8), hp / 100, Color.cyan);
            GUI.Label(new Rect(36, 102, 380, 34), (blade ? "ENERGY BLADE" : "PULSE BLASTER") + " · DASH " + (dashDelay > 0 ? dashDelay.ToString("0.0") + "s" : airDash ? "TOCÁ EL SUELO" : "LISTO"), label);
            if (bossActive && bossHP > 0)
            {
                Panel(new Rect(720, 20, 540, 90));
                GUI.Label(new Rect(738, 30, 500, 28), "GUARDIÁN // FASE " + (bossHP > 120 ? 1 : bossHP > 60 ? 2 : 3), label);
                Bar(new Rect(738, 69, 500, 10), bossHP / 180, telegraph > 0 ? Color.red : Color.magenta);
            }
            else
            {
                Panel(new Rect(780, 20, 480, 100));
                string objective = RequiredFragments < 2 ? "Recuperá los dos fragmentos del recorrido principal." : !gateUnlocked ? "Hackeá la terminal y atravesá el firewall." : bossHP > 0 ? "Entrá al Núcleo de Seguridad y derrotá al Guardián." : "Entrá al portal de salida.";
                GUI.Label(new Rect(796, 28, 448, 80), "FRAGMENTOS " + collected.Count + "/3 · OBLIGATORIOS " + RequiredFragments + "/2\n" + objective, label);
            }
            if (subtitleTime > 0 || !started)
            {
                Panel(new Rect(170, 565, 940, 83));
                GUI.Label(new Rect(190, 577, 900, 66), subtitle, label);
            }
            if (!gateUnlocked && Mathf.Abs(player.position.x - 64) < 3 && started && !hacking)
            {
                Panel(new Rect(390, 480, 500, 55));
                GUI.Label(new Rect(410, 491, 470, 40), terminalCooldown > 0 ? "TERMINAL EN REINICIO · " + Mathf.CeilToInt(terminalCooldown) + "s" : "[E] HACKEAR FIREWALL DE SEGURIDAD", label);
            }
            GUI.Label(new Rect(20, 672, 1240, 40), "A/D mover · Espacio saltar · Shift dash · Mouse apuntar · Click atacar · 1/2 arma · E interactuar · Esc pausa", label);
            if (hitFeedback > 0)
            {
                GUI.color = Color.cyan; GUI.Label(new Rect(620, 320, 80, 40), "×", title); GUI.color = Color.white;
            }
            if (hacking) { DrawCircuit(label, title, button); return; }
            if (!started || paused || defeated || won)
            {
                Panel(new Rect(350, 170, 580, 380));
                string heading = won ? "SECTOR RESTAURADO" : defeated ? "INTEGRIDAD PERDIDA" : paused ? "PROTOCOLO EN PAUSA" : "NIVEL CERO // AE4 2D";
                GUI.Label(new Rect(390, 205, 500, 50), heading, title);
                GUI.Label(new Rect(390, 268, 500, 85), won ? "Bit: recuperamos el Sector de Arranque. Memoria Fragmentada, Torre Firewall y el Núcleo Corrupto todavía nos esperan.\nCaídas: " + deaths + " · Fragmentos: " + collected.Count + "/3" : "Dos fragmentos, un circuito y un Guardián.\nExplorá la ruta superior para recuperar el código opcional.", label);
                if (!won && GUI.Button(new Rect(410, 370, 460, 48), defeated ? "REAPARECER EN CHECKPOINT" : started ? "CONTINUAR" : "INICIAR SECTOR 2D", button))
                { if (defeated) Respawn(); else if (!started) StartGame(); else { paused = false; SetClock(); } }
                if (GUI.Button(new Rect(410, 430, 460, 42), "REINICIAR ESCENA", button)) { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                if (GUI.Button(new Rect(410, 485, 460, 38), "VOLVER AL MENÚ 3D", button)) { Time.timeScale = 1; SceneManager.LoadScene("SectorDeArranque3D"); }
            }
        }

        void DrawCircuit(GUIStyle label, GUIStyle title, GUIStyle button)
        {
            Panel(new Rect(290, 95, 700, 550));
            GUI.Label(new Rect(325, 115, 650, 45), "HACK // FIREWALL DE SEGURIDAD", title);
            GUI.Label(new Rect(325, 165, 640, 60), "Rotá cada módulo con un clic. Conectá la entrada izquierda de la primera celda con la salida derecha de la última.\nTIEMPO " + Mathf.CeilToInt(hackTime) + "s", label);
            float left = 490, top = 250, cell = 82;
            for (int y = 0; y < circuit.Size; y++)
            for (int x = 0; x < circuit.Size; x++)
            {
                Rect tile = new Rect(left + x * cell, top + y * cell, cell - 5, cell - 5);
                if (GUI.Button(tile, GUIContent.none)) circuit.Rotate(x, y);
                Color color = circuit.IsPowered(x, y) ? Color.cyan : new Color(.43f, .58f, .7f);
                int mask = circuit.MaskAt(x, y); Vector2 center = tile.center;
                GUI.color = color;
                GUI.DrawTexture(new Rect(center.x - 6, center.y - 6, 12, 12), Texture2D.whiteTexture);
                if ((mask & CircuitPuzzle.North) != 0) GUI.DrawTexture(new Rect(center.x - 3, tile.y + 4, 6, tile.height * .5f), Texture2D.whiteTexture);
                if ((mask & CircuitPuzzle.East) != 0) GUI.DrawTexture(new Rect(center.x, center.y - 3, tile.width * .5f - 4, 6), Texture2D.whiteTexture);
                if ((mask & CircuitPuzzle.South) != 0) GUI.DrawTexture(new Rect(center.x - 3, center.y, 6, tile.height * .5f - 4), Texture2D.whiteTexture);
                if ((mask & CircuitPuzzle.West) != 0) GUI.DrawTexture(new Rect(tile.x + 4, center.y - 3, tile.width * .5f, 6), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(365, 265, 115, 55), "ENTRADA →", label);
            GUI.Label(new Rect(745, 430, 190, 55), "→ SALIDA", label);
            if (hackFeedback > 0) GUI.Label(new Rect(350, 510, 600, 30), "SIN CONEXIÓN · penalización de cinco segundos", label);
            if (GUI.Button(new Rect(325, 565, 230, 44), "VALIDAR CIRCUITO", button)) ValidateCircuit();
            if (GUI.Button(new Rect(570, 565, 175, 44), "RESTABLECER", button)) circuit.Reset();
            if (GUI.Button(new Rect(760, 565, 195, 44), "DESCONECTAR", button)) { hacking = false; SetClock(); }
        }
        static void Panel(Rect rect)
        {
            GUI.color = new Color(.025f, .06f, .095f, .97f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(.2f, .75f, .85f); GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), Texture2D.whiteTexture); GUI.color = Color.white;
        }
        static void Bar(Rect rect, float amount, Color color)
        {
            GUI.color = new Color(.12f, .19f, .26f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(amount), rect.height), Texture2D.whiteTexture); GUI.color = Color.white;
        }
    }
}
