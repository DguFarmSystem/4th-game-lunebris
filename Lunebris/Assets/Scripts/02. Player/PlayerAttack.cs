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

        [Header("Upgrade Settings")]
        [SerializeField] private float baseBulletScale = 1f;                // 기본 총알 크기
        [SerializeField] private float bulletScalePerLevel = 0.5f;          // 레벨당 크기 증가량
        [SerializeField] private int baseShotCount = 1;                     // 기본 총알 개수
        [SerializeField] private float multiShotSpreadAngle = 45f;          // 멀티샷 퍼짐 각도

        private float rotationSpeed = 10f;
        private Player player;
        private Vector3 direction;
        private int baseAttackPrefabID = 0;
        private Animator animator;

        // 영구 강화 상태
        private int bulletSizeLevel = 0;        // 총알 크기 레벨
        private int multiShotLevel = 0;         // 멀티샷 레벨

        public Vector3 GetLookDirection()
        {
            return direction;
        }

        private void Start()
        {
            player = GetComponent<Player>();
            animator = GetComponent<Animator>();
            StartCoroutine(AttackCoroutine());
        }

        private void Update()
        {
            RotationPlayer();
            HandleTestInputs(); // 테스트용 입력 처리
        }

        /// <summary>
        /// 테스트용 키보드 입력 처리
        /// </summary>
        private void HandleTestInputs()
        {
            // Q키: 총알 크기 강화
            if (Input.GetKeyDown(KeyCode.Q))
            {
                UpgradeBulletSize();
                Debug.Log($"총알 크기 강화! 레벨: {bulletSizeLevel}, 현재 크기: x{GetCurrentBulletScale():F1}");

                if (bulletSizeLevel == 3)
                {
                    Debug.Log("스플래쉬 효과 활성화! 이제 범위 데미지를 줍니다!");
                }
                else if (bulletSizeLevel > 3)
                {
                    Debug.Log($"스플래쉬 효과 강화! 범위와 데미지가 증가했습니다!");
                }
            }

            // E키: 멀티샷 강화
            if (Input.GetKeyDown(KeyCode.E))
            {
                UpgradeMultiShot();
                Debug.Log($"멀티샷 강화! 레벨: {multiShotLevel}, 현재 총알 개수: {GetCurrentShotCount()}발");
            }
        }

        /// <summary>
        /// 총알 크기 강화 (외부에서 호출)
        /// </summary>
        public void UpgradeBulletSize()
        {
            bulletSizeLevel++;
        }

        /// <summary>
        /// 멀티샷 강화 (외부에서 호출)
        /// </summary>
        public void UpgradeMultiShot()
        {
            multiShotLevel++;
        }

        /// <summary>
        /// 현재 총알 크기 계산
        /// </summary>
        public float GetCurrentBulletScale()
        {
            return baseBulletScale + (bulletSizeLevel * bulletScalePerLevel);
        }

        /// <summary>
        /// 현재 총알 개수 계산
        /// </summary>
        public int GetCurrentShotCount()
        {
            return baseShotCount + multiShotLevel;
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
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Base attack coroutine
        /// </summary>
        private IEnumerator AttackCoroutine()
        {
            while (true)
            {
                // Attack Speed = 1 / AttackSpeed_Stat
                yield return new WaitForSeconds(1f / player.GetPlayerStat().Get(StatType.AttackSpeed));
                animator.SetTrigger("Attack");
                yield return new WaitForSeconds(0.3f);

                FireShots();
            }
        }

        /// <summary>
        /// 현재 강화 상태에 따라 총알 발사
        /// </summary>
        private void FireShots()
        {
            int shotCount = GetCurrentShotCount();
            float currentScale = GetCurrentBulletScale();

            if (shotCount == 1)
            {
                // 단일 발사
                FireSingleShot(direction, currentScale);
            }
            else
            {
                // 멀티샷 발사
                FireMultiShot(shotCount, currentScale);
            }
        }

        /// <summary>
        /// 단일 총알 발사
        /// </summary>
        private void FireSingleShot(Vector3 shootDirection, float scale)
        {
            BaseAttack baseAttack = pool.Get(baseAttackPrefabID).GetComponent<BaseAttack>();
            baseAttack.transform.position = shooter.position;
            baseAttack.SetBulletScale(scale);

            // 플레이어 스탯에서 실제 데미지 계산
            float actualDamage = player.GetPlayerStat().Get(StatType.AttackDamage);
            baseAttack.SetBaseDamage(actualDamage);

            // 스플래쉬 효과 조건 확인
            CheckAndApplySplashEffect(baseAttack, scale);

            baseAttack.Shoot(shootDirection);
        }

        /// <summary>
        /// 멀티샷 발사
        /// </summary>
        private void FireMultiShot(int shotCount, float scale)
        {
            float angleStep = multiShotSpreadAngle / (shotCount - 1);
            float startAngle = -multiShotSpreadAngle / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                BaseAttack baseAttack = pool.Get(baseAttackPrefabID).GetComponent<BaseAttack>();
                baseAttack.transform.position = shooter.position;
                baseAttack.SetBulletScale(scale);

                // 플레이어 스탯에서 실제 데미지 계산
                float actualDamage = player.GetPlayerStat().Get(StatType.AttackDamage);
                baseAttack.SetBaseDamage(actualDamage);

                // 스플래쉬 효과 조건 확인
                CheckAndApplySplashEffect(baseAttack, scale);

                // 각각 다른 방향으로 발사
                float currentAngle = startAngle + (angleStep * i);
                Vector3 shootDirection = Quaternion.Euler(0, currentAngle, 0) * direction;
                baseAttack.Shoot(shootDirection);
            }
        }

        /// <summary>
        /// 스플래쉬 효과 적용 조건 확인 및 적용
        /// </summary>
        private void CheckAndApplySplashEffect(BaseAttack baseAttack, float scale)
        {
            // 조건: 총알 크기 레벨이 3 이상일 때만 스플래쉬 효과 활성화
            if (bulletSizeLevel >= 3)
            {
                // 스플래쉬 범위는 총알 크기에 비례 (기본 2.5f + 레벨당 0.5f)
                float splashRange = 2.5f + ((bulletSizeLevel - 3) * 0.5f);

                // 스플래쉬 데미지 비율 (기본 50% + 레벨당 5%, 최대 80%)
                float damageRatio = 0.5f + ((bulletSizeLevel - 3) * 0.05f);
                damageRatio = Mathf.Min(damageRatio, 0.8f);

                baseAttack.SetSplashEffect(true, splashRange, damageRatio);

                Debug.Log($"스플래쉬 효과 적용! 레벨: {bulletSizeLevel}, 범위: {splashRange:F1}, 데미지 비율: {damageRatio:P0}");
            }
        }
    }
}