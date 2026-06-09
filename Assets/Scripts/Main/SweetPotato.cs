using System.Collections.Generic;
using UnityEngine;

namespace Main
{
    [RequireComponent(typeof(Rigidbody))]
    public class SweetPotato : MonoBehaviour
    {
        private enum CookState
        {
            Raw,
            Cooked,
            Burnt
        }

        [Header("Cooking Times")]
        [SerializeField] private float cookedSmokeSeconds = 8f;
        [SerializeField] private float burntSmokeSeconds = 12f;

        [Header("Colors")]
        [SerializeField] private bool applyRawColorOnStart;
        [SerializeField] private Color rawColor = new Color(0.58f, 0.27f, 0.11f, 1f);
        [SerializeField] private Color cookedColor = new Color(1f, 0.54f, 0.18f, 1f);
        [SerializeField] private Color burntColor = Color.black;

        private readonly HashSet<SmokeCloud> touchingSmokeClouds = new HashSet<SmokeCloud>();
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private Renderer[] renderers;
        private Rigidbody rigidBody;
        private CookState cookState = CookState.Raw;
        private float smokeTouchSeconds;

        public float SmokeTouchSeconds => smokeTouchSeconds;
        public bool IsCooked => cookState == CookState.Cooked;
        public bool IsBurnt => cookState == CookState.Burnt;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.useGravity = true;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            if (applyRawColorOnStart)
            {
                ApplyRawColor();
            }
        }

        private void Update()
        {
            if (cookState == CookState.Burnt)
            {
                return;
            }

            CleanupMissingSmokeClouds();

            if (touchingSmokeClouds.Count == 0)
            {
                return;
            }

            smokeTouchSeconds += Time.deltaTime;

            if (smokeTouchSeconds >= burntSmokeSeconds)
            {
                Burn();
                return;
            }

            if (cookState == CookState.Raw && smokeTouchSeconds >= cookedSmokeSeconds)
            {
                Cook();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            SmokeCloud smokeCloud = other.GetComponentInParent<SmokeCloud>();
            if (smokeCloud != null)
            {
                touchingSmokeClouds.Remove(smokeCloud);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleEnter(collision.collider);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleEnter(collision.collider);
        }

        private void HandleEnter(Collider other)
        {
            if (cookState == CookState.Burnt)
            {
                return;
            }

            SmokeCloud smokeCloud = other.GetComponentInParent<SmokeCloud>();
            if (smokeCloud != null)
            {
                touchingSmokeClouds.Add(smokeCloud);
                return;
            }

            if (other.GetComponentInParent<FireSurface>() != null)
            {
                Burn();
            }
        }

        private void Cook()
        {
            if (cookState != CookState.Raw)
            {
                return;
            }

            cookState = CookState.Cooked;
            ApplyColor(cookedColor);
            Debug.Log($"[SweetPotato] Cooked after {smokeTouchSeconds:0.0}s touching smoke.", this);
        }

        private void Burn()
        {
            if (cookState == CookState.Burnt)
            {
                return;
            }

            cookState = CookState.Burnt;
            touchingSmokeClouds.Clear();
            ApplyColor(burntColor);
            Debug.Log($"[SweetPotato] Burnt after {smokeTouchSeconds:0.0}s touching smoke.", this);
        }

        public void ApplyRawColor()
        {
            ApplyColor(rawColor);
        }

        private void ApplyColor(Color color)
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            foreach (Renderer sweetPotatoRenderer in renderers)
            {
                sweetPotatoRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_BaseColor", color);
                sweetPotatoRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CleanupMissingSmokeClouds()
        {
            touchingSmokeClouds.RemoveWhere(smokeCloud => smokeCloud == null || !smokeCloud.gameObject.activeInHierarchy);
        }
    }
}
