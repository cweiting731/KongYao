using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Main
{
    [System.Serializable]
    public class MagicState
    {
        public string name;
        public MagicType magicType;
        public Transform magicPlane;
        public GameObject magicPrefab;
        public float projectileSpeed = 8f;

        public MagicState(string name, Transform magicPlane)
        {
            this.name = name;
            this.magicPlane = magicPlane;
        }

        public void setActive(bool active)
        {
            if (magicPlane != null)
            {
                magicPlane.gameObject.SetActive(active);
            }
        }
    }

    public enum MagicType
    {
        Fire,
        Water,
        Wind,
        Earth
    }

    public class MagicStateManager : MonoBehaviour
    {
        [SerializeField] private List<MagicState> magicStates = new List<MagicState>();
        [SerializeField] private TMP_Text magicNameText;
        [SerializeField] private Transform castPoint;
        [SerializeField] private float defaultProjectileSpeed = 8f;
        [SerializeField] private float spawnForwardOffset = 0.35f;
        [SerializeField] private float castCooldown = 0.25f;
        [SerializeField] private GameObject defaultFirePrefab;
        [SerializeField] private GameObject defaultWaterPrefab;
        [SerializeField] private GameObject defaultWindPrefab;
        [SerializeField] private GameObject defaultEarthPrefab;
        [SerializeField] private ParticleSystem fireBurningEffectPrefab;
        [SerializeField] private ParticleSystem mediumFireBurningEffectPrefab;
        [SerializeField] private ParticleSystem largeFireBurningEffectPrefab;
        [SerializeField] private ParticleSystem smokeEffectPrefab;
        [SerializeField] private GameObject brickPrefab;

        [SerializeField] private int currentIndex = 0;
        [SerializeField] private bool showMagicPlane = false;
        private float nextCastTime;

        private void Start()
        {
            UpdateMagicNameText();
        }

        public void CastCurrentMagic()
        {
            if (Time.time < nextCastTime || magicStates.Count == 0 || currentIndex < 0 || currentIndex >= magicStates.Count)
            {
                return;
            }

            MagicState currentState = magicStates[currentIndex];
            Transform spawnPoint = castPoint != null ? castPoint : transform;
            Vector3 spawnPosition = spawnPoint.position + spawnPoint.forward * spawnForwardOffset;
            GameObject prefab = GetMagicPrefab(currentState);
            GameObject magicInstance = prefab != null
                ? CreatePrefabProjectile(currentState.magicType, prefab, spawnPosition, spawnPoint.rotation)
                : CreateFallbackMagicObject(currentState.magicType, spawnPosition, spawnPoint.rotation);

            MagicProjectile projectile = magicInstance.GetComponent<MagicProjectile>();
            if (projectile == null)
            {
                projectile = magicInstance.AddComponent<MagicProjectile>();
            }

            float speed = currentState.projectileSpeed > 0f ? currentState.projectileSpeed : defaultProjectileSpeed;
            projectile.Launch(
                currentState.magicType,
                spawnPoint.forward,
                speed,
                fireBurningEffectPrefab,
                mediumFireBurningEffectPrefab,
                largeFireBurningEffectPrefab,
                smokeEffectPrefab,
                brickPrefab);
            nextCastTime = Time.time + castCooldown;
        }

        public void SetShowMagicPlaneTrue()
        {
            showMagicPlane = true;

            if (magicStates.Count > 0 && currentIndex >= 0 && currentIndex < magicStates.Count)
            {
                magicStates[currentIndex].setActive(true);
                CastCurrentMagic();
            }
        }

        public void SetShowMagicPlaneFalse()
        {
            showMagicPlane = false;

            if (magicStates.Count > 0 && currentIndex >= 0 && currentIndex < magicStates.Count)
            {
                magicStates[currentIndex].setActive(false);
            }
        }

        public void NextState()
        {
            if (magicStates.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex + 1) % magicStates.Count;
            UpdateMagicNameText();

            if (showMagicPlane)
            {
                SetActiveState(currentIndex);
            }
        }

        public void PreviousState()
        {
            if (magicStates.Count == 0)
            {
                return;
            }

            currentIndex = (currentIndex - 1 + magicStates.Count) % magicStates.Count;
            UpdateMagicNameText();

            if (showMagicPlane)
            {
                SetActiveState(currentIndex);
            }
        }

        private void SetActiveState(int index)
        {
            for (int i = 0; i < magicStates.Count; i++)
            {
                magicStates[i].setActive(i == index);
            }
        }

        private void UpdateMagicNameText()
        {
            if (magicNameText != null && currentIndex >= 0 && currentIndex < magicStates.Count)
            {
                magicNameText.text = magicStates[currentIndex].name;
            }
        }

        private GameObject CreatePrefabProjectile(MagicType magicType, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject projectileRoot = new GameObject($"{magicType} Projectile");
            projectileRoot.transform.SetPositionAndRotation(position, rotation);

            GameObject visual = Instantiate(prefab, projectileRoot.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            projectileRoot.AddComponent<MagicProjectile>();
            return projectileRoot;
        }

        private GameObject CreateFallbackMagicObject(MagicType magicType, Vector3 position, Quaternion rotation)
        {
            GameObject magicObject;

            if (magicType == MagicType.Wind)
            {
                magicObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                magicObject.name = "Wind Magic";
                magicObject.transform.SetPositionAndRotation(position, rotation);
                magicObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.7f);
            }
            else
            {
                magicObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                magicObject.name = $"{magicType} Magic";
                magicObject.transform.SetPositionAndRotation(position, rotation);
                magicObject.transform.localScale = Vector3.one * 0.25f;

                Renderer renderer = magicObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetFallbackMagicColor(magicType);
                }
            }

            Renderer fallbackRenderer = magicObject.GetComponent<Renderer>();
            if (fallbackRenderer != null)
            {
                fallbackRenderer.material.color = GetFallbackMagicColor(magicType);
            }

            return magicObject;
        }

        private Color GetFallbackMagicColor(MagicType magicType)
        {
            switch (magicType)
            {
                case MagicType.Fire:
                    return new Color(1f, 0.28f, 0.05f, 1f);
                case MagicType.Water:
                    return new Color(0.1f, 0.55f, 1f, 0.85f);
                case MagicType.Earth:
                    return new Color(0.66f, 0.47f, 0.25f, 1f);
                case MagicType.Wind:
                    return new Color(0.8f, 1f, 1f, 1f);
                default:
                    return Color.white;
            }
        }

        private GameObject GetMagicPrefab(MagicState magicState)
        {
            if (magicState.magicPrefab != null)
            {
                return magicState.magicPrefab;
            }

            switch (magicState.magicType)
            {
                case MagicType.Fire:
                    return defaultFirePrefab;
                case MagicType.Water:
                    return defaultWaterPrefab;
                case MagicType.Wind:
                    return defaultWindPrefab;
                case MagicType.Earth:
                    return defaultEarthPrefab;
                default:
                    return null;
            }
        }

        public int CurrentIndex => currentIndex;
    }
}
