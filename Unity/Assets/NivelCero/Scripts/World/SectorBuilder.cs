using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NivelCero
{
    // First reconstruction pass. Architecture and assembled models are explicitly provisional.
    public sealed class SectorBuilder
    {
        public static Material Cyan, Red;
        Material steel, dark, gold;
        GameAssets assets;
        Transform root;
        public void Build(Session session)
        {
            assets = GameAssets.Load();
            root = new GameObject("Sector de Arranque · instalación industrial").transform;
            steel = Mat(new Color(.12f, .17f, .22f), false);
            dark = Mat(new Color(.035f, .05f, .07f), false);
            Cyan = Mat(new Color(.02f, .7f, .85f), true);
            Red = Mat(new Color(.95f, .08f, .16f), true);
            gold = Mat(new Color(1, .7f, .12f), true);
            session.CyanMaterial = Cyan; session.RedMaterial = Red;
            RenderSettings.ambientLight = new Color(.2f, .25f, .35f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.025f, .04f, .065f); RenderSettings.fogDensity = .013f;
            var sun = new GameObject("Luz de servicio").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0); sun.shadows = LightShadows.Soft; sun.intensity = .65f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            Block("Cubierta de la instalación", new Vector3(0, 10.5f, 70), new Vector3(50, 1, 164), dark);
            foreach (float side in new[] {-1f, 1f})
            {
                Block("Muro de contención exterior", new Vector3(side * 24, 4, 70), new Vector3(1, 12, 164), dark);
                for (float z = -4; z < 150; z += 8)
                {
                    var panel = assets != null ? Model(assets.WallPanel, root, new Vector3(side * 23.4f, 1, z), Vector3.one * 2) : null;
                    if (panel != null) panel.transform.rotation = Quaternion.Euler(0, side * 90, 0);
                }
            }
            for (float z = 4; z < 145; z += 12)
            {
                Block("Luminaria suspendida", new Vector3(0, 9.9f, z), new Vector3(6, .08f, .25f), Cyan, false);
                var light = new GameObject("Luz ambiental de servicio").AddComponent<Light>(); light.transform.SetParent(root);
                light.transform.position = new Vector3(0, 7, z); light.type = LightType.Point;
                light.color = new Color(.35f, .65f, .85f); light.intensity = 5; light.range = 16; light.shadows = LightShadows.None;
            }
            Platform(0, 8, 14, 24); Platform(0, 29, 10, 12);
            Platform(0, 46, 16, 20); Platform(0, 67, 12, 18);
            Platform(0, 87, 18, 22); Platform(0, 110, 28, 28); Platform(0, 132, 10, 16);
            // Two secondary routes reconnect to the main route.
            Platform(-11, 28, 6, 16); Platform(-8, 38, 12, 5);
            Platform(11, 68, 7, 20); Platform(8, 79, 12, 5);
            Block("Contención", new Vector3(-3, 1.3f, 47), new Vector3(2, 2.6f, 2), steel);
            Block("Contención", new Vector3(4, 1.3f, 72), new Vector3(2, 2.6f, 4), steel);
            session.AccessDoor = Block("Firewall · acceso por circuito", new Vector3(0, 3, 56), new Vector3(30, 6, .6f), Red);
            session.AccessDoor.GetComponent<Renderer>().enabled = false;
            for (int x = -14; x <= 14; x++)
            {
                var beam = Part("Malla del firewall", PrimitiveType.Cylinder, new Vector3(x, 3, 56), new Vector3(.055f, 3, .055f), Red, false);
                beam.transform.SetParent(session.AccessDoor.transform, true);
            }
            TerminalAt(new Vector3(4, 0, 51), "DESBLOQUEAR ACCESO", false, 51);
            TerminalAt(new Vector3(-9, 0, 103), "NODO ALFA · ESCUDO", true, 103);
            TerminalAt(new Vector3(9, 0, 115), "NODO BETA · ESCUDO", true, 115);
            Shard(new Vector3(0, 1.3f, 31), "fragment-main-1");
            Shard(new Vector3(12, 1.3f, 70), "fragment-main-2");
            Shard(new Vector3(-11, 1.3f, 29), "fragment-optional");
            Block("Conducto bajo · ruta opcional", new Vector3(-11, 1.65f, 25), new Vector3(6, .8f, 1.6f), dark);
            foreach (var p in new[] {new Vector3(-11, 1, 30), new Vector3(10, 1, 119)})
                Part("Celda de reparación", PrimitiveType.Cylinder, p, new Vector3(.35f, .5f, .35f), Cyan, false).AddComponent<RepairCell>();
            for (int i = 1; i < session.SpawnPoints.Length; i++)
            {
                var beacon = Part("Checkpoint " + i, PrimitiveType.Cylinder, session.SpawnPoints[i] - Vector3.up * .9f, new Vector3(1.4f, .1f, 1.4f), Cyan, false);
                beacon.AddComponent<CheckpointBeacon>().Id = i;
            }
            BuildPlayer(session);
            var bit = Part("Bit · acompañante provisional", PrimitiveType.Sphere, new Vector3(2, 2, 4), Vector3.one * .45f, gold, false);
            var ring = Part("Anillo de Bit", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.8f, .025f, .8f), Cyan, false);
            ring.transform.SetParent(bit.transform, false); bit.AddComponent<BitFollower>();
            if (assets != null && assets.Bit != null)
            {
                bit.GetComponent<Renderer>().enabled = false; ring.SetActive(false);
                Model(assets.Bit, bit.transform, Vector3.zero, Vector3.one * 2.2f);
            }
            Enemy(new Vector3(0, 2, 43), SecurityAgent.Kind.Drone);
            Enemy(new Vector3(-2, 1, 65), SecurityAgent.Kind.Ground);
            Enemy(new Vector3(5, 1.2f, 85), SecurityAgent.Kind.Turret);
            BuildBoss(session);
            foreach (float z in new[] {105f, 116f})
            {
                var hazard = new GameObject("Seguridad de arena · láser anunciado");
                hazard.transform.SetParent(root); hazard.transform.position = new Vector3(0, .7f, z);
                hazard.AddComponent<ArenaHazard>().Boss = session.Boss;
            }
            var portal = Part("Portal hacia Memoria Fragmentada", PrimitiveType.Cylinder, new Vector3(0, 2, 136), new Vector3(3, .12f, 3), Cyan, false);
            portal.transform.rotation = Quaternion.Euler(90, 0, 0); portal.AddComponent<ExitPortal>();
            Physics.SyncTransforms();
            var nav = root.gameObject.AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.Children; nav.layerMask = 1;
            nav.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            nav.BuildNavMesh();
            session.gameObject.AddComponent<GameHud>();
            session.gameObject.AddComponent<AdaptiveAudio>();
            var volume = root.gameObject.AddComponent<Volume>(); volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = volume.profile.Add<Bloom>(); bloom.intensity.Override(.3f); bloom.threshold.Override(1.2f);
            var tone = volume.profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            root.gameObject.AddComponent<StaticEnvironmentBatch>();
        }
        void BuildPlayer(Session s)
        {
            var player = new GameObject("Zero"); player.layer = 2; player.transform.position = s.SpawnPoints[0];
            var body = player.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = .32f; body.center = Vector3.up * .9f; body.stepOffset = .3f;
            s.PlayerHealth = player.AddComponent<Health>(); s.PlayerHealth.Maximum = 100; s.PlayerHealth.Restore();
            var avatar = new GameObject("Zero · modelo ensamblado PROVISIONAL"); avatar.transform.SetParent(player.transform, false);
            var torso = Part("Armadura", PrimitiveType.Capsule, Vector3.zero, new Vector3(.65f, .55f, .4f), steel, false);
            torso.transform.SetParent(avatar.transform, false); torso.transform.localPosition = Vector3.up;
            var head = Part("Visor", PrimitiveType.Sphere, Vector3.zero, new Vector3(.45f, .35f, .4f), Cyan, false);
            head.transform.SetParent(avatar.transform, false); head.transform.localPosition = Vector3.up * 1.65f;
            if (assets != null && assets.Zero != null)
            {
                torso.SetActive(false); head.SetActive(false);
                var model = Model(assets.Zero, avatar.transform, Vector3.zero, Vector3.one);
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = assets.ZeroAnimator; animator.applyRootMotion = false;
                if (assets.ZeroAvatar != null) animator.avatar = assets.ZeroAvatar;
            }
            foreach (var t in avatar.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
            var cameraObject = new GameObject("Cámara de Zero"); var lens = cameraObject.AddComponent<Camera>();
            lens.tag = "MainCamera"; lens.nearClipPlane = .05f; lens.farClipPlane = 300;
            lens.backgroundColor = RenderSettings.fogColor; lens.clearFlags = CameraClearFlags.SolidColor;
            lens.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cameraObject.AddComponent<AudioListener>();
            var view = cameraObject.AddComponent<PlayerCamera>(); view.Lens = lens; view.Target = player.transform; view.Visual = avatar.transform;
            s.Player = player.AddComponent<PlayerMotor>(); s.Player.View = view; s.Player.Visual = avatar.transform;
            s.Combat = player.AddComponent<PlayerCombat>(); s.Combat.View = view;
            player.AddComponent<AvatarAnimation>();
            player.AddComponent<DashTrail>();
            player.AddComponent<InteractionScanner>();
        }
        void Platform(float x, float z, float width, float length)
        {
            Block("Suelo industrial", new Vector3(x, -.4f, z), new Vector3(width, .8f, length), steel);
            if (assets != null && assets.FloorPanel != null)
            {
                for (float tx = x - width / 2 + 2; tx <= x + width / 2 - 1.9f; tx += 4)
                for (float tz = z - length / 2 + 2; tz <= z + length / 2 - 1.9f; tz += 4)
                    Model(assets.FloorPanel, root, new Vector3(tx, .015f, tz), Vector3.one);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Block("Guía cian", new Vector3(x + side * (width * .5f - .1f), .04f, z), new Vector3(.1f, .08f, length), Cyan, false);
                for (float p = z - length * .5f + 2; p < z + length * .5f; p += 5)
                {
                    Block("Pilar", new Vector3(x + side * (width * .5f + .2f), 2, p), new Vector3(.5f, 4, .6f), dark);
                    Block("Luz de orientación", new Vector3(x + side * (width * .5f - .1f), 2.8f, p), new Vector3(.15f, 1.3f, .2f), Cyan, false);
                    if (assets != null)
                    {
                        Model(assets.Pillar, root, new Vector3(x + side * (width / 2 + .25f), 0, p), Vector3.one);
                        if (p > z && width >= 10)
                        {
                            var wall = Model(assets.WallPanel, root, new Vector3(x + side * (width / 2 + .5f), 0, p), Vector3.one);
                            if (wall != null) wall.transform.rotation = Quaternion.Euler(0, 90, 0);
                        }
                    }
                }
            }
            Block("Conducto", new Vector3(x, -1, z), new Vector3(width * .6f, .7f, length * .85f), dark);
        }
        void TerminalAt(Vector3 p, string label, bool boss, int seed)
        {
            var go = Block(label, p + Vector3.up * .6f, new Vector3(1, 1.2f, .7f), dark);
            var screen = Block("Pantalla", p + new Vector3(0, 1.2f, 0), new Vector3(.85f, .6f, .15f), Cyan, false);
            screen.transform.SetParent(go.transform, true);
            var t = go.AddComponent<Terminal>(); t.Label = label; t.BossTerminal = boss; t.Seed = seed;
            if (assets != null && assets.Terminal != null)
            {
                go.GetComponent<Renderer>().enabled = false; screen.SetActive(false);
                Model(assets.Terminal, go.transform, new Vector3(0, -.5f, 0), Vector3.one);
            }
        }
        void Shard(Vector3 p, string id)
        {
            var go = Part(id, PrimitiveType.Capsule, p, new Vector3(.35f, .5f, .35f), gold, false);
            go.AddComponent<CodeShard>().Id = id;
        }
        void Enemy(Vector3 p, SecurityAgent.Kind kind)
        {
            var go = new GameObject("Seguridad · " + kind); go.transform.position = p; go.layer = 8;
            if (kind == SecurityAgent.Kind.Ground)
            {
                var cc = go.AddComponent<CapsuleCollider>(); cc.height = 1.8f; cc.center = Vector3.up * .4f; cc.radius = .4f;
            }
            else go.AddComponent<SphereCollider>().radius = .75f;
            go.AddComponent<Health>();
            var shell = Part("Carcasa provisional", PrimitiveType.Sphere, p, new Vector3(1.4f, .8f, 1.1f), steel, false);
            shell.transform.SetParent(go.transform, true);
            var eye = Part("Sensor", PrimitiveType.Sphere, p + Vector3.forward * .5f, Vector3.one * .3f, Red, false); eye.transform.SetParent(go.transform, true);
            if (assets != null)
            {
                var prefab = kind == SecurityAgent.Kind.Ground ? assets.Zero : kind == SecurityAgent.Kind.Drone ? assets.Bit : assets.PulseBlaster;
                if (prefab != null)
                {
                    shell.SetActive(false);
                    Model(prefab, go.transform, kind == SecurityAgent.Kind.Ground ? Vector3.down * .5f : Vector3.zero, Vector3.one * (kind == SecurityAgent.Kind.Drone ? 1.8f : 1));
                }
            }
            go.AddComponent<SecurityAgent>().Type = kind;
        }
        void BuildBoss(Session s)
        {
            var go = new GameObject("Guardián del Sistema"); go.transform.position = new Vector3(0, 1, 111); go.layer = 8;
            go.AddComponent<SphereCollider>().radius = 2; go.AddComponent<Health>();
            var shell = Part("Armadura Guardián provisional", PrimitiveType.Sphere, go.transform.position, Vector3.one * 4, steel, false); shell.transform.SetParent(go.transform, true);
            var core = Part("Núcleo vulnerable", PrimitiveType.Sphere, go.transform.position + Vector3.back * 1.9f, Vector3.one * 1.4f, Red, false); core.transform.SetParent(go.transform, true);
            for (int i = -1; i <= 1; i += 2)
            {
                var arm = Part("Brazo", PrimitiveType.Capsule, go.transform.position + Vector3.right * i * 2.5f, new Vector3(.9f, 2, .9f), steel, false); arm.transform.SetParent(go.transform, true);
            }
            s.Boss = go.AddComponent<Guardian>(); s.Boss.Core = core.GetComponent<Renderer>();
            core.layer = 8;
            core.AddComponent<SphereCollider>().radius = .5f;
            core.AddComponent<DamageHitbox>().Target = go.GetComponent<Health>();
            if (assets != null && assets.Guardian != null)
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>()) if (renderer != s.Boss.Core) renderer.enabled = false;
                Model(assets.Guardian, go.transform, Vector3.down, Vector3.one);
            }
        }
        GameObject Model(GameObject prefab, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            if (prefab == null) return null;
            var result = Object.Instantiate(prefab, parent); result.transform.localPosition = localPosition;
            result.transform.localRotation = Quaternion.identity; result.transform.localScale = scale;
            foreach (var item in result.GetComponentsInChildren<Transform>()) item.gameObject.layer = parent.gameObject.layer;
            return result;
        }
        Material Mat(Color color, bool glow)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader); mat.color = color; mat.enableInstancing = true;
            mat.SetFloat("_Metallic", glow ? .1f : .7f); mat.SetFloat("_Smoothness", .5f);
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2); }
            return mat;
        }
        GameObject Block(string name, Vector3 p, Vector3 size, Material mat, bool collision = true) { return Part(name, PrimitiveType.Cube, p, size, mat, collision); }
        GameObject Part(string name, PrimitiveType shape, Vector3 p, Vector3 size, Material mat, bool collision)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(root);
            go.transform.position = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collision)
            {
                var collider = go.GetComponent<Collider>(); collider.enabled = false;
                if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            }
            return go;
        }
    }
}
