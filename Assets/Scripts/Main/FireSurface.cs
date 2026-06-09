using UnityEngine;

namespace Main
{
    public class FireSurface : MonoBehaviour
    {
        [SerializeField] private float triggerRadius = 0.12f;
        [SerializeField] private float smokeLingeringLifeTime = 10f;
        [SerializeField] private float mediumSmokeLingeringLifeTime = 15f;
        [SerializeField] private float largeSmokeLingeringLifeTime = 20f;
        [SerializeField] private float smallFireScale = 1f;
        [SerializeField] private float mediumFireScale = 1.6f;
        [SerializeField] private float largeFireScale = 2.4f;

        private ParticleSystem fireEffect;
        private ParticleSystem[] fireStagePrefabs;
        private SmokeCloud smokeCloud;
        private GameObject fallbackFireVisual;
        private bool extinguished;
        private int fireStage;

        public bool IsBurning => !extinguished;

        public void Initialize(ParticleSystem fireEffectPrefab, ParticleSystem smokeEffectPrefab)
        {
            Initialize(fireEffectPrefab, null, null, smokeEffectPrefab);
        }

        public void Initialize(
            ParticleSystem smallFireEffectPrefab,
            ParticleSystem mediumFireEffectPrefab,
            ParticleSystem largeFireEffectPrefab,
            ParticleSystem smokeEffectPrefab)
        {
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = triggerRadius;

            fireStagePrefabs = new[] { smallFireEffectPrefab, mediumFireEffectPrefab, largeFireEffectPrefab };
            SetFireStage(0);
            Debug.Log("[FireSurface] Fire created at stage 1 (Small).", this);

            if (smokeEffectPrefab != null)
            {
                ParticleSystem smokeEffect = Instantiate(smokeEffectPrefab, transform.position + Vector3.up * 0.1f, Quaternion.identity, transform);
                smokeEffect.transform.localPosition = Vector3.up * 0.1f;
                smokeEffect.transform.rotation = Quaternion.identity;
                var smokeMain = smokeEffect.main;
                smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
                smokeEffect.Play();

                smokeCloud = smokeEffect.gameObject.GetComponent<SmokeCloud>();
                if (smokeCloud == null)
                {
                    smokeCloud = smokeEffect.gameObject.AddComponent<SmokeCloud>();
                }
            }
            else
            {
                GameObject smokeVisual = CreateFallbackVisual("Smoke Visual", PrimitiveType.Sphere, new Color(0.25f, 0.25f, 0.25f, 1f), Vector3.up * 0.25f, Vector3.one * 0.55f);
                smokeCloud = smokeVisual.AddComponent<SmokeCloud>();
            }

            if (smokeCloud != null)
            {
                smokeCloud.Initialize(this, smokeLingeringLifeTime);
                ApplySmokeLingeringTimeForCurrentFireStage();
            }
        }

        public void Extinguish()
        {
            Extinguish(true);
        }

        public void Extinguish(bool removeSmoke)
        {
            if (extinguished)
            {
                Debug.Log("[FireSurface] Extinguish ignored because fire is already extinguished.", this);
                return;
            }

            extinguished = true;
            Debug.Log($"[FireSurface] Fire extinguished. removeSmoke={removeSmoke}", this);

            if (fireEffect != null)
            {
                fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (fallbackFireVisual != null)
            {
                fallbackFireVisual.SetActive(false);
            }

            if (smokeCloud != null)
            {
                if (removeSmoke)
                {
                    smokeCloud.RemoveImmediately();
                }
                else
                {
                    // Keep smoke under this object so the parent trigger remains active
                    // until the lingering smoke is destroyed.
                    smokeCloud.DetachFromFire(gameObject);
                }
            }

            if (removeSmoke || smokeCloud == null)
            {
                Destroy(gameObject);
                return;
            }

            Destroy(this);
        }

        public void IntensifyFire()
        {
            if (extinguished)
            {
                Debug.Log("[FireSurface] Wind tried to intensify fire, but it is already extinguished.", this);
                return;
            }

            if (fireStage >= 2)
            {
                Debug.Log("[FireSurface] Wind hit fire smoke, but fire is already at max stage 3 (Large).", this);
                return;
            }

            SetFireStage(fireStage + 1);
            ApplySmokeLingeringTimeForCurrentFireStage();
        }

        private void SetFireStage(int stage)
        {
            int previousStage = fireStage;
            fireStage = Mathf.Clamp(stage, 0, 2);
            float stageScale = GetFireStageScale(fireStage);

            if (fireEffect != null)
            {
                Destroy(fireEffect.gameObject);
                fireEffect = null;
            }

            if (fallbackFireVisual != null)
            {
                Destroy(fallbackFireVisual);
                fallbackFireVisual = null;
            }

            ParticleSystem firePrefab = fireStagePrefabs != null && fireStage < fireStagePrefabs.Length
                ? fireStagePrefabs[fireStage]
                : null;

            if (firePrefab != null)
            {
                fireEffect = Instantiate(firePrefab, transform.position, Quaternion.identity, transform);
                fireEffect.transform.localPosition = Vector3.zero;
                fireEffect.transform.rotation = Quaternion.identity;
                fireEffect.transform.localScale = Vector3.one * stageScale;
                var fireMain = fireEffect.main;
                fireMain.simulationSpace = ParticleSystemSimulationSpace.World;
                fireEffect.Play();
                Debug.Log(
                    $"[FireSurface] Fire stage changed {GetStageDisplayName(previousStage)} -> {GetStageDisplayName(fireStage)} using prefab '{firePrefab.name}' with scale {stageScale}.",
                    this);
                return;
            }

            float fallbackScale = (fireStage == 0 ? 0.35f : fireStage == 1 ? 0.55f : 0.8f) * stageScale;
            Color fallbackColor = fireStage == 0
                ? new Color(1f, 0.24f, 0.02f, 1f)
                : fireStage == 1
                    ? new Color(1f, 0.45f, 0.02f, 1f)
                    : new Color(1f, 0.68f, 0.04f, 1f);

            fallbackFireVisual = CreateFallbackVisual("Fire Visual", PrimitiveType.Sphere, fallbackColor, Vector3.zero, Vector3.one * fallbackScale);
            Debug.Log(
                $"[FireSurface] Fire stage changed {GetStageDisplayName(previousStage)} -> {GetStageDisplayName(fireStage)} using fallback visual with scale {fallbackScale}.",
                this);
        }

        private float GetFireStageScale(int stage)
        {
            switch (stage)
            {
                case 1:
                    return mediumFireScale;
                case 2:
                    return largeFireScale;
                default:
                    return smallFireScale;
            }
        }

        private float GetSmokeLingeringLifeTimeForStage(int stage)
        {
            switch (stage)
            {
                case 1:
                    return mediumSmokeLingeringLifeTime;
                case 2:
                    return largeSmokeLingeringLifeTime;
                default:
                    return smokeLingeringLifeTime;
            }
        }

        private void ApplySmokeLingeringTimeForCurrentFireStage()
        {
            if (smokeCloud == null)
            {
                return;
            }

            smokeCloud.SetLingeringLifeTime(GetSmokeLingeringLifeTimeForStage(fireStage));
        }

        private string GetStageDisplayName(int stage)
        {
            switch (stage)
            {
                case 1:
                    return "stage 2 (Medium)";
                case 2:
                    return "stage 3 (Large)";
                default:
                    return "stage 1 (Small)";
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsBrick(other))
            {
                Extinguish(false);
            }
        }

        private bool IsBrick(Collider other)
        {
            return other.GetComponentInParent<BrickFireExtinguisher>() != null ||
                   other.name.Contains("Brick");
        }

        private GameObject CreateFallbackVisual(string objectName, PrimitiveType primitiveType, Color color, Vector3 localPosition, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = objectName;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.rotation = Quaternion.identity;

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                visualRenderer.material.color = color;
            }

            return visual;
        }
    }

    public class BrickFireExtinguisher : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            ExtinguishFire(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            ExtinguishFire(other);
        }

        private void ExtinguishFire(Collider hitCollider)
        {
            FireSurface fireSurface = hitCollider.GetComponentInParent<FireSurface>();
            if (fireSurface != null)
            {
                fireSurface.Extinguish(false);
            }
        }
    }
}
