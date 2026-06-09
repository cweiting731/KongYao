using UnityEngine;

namespace Main
{
    [RequireComponent(typeof(Rigidbody))]
    public class MagicProjectile : MonoBehaviour
    {
        [SerializeField] private MagicType magicType = MagicType.Fire;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float hitRadius = 0.12f;
        [SerializeField] private float impactRadius = 1.5f;
        [SerializeField] private float windSmokeDisperseRadius = 2.5f;
        [SerializeField] private float earthBrickSpawnHeight = 3f;
        [SerializeField] private float collisionDelay = 0.08f;
        [SerializeField] private LayerMask impactLayers = ~0;

        private Rigidbody rigidBody;
        private Vector3 moveDirection = Vector3.forward;
        private float moveSpeed;
        private float canHitTime;
        private ParticleSystem fireBurningEffectPrefab;
        private ParticleSystem mediumFireBurningEffectPrefab;
        private ParticleSystem largeFireBurningEffectPrefab;
        private ParticleSystem smokeEffectPrefab;
        private GameObject brickPrefab;
        private bool hasImpacted;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.useGravity = false;
            rigidBody.isKinematic = true;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Collider projectileCollider = GetComponent<Collider>();
            if (projectileCollider == null)
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = hitRadius;
                projectileCollider = sphereCollider;
            }

            projectileCollider.isTrigger = true;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void FixedUpdate()
        {
            if (hasImpacted || moveSpeed <= 0f)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            float distance = moveSpeed * Time.fixedDeltaTime;
            Vector3 nextPosition = currentPosition + moveDirection * distance;

            if (magicType == MagicType.Wind)
            {
                DisperseSmoke(nextPosition);
            }

            if (Time.time >= canHitTime && Physics.SphereCast(
                    currentPosition,
                    hitRadius,
                    moveDirection,
                    out RaycastHit hit,
                    distance,
                    impactLayers,
                    QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                HandleImpact(hit.point, hit.normal, hit.transform);
                return;
            }

            rigidBody.MovePosition(nextPosition);
        }

        public void Launch(
            MagicType type,
            Vector3 direction,
            float speed,
            ParticleSystem fireEffectPrefab,
            ParticleSystem mediumFireEffectPrefab,
            ParticleSystem largeFireEffectPrefab,
            ParticleSystem smokePrefab,
            GameObject brickSpawnPrefab)
        {
            magicType = type;
            moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;
            moveSpeed = speed;
            canHitTime = Time.time + collisionDelay;
            fireBurningEffectPrefab = fireEffectPrefab;
            mediumFireBurningEffectPrefab = mediumFireEffectPrefab;
            largeFireBurningEffectPrefab = largeFireEffectPrefab;
            smokeEffectPrefab = smokePrefab;
            brickPrefab = brickSpawnPrefab;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasImpacted || Time.time < canHitTime || !IsInImpactLayer(other.gameObject))
            {
                return;
            }

            FireSurface fireSurface = other.GetComponentInParent<FireSurface>();
            SmokeCloud smokeCloud = other.GetComponentInParent<SmokeCloud>();

            if (magicType == MagicType.Water && (fireSurface != null || smokeCloud != null))
            {
                hasImpacted = true;
                if (fireSurface != null)
                {
                    fireSurface.Extinguish();
                }

                if (smokeCloud != null && fireSurface == null)
                {
                    smokeCloud.RemoveImmediately();
                }

                Destroy(gameObject);
                return;
            }

            if (magicType == MagicType.Wind)
            {
                // Ensure wind only affects once
                hasImpacted = true;
                if (smokeCloud != null)
                {
                    Debug.Log($"[MagicProjectile] Wind projectile touched smoke: {smokeCloud.name}", this);
                    var attached = smokeCloud.GetAttachedFireSurface();
                    if (attached != null && attached.IsBurning)
                    {
                        // Intensify attached fire instead of dispersing smoke
                        attached.IntensifyFire();
                    }
                    else
                    {
                        smokeCloud.Disperse();
                    }
                    Destroy(gameObject);
                    return;
                }

                if (fireSurface != null)
                {
                    Debug.Log($"[MagicProjectile] Wind projectile touched fire directly: {fireSurface.name}", this);
                    fireSurface.IntensifyFire();
                    Destroy(gameObject);
                    return;
                }

                // Wind should be cleared after touching any impact-layer object.
                Destroy(gameObject);
                return;
            }
        }

        private void HandleImpact(Vector3 point, Vector3 normal, Transform hitTransform)
        {
            hasImpacted = true;

            switch (magicType)
            {
                case MagicType.Fire:
                    AttachFire(point, normal, hitTransform);
                    break;
                case MagicType.Water:
                    ExtinguishFire(point);
                    break;
                case MagicType.Wind:
                    DisperseSmoke(point);
                    break;
                case MagicType.Earth:
                    SpawnBrick(point);
                    break;
            }

            Destroy(gameObject);
        }

        private void AttachFire(Vector3 point, Vector3 normal, Transform hitTransform)
        {
            GameObject fireObject = new GameObject("Attached Fire");
            fireObject.transform.SetPositionAndRotation(
                point + normal * 0.02f,
                Quaternion.FromToRotation(Vector3.up, normal));
            fireObject.transform.SetParent(hitTransform, true);

            FireSurface fireSurface = fireObject.AddComponent<FireSurface>();
            fireSurface.Initialize(fireBurningEffectPrefab, mediumFireBurningEffectPrefab, largeFireBurningEffectPrefab, smokeEffectPrefab);
        }

        private void ExtinguishFire(Vector3 point)
        {
            Collider[] colliders = Physics.OverlapSphere(point, impactRadius, impactLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hitCollider in colliders)
            {
                FireSurface fireSurface = hitCollider.GetComponentInParent<FireSurface>();
                if (fireSurface != null)
                {
                    fireSurface.Extinguish();
                    continue;
                }

                SmokeCloud smokeCloud = hitCollider.GetComponentInParent<SmokeCloud>();
                if (smokeCloud != null)
                {
                    smokeCloud.RemoveImmediately();
                }
            }
        }

        private void SpawnBrick(Vector3 point)
        {
            Vector3 spawnPosition = point + Vector3.up * earthBrickSpawnHeight;
            GameObject brickObject;

            if (brickPrefab != null)
            {
                brickObject = Instantiate(brickPrefab, spawnPosition, Random.rotation);
                // Brick proportions L:W:H ~ 2:1:1, smaller overall
                float baseSize = Random.Range(0.09f, 0.12f); // base unit
                float length = baseSize * 2f; // 長度 (x)
                float width = baseSize * 1f;  // 寬度 (z)
                float height = baseSize * 1f; // 高度 (y)
                brickObject.transform.localScale = new Vector3(length, height, width);
            }
            else
            {
                brickObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brickObject.name = "Brick";
                brickObject.transform.SetPositionAndRotation(spawnPosition, Random.rotation);
                // Default fallback dimensions with L:W:H ~= 2:1:1 (meters)
                brickObject.transform.localScale = new Vector3(0.20f, 0.10f, 0.10f);
            }

            if (brickObject.GetComponentInChildren<Collider>() == null)
            {
                brickObject.AddComponent<BoxCollider>();
            }

            if (brickObject.GetComponent<Rigidbody>() == null)
            {
                brickObject.AddComponent<Rigidbody>();
            }

            if (brickObject.GetComponent<BrickFireExtinguisher>() == null)
            {
                brickObject.AddComponent<BrickFireExtinguisher>();
            }
        }

        private void DisperseSmoke(Vector3 point)
        {
            Collider[] colliders = Physics.OverlapSphere(point, windSmokeDisperseRadius, impactLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hitCollider in colliders)
            {
                SmokeCloud smokeCloud = hitCollider.GetComponentInParent<SmokeCloud>();
                if (smokeCloud != null)
                {
                    Debug.Log($"[MagicProjectile] Wind overlap found smoke: {smokeCloud.name}", this);
                    // Mark impacted so this projectile won't apply additional effects
                    hasImpacted = true;
                    var attached = smokeCloud.GetAttachedFireSurface();
                    if (attached != null && attached.IsBurning)
                    {
                        attached.IntensifyFire();
                    }
                    else
                    {
                        smokeCloud.Disperse();
                    }
                    Destroy(gameObject);
                    return;
                }
            }

            foreach (Collider hitCollider in colliders)
            {
                FireSurface fireSurface = hitCollider.GetComponentInParent<FireSurface>();
                if (fireSurface != null && fireSurface.IsBurning)
                {
                    Debug.Log($"[MagicProjectile] Wind overlap found burning fire directly: {fireSurface.name}", this);
                    // Mark impacted so this projectile won't apply additional effects
                    hasImpacted = true;
                    fireSurface.IntensifyFire();
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private bool IsInImpactLayer(GameObject hitObject)
        {
            return (impactLayers.value & (1 << hitObject.layer)) != 0;
        }
    }
}
