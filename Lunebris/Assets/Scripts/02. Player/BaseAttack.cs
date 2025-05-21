// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class BaseAttack : MonoBehaviour
    {
        [SerializeField] private float velocity = 8f;

        private Rigidbody rigid;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();
        }

        public void Shoot(Vector3 _direction)
        {
            rigid.velocity = velocity * _direction.normalized;
        }
    }
}