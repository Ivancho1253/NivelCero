using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NivelCero
{
    // Builds the 3D Sector de Arranque (baked into the scene by SceneAuthoring). Primitive
    // shells named "provisional" are fallbacks hidden whenever the authored FBX exists.
    public sealed class SectorBuilder
    {
        public static Material Cyan, Red, Exposed;
        Material steel, dark, gold, edge, cyanDim, steelLight, floorLine, redDark, accent;
        GameAssets assets;
        Transform root;
        public void Build(Session session)
        {
            assets = GameAssets.Load();
            root = new GameObject("Sector de Arranque · instalación industrial").transform;
            // Palette: Docs/Arte/DIRECCION_ARTISTICA.md. Metal mid-values so silhouettes read;
            // cyan only for usable things, red only for threats, amber for rewards/exposed core.
            steel = Mat(new Color(.15f, .2f, .25f), false);
            steelLight = Mat(new Color(.24f, .3f, .36f), false);
            dark = Mat(new Color(.05f, .065f, .085f), false);
            edge = Mat(new Color(.5f, .58f, .64f), false);
            Cyan = Mat(new Color(.02f, .7f, .85f), true);
            cyanDim = Mat(new Color(.02f, .45f, .55f), true, .9f);
            Red = Mat(new Color(.95f, .08f, .16f), true);
            gold = Mat(new Color(1, .7f, .12f), true);
            Exposed = Mat(new Color(1f, .78f, .38f), true, 2.6f);
            floorLine = Mat(new Color(.2f, .3f, .38f), false);
            accent = Mat(new Color(.1f, .5f, .58f), true, .35f); // interactive marking without bloom
            redDark = Mat(new Color(.32f, .05f, .08f), false);
            session.CyanMaterial = Cyan; session.RedMaterial = Red; session.ExposedMaterial = Exposed;
            RenderSettings.ambientLight = new Color(.28f, .31f, .39f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.04f, .055f, .08f); RenderSettings.fogDensity = .0095f;
            var sun = new GameObject("Luz de servicio").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0); sun.shadows = LightShadows.Soft; sun.intensity = .8f;
            sun.color = new Color(.85f, .9f, 1f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            Block("Cubierta de la instalación", new Vector3(0, 10.5f, 70), new Vector3(50, 1, 164), dark);
            foreach (float side in new[] {-1f, 1f})
            {
                Block("Muro de contención exterior", new Vector3(side * 24, 4, 70), new Vector3(1, 12, 164), dark);
                for (float z = -4; z < 150; z += 8)
                {
                    var panel = assets != null ? Model(assets.WallPanel, root, new Vector3(side * 23.4f, 1, z), Vector3.one * 2) : null;
                    if (panel != null) { panel.transform.rotation = Quaternion.Euler(0, side * 90, 0); Retint(panel, cyanDim); }
                }
            }
            for (float z = 4; z < 145; z += 12)
            {
                // Zones: cool service light at the start, neutral in the archive, red-tinged arena.
                bool arena = z > 92 && z < 128;
                Block("Luminaria suspendida", new Vector3(0, 9.9f, z), new Vector3(6, .08f, .25f), arena ? Red : cyanDim, false);
                var light = new GameObject(arena ? "Luz de arena" : "Luz ambiental de servicio").AddComponent<Light>(); light.transform.SetParent(root);
                light.transform.position = new Vector3(0, 7, z); light.type = LightType.Point;
                light.color = arena ? new Color(.95f, .55f, .55f) : z < 56 ? new Color(.62f, .78f, .92f) : new Color(.72f, .74f, .86f);
                light.intensity = arena ? 4.5f : 5.5f; light.range = 17; light.shadows = LightShadows.None;
            }
            // Readability key light over the boss arena (the fight must stay visible).
            var key = new GameObject("Luz principal de arena").AddComponent<Light>(); key.transform.SetParent(root);
            key.type = LightType.Spot; key.transform.position = new Vector3(0, 10, 104); key.transform.rotation = Quaternion.Euler(70, 0, 0);
            key.spotAngle = 95; key.range = 30; key.intensity = 9; key.color = new Color(.92f, .94f, 1f); key.shadows = LightShadows.Soft;
            foreach (float side in new[] {-1f, 1f})
            {
                var rim = new GameObject("Luz de contorno de arena").AddComponent<Light>(); rim.transform.SetParent(root);
                rim.type = LightType.Point; rim.transform.position = new Vector3(side * 12, 3, 111);
                rim.color = new Color(1f, .25f, .3f); rim.intensity = 4; rim.range = 14; rim.shadows = LightShadows.None;
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
                var beacon = Part("Checkpoint " + i, PrimitiveType.Cylinder, session.SpawnPoints[i] - Vector3.up * .9f, new Vector3(1.4f, .1f, 1.4f), cyanDim, false);
                beacon.AddComponent<CheckpointBeacon>().Id = i;
                // Checkpoint gate: two light posts framing the pad; the pad lights up when synced.
                foreach (float s2 in new[] {-1f, 1f})
                {
                    Block("Poste de checkpoint", session.SpawnPoints[i] + new Vector3(s2 * 1.3f, .35f, 0), new Vector3(.22f, 2.5f, .22f), steelLight, false);
                    Block("Señal de checkpoint", session.SpawnPoints[i] + new Vector3(s2 * 1.3f, 1.35f, 0), new Vector3(.24f, .32f, .24f), accent, false);
                }
                var beam = new GameObject("Luz de checkpoint").AddComponent<Light>(); beam.transform.SetParent(beacon.transform, false);
                beam.transform.position = session.SpawnPoints[i] + Vector3.up * 2; beam.type = LightType.Point;
                beam.color = new Color(.35f, .9f, .95f); beam.intensity = 3.5f; beam.range = 7; beam.shadows = LightShadows.None;
            }
            BuildArchitecture();
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
            Gateway(136.6f, 5.4f, cyanDim, "Marco del portal");
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
            var bloom = volume.profile.Add<Bloom>(); bloom.intensity.Override(.22f); bloom.threshold.Override(1.15f);
            var tone = volume.profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            var grade = volume.profile.Add<ColorAdjustments>(); grade.postExposure.Override(.25f); grade.contrast.Override(8);
            var vignette = volume.profile.Add<Vignette>(); vignette.intensity.Override(.18f);
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
            // Soft back light so Zero's dark armour separates from the background.
            var rim = new GameObject("Luz de contorno de Zero").AddComponent<Light>(); rim.transform.SetParent(player.transform, false);
            rim.transform.localPosition = new Vector3(0, 2.4f, -1.2f); rim.type = LightType.Point; rim.color = new Color(.75f, .88f, 1f);
            rim.intensity = 1.6f; rim.range = 3.5f; rim.shadows = LightShadows.None;
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
                    Retint(Model(assets.FloorPanel, root, new Vector3(tx, .015f, tz), Vector3.one), floorLine);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                // Walkable edge: light, non-emissive trim (reads as "floor ends here" without neon).
                Block("Borde transitable", new Vector3(x + side * (width * .5f - .1f), .04f, z), new Vector3(.14f, .1f, length), edge, false);
                int count = 0;
                for (float p = z - length * .5f + 2; p < z + length * .5f; p += 5, count++)
                {
                    Block("Pilar", new Vector3(x + side * (width * .5f + .2f), 2, p), new Vector3(.5f, 4, .6f), dark);
                    if (count % 2 == 0 && side > 0) Block("Luz de orientación", new Vector3(x + side * (width * .5f - .1f), 2.8f, p), new Vector3(.12f, 1.1f, .16f), cyanDim, false);
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
            foreach (float end in new[] {-1f, 1f})
                Block("Borde de caída", new Vector3(x, .04f, z + end * (length * .5f - .08f)), new Vector3(width, .1f, .16f), edge, false);
        }
        void TerminalAt(Vector3 p, string label, bool boss, int seed)
        {
            var go = Block(label, p + Vector3.up * .6f, new Vector3(1, 1.2f, .7f), dark);
            var screen = Block("Pantalla", p + new Vector3(0, 1.2f, 0), new Vector3(.85f, .6f, .15f), Cyan, false);
            screen.transform.SetParent(go.transform, true);
            var t = go.AddComponent<Terminal>(); t.Label = label; t.BossTerminal = boss; t.Seed = seed;
            var glow = new GameObject("Luz de terminal").AddComponent<Light>(); glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0, 1.2f, -.9f); glow.type = LightType.Point;
            glow.color = new Color(.3f, .85f, .95f); glow.intensity = 3; glow.range = 5; glow.shadows = LightShadows.None;
            Block("Plataforma de acceso", p + new Vector3(0, .03f, -1.1f), new Vector3(1.5f, .04f, 1f), accent, false);
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
            // Security units get their own hostile silhouettes (angular, red sensor bar),
            // instead of reusing Zero/Bit models, so allies and threats never look alike.
            if (kind == SecurityAgent.Kind.Drone)
            {
                Piece(go, "Casco del dron", PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(1.1f, .32f, 1.1f), steel, 45);
                Piece(go, "Quilla", PrimitiveType.Cube, new Vector3(0, -.22f, 0), new Vector3(.5f, .2f, .9f), dark, 0);
                Piece(go, "Barra sensora", PrimitiveType.Cube, new Vector3(0, .02f, .62f), new Vector3(.6f, .1f, .08f), Red, 0);
                foreach (float s in new[] {-1f, 1f})
                {
                    Piece(go, "Brazo de rotor", PrimitiveType.Cube, new Vector3(s * .75f, .05f, 0), new Vector3(.5f, .06f, .12f), dark, 0);
                    Piece(go, "Rotor", PrimitiveType.Cylinder, new Vector3(s * 1.05f, .12f, 0), new Vector3(.55f, .02f, .55f), steelLight, 0);
                }
            }
            else if (kind == SecurityAgent.Kind.Ground)
            {
                foreach (float s in new[] {-1f, 1f})
                {
                    Piece(go, "Pierna", PrimitiveType.Cube, new Vector3(s * .28f, -.15f, 0), new Vector3(.26f, .9f, .34f), dark, 0);
                    Piece(go, "Pie", PrimitiveType.Cube, new Vector3(s * .28f, -.58f, .08f), new Vector3(.32f, .14f, .5f), steel, 0);
                    Piece(go, "Hombrera", PrimitiveType.Cube, new Vector3(s * .62f, .95f, 0), new Vector3(.34f, .3f, .5f), steelLight, s * 18);
                    Piece(go, "Brazo", PrimitiveType.Cube, new Vector3(s * .66f, .55f, .1f), new Vector3(.18f, .6f, .2f), dark, 0);
                }
                Piece(go, "Torso blindado", PrimitiveType.Cube, new Vector3(0, .65f, 0), new Vector3(.95f, .75f, .6f), steel, 0);
                Piece(go, "Visor", PrimitiveType.Cube, new Vector3(0, .95f, .31f), new Vector3(.6f, .12f, .05f), Red, 0);
            }
            else
            {
                Piece(go, "Base de torreta", PrimitiveType.Cylinder, new Vector3(0, -.85f, 0), new Vector3(1.1f, .25f, 1.1f), dark, 0);
                Piece(go, "Columna", PrimitiveType.Cylinder, new Vector3(0, -.35f, 0), new Vector3(.45f, .35f, .45f), steel, 0);
                Piece(go, "Cabezal", PrimitiveType.Cube, new Vector3(0, .15f, 0), new Vector3(.8f, .5f, .8f), steelLight, 0);
                Piece(go, "Cañón", PrimitiveType.Cube, new Vector3(0, .15f, .65f), new Vector3(.16f, .16f, .7f), dark, 0);
                Piece(go, "Sensor", PrimitiveType.Cube, new Vector3(0, .32f, .41f), new Vector3(.5f, .08f, .04f), Red, 0);
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
        Material Mat(Color color, bool glow, float emission = 2)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader); mat.color = color; mat.enableInstancing = true;
            mat.SetFloat("_Metallic", glow ? .1f : .6f); mat.SetFloat("_Smoothness", glow ? .5f : .42f);
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            return mat;
        }
        /// <summary>Replace the authored emissive cyan of repeated kit pieces (too much neon).</summary>
        static void Retint(GameObject model, Material replacement)
        {
            if (model == null || replacement == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.Contains("Cyan")) { mats[i] = replacement; changed = true; }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        // Zone thresholds: a frame across the route with a coloured header (landmark + orientation).
        void Gateway(float z, float halfWidth, Material accent, string name)
        {
            foreach (float s in new[] {-1f, 1f})
            {
                Block(name + " · columna", new Vector3(s * halfWidth, 3, z), new Vector3(.7f, 6, .7f), steelLight, false);
                Block(name + " · base", new Vector3(s * halfWidth, .25f, z), new Vector3(1.1f, .5f, 1.1f), dark, false);
            }
            Block(name + " · dintel", new Vector3(0, 6.25f, z), new Vector3(halfWidth * 2 + .7f, .7f, .7f), steel, false);
            Block(name + " · franja", new Vector3(0, 5.75f, z - .36f), new Vector3(halfWidth * 2 - .6f, .14f, .04f), accent, false);
        }

        void BuildArchitecture()
        {
            // Ceiling rhythm instead of a flat slab: cross beams and two service ducts.
            for (float z = -4; z < 150; z += 6) Block("Viga de techo", new Vector3(0, 9.7f, z), new Vector3(46, .45f, .35f), steel, false);
            foreach (float s in new[] {-1f, 1f}) Block("Conducto de servicio", new Vector3(s * 9, 9.1f, 70), new Vector3(1.1f, 1.1f, 160), steelLight, false);
            // Zone thresholds: Seguridad, Acceso restringido, Núcleo, Arena (red: threat ahead).
            Gateway(36.4f, 7.6f, cyanDim, "Umbral · Seguridad");
            Gateway(58.4f, 5.6f, cyanDim, "Umbral · Exploración");
            Gateway(77.4f, 5.6f, cyanDim, "Umbral · Núcleo de Seguridad");
            Gateway(97.2f, 8.6f, Red, "Umbral · Arena del Guardián");
            // Set dressing with collision, kept off the main line and side-route connectors.
            foreach (var p in new[] { new Vector3(-5.6f, 0, 12), new Vector3(-6.4f, 0, 50), new Vector3(-7.6f, 0, 84), new Vector3(-12.4f, 0, 100.5f), new Vector3(12.4f, 0, 121) })
                Crates(p);
            // Arena: dark-red floor ring framing the Guardian, and security pylons at the back.
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16;
                var seg = Block("Anillo de la arena", new Vector3(Mathf.Sin(a) * 9.5f, .05f, 111 + Mathf.Cos(a) * 9.5f), new Vector3(3, .06f, .35f), redDark, false);
                seg.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg + 90, 0);
            }
            foreach (float s in new[] {-1f, 1f})
            {
                Block("Pilón de seguridad", new Vector3(s * 11, 3.5f, 123), new Vector3(1.4f, 7, 1.4f), steel, false);
                Block("Franja de amenaza", new Vector3(s * 11, 4.5f, 122.27f), new Vector3(.3f, 4, .06f), Red, false);
            }
        }

        void Crates(Vector3 at)
        {
            var big = Block("Contenedor de servicio", at + new Vector3(0, .6f, 0), new Vector3(1.6f, 1.2f, 1.2f), steel);
            Block("Fleje", at + new Vector3(0, 1.21f, 0), new Vector3(1.62f, .04f, .25f), edge, false);
            var small = Block("Caja", at + new Vector3(.2f, 1.6f, .1f), new Vector3(.8f, .8f, .8f), steelLight);
            small.transform.rotation = Quaternion.Euler(0, 18, 0);
            Block("Caja", at + new Vector3(1.4f, .4f, .5f), new Vector3(.8f, .8f, .8f), dark);
        }

        void Piece(GameObject owner, string name, PrimitiveType shape, Vector3 local, Vector3 size, Material mat, float yaw)
        {
            var part = Part(name, shape, owner.transform.position + local, size, mat, false);
            part.transform.SetParent(owner.transform, true);
            part.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            part.layer = owner.layer;
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
