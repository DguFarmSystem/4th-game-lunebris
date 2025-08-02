using UnityEngine;
using Enemy;

/// <summary>
/// 단순화된 근접 탱커 - 기본 스탯만 사용하는 적
/// 높은 체력과 방어력을 가진 내구형 적
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Tanker : Enemy_Base
{
    [Header("기본 공격 설정")]
    [SerializeField] private float attackDuration = 0.5f; // 공격 지속시간

    // 상태 관리
    private bool isAttacking = false;
    private float lastAttackTime;

    // Move 스크립트에서 참조할 수 있는 프로퍼티들
    public bool IsAttacking => isAttacking;

    protected override void Awake()
    {
        // 탱커 기본 설정
        enemyType = EnemyType.MeleeTanker;
        elementType = ElementType.Neutral;
        primaryDamageType = DamageType.Physical;
        enemyName = "Simple Tanker";

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        // 공격 지속시간을 공격속도에 따라 조정
        attackDuration = 1f / enemyStats.Get(EnemyStatType.AttackSpeed) * 0.5f;

        base.InitializeEnemy();
    }

    protected override void UpdateBehavior()
    {
        // 공격 중인지 체크
        if (isAttacking)
        {
            if (Time.time - lastAttackTime >= attackDuration)
            {
                isAttacking = false;
                Debug.Log($"{enemyName}: 공격 완료");
            }
            return; // 공격 중이면 다른 행동 하지 않음
        }

        if (playerTransform == null) return;

        // 공격 범위 내면 공격
        if (IsPlayerInAttackRange())
        {
            TryAttack();
        }
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Tanker_Move에서 처리하므로 비워둠
    }

    protected override void PerformAttack()
    {
        if (playerScript == null) return;

        // 강력한 물리 공격 (탱커는 데미지가 높음)
        DealDamageToPlayer(DamageType.Physical);
        Debug.Log($"{enemyName}: 강력한 물리 공격!");
    }

    private void TryAttack()
    {
        float attackSpeed = enemyStats.Get(EnemyStatType.AttackSpeed);
        float attackCooldown = 1f / attackSpeed;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            isAttacking = true;
            PerformAttack();
            lastAttackTime = Time.time;

            // 일정 시간 후 공격 상태 해제
            Invoke(nameof(EndAttack), attackDuration);
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
    }





    protected override int GetExperienceReward()
    {
        return 20; // 탱커는 더 많은 경험치
    }
}