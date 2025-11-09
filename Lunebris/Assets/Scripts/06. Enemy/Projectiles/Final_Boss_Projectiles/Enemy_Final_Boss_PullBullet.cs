using UnityEngine;
using Player; // 플레이어 Rigidbody를 직접 참조하기 위해 추가 (필요시)

namespace Enemy
{
    public class Enemy_Final_Boss_PullBullet : MonoBehaviour
    {
        [Header("탄환 설정")]
        private Vector3 moveDirection;
        private float speed;
        private float lifeTime;
        private Vector3 targetPosition;

        // ✨ 수정: 인스펙터에서 당기는 힘을 조절할 수 있도록 설정
        [SerializeField] private float pullForce = 15f;

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
            // Update에서 움직이지 않고 Rigidbody를 사용할 경우 FixedUpdate로 이동하는 것을 고려할 수 있습니다.
            // 현재는 Transform 이동을 사용합니다.
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어 태그와 충돌했는지 확인
            if (other.CompareTag("Player"))
            {
                // 플레이어 Rigidbody를 가져옵니다.
                // Player.Player 스크립트가 아닌 Rigidbody에 직접 힘을 가합니다.
                Rigidbody playerRb = other.GetComponent<Rigidbody>();

                if (playerRb != null)
                {
                    // 보스 쪽으로 당기는 방향을 계산합니다.
                    Vector3 pullDir = (targetPosition - other.transform.position).normalized;

                    // ✨ 수정: pullForce 변수를 사용하여 힘을 가합니다.
                    // Impulse 모드를 사용하여 순간적인 힘을 적용합니다.
                    playerRb.AddForce(pullDir * pullForce, ForceMode.Impulse);

                    Debug.Log($"플레이어에게 {pullForce}의 힘으로 당김 적용!");
                }
                else
                {
                    Debug.LogWarning("플레이어 오브젝트에 Rigidbody가 없습니다. 당기기 효과를 적용할 수 없습니다.");
                }

                Destroy(gameObject); // 충돌 후 소멸
            }
        }

        // ⚠️ 에디터 전용: 컴포넌트 설정을 확인하여 흔한 오류를 방지합니다.
        private void OnValidate()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"{gameObject.name}: 끌어당기기 탄환에 Collider가 없습니다! 작동하지 않을 수 있습니다.");
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"{gameObject.name}: 끌어당기기 탄환의 Collider에 Is Trigger가 체크되어 있지 않습니다! OnTriggerEnter가 작동하지 않습니다.");
            }
        }
    }
}