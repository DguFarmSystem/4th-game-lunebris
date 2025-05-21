// Unity
using UnityEngine;

namespace Camera
{
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target; // Player

        [SerializeField] private Vector3 offset;
        [SerializeField] private float xRotation;

        private void Start()
        {
            TrackTarget();
        }

        private void Update()
        {
            TrackTarget();
        }

        private void TrackTarget()
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(xRotation, transform.rotation.y, transform.rotation.z);
        }
    }
}

