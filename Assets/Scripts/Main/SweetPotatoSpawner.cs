using UnityEngine;

namespace Main
{
    public class SweetPotatoSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject sweetPotatoPrefab;
        [SerializeField] private Transform centerEyeAnchor;

        [Header("Spawn")]
        [SerializeField] private float spawnDistance = 1f;
        [SerializeField] private float spawnHeight = 1f;
        [SerializeField] private Vector3 fallbackSweetPotatoScale = new Vector3(0.45f, 0.22f, 0.22f);

        public void SpawnSweetPotatoInFrontOfUser()
        {
            Transform viewTransform = GetViewTransform();
            if (viewTransform == null)
            {
                Debug.LogError("[SweetPotatoSpawner] No CenterEyeAnchor or Camera.main found.");
                return;
            }

            Vector3 spawnPosition = viewTransform.position
                                    + viewTransform.forward * spawnDistance
                                    + Vector3.up * spawnHeight;
            Quaternion spawnRotation = Quaternion.LookRotation(viewTransform.forward, Vector3.up);

            GameObject sweetPotatoObject = sweetPotatoPrefab != null
                ? Instantiate(sweetPotatoPrefab, spawnPosition, spawnRotation)
                : CreateFallbackSweetPotato(spawnPosition, spawnRotation);

            EnsureSweetPotatoComponents(sweetPotatoObject);
            ApplySweetPotatoColor(sweetPotatoObject);
            Debug.Log($"[SweetPotatoSpawner] Spawned sweet potato at {spawnPosition}.", sweetPotatoObject);
        }

        private Transform GetViewTransform()
        {
            if (centerEyeAnchor != null)
            {
                return centerEyeAnchor;
            }

            return Camera.main != null ? Camera.main.transform : null;
        }

        private GameObject CreateFallbackSweetPotato(Vector3 position, Quaternion rotation)
        {
            GameObject sweetPotatoObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sweetPotatoObject.name = "Sweet Potato";
            sweetPotatoObject.transform.SetPositionAndRotation(position, rotation);
            sweetPotatoObject.transform.localScale = fallbackSweetPotatoScale;

            Renderer sweetPotatoRenderer = sweetPotatoObject.GetComponent<Renderer>();
            if (sweetPotatoRenderer != null)
            {
                sweetPotatoRenderer.material.color = new Color(0.58f, 0.27f, 0.11f, 1f);
            }

            return sweetPotatoObject;
        }

        private void EnsureSweetPotatoComponents(GameObject sweetPotatoObject)
        {
            if (sweetPotatoObject.GetComponentInChildren<Collider>() == null)
            {
                SphereCollider collider = sweetPotatoObject.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
            }

            if (sweetPotatoObject.GetComponent<Rigidbody>() == null)
            {
                sweetPotatoObject.AddComponent<Rigidbody>();
            }

            if (sweetPotatoObject.GetComponent<SweetPotato>() == null)
            {
                sweetPotatoObject.AddComponent<SweetPotato>();
            }
        }

        private void ApplySweetPotatoColor(GameObject sweetPotatoObject)
        {
            SweetPotato sweetPotato = sweetPotatoObject.GetComponent<SweetPotato>();
            if (sweetPotato != null)
            {
                sweetPotato.ApplyRawColor();
                return;
            }

            Renderer sweetPotatoRenderer = sweetPotatoObject.GetComponentInChildren<Renderer>();
            if (sweetPotatoRenderer != null)
            {
                sweetPotatoRenderer.material.color = new Color(0.58f, 0.27f, 0.11f, 1f);
            }
        }
    }
}
