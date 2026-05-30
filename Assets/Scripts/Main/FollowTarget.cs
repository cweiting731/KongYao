using UnityEngine;

namespace Main
{
    public class FollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float speed = 5f;
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private bool isfollow = true;
        [SerializeField] private bool followRotation = true;
        [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;
        [SerializeField] private bool zFaceTargetNegativeY = false;
        [SerializeField] private bool smoothRotation = false;
        [SerializeField] private float rotationSpeed = 720f;

        private void Start()
        {
            
        }

        private void Update()
        {
            if (isfollow && target != null)
            {
                Follow();
            }
        }

        private void Follow()
        {
            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, speed * Time.deltaTime);

            if (followRotation)
            {
                Quaternion desiredRotation;

                if (zFaceTargetNegativeY)
                {
                    Vector3 forward = -target.up;
                    Vector3 up = Vector3.up;

                    if (Mathf.Abs(Vector3.Dot(forward.normalized, up)) > 0.99f)
                    {
                        up = target.forward;
                    }

                    desiredRotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(rotationOffsetEuler);
                }
                else
                {
                    desiredRotation = target.rotation * Quaternion.Euler(rotationOffsetEuler);
                }

                if (smoothRotation)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = desiredRotation;
                }
            }
        }

    }
}