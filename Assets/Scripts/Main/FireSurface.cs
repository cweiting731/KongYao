using UnityEngine;

namespace Main
{
    public class FireSurface : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 12f;
        [SerializeField] private float triggerRadius = 0.45f;

        private ParticleSystem fireEffect;
        private SmokeCloud smokeCloud;
        private GameObject fallbackFireVisual;
        private bool extinguished;

        public void Initialize(float duration, ParticleSystem fireEffectPrefab, ParticleSystem smokeEffectPrefab)
        {
            lifeTime = duration;

            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = triggerRadius;

            if (fireEffectPrefab != null)
            {
                fireEffect = Instantiate(fireEffectPrefab, transform);
                fireEffect.transform.localPosition = Vector3.zero;
                fireEffect.transform.localRotation = Quaternion.identity;
                fireEffect.Play();
            }
            else
            {
                fallbackFireVisual = CreateFallbackVisual("Fire Visual", PrimitiveType.Sphere, new Color(1f, 0.24f, 0.02f, 1f), Vector3.zero, Vector3.one * 0.35f);
            }

            // if (smokeEffectPrefab != null)
            // {
            //     ParticleSystem smokeEffect = Instantiate(smokeEffectPrefab, transform);
            //     smokeEffect.transform.localPosition = Vector3.up * 0.1f;
            //     smokeEffect.transform.localRotation = Quaternion.identity;
            //     smokeEffect.Play();

            //     smokeCloud = smokeEffect.gameObject.GetComponent<SmokeCloud>();
            //     if (smokeCloud == null)
            //     {
            //         smokeCloud = smokeEffect.gameObject.AddComponent<SmokeCloud>();
            //     }
            // }
            // else
            // {
            //     GameObject smokeVisual = CreateFallbackVisual("Smoke Visual", PrimitiveType.Sphere, new Color(0.25f, 0.25f, 0.25f, 1f), Vector3.up * 0.25f, Vector3.one * 0.55f);
            //     smokeCloud = smokeVisual.AddComponent<SmokeCloud>();
            // }

            Destroy(gameObject, lifeTime);
        }

        public void Extinguish()
        {
            if (extinguished)
            {
                return;
            }

            extinguished = true;

            if (fireEffect != null)
            {
                fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (fallbackFireVisual != null)
            {
                fallbackFireVisual.SetActive(false);
            }

            // if (smokeCloud != null)
            // {
            //     smokeCloud.Disperse();
            // }

            Destroy(gameObject, 1.5f);
        }

        private GameObject CreateFallbackVisual(string objectName, PrimitiveType primitiveType, Color color, Vector3 localPosition, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = objectName;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;

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
}
