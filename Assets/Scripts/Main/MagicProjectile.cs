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

            if (magicType == MagicType.Water && fireSurface != null)
            {
                fireSurface.Extinguish();
                Destroy(gameObject);
                return;
            }

            if (magicType == MagicType.Wind && smokeCloud != null)
            {
                Debug.Log($"[MagicProjectile] Wind projectile touched smoke: {smokeCloud.name}", this);
                smokeCloud.Disperse();
                Destroy(gameObject);
                return;
            }

            if (magicType == MagicType.Wind && fireSurface != null)
            {
                Debug.Log($"[MagicProjectile] Wind projectile touched fire directly: {fireSurface.name}", this);
                fireSurface.IntensifyFire();
                Destroy(gameObject);
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
                float baseScale = Random.Range(0.3f, 0.5f);
                float randomX = baseScale * Random.Range(1.8f, 2.2f); // 變長 (長度)
                float randomY = baseScale * Random.Range(0.8f, 1.2f); // 變矮 (高度)
                float randomZ = baseScale * Random.Range(1.0f, 1.4f); // 變寬 (寬度)
                brickObject.transform.localScale = new Vector3(randomX, randomY, randomZ);
            }
            else
            {
                brickObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brickObject.name = "Brick";
                brickObject.transform.SetPositionAndRotation(spawnPosition, Random.rotation);
                brickObject.transform.localScale = new Vector3(0.9f, 0.35f, 0.55f);
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
                    smokeCloud.Disperse();
                    return;
                }
            }

            foreach (Collider hitCollider in colliders)
            {
                FireSurface fireSurface = hitCollider.GetComponentInParent<FireSurface>();
                if (fireSurface != null && fireSurface.IsBurning)
                {
                    Debug.Log($"[MagicProjectile] Wind overlap found burning fire directly: {fireSurface.name}", this);
                    fireSurface.IntensifyFire();
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
