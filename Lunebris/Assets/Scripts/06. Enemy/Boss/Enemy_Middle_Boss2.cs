using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 중간보스 2 - 대시 공격과 원사격 공격을 사용하는 보스
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Middle_Boss2 : Enemy_Base
{
    [Header("차징 레일건")]
    [SerializeField] private Enemy_Middle_Boss_Railgun railgunSystem;
    [SerializeField] private Transform railgunFirePoint;
    [SerializeField] private float railgunCooldown = 6f;    // 차징 시간 고려해서 조금 더 김

    [Header("대시 공격")]
    [SerializeField] private float dashRange = 15f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDistance = 12f;
    [SerializeField] private float dashCooldown = 8f;
    [SerializeField] private float dashWarningTime = 1f;

    [Header("원사격 공격")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private int bulletCount = 8;
    [SerializeField] private float bulletCooldown = 5f;
    [SerializeField] private float bulletRange = 10f;

    [Header("이펙트")]
    [SerializeField] private GameObject dashWarningEffect;
    [SerializeField] private GameObject dashTrailEffect;

    [Header("몬스터 무시 설정")]
    [SerializeField] private float monsterIgnoreRadius = 20f;
    [SerializeField] private string[] monsterTags = { "Enemy", "Boss", "MiddleBoss" };

    [Header("공격 간격 제어")]
    [SerializeField] private float attackCooldownTime = 2.5f; // 공격 간 대기 시간

    // 상태
    private bool isDashing = false;
    private bool isDashWarning = false;
    private bool isShooting = false;
    private bool shouldFireAfterDash = false; // 대쉬 후 탄막 공격 플래그

    // 타이밍
    private float lastDashTime;
    private float lastBulletTime;
    private float lastRailgunTime = 0f;
    private float lastAnyAttackTime; // 마지막 공격 시간 (모든 공격 통합)

    // 대시 관련
    private Vector3 dashDirection;
    private Vector3 dashStartPosition;
    private float dashStartTime;
    private GameObject currentDashTrail;

    // 컴포넌트
    private Rigidbody rigid;
    private Collider bossCollider;
    private List<Collider> ignoredMonsterColliders = new List<Collider>();

    // 애니메이터 파라미터 이름들
    private readonly string ANIM_DASH_WARNING_TRIGGER = "startDashWarning";
    private readonly string ANIM_IS_DASH_WARNING = "isDashWarning";
    private readonly string ANIM_DASH_TRIGGER = "startDash";
    private readonly string ANIM_IS_DASHING = "isDashing";
    private readonly string ANIM_BULLET_TRIGGER = "startBulletAttack";
    private readonly string ANIM_IS_SHOOTING = "isShooting";
    private readonly string ANIM_RAILGUN_TRIGGER = "startRailgun";
    private readonly string ANIM_IS_RAILGUN_ACTIVE = "isRailgunActive";
    private readonly string ANIM_RAILGUN_CHARGING = "isRailgunCharging";
    private readonly string ANIM_DASH_END_TRIGGER = "endDash";

    // 프로퍼티
    public bool IsDashing => isDashing;
    public bool IsDashWarning => isDashWarning;
    public bool IsShooting => isShooting;
    public bool IsRailgunActive => railgunSystem != null && !railgunSystem.IsReady; // 레일건 사용 중 여부
    public Vector3 DashDirection => dashDirection;
    public float DashSpeed => dashSpeed;

    protected override void Awake()
    {
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Neutral;
        primaryDamageType = DamageType.Physical;
        enemyName = "Berserker Champion";

        base.Awake();

        rigid = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();

        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();

        if (firePoint == null)
            firePoint = transform;

        // 레일건 시스템 초기화
        if (railgunSystem != null)
        {
            railgunSystem.Initialize(this, railgunFirePoint);
        }

        Debug.Log($"중간보스2 {enemyName} 등장! HP: {currentHp}");
    }

    protected override void UpdateBehavior()
    {
        if (isDashing)
        {
            UpdateDashBehavior();
            return;
        }

        // 대쉬 후 탄막 공격 처리
        if (shouldFireAfterDash && !isDashWarning && !isShooting && !IsRailgunActive)
        {
            shouldFireAfterDash = false;
            if (CanUseBulletsAfterDash())
            {
                StartCoroutine(PerformBulletAttack());
                return;
            }
        }

        if (isDashWarning || isShooting || playerTransform == null || IsRailgunActive)
        {
            UpdateAnimationStates();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();

        // 공격 쿨다운 체크 - 모든 공격에 공통 적용
        float timeSinceLastAttack = Time.time - lastAnyAttackTime;
        if (timeSinceLastAttack < attackCooldownTime)
        {
            return;
        }

        // 공격 패턴 우선순위 - 레일건이 최우선
        if (CanUseChargingRailgun())
        {
            UseChargingRailgun();
        }
        else if (distanceToPlayer >= dashRange * 0.8f && CanUseDash())
        {
            StartCoroutine(PerformDashAttack());
        }
        else if (distanceToPlayer <= bulletRange && CanUseBullets())
        {
            StartCoroutine(PerformBulletAttack());
        }

        // 애니메이션 상태 업데이트
        UpdateAnimationStates();
    }

    protected override void UpdateMovement()
    {
        // Enemy_Boss2_Move에서 처리
    }

    protected override void PerformAttack()
    {
        // 사용하지 않음
    }

    #region 애니메이션 제어

    private void UpdateAnimationStates()
    {
        if (characterAnimator == null) return;

        // 대시 관련 애니메이션
        characterAnimator.SetBool(ANIM_IS_DASH_WARNING, isDashWarning);
        characterAnimator.SetBool(ANIM_IS_DASHING, isDashing);

        // 총알 공격 애니메이션
        characterAnimator.SetBool(ANIM_IS_SHOOTING, isShooting);

        // 레일건 애니메이션
        bool isRailgunActive = IsRailgunActive;
        characterAnimator.SetBool(ANIM_IS_RAILGUN_ACTIVE, isRailgunActive);

        // 레일건 차징 상태 (레일건이 활성화되어 있으면 차징 중으로 간주)
        bool isRailgunCharging = isRailgunActive;
        characterAnimator.SetBool(ANIM_RAILGUN_CHARGING, isRailgunCharging);

        // 이동 애니메이션 (특수 공격 중이 아닐 때만)
        if (!IsPerformingSpecialAttack())
        {
            // Move 스크립트에서 이동 상태 확인
            Enemy_Middle_Boss2_Move moveScript = GetComponent<Enemy_Middle_Boss2_Move>();
            bool isMoving = moveScript != null && moveScript.IsMoving;
            float currentMoveSpeed = isMoving ? enemyStats.Get(EnemyStatType.MoveSpeed) : 0f;

            characterAnimator.SetBool(ANIM_IS_MOVING, isMoving);
            characterAnimator.SetFloat(ANIM_MOVE_SPEED, currentMoveSpeed);
        }
        else
        {
            characterAnimator.SetBool(ANIM_IS_MOVING, false);
            characterAnimator.SetFloat(ANIM_MOVE_SPEED, 0f);
        }
    }

    private void PlayDashWarningAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_DASH_WARNING_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DASH_WARNING, true);
        Debug.Log("대시 경고 애니메이션 재생");
    }

    private void PlayDashAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_DASH_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DASHING, true);
        Debug.Log("대시 애니메이션 재생");
    }

    private void PlayDashEndAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_DASH_END_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DASHING, false);
        Debug.Log("대시 종료 애니메이션 재생");
    }

    private void PlayBulletAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_BULLET_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_SHOOTING, true);
        Debug.Log("탄막 공격 애니메이션 재생");
    }

    private void PlayRailgunAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_RAILGUN_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_RAILGUN_ACTIVE, true);
        Debug.Log("레일건 애니메이션 재생");
    }

    #endregion

    #region 대시 공격

    private bool CanUseDash()
    {
        // 레일건 캐스팅 중에는 대쉬 불가
        return Time.time - lastDashTime >= dashCooldown &&
               !isDashing &&
               !isDashWarning &&
               !isShooting &&
               !IsRailgunActive && // 레일건 사용 중에는 대쉬 불가
               Time.time - lastAnyAttackTime >= attackCooldownTime; // 공통 공격 쿨다운 체크
    }

    private IEnumerator PerformDashAttack()
    {
        isDashWarning = true;
        lastDashTime = Time.time;

        // 대시 경고 애니메이션 재생
        PlayDashWarningAnimation();

        if (playerTransform != null)
        {
            dashDirection = (playerTransform.position - transform.position).normalized;
            dashDirection.y = 0;
        }

        // 경고 이펙트 생성
        if (dashWarningEffect != null)
        {
            Vector3 warningPos = transform.position + dashDirection * dashDistance * 0.5f;
            GameObject warning = Instantiate(dashWarningEffect, warningPos, Quaternion.LookRotation(dashDirection));
            Destroy(warning, dashWarningTime);
        }

        yield return new WaitForSeconds(dashWarningTime);

        isDashWarning = false;
        ExecuteDash();
    }

    private void ExecuteDash()
    {
        if (playerTransform == null) return;

        isDashing = true;
        dashStartTime = Time.time;
        dashStartPosition = transform.position;

        // 대시 애니메이션 재생
        PlayDashAnimation();

        // 대시 트레일 이펙트
        if (dashTrailEffect != null)
        {
            currentDashTrail = Instantiate(dashTrailEffect, transform.position, Quaternion.identity);
            currentDashTrail.transform.SetParent(transform);
        }

        IgnoreMonsterCollisions(true);

        float dashDuration = dashDistance / dashSpeed;
        StartCoroutine(DashCoroutine(dashDuration));
    }

    private IEnumerator DashCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        EndDash();
    }

    private void UpdateDashBehavior()
    {
        float traveledDistance = Vector3.Distance(dashStartPosition, transform.position);
        if (traveledDistance >= dashDistance || Time.time - dashStartTime >= 3f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        isDashing = false;

        // 대시 종료 애니메이션 재생
        PlayDashEndAnimation();

        // 대쉬 완료 후 탄막 공격 예약
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= bulletRange * 1.5f) // 탄막 사거리보다 약간 더 넓게
            {
                shouldFireAfterDash = true;
                Debug.Log("대쉬 완료 - 탄막 공격 예약됨");
            }
        }

        if (currentDashTrail != null)
        {
            currentDashTrail.transform.SetParent(null);
            Destroy(currentDashTrail, 1f);
            currentDashTrail = null;
        }

        IgnoreMonsterCollisions(false);

        // 공격 완료 시간 업데이트
        lastAnyAttackTime = Time.time;
        Debug.Log($"{enemyName}: 대시 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
    }

    public void OnDashCollision(Collider other)
    {
        if (IsMonsterCollider(other)) return;

        if (other.CompareTag("Player") && isDashing)
        {
            if (playerScript != null)
            {
                DealDamageToPlayer(DamageType.Physical);
            }
        }

        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            EndDash();
        }
    }

    #endregion

    #region 원사격 공격

    private bool CanUseBullets()
    {
        return Time.time - lastBulletTime >= bulletCooldown &&
               !isDashing &&
               !isDashWarning &&
               !isShooting &&
               !IsRailgunActive && // 레일건 사용 중에는 탄막 불가
               Time.time - lastAnyAttackTime >= attackCooldownTime; // 공통 공격 쿨다운 체크
    }

    private bool CanUseBulletsAfterDash()
    {
        // 대쉬 후 탄막은 쿨다운 무시하고 실행 가능
        return !isDashing &&
               !isDashWarning &&
               !isShooting &&
               !IsRailgunActive;
    }

    private IEnumerator PerformBulletAttack()
    {
        isShooting = true;

        // 탄막 공격 애니메이션 재생
        PlayBulletAnimation();

        // 대쉬 후가 아닐 때만 쿨다운 업데이트
        if (!shouldFireAfterDash)
        {
            lastBulletTime = Time.time;
        }

        Debug.Log("탄막 공격 시작");

        yield return new WaitForSeconds(0.3f); // 약간의 딜레이
        FireBulletsInCircle();
        yield return new WaitForSeconds(0.7f); // 공격 후 딜레이

        isShooting = false;

        // 대쉬 후 탄막이 아닐 때만 공격 완료 시간 업데이트
        if (!shouldFireAfterDash)
        {
            lastAnyAttackTime = Time.time;
            Debug.Log($"{enemyName}: 탄막 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
        }
        else
        {
            Debug.Log($"{enemyName}: 대시 후 탄막 공격 완료 (쿨다운 적용 안함)");
        }

        Debug.Log("탄막 공격 완료");
    }

    private void FireBulletsInCircle()
    {
        if (bulletPrefab == null) return;

        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                bulletRb.velocity = direction * bulletSpeed;
            }

            Enemy_Middle_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Middle_Boss_Bullet>();
            if (bulletScript != null)
            {
                float bulletDamage = enemyStats.Get(EnemyStatType.PhysicalDamage) * 0.4f;
                bulletScript.Initialize(bulletDamage, DamageType.Physical);
            }

            Destroy(bullet, 5f);
        }
    }

    #endregion

    #region 차징 레일건 시스템

    private bool CanUseChargingRailgun()
    {
        return railgunSystem != null &&
               railgunSystem.IsReady &&
               Time.time - lastRailgunTime >= railgunCooldown &&
               IsPlayerInDetectionRange() &&
               !isDashing &&
               !isDashWarning &&
               !isShooting &&
               Time.time - lastAnyAttackTime >= attackCooldownTime; // 공통 공격 쿨다운 체크
    }

    private void UseChargingRailgun()
    {
        if (playerTransform != null)
        {
            lastRailgunTime = Time.time;

            // 레일건 애니메이션 재생
            PlayRailgunAnimation();

            // 플레이어 위치로 차징 후 발사
            Vector3 targetPos = playerTransform.position;
            railgunSystem.FireInstantRailgun(targetPos);

            Debug.Log("레일건 발사 시작");
        }
    }

    #endregion

    #region 몬스터 충돌 무시

    private void IgnoreMonsterCollisions(bool ignore)
    {
        if (bossCollider == null) return;

        if (ignore)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, monsterIgnoreRadius);

            foreach (Collider col in nearbyColliders)
            {
                if (col == bossCollider || !IsMonsterCollider(col)) continue;

                if (!ignoredMonsterColliders.Contains(col))
                {
                    Physics.IgnoreCollision(bossCollider, col, true);
                    ignoredMonsterColliders.Add(col);
                }
            }
        }
        else
        {
            foreach (Collider col in ignoredMonsterColliders)
            {
                if (col != null && bossCollider != null)
                {
                    Physics.IgnoreCollision(bossCollider, col, false);
                }
            }
            ignoredMonsterColliders.Clear();
        }
    }

    private bool IsMonsterCollider(Collider col)
    {
        if (col == null) return false;

        foreach (string tag in monsterTags)
        {
            if (col.CompareTag(tag)) return true;
        }

        return col.GetComponent<Enemy_Base>() != null;
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 특수 공격 수행 중인지 확인
    /// </summary>
    public bool IsPerformingSpecialAttack()
    {
        return isDashing || isDashWarning || isShooting || IsRailgunActive;
    }

    /// <summary>
    /// 현재 보스의 행동 상태를 반환하는 메서드 (디버그용)
    /// </summary>
    public string GetCurrentState()
    {
        if (IsRailgunActive) return "Railgun Active";
        if (isDashing) return "Dashing";
        if (isDashWarning) return "Dash Warning";
        if (isShooting) return "Shooting";
        if (shouldFireAfterDash) return "Waiting to Fire After Dash";
        return "Idle";
    }

    /// <summary>
    /// 강제로 모든 행동을 중단하는 메서드 (디버그용)
    /// </summary>
    public void ForceStopAllActions()
    {
        StopAllCoroutines();

        isDashing = false;
        isDashWarning = false;
        isShooting = false;
        shouldFireAfterDash = false;

        if (currentDashTrail != null)
        {
            Destroy(currentDashTrail);
            currentDashTrail = null;
        }

        if (railgunSystem != null)
        {
            railgunSystem.ForceStop();
        }

        IgnoreMonsterCollisions(false);

        // 애니메이션 상태 초기화
        UpdateAnimationStates();
    }

    #endregion

    #region 오버라이드 메서드

    protected override void Die()
    {
        isDashing = false;
        isDashWarning = false;
        isShooting = false;
        shouldFireAfterDash = false;

        if (currentDashTrail != null)
        {
            Destroy(currentDashTrail);
        }

        if (railgunSystem != null)
        {
            railgunSystem.ForceStop();
        }

        IgnoreMonsterCollisions(false);
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 400;
    }

    #endregion
}