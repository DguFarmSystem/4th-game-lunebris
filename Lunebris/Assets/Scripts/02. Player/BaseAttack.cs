// Unity
using UnityEngine;
// Enemy 네임스페이스 추가
using Enemy;

namespace Player
{
    [DisallowMultipleComponent]
    public class BaseAttack : MonoBehaviour
    {
        [SerializeField] private float velocity = 8f;

        [Header("스플래쉬 효과 설정")]
        [SerializeField] private GameObject splashEffect;            // 스플래쉬 이펙트 프리팹

        private Rigidbody rigid;
        private Vector3 originalScale;

        // 스플래쉬 효과 관련 변수들
        private bool hasSplashEffect = false;
        private float splashRadius = 3f;
        private float splashDamageRatio = 0.5f;
        private float baseDamage = 10f;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            originalScale = transform.localScale;
        }

        public void Shoot(Vector3 _direction)
        {
            rigid.velocity = velocity * _direction.normalized;
        }

        /// <summary>
        /// 총알 크기 조정
        /// </summary>
        /// <param name="scaleMultiplier">크기 배수 (1.5f = 1.5배 크기)</param>
        public void SetBulletScale(float scaleMultiplier)
        {
            transform.localScale = originalScale * scaleMultiplier;
        }

        /// <summary>
        /// 스플래쉬 효과 설정
        /// </summary>
        /// <param name="enabled">스플래쉬 효과 활성화 여부</param>
        /// <param name="radius">스플래쉬 범위</param>
        /// <param name="damageRatio">스플래쉬 데미지 비율</param>
        public void SetSplashEffect(bool enabled, float radius = 3f, float damageRatio = 0.5f)
        {
            hasSplashEffect = enabled;
            splashRadius = radius;
            splashDamageRatio = damageRatio;
        }

        /// <summary>
        /// 기본 데미지 설정
        /// </summary>
        public void SetBaseDamage(float damage)
        {
            baseDamage = damage;
        }

        /// <summary>
        /// 스플래쉬 효과 실행 (적이 호출)
        /// </summary>
        /// <param name="hitPoint">충돌 지점</param>
        /// <param name="hitEnemy">직접 맞은 적 (중복 데미지 방지)</param>
        public void ExecuteSplashEffect(Vector3 hitPoint, Enemy_Base hitEnemy)
        {
            if (!hasSplashEffect) return;

            // 스플래쉬 이펙트 생성
            if (splashEffect != null)
            {
                GameObject effect = Instantiate(splashEffect, hitPoint, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // 범위 내 적들 찾기
            Collider[] enemiesInRange = Physics.OverlapSphere(hitPoint, splashRadius);

            float splashDamage = baseDamage * splashDamageRatio;
            int hitCount = 0;

            foreach (Collider col in enemiesInRange)
            {
                Enemy_Base enemy = col.GetComponent<Enemy_Base>();

                // 적이고, 직접 맞은 적이 아니며, 죽지 않은 상태인지 확인
                if (enemy != null && enemy != hitEnemy && !enemy.IsDead())
                {
                    // 스플래쉬 데미지 적용 - 올바른 네임스페이스 사용
                    enemy.TakeDamage(splashDamage, DamageType.Physical, ElementType.Neutral);
                    hitCount++;

                    Debug.Log($"스플래쉬 데미지: {enemy.name}에게 {splashDamage} 데미지!");
                }
            }

            Debug.Log($"스플래쉬 효과 실행 - 범위: {splashRadius}, 데미지: {splashDamage}, 피해 적 수: {hitCount}");
        }

        /// <summary>
        /// 총알을 원래 크기로 리셋
        /// </summary>
        public void ResetScale()
        {
            transform.localScale = originalScale;
        }

        /// <summary>
        /// 풀링을 위한 상태 리셋
        /// </summary>
        public void ResetForPooling()
        {
            ResetScale();
            hasSplashEffect = false;
            splashRadius = 3f;
            splashDamageRatio = 0.5f;
            baseDamage = 10f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Wall"))
            {
                ResetScale(); // 비활성화 전에 크기 리셋
                gameObject.SetActive(false);
            }
        }

        // Getters
        public float GetBaseDamage() => baseDamage;
        public bool HasSplashEffect() => hasSplashEffect;
        public float GetSplashRadius() => splashRadius;
        public float GetSplashDamageRatio() => splashDamageRatio;
    }
}