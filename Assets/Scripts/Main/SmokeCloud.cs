using UnityEngine;

namespace Main
{
    public class SmokeCloud : MonoBehaviour
    {
        [SerializeField] private float triggerRadius = 0.7f;
        [SerializeField] private float destroyDelay = 1.5f;
        [SerializeField] private float lingeringLifeTime = 25f;

        private ParticleSystem[] particles;
        private Renderer[] renderers;
        private FireSurface attachedFireSurface;
        private bool dispersed;

        private void Awake()
        {
            particles = GetComponentsInChildren<ParticleSystem>();
            renderers = GetComponentsInChildren<Renderer>();

            Collider smokeCollider = GetComponent<Collider>();
            if (smokeCollider == null)
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = triggerRadius;
                sphereCollider.isTrigger = true;
            }
            else
            {
                smokeCollider.isTrigger = true;
            }
        }

        public void Initialize(FireSurface fireSurface, float lingerDuration)
        {
            attachedFireSurface = fireSurface;
            lingeringLifeTime = lingerDuration;
        }

        public void DetachFromFire()
        {
            attachedFireSurface = null;
            Destroy(gameObject, lingeringLifeTime);
        }

        public void Disperse()
        {
            if (dispersed)
            {
                return;
            }

            if (attachedFireSurface != null && attachedFireSurface.IsBurning)
            {
                Debug.Log("[SmokeCloud] Wind hit smoke attached to burning fire. Intensifying fire instead of dispersing smoke.", this);
                attachedFireSurface.IntensifyFire();
                return;
            }

            dispersed = true;
            Debug.Log("[SmokeCloud] Smoke dispersed.", this);

            if (particles == null || particles.Length == 0)
            {
                particles = GetComponentsInChildren<ParticleSystem>();
            }

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            foreach (Renderer smokeRenderer in renderers)
            {
                smokeRenderer.enabled = false;
            }

            Destroy(gameObject, destroyDelay);
        }
    }
}
