using UnityEngine;

namespace Enemy
{
    public class Enemy_Final_Boss_PullBullet : MonoBehaviour
    {
        private Vector3 moveDirection;
        private float speed;
        private float lifeTime;
        private Vector3 targetPosition;

        // 초기화 메서드: 방향, 속도, 생존 시간, 당기는 위치(보스 쪽)
        public void Initialize(Vector3 direction, float speed, float lifeTime, Vector3 targetPosition)
        {
            this.moveDirection = direction.normalized;
            this.speed = speed;
            this.lifeTime = lifeTime;
            this.targetPosition = targetPosition;

            Destroy(gameObject, lifeTime);  // 일정 시간 후 자동 삭제
        }

        private void Update()
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody playerRb = other.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    // 보스 쪽으로 당기기
                    Vector3 pullDir = (targetPosition - other.transform.position).normalized;
                    playerRb.AddForce(pullDir * 10f, ForceMode.Impulse);
                }

                Destroy(gameObject); // 충돌 후 소멸
            }
        }
    }
}
