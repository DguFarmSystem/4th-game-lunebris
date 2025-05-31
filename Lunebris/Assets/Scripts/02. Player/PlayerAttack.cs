// System
using System.Collections;

// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private PoolManager pool;
        [SerializeField] private Transform shooter;

        private float rotationSpeed = 10f;
        private Player player;
        private Vector3 direction;
        private int baseAttackPrefabID = 0;

        private void Start()
        {
            player = GetComponent<Player>();

            StartCoroutine(AttackCoroutine());
        }

        private void Update()
        {
            RotationPlayer();
        }

        /// <summary>
        /// Rotation player using mouse
        /// </summary>
        private void RotationPlayer()
        {
            // Define play for raycast
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            // Ray Shoot
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);

            // Only hit ray
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                direction = hitPoint - transform.position;
                direction.y = 0f;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);  // Slerp Rotation
                }
            }
        }

        /// <summary>
        /// Base atttack coroutine
        /// </summary>
        /// <returns></returns>
        private IEnumerator AttackCoroutine()
        {
            while (true)
            {
                // Attack Speed = 1 / AttackSpeed_Stat
                yield return new WaitForSeconds(1f / player.GetPlayerStat().Get(StatType.AttackSpeed));

                BaseAttack baseAttack = pool.Get(baseAttackPrefabID).GetComponent<BaseAttack>();
                baseAttack.transform.position = shooter.position;
                baseAttack.Shoot(direction);
            }
        }
    }
}

