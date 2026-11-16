using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace NivelCero
{
    // Complementary academic scene. Original pixel sprites remain provisional art.
    public sealed class Academic2D : MonoBehaviour
    {
        public Texture2D Tilesheet;
        Camera view;
        Rigidbody2D player;
        SpriteRenderer playerSprite, bit, guardian, portal;
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
        Vector2 checkpoint = new Vector2(4, 2);
        bool started, paused, defeated, won, grounded, airDash, blade;
        float hp = 100, bossHP = 180, damageGrace, fireDelay, dashDelay, dashTime, jumpBuffer, coyote;
        float bossAttack = 2, telegraph, subtitleTime;
        string subtitle = "Bit: Zero, despertá. GL1TCH está corrompiendo este sector.";
        bool bossActive;
        int deaths;

        sealed class Shard { public int Id; public Transform Visual; }
        sealed class Shot { public Transform Visual; public Vector2 Velocity; public float Life = 6; }

        void Start()
        {
            if (Tilesheet == null) { Debug.LogError("Academic2D requires the original tiles.png atlas."); enabled = false; return; }
            sprites = new Sprite[8];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = Sprite.Create(Tilesheet, new Rect(i * 32, 0, 32, 32), Vector2.one * .5f, 32);
            beamMaterial = new Material(Shader.Find("Sprites/Default"));
            BuildTilemap();
            GameObject cameraObject = new GameObject("Camara 2D");
            view = cameraObject.AddComponent<Camera>();
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
            GameObject grid = new GameObject("AE4 Grid"); grid.AddComponent<Grid>();
            GameObject mapObject = new GameObject("Suelo Tilemap - atlas original"); mapObject.transform.SetParent(grid.transform);
            Tilemap map = mapObject.AddComponent<Tilemap>(); mapObject.AddComponent<TilemapRenderer>();
            Tile metal = ScriptableObject.CreateInstance<Tile>(); metal.sprite = sprites[0]; metal.colliderType = Tile.ColliderType.Grid;
            for (int x = -2; x <= 108; x++)
            {
                if ((x >= 18 && x <= 21) || (x >= 48 && x <= 51)) continue;
                map.SetTile(new Vector3Int(x, 0, 0), metal); map.SetTile(new Vector3Int(x, -1, 0), metal);
            }
            for (int x = 38; x <= 45; x++) map.SetTile(new Vector3Int(x, 3, 0), metal);
            map.SetTile(new Vector3Int(36, 1, 0), metal); map.SetTile(new Vector3Int(37, 2, 0), metal);
            for (int y = 1; y <= 8; y++) { map.SetTile(new Vector3Int(-2, y, 0), metal); map.SetTile(new Vector3Int(108, y, 0), metal); }
            mapObject.AddComponent<TilemapCollider2D>();
        }

        SpriteRenderer Visual(string label, int index, Vector3 position, Vector2 scale)
        {
            GameObject obj = new GameObject(label); obj.transform.position = position; obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[index]; return renderer;
        }
        void AddShard(int id, Vector2 position) { shards.Add(new Shard { Id = id, Visual = Visual("Fragmento " + id, 4, position, Vector2.one * .8f).transform }); }

        void Update()
        {
            if (player == null) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame && started && !defeated && !won) { paused = !paused; SetClock(); }
            if (!started || paused || defeated || won) return;
            float dt = Time.deltaTime;
            jumpBuffer -= dt; coyote -= dt; damageGrace -= dt; fireDelay -= dt; dashDelay -= dt; dashTime -= dt; subtitleTime -= dt;
            if (k.spaceKey.wasPressedThisFrame) jumpBuffer = .14f;
            if (k.digit1Key.wasPressedThisFrame) blade = false;
            if (k.digit2Key.wasPressedThisFrame) blade = true;
            if (k.leftShiftKey.wasPressedThisFrame && dashDelay <= 0 && (!airDash || grounded))
            {
                dashTime = .16f; dashDelay = .9f;
                if (!grounded) airDash = true;
            }
            if (k.eKey.wasPressedThisFrame && Vector2.Distance(player.position, bit.transform.position) < 4)
                Say(bossActive ? "Bit: el Guardián anuncia cada descarga. Saltá y atacá entre ráfagas." : "Bit: dos fragmentos abren la salida; el tercero está en la ruta superior. Espacio salta; Shift hace dash.");
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && fireDelay <= 0) Attack();
            if (player.position.y < -7) ReceiveDamage(100);
            foreach (Shard shard in shards)
            {
                if (collected.Contains(shard.Id)) continue;
                shard.Visual.Rotate(0, 0, dt * 60);
                if (Vector2.Distance(player.position, shard.Visual.position) < 1.1f)
                {
                    collected.Add(shard.Id); shard.Visual.gameObject.SetActive(false);
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
            portal.color = CanExit ? Color.white : new Color(.25f, .3f, .4f);
            if (CanExit && Vector2.Distance(player.position, portal.transform.position) < 2) { won = true; SetClock(); }
        }

        void FixedUpdate()
        {
            if (player == null || !started || paused || defeated || won) return;
            Keyboard k = Keyboard.current; if (k == null) return;
            grounded = Physics2D.OverlapCircle(player.position + Vector2.down * .86f, .12f, ~(1 << 2)) != null;
            if (grounded) { coyote = .1f; airDash = false; }
            float axis = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
            if (axis != 0) playerSprite.flipX = axis < 0;
            Vector2 velocity = player.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, axis * 7, 75 * Time.fixedDeltaTime);
            if (jumpBuffer > 0 && coyote > 0) { velocity.y = 11; jumpBuffer = coyote = 0; }
            if (dashTime > 0) { velocity.x = (axis == 0 ? (playerSprite.flipX ? -1 : 1) : axis) * 19; velocity.y = Mathf.Max(0, velocity.y); }
            player.linearVelocity = velocity;
        }

        void Attack()
        {
            fireDelay = blade ? .42f : .23f;
            if (bossHP <= 0 || !bossActive) return;
            Vector2 aim = view.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 direction = (aim - player.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(player.position + Vector2.up * .2f, direction, blade ? 2.8f : 22, ~(1 << 2));
            if (hit.collider != null && hit.collider.gameObject == guardian.gameObject)
            {
                bossHP = Mathf.Max(0, bossHP - (blade ? 25 : 9));
                guardian.color = Color.white;
                if (bossHP <= 0)
                {
                    guardian.gameObject.SetActive(false); shots.ForEach(s => Destroy(s.Visual.gameObject)); shots.Clear();
                    Say("Bit: Guardián desactivado. Recuperá dos fragmentos para abrir el portal.");
                }
            }
            Vector2 end = hit.collider != null ? hit.point : player.position + direction * (blade ? 2.8f : 22);
            GameObject beam = new GameObject("Pulso de Zero"); LineRenderer line = beam.AddComponent<LineRenderer>();
            line.sharedMaterial = beamMaterial; line.startColor = line.endColor = Color.cyan;
            line.startWidth = .06f; line.endWidth = .02f; line.positionCount = 2;
            line.SetPosition(0, player.position + Vector2.up * .2f); line.SetPosition(1, end);
            Destroy(beam, .07f);
        }

        void UpdateBoss(float dt)
        {
            if (bossHP <= 0) return;
            if (!bossActive && player.position.x > 80) { bossActive = true; Say("GL1TCH: esta memoria me pertenece. Guardián, eliminá al intruso."); }
            if (!bossActive) return;
            int phase = bossHP > 120 ? 1 : bossHP > 60 ? 2 : 3;
            bossAttack -= dt;
            guardian.transform.position = new Vector3(92, 3 + Mathf.Sin(Time.time * 1.8f) * .5f, 0);
            guardian.color = Color.Lerp(guardian.color, telegraph > 0 ? Color.red : Color.white, dt * 12);
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

        bool CanExit { get { return bossHP <= 0 && collected.Contains(0) && collected.Contains(1); } }
        void ReceiveDamage(float amount)
        {
            if (damageGrace > 0) return;
            hp = Mathf.Max(0, hp - amount); damageGrace = 1;
            if (hp <= 0) { defeated = true; deaths++; SetClock(); }
        }
        void Respawn()
        {
            player.position = checkpoint; player.linearVelocity = Vector2.zero; hp = 100; defeated = false; damageGrace = 2;
            foreach (Shot shot in shots) Destroy(shot.Visual.gameObject); shots.Clear(); SetClock();
        }
        void Say(string message) { subtitle = message; subtitleTime = 7; }
        void SetClock()
        {
            Time.timeScale = !started || paused || defeated || won ? 0 : 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            if (beamMaterial != null) Destroy(beamMaterial);
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (sprites != null) foreach (Sprite sprite in sprites) if (sprite != null) Destroy(sprite);
        }

        void OnGUI()
        {
            if (player == null) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.box.fontSize = 18;
            GUI.Box(new Rect(20, 20, 460, 98), "AE4 // SECTOR DE ARRANQUE 2D");
            GUI.Label(new Rect(36, 52, 450, 60), "INTEGRIDAD " + hp + " / 100   ·   FRAGMENTOS " + collected.Count + "/3\n" + (blade ? "ENERGY BLADE" : "PULSE BLASTER") + "   ·   CAÍDAS " + deaths);
            if (bossActive && bossHP > 0) GUI.Box(new Rect(735, 20, 500, 65), "GUARDIÁN // FASE " + (bossHP > 120 ? 1 : bossHP > 60 ? 2 : 3) + "\nINTEGRIDAD " + bossHP + " / 180");
            if (subtitleTime > 0 || !started) GUI.Box(new Rect(170, 575, 940, 70), subtitle);
            GUI.Label(new Rect(20, 672, 1240, 35), "A/D mover · Espacio saltar · Shift dash · Mouse apuntar · Click atacar · 1/2 arma · E hablar con Bit · Esc pausa");
            if (!started || paused || defeated || won)
            {
                GUI.Box(new Rect(390, 180, 500, 350), "NIVEL CERO // ESCAPE DEL GLITCH");
                string title = won ? "SECTOR RESTAURADO" : defeated ? "INTEGRIDAD PERDIDA" : paused ? "PROTOCOLO EN PAUSA" : "ESCENA ACADÉMICA AE4";
                GUI.Label(new Rect(430, 230, 430, 40), title);
                GUI.Label(new Rect(430, 280, 420, 70), "Dos fragmentos, un Guardián y una salida.\nLa ruta superior guarda código opcional.");
                if (!won && GUI.Button(new Rect(450, 375, 380, 50), defeated ? "REAPARECER EN CHECKPOINT" : started ? "CONTINUAR" : "INICIAR SECTOR 2D"))
                { if (defeated) Respawn(); else { started = true; paused = false; Say(subtitle); SetClock(); } }
                if (GUI.Button(new Rect(450, 445, 380, 45), "REINICIAR ESCENA")) { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                if (GUI.Button(new Rect(450, 495, 380, 35), "VOLVER AL MENÚ 3D")) { Time.timeScale = 1; SceneManager.LoadScene("SectorDeArranque3D"); }
            }
        }
    }
}
