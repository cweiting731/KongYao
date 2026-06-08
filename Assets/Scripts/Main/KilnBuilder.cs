using UnityEngine;

namespace Main
{
    public class KilnBuilder : MonoBehaviour
    {
        [Header("Prefabs and options")]
        public GameObject brickPrefab;
        public GameObject woodPrefab;
        public GameObject firePrefab;

        [Header("Kiln shape")]
        public int wallLayers = 6;
        public float innerRadius = 0.9f;
        public float openingAngle = 60f; // degrees
        public bool capTop = true;

        [Header("Brick size (L:W:H = 2:1:1)")]
        public float brickLength = 0.20f; // x
        public float brickWidth = 0.10f;  // z
        public float brickHeight = 0.10f; // y

        [Header("Placement")]
        public float spawnDistanceInFront = 1f;
        public LayerMask floorMask = ~0;

        // Public method you can bind to a UI Button
        public void GenerateKilnInFront()
        {
            Vector3 origin = transform.position + transform.forward * spawnDistanceInFront + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, floorMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 center = hit.point;
                BuildKilnAt(center);
            }
            else
            {
                Vector3 center = transform.position + transform.forward * spawnDistanceInFront;
                center.y = transform.position.y;
                BuildKilnAt(center);
            }
        }

        public void BuildKilnAt(Vector3 center)
        {
            if (brickPrefab == null)
            {
                Debug.LogWarning("KilnBuilder: brickPrefab is not assigned.");
                return;
            }

            GameObject kilnRoot = new GameObject("Kiln");
            kilnRoot.transform.position = center;

            // Entrance faces the player position
            Vector3 entranceDir = (transform.position - center);
            entranceDir.y = 0f;
            if (entranceDir.sqrMagnitude < 0.0001f) entranceDir = -transform.forward;
            Vector2 entranceDir2 = new Vector2(entranceDir.x, entranceDir.z).normalized;

            for (int layer = 0; layer < wallLayers; layer++)
            {
                float y = center.y + layer * (brickHeight * 0.95f);
                float layerRadius = innerRadius + layer * (brickWidth * 0.25f);
                int numBricks = Mathf.Max(4, Mathf.RoundToInt((2f * Mathf.PI * layerRadius) / brickLength));

                for (int i = 0; i < numBricks; i++)
                {
                    float angle = (2f * Mathf.PI / numBricks) * i;
                    float deg = angle * Mathf.Rad2Deg;
                    Vector2 brickDir2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // Skip bricks to make entrance on lower layers
                    float angleBetween = Vector2.Angle(brickDir2, entranceDir2);
                    if (layer < 2 && angleBetween < openingAngle * 0.5f)
                    {
                        continue;
                    }

                    Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * layerRadius + new Vector3(center.x, y, center.z);
                    Quaternion rot = Quaternion.Euler(0f, -deg + 90f, 0f);

                    GameObject brick = Instantiate(brickPrefab, pos, rot, kilnRoot.transform);
                    brick.transform.localScale = new Vector3(brickLength, brickHeight, brickWidth);
                }
            }

            // Cap top with concentric rings
            if (capTop)
            {
                float topY = center.y + wallLayers * (brickHeight * 0.95f);
                int capRings = Mathf.Max(1, Mathf.CeilToInt(innerRadius / (brickLength * 0.6f)));
                for (int ring = 0; ring < capRings; ring++)
                {
                    float r = innerRadius - ring * (brickLength * 0.6f);
                    if (r <= 0f) break;
                    int num = Mathf.Max(4, Mathf.RoundToInt((2f * Mathf.PI * r) / brickLength));
                    for (int i = 0; i < num; i++)
                    {
                        float angle = (2f * Mathf.PI / num) * i;
                        float deg = angle * Mathf.Rad2Deg;
                        Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r + new Vector3(center.x, topY + ring * (brickHeight * 0.5f), center.z);
                        Quaternion rot = Quaternion.Euler(0f, -deg + 90f, 0f);
                        GameObject brick = Instantiate(brickPrefab, pos, rot, kilnRoot.transform);
                        brick.transform.localScale = new Vector3(brickLength, brickHeight * 0.9f, brickWidth);
                    }
                }
            }

            // Create a small fire pit placeholder inside (hole for wood)
            GameObject firePit = new GameObject("FirePit");
            firePit.transform.SetParent(kilnRoot.transform, false);
            firePit.transform.position = center + Vector3.up * 0.05f;

            if (woodPrefab != null)
            {
                GameObject wood = Instantiate(woodPrefab, firePit.transform.position, Quaternion.identity, firePit.transform);
            }

            if (firePrefab != null)
            {
                GameObject fire = Instantiate(firePrefab, firePit.transform.position + Vector3.up * 0.05f, Quaternion.identity, firePit.transform);
            }

            Debug.Log("Kiln generated at " + center);
        }
    }
}
