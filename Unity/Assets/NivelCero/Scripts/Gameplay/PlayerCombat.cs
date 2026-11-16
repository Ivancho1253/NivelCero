using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    [RequireComponent(typeof(Health))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public PlayerCamera View;
        public int Magazine = 18;
        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public string WeaponName { get { return blade ? "Energy Blade" : "Pulse Blaster"; } }
        public bool Blocking { get { return blade && !Reloading && Session.I != null && !Session.I.Blocked &&
                    Mouse.current != null && Mouse.current.rightButton.isPressed; } }
        public Vector3 AimForward { get { return View != null && View.Lens != null ?
                    Vector3.ProjectOnPlane(View.Lens.transform.forward, Vector3.up).normalized : transform.forward; } }
        private bool blade;
        private float nextAttack, comboUntil;
        private int combo;
        private Transform gunVisual, bladeVisual;
        private Material cyan;
        private AudioSource audioSource;
        private AudioClip shotSound, bladeSound, reloadSound;

        private void Awake()
        {
            Ammo = Magazine;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0;
            audioSource.volume = .12f;
            shotSound = Tone("Pulse", 540, .08f);
            bladeSound = Tone("Blade", 160, .13f);
            reloadSound = Tone("Reload", 900, .16f);
        }

        private void Start()
        {
            // These are explicitly provisional assembled weapon meshes, not final authored assets.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            cyan = new Material(shader);
            cyan.color = new Color(.05f, .8f, 1);
            cyan.EnableKeyword("_EMISSION");
            cyan.SetColor("_EmissionColor", new Color(.05f, .8f, 1) * 2);
            gunVisual = MakeWeapon("Pulse Blaster [PROVISIONAL]", new Vector3(.16f, .13f, .48f));
            bladeVisual = MakeWeapon("Energy Blade [PROVISIONAL]", new Vector3(.045f, .8f, .06f));
        }

        private Transform MakeWeapon(string name, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Destroy(part.GetComponent<Collider>());
            part.layer = 2;
            part.transform.SetParent(View != null && View.Lens != null ? View.Lens.transform : transform, false);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = cyan;
            return part.transform;
        }

        private void Update()
        {
            if (View == null || View.Lens == null) return;
            bool blocked = Session.I == null || Session.I.Blocked || GetComponent<Health>().Dead;
            if (gunVisual != null) gunVisual.gameObject.SetActive(!blocked && !blade);
            if (bladeVisual != null) bladeVisual.gameObject.SetActive(!blocked && blade);
            if (blocked) return;
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keys == null || mouse == null) return;
            if (keys.digit1Key.wasPressedThisFrame) blade = false;
            if (keys.digit2Key.wasPressedThisFrame) blade = true;
            if (keys.rKey.wasPressedThisFrame && !blade && !Reloading && Ammo < Magazine) StartCoroutine(Reload());
            if (gunVisual != null)
            {
                float tilt = Reloading ? Mathf.Sin(Time.time * 9) * 35 : 0;
                gunVisual.localPosition = new Vector3(.26f, -.23f, .52f);
                gunVisual.localRotation = Quaternion.Euler(tilt, 0, 0);
            }
            if (bladeVisual != null)
            {
                float swing = Mathf.Clamp01((nextAttack - Time.time) / .35f);
                bladeVisual.localPosition = new Vector3(.32f - Mathf.Sin(swing * Mathf.PI) * .65f, -.19f, .65f);
                bladeVisual.localRotation = Quaternion.Euler(0, 0, Blocking ? 65 : Mathf.Sin(swing * Mathf.PI) * -100);
            }
            bool attack = blade ? mouse.leftButton.wasPressedThisFrame : mouse.leftButton.isPressed;
            if (!attack || Reloading || Time.time < nextAttack || Blocking || Cursor.lockState != CursorLockMode.Locked) return;
            if (blade) Swing();
            else Shoot();
        }

        private IEnumerator Reload()
        {
            Reloading = true;
            audioSource.PlayOneShot(reloadSound);
            yield return new WaitForSeconds(1.25f);
            Ammo = Magazine;
            Reloading = false;
        }

        private void Shoot()
        {
            nextAttack = Time.time + .16f;
            if (Ammo <= 0)
            {
                Session.I.Notify("Sin energía en el cargador. Presioná R para recargar.");
                return;
            }
            Ammo--;
            Transform camera = View.Lens.transform;
            RaycastHit hit;
            Vector3 destination = camera.position + camera.forward * 100;
            Health target = null;
            if (Physics.Raycast(camera.position, camera.forward, out hit, 100, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                destination = hit.point;
                target = hit.collider.GetComponentInParent<Health>();
            }
            // Second ray starts at the avatar muzzle. Shoulder cameras cannot shoot around walls.
            Vector3 chest = transform.position + Vector3.up * 1.25f;
            Vector3 muzzle = chest + AimForward * .32f;
            if (Physics.Raycast(chest, AimForward, out hit, .32f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                // A narrow obstacle between body and muzzle must not become an inside-collider ray origin.
                Trace(chest, hit.point, new Color(.1f, .9f, 1));
                audioSource.PlayOneShot(shotSound);
                View.Recoil(.65f);
                return;
            }
            Vector3 line = destination - muzzle;
            if (Physics.Raycast(muzzle, line.normalized, out hit, line.magnitude + .05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                destination = hit.point;
                target = hit.collider.GetComponentInParent<Health>();
            }
            if (target != null && target != GetComponent<Health>()) target.Damage(20, muzzle);
            Trace(muzzle, destination, new Color(.1f, .9f, 1));
            audioSource.PlayOneShot(shotSound);
            View.Recoil(.65f);
        }

        private void Swing()
        {
            combo = Time.time > comboUntil ? 0 : (combo + 1) % 3;
            comboUntil = Time.time + .85f;
            nextAttack = Time.time + (combo == 2 ? .55f : .34f);
            Vector3 start = transform.position + Vector3.up * 1.05f;
            HashSet<Health> struck = new HashSet<Health>();
            Collider[] contacts = Physics.OverlapSphere(start + AimForward * 1.2f, 1.3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider contact in contacts)
            {
                Health target = contact.GetComponentInParent<Health>();
                if (target == null || target == GetComponent<Health>() || struck.Contains(target)) continue;
                Vector3 nearest = contact.ClosestPoint(start);
                Vector3 toward = nearest - start;
                if (toward.magnitude > 2.4f || Vector3.Dot(AimForward, toward.normalized) < .2f) continue;
                RaycastHit obstruction;
                if (Physics.Raycast(start, toward.normalized, out obstruction, toward.magnitude + .05f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    obstruction.collider.GetComponentInParent<Health>() != target) continue;
                struck.Add(target);
                target.Damage(combo == 2 ? 45 : 28, start);
                Trace(start, nearest, new Color(.3f, 1, .9f));
            }
            audioSource.PlayOneShot(bladeSound);
            View.Recoil(.3f);
        }

        private void Trace(Vector3 from, Vector3 to, Color color)
        {
            GameObject effect = new GameObject("Impacto de energía");
            LineRenderer line = effect.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPositions(new Vector3[] { from, to });
            line.startWidth = .035f;
            line.endWidth = .012f;
            line.sharedMaterial = cyan;
            line.startColor = line.endColor = color;
            Destroy(effect, .08f);
        }

        private AudioClip Tone(string name, float frequency, float duration)
        {
            int count = Mathf.CeilToInt(22050 * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / 22050;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t * (1 - t)) * Mathf.Exp(-t * 30);
            }
            AudioClip clip = AudioClip.Create(name, count, 1, 22050, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (cyan != null) Destroy(cyan);
            if (shotSound != null) Destroy(shotSound);
            if (bladeSound != null) Destroy(bladeSound);
            if (reloadSound != null) Destroy(reloadSound);
        }
    }
}
