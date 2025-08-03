using UnityEngine;
using Enemy;

/// <summary>
/// 기존 원거리 코드를 Enemy_Base에 통합 (AD형)
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Ranged : Enemy_Base
{
    [Header("원거리 공격 설정")]
    public GameObject bulletPrefab;      // 총알 프리팹
    public Transform firePoint;          // 총알 발사 위치
    public float attackRange = 8f;       // 공격 사거리
    public float attackCooldown = 2f;    // 공격 쿨다운
    public float attackDuration = 0.5f;  // 공격 지속 시간 (멈춰있는 시간)

    private float lastAttackTime;
    private bool isAttacking = false;    // 공격 중인지 여부

    // Move 스크립트에서 참조할 수 있는 프로퍼티 (기존과 동일)
    public bool IsAttacking => isAttacking;

    #region Enemy_Base 오버라이드

    protected override void InitializeEnemy()
    {
        // 새로운 시스템 설정
        enemyType = EnemyType.RangedAD;
        primaryDamageType = DamageType.Physical; // AD 딜러이므로 물리 데미지

        // 기존 초기화 로직
        if (firePoint == null)
        {
            Transform childFirePoint = transform.Find("FirePoint");
            if (childFirePoint != null)
            {
                firePoint = childFirePoint;
            }
        }

        // 새로운 스탯 시스템의 값으로 기존 설정 업데이트
        attackRange = enemyStats.Get(EnemyStatType.AttackRange);
        attackCooldown = 1f / enemyStats.Get(EnemyStatType.AttackSpeed);

        base.InitializeEnemy();
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        HandleCombat(distanceToPlayer);
    }

    protected override void PerformAttack()
    {
        // 기존 Attack 로직 그대로 사용 + 새로운 시스템 추가
        if (bulletPrefab != null && firePoint != null && playerTransform != null)
        {
            // 공격 시작
            isAttacking = true;

            // 총알 복제 생성 (기존 로직)
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // 플레이어 방향으로 총알 회전 (기존 로직)
            Vector3 targetPos = playerTransform.position;
            targetPos.y = firePoint.position.y;
            bullet.transform.LookAt(targetPos);

            // 기존 총알 시스템에 새로운 스탯 적용
            var bulletComponent = bullet.GetComponent<Enemy_Ranged_Bullet>();
            

            lastAttackTime = Time.time;

            // 일정 시간 후 공격 상태 해제 (기존 로직)
            Invoke(nameof(EndAttack), attackDuration);

        }
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Ranged_Move에서 처리하므로 비워둠
        // Enemy_Ranged_Move가 IsAttacking 프로퍼티를 참조해서 움직임 제어
    }

    #endregion

    #region 기존 로직 유지

    private void HandleCombat(float distanceToPlayer)
    {
        LookAtPlayer();

        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            PerformAttack(); // Enemy_Base의 추상 메서드 호출
        }
    }

    private void LookAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    #endregion

    #region 기존 Gizmo (유지)

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 새로운 시스템: 감지 범위도 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.DetectionRange));
        }
    }

    #endregion

    #region 추가 기능 (선택사항)

    /// <summary>
    /// 인스펙터에서 실시간으로 스탯 확인 가능
    /// </summary>
    [System.Serializable]
    public class RuntimeStats
    {
        [SerializeField] private float currentHP;
        [SerializeField] private float physicalDamage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float moveSpeed;

        public void UpdateStats(Enemy_Ranged enemy)
        {
            if (enemy.enemyStats != null)
            {
                currentHP = enemy.currentHp;
                physicalDamage = enemy.enemyStats.Get(EnemyStatType.PhysicalDamage);
                attackSpeed = enemy.enemyStats.Get(EnemyStatType.AttackSpeed);
                moveSpeed = enemy.enemyStats.Get(EnemyStatType.MoveSpeed);
            }
        }
    }

    [Header("실시간 스탯 확인 (읽기전용)")]
    [SerializeField] private RuntimeStats runtimeStats = new RuntimeStats();

    protected override void Update()
    {
        base.Update();

        // 디버그용: 실시간 스탯 업데이트
        if (Application.isEditor)
        {
            runtimeStats.UpdateStats(this);
        }
    }

    #endregion
}