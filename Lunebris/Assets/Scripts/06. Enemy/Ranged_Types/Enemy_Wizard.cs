using UnityEngine;
using Enemy;

/// <summary>
/// 마법사 적 클래스 - Enemy_Base에 통합 (AP형)
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Wizard : Enemy_Base
{
    [Header("마법사 공격 설정")]
    public GameObject magicOrbPrefab;     // 마법 구체 프리팹
    public Transform castPoint;           // 마법 시전 위치
    public float attackRange = 10f;       // 공격 사거리
    public float attackCooldown = 2.5f;   // 공격 쿨다운
    public float castDuration = 0.5f;     // 마법 시전 지속 시간 (멈춰있는 시간)
    public float orbSpeed = 6f;           // 마법 구체 속도

    private float lastAttackTime;
    private bool isCasting = false;       // 마법 시전 중인지 여부

    // Move 스크립트에서 참조할 수 있는 프로퍼티
    public bool IsCasting => isCasting;

    #region Enemy_Base 오버라이드

    protected override void InitializeEnemy()
    {
        // 스탯 시스템 설정
        enemyType = EnemyType.RangedAP;
        primaryDamageType = DamageType.Magical; // AP 드라이버로 마법 데미지

        // 기본 초기화 로직
        if (castPoint == null)
        {
            Transform childCastPoint = transform.Find("CastPoint");
            if (childCastPoint != null)
            {
                castPoint = childCastPoint;
            }
        }

        // 스탯 시스템의 값으로 기본 설정 업데이트
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
        // 기본 마법 구체 공격
        if (magicOrbPrefab != null && castPoint != null && playerTransform != null)
        {
            // 시전 시작
            isCasting = true;

            // 마법 구체 복제 생성
            GameObject orb = Instantiate(magicOrbPrefab, castPoint.position, castPoint.rotation);

            // 플레이어 방향으로 마법 구체 회전
            Vector3 targetPos = playerTransform.position;
            targetPos.y = castPoint.position.y;
            orb.transform.LookAt(targetPos);

            // 마법 구체에 스탯 적용
            var orbComponent = orb.GetComponent<Enemy_Wizard_MagicOrb>();
            

            lastAttackTime = Time.time;

            // 일정 시간 후 시전 상태 해제
            Invoke(nameof(EndCasting), castDuration);

            Debug.Log($"{enemyName} 마법 구체 공격! 마법 데미지: {GetMainDamage()}");
        }
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Wizard_Move에서 처리하므로 비워둠
    }

    #endregion

    #region 마법사 전용 로직

    private void HandleCombat(float distanceToPlayer)
    {
        LookAtPlayer();

        // 기본 마법 구체 공격만
        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown && !isCasting)
        {
            PerformAttack();
        }
    }

    private void LookAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 2f);
        }
    }

    private void EndCasting()
    {
        isCasting = false;
        Debug.Log($"{enemyName} 마법 시전 완료, 이동 재개");
    }


    #endregion

    #region 기즈모 (유지)

    private void OnDrawGizmosSelected()
    {
        // 기본 공격 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 스탯 시스템: 감지 범위 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.DetectionRange));
        }
    }

    #endregion
}