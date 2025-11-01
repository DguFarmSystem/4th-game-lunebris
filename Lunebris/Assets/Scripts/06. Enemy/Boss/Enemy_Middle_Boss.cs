using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 중간보스 몬스터 (뱀서류용 - 단순화)
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Middle_Boss : Enemy_Base
{
    [Header("그랩 공격 설정")]
    [SerializeField] private float grabRange = 12f;
    [SerializeField] private float grabDamage = 60f;
    [SerializeField] private float grabCooldown = 6f;
    [SerializeField] private float grabDuration = 1.5f;
    [SerializeField] private float pullForce = 8f;
    [SerializeField] private float grabProjectileSpeed = 15f;
    [SerializeField] private Transform grabFirePoint;

    [Header("총알 공격 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private int bulletCount = 6;
    [SerializeField] private float bulletCooldown = 4f;
    [SerializeField] private float bulletRange = 8f;
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashSpeed = 1f;

    [Header("근거리 공격 설정")]
    [SerializeField] private float meleeRange = 4f;
    [SerializeField] private float meleeDamage = 80f;
    [SerializeField] private float meleeCooldown = 3f;
    [SerializeField] private float meleeRadius = 3f;
    [SerializeField] private float meleeKnockbackForce = 10f;
    [SerializeField] private float meleeAttackDuration = 1f;

    [Header("장판 공격 설정")]
    [SerializeField] private GameObject floorHazardPrefab;
    [SerializeField] private GameObject floorHazardOrbPrefab;
    [SerializeField] private float floorHazardDamage = 40f;
    [SerializeField] private float floorHazardDuration = 4f;
    [SerializeField] private float floorHazardCooldown = 8f;
    [SerializeField] private float floorHazardRadius = 3f;
    [SerializeField] private int maxFloorHazards = 6;
    [SerializeField] private float floorHazardRange = 15f;
    [SerializeField] private int hazardsPerCast = 3;
    [SerializeField] private float minDistanceFromPlayer = 2f;
    [SerializeField] private float minDistanceBetweenHazards = 4f;
    [SerializeField] private float orbSpeed = 12f;
    [SerializeField] private float orbArcHeight = 5f;

    [Header("이펙트 및 프리팹")]
    [SerializeField] private GameObject grabProjectilePrefab;
    [SerializeField] private GameObject meleeEffectPrefab;

    [Header("오디오 설정")] // ✨ 오디오 설정 추가
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip bulletSound;
    [SerializeField] private AudioClip meleeSound;
    [SerializeField] private AudioClip floorHazardThrowSound;

    [Header("공격 간격 제어")]
    [SerializeField] private float attackCooldownTime = 1f; // 공격 간 대기 시간 
    [SerializeField] private float maxChaseDistance = 20f; // 최대 추적 거리

    // 보스 상태
    private bool isGrabbing = false;
    private bool isShooting = false;
    private bool isCreatingFloorHazard = false;
    private bool isDashing = false;
    private bool isMeleeAttacking = false;

    // 공격 상태 관리
    private bool isAnyAttackInProgress = false; // 모든 공격 통합 플래그
    private float lastGrabTime;
    private float lastBulletTime;
    private float lastFloorHazardTime;
    private float lastMeleeTime;
    private float lastAnyAttackTime; // 마지막 공격 시간 (모든 공격 통합)

    // 현재 그랩된 플레이어 정보
    private Transform grabbedPlayer = null;
    private bool isPlayerBeingPulled = false;
    private float pullStartTime;
    private float currentPullDuration;

    // 현재 활성화된 장판 개수 추적
    private int currentFloorHazardCount = 0;

    // 이동 스크립트 참조
    private Enemy_Middle_Boss_Move moveScript;

    // 애니메이터 파라미터 이름들
    private readonly string ANIM_GRAB_TRIGGER = "startGrab";
    private readonly string ANIM_IS_GRABBING = "isGrabbing";
    private readonly string ANIM_BULLET_TRIGGER = "startBullet";
    private readonly string ANIM_IS_SHOOTING = "isShooting";
    private readonly string ANIM_FLOOR_HAZARD_TRIGGER = "startFloorHazard";
    private readonly string ANIM_IS_CREATING_FLOOR_HAZARD = "isCreatingFloorHazard";
    private readonly string ANIM_IS_PULLING_PLAYER = "isPullingPlayer";
    private readonly string ANIM_DASH_TRIGGER = "startDash";
    private readonly string ANIM_IS_DASHING = "isDashing";
    private readonly string ANIM_ATTACK_TRIGGER = "attack";
    private readonly string ANIM_IS_ATTACKING = "isAttacking";

    // Move 스크립트에서 참조할 수 있는 프로퍼티들
    public bool IsGrabbing => isGrabbing;
    public bool IsShooting => isShooting;
    public bool IsPlayerBeingPulled => isPlayerBeingPulled;
    public bool IsCreatingFloorHazard => isCreatingFloorHazard;
    public bool IsDashing => isDashing;
    public bool IsMeleeAttacking => isMeleeAttacking;

    protected override void Awake()
    {
        // 중간보스 기본 설정
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Tenebris;
        primaryDamageType = DamageType.Magical;
        enemyName = "Shadow Guardian";

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();

        // 이동 스크립트 참조
        moveScript = GetComponent<Enemy_Middle_Boss_Move>();

        // ✨ AudioSource 컴포넌트 참조
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 발사점이 없으면 자신의 위치 사용
        if (firePoint == null)
            firePoint = transform;
        if (grabFirePoint == null)
            grabFirePoint = transform;

        Debug.Log($"중간보스 {enemyName} 등장! HP: {currentHp}");
    }

    protected override void UpdateBehavior()
    {
        // 플레이어를 끌어당기는 중이면 다른 행동 제한
        if (isPlayerBeingPulled)
        {
            UpdatePullBehavior();
            return;
        }

        // 아무 공격이라도 진행 중이면 리턴
        if (isAnyAttackInProgress)
        {
            return;
        }

        // 플레이어가 없으면 리턴
        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 너무 멀면 추적만 하고 공격하지 않음
        if (distanceToPlayer > maxChaseDistance)
        {
            return;
        }

        // 공격 쿨다운 체크 - 모든 공격에 공통 적용
        float timeSinceLastAttack = Time.time - lastAnyAttackTime;
        if (timeSinceLastAttack < attackCooldownTime)
        {
            return;
        }

        // 거리에 따른 공격 패턴 선택 (근거리 공격은 조건을 더 까다롭게)
        if (distanceToPlayer <= meleeRange && CanUseMelee() && Time.time - lastMeleeTime >= meleeCooldown * 3f)
        {
            // 근거리 공격 (더 긴 쿨다운 적용)
            Debug.Log($"{enemyName}: 근거리 공격 시전! 거리: {distanceToPlayer:F1}m");
            StartCoroutine(PerformMeleeAttack());
        }
        else if (distanceToPlayer >= grabRange * 0.8f && CanUseGrab())
        {
            Debug.Log($"{enemyName}: 그랩 공격 시전! 거리: {distanceToPlayer:F1}m");
            StartCoroutine(PerformGrabAttack());
        }
        else if (distanceToPlayer <= floorHazardRange && CanUseFloorHazard())
        {
            Debug.Log($"{enemyName}: 장판 공격 시전! 거리: {distanceToPlayer:F1}m");
            StartCoroutine(PerformFloorHazardAttack());
        }
        else if (distanceToPlayer <= bulletRange && CanUseBullets())
        {
            Debug.Log($"{enemyName}: 총알 공격 시전! 거리: {distanceToPlayer:F1}m");
            StartCoroutine(PerformBulletAttack());
        }

        // 애니메이션 상태 업데이트
        UpdateAnimationStates();
    }

    protected override void UpdateMovement()
    {
        // Enemy_Middle_Boss_Move가 처리
    }

    protected override void PerformAttack()
    {
        // 사용 안 함
    }

    #region 오디오 제어

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region 애니메이션 제어

    private void UpdateAnimationStates()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetBool(ANIM_IS_GRABBING, isGrabbing);
        characterAnimator.SetBool(ANIM_IS_SHOOTING, isShooting);
        characterAnimator.SetBool(ANIM_IS_CREATING_FLOOR_HAZARD, isCreatingFloorHazard);
        characterAnimator.SetBool(ANIM_IS_PULLING_PLAYER, isPlayerBeingPulled);
        characterAnimator.SetBool(ANIM_IS_DASHING, isDashing);
        characterAnimator.SetBool(ANIM_IS_ATTACKING, isMeleeAttacking);

        // 이동 애니메이션
        bool isMoving = moveScript != null ? moveScript.IsMoving : false;
        bool shouldBeMoving = isMoving && !IsPerformingSpecialAttack();
        float currentMoveSpeed = shouldBeMoving ? enemyStats.Get(EnemyStatType.MoveSpeed) : 0f;

        characterAnimator.SetBool(ANIM_IS_MOVING, shouldBeMoving);
        characterAnimator.SetFloat(ANIM_MOVE_SPEED, currentMoveSpeed);
    }

    private void PlayMeleeAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_ATTACK_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_ATTACKING, true);
    }

    private void PlayGrabAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_GRAB_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_GRABBING, true);
    }

    private void PlayBulletAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_BULLET_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_SHOOTING, true);
    }

    private void PlayFloorHazardAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_FLOOR_HAZARD_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_CREATING_FLOOR_HAZARD, true);
    }

    private void PlayDashAnimation()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetTrigger(ANIM_DASH_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DASHING, true);
    }

    #endregion

    #region 근거리 공격

    private bool CanUseMelee()
    {
        return Time.time - lastMeleeTime >= meleeCooldown && !isAnyAttackInProgress;
    }

    private IEnumerator PerformMeleeAttack()
    {
        // 이미 공격 중이면 중단
        if (isAnyAttackInProgress)
        {
            Debug.Log($"{enemyName}: 이미 공격 중이므로 근거리 공격 취소!");
            yield break;
        }

        isAnyAttackInProgress = true; // 공격 시작
        isMeleeAttacking = true;
        lastMeleeTime = Time.time;

        Debug.Log($"{enemyName}: 근거리 공격 시작!");

        PlayMeleeAnimation();
        yield return new WaitForSeconds(0.5f);

        // ✨ 근거리 공격 사운드 재생
        PlaySound(meleeSound);
        ExecuteMeleeAttack();

        yield return new WaitForSeconds(meleeAttackDuration - 0.5f);

        isMeleeAttacking = false;
        isAnyAttackInProgress = false; // 공격 완전 종료
        lastAnyAttackTime = Time.time; // 공격이 완전히 끝날 때 설정

        Debug.Log($"{enemyName}: 근거리 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
    }

    private void ExecuteMeleeAttack()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= meleeRadius)
        {
            if (playerScript != null)
            {
                float totalDamage = DamageCalculator.CalculateDamageToPlayer(
                    enemyStats,
                    elementType,
                    DamageType.Physical,
                    playerScript.GetPlayerStat(),
                    ElementType.Neutral
                ) + meleeDamage;

                playerScript.DecreaseHP(totalDamage);
            }

            ApplyKnockbackToPlayer();
        }

        if (meleeEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position + transform.forward * (meleeRadius * 0.5f);
            GameObject effect = Instantiate(meleeEffectPrefab, effectPosition, transform.rotation);
            Destroy(effect, 2f);
        }
    }

    private void ApplyKnockbackToPlayer()
    {
        if (playerTransform == null) return;

        Vector3 knockbackDirection = (playerTransform.position - transform.position).normalized;
        knockbackDirection.y = 0;

        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockbackForce = knockbackDirection * meleeKnockbackForce;
            knockbackForce.y = 2f;
            playerRb.AddForce(knockbackForce, ForceMode.Impulse);
        }
    }

    #endregion

    #region 그랩 투사체 공격

    private bool CanUseGrab()
    {
        return Time.time - lastGrabTime >= grabCooldown && !isAnyAttackInProgress;
    }

    private IEnumerator PerformGrabAttack()
    {
        if (isAnyAttackInProgress) yield break;

        isAnyAttackInProgress = true;
        isGrabbing = true;
        lastGrabTime = Time.time;

        PlayGrabAnimation();
        yield return new WaitForSeconds(0.3f);

        if (playerTransform != null)
        {
            // ✨ 그랩 공격 사운드 재생
            PlaySound(grabSound);
            FireGrabProjectile();
        }

        yield return new WaitForSeconds(0.2f);
        isGrabbing = false;
        isAnyAttackInProgress = false;
        lastAnyAttackTime = Time.time;
        Debug.Log($"{enemyName}: 그랩 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
    }

    private void FireGrabProjectile()
    {
        if (grabProjectilePrefab == null || playerTransform == null) return;

        Vector3 direction = (playerTransform.position - grabFirePoint.position).normalized;
        direction.y = 0;

        GameObject grabProjectile = Instantiate(grabProjectilePrefab, grabFirePoint.position, Quaternion.LookRotation(direction));

        Rigidbody projectileRb = grabProjectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.velocity = direction * grabProjectileSpeed;
        }

        Enemy_Middle_Boss_Grab grabScript = grabProjectile.GetComponent<Enemy_Middle_Boss_Grab>();
        if (grabScript != null)
        {
            grabScript.Initialize(this, pullForce, grabDuration, grabDamage);
        }
    }

    public void OnGrabProjectileHit(Transform player, float force, float duration)
    {
        if (isPlayerBeingPulled) return;

        grabbedPlayer = player;
        isPlayerBeingPulled = true;
        pullStartTime = Time.time;
        currentPullDuration = duration;

        StartCoroutine(PullPlayerCoroutine(force, duration));
    }

    private IEnumerator PullPlayerCoroutine(float force, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && isPlayerBeingPulled && grabbedPlayer != null)
        {
            Vector3 directionToBoss = (transform.position - grabbedPlayer.position).normalized;
            directionToBoss.y = 0;

            Vector3 pullPosition = grabbedPlayer.position + directionToBoss * force * Time.deltaTime;
            pullPosition.y = grabbedPlayer.position.y;

            float distanceToBoss = Vector3.Distance(new Vector3(pullPosition.x, 0, pullPosition.z),
                                                  new Vector3(transform.position.x, 0, transform.position.z));
            if (distanceToBoss > 2f)
            {
                grabbedPlayer.position = pullPosition;
            }

            if (elapsed % 0.5f < Time.deltaTime && elapsed > 0.1f)
            {
                DealDamageToPlayer(DamageType.Magical);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isPlayerBeingPulled = false;
        grabbedPlayer = null;
    }

    private void UpdatePullBehavior()
    {
        if (isPlayerBeingPulled && Time.time - pullStartTime >= currentPullDuration)
        {
            isPlayerBeingPulled = false;
            grabbedPlayer = null;
        }
    }

    #endregion

    #region 총알 공격

    private bool CanUseBullets()
    {
        return Time.time - lastBulletTime >= bulletCooldown && !isAnyAttackInProgress;
    }

    private IEnumerator PerformBulletAttack()
    {
        if (isAnyAttackInProgress) yield break;

        isAnyAttackInProgress = true;
        isShooting = true;
        lastBulletTime = Time.time;

        PlayDashAnimation();
        yield return StartCoroutine(PerformDash());

        PlayBulletAnimation();
        yield return new WaitForSeconds(0.3f);

        // ✨ 총알 공격 사운드 재생
        PlaySound(bulletSound);
        FireBulletsInCircle();

        yield return new WaitForSeconds(0.5f);

        isShooting = false;
        isAnyAttackInProgress = false;
        lastAnyAttackTime = Time.time;
        Debug.Log($"{enemyName}: 총알 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
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
                bulletRb.velocity = direction * bulletSpeed;
            }

            Enemy_Middle_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Middle_Boss_Bullet>();
            if (bulletScript != null)
            {
                float bulletDamage = enemyStats.Get(EnemyStatType.MagicalDamage) * 0.4f;
                bulletScript.Initialize(bulletDamage, DamageType.Magical);
            }

            Destroy(bullet, 5f);
        }
    }

    #endregion

    #region 대시 시스템

    private IEnumerator PerformDash()
    {
        if (playerTransform == null)
        {
            isDashing = false;
            yield break;
        }

        isDashing = true;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 dashDirection = directionToPlayer;

        // 간단한 장애물 체크
        if (Physics.Raycast(transform.position, dashDirection, dashDistance))
        {
            dashDirection = -dashDirection;
            if (Physics.Raycast(transform.position, dashDirection, dashDistance))
            {
                isDashing = false;
                yield break;
            }
        }

        // 대시 실행
        float dashDuration = 1f;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + dashDirection * dashDistance;
        targetPosition.y = startPosition.y;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            transform.position = currentPosition;

            yield return null;
        }

        transform.position = targetPosition;
        isDashing = false;
        yield return new WaitForSeconds(0.1f);
    }

    #endregion

    #region 장판 공격

    private bool CanUseFloorHazard()
    {
        return Time.time - lastFloorHazardTime >= floorHazardCooldown &&
               !isAnyAttackInProgress &&
               currentFloorHazardCount + hazardsPerCast <= maxFloorHazards;
    }

    private IEnumerator PerformFloorHazardAttack()
    {
        if (isAnyAttackInProgress) yield break;

        isAnyAttackInProgress = true;
        isCreatingFloorHazard = true;
        lastFloorHazardTime = Time.time;

        PlayFloorHazardAnimation();
        yield return new WaitForSeconds(0.8f);

        if (playerTransform != null)
        {
            // ✨ 장판 투척 사운드 재생
            PlaySound(floorHazardThrowSound);
            ThrowFloorHazardOrbs();
        }

        yield return new WaitForSeconds(0.5f);
        isCreatingFloorHazard = false;
        isAnyAttackInProgress = false;
        lastAnyAttackTime = Time.time;
        Debug.Log($"{enemyName}: 장판 공격 완료! 다음 공격까지 {attackCooldownTime}초 대기");
    }

    private void ThrowFloorHazardOrbs()
    {
        if (floorHazardOrbPrefab == null || playerTransform == null) return;

        int orbsToThrow = Mathf.Min(hazardsPerCast, maxFloorHazards - currentFloorHazardCount);
        if (orbsToThrow <= 0) return;

        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < orbsToThrow; i++)
        {
            Vector3 targetPosition = FindValidHazardPosition(targetPositions);

            if (targetPosition != Vector3.zero)
            {
                targetPositions.Add(targetPosition);
                StartCoroutine(ThrowSingleOrb(targetPosition, i * 0.2f));
            }
        }
    }

    private IEnumerator ThrowSingleOrb(Vector3 targetPosition, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 startPosition = firePoint.position + Vector3.up * 1f;
        GameObject orb = Instantiate(floorHazardOrbPrefab, startPosition, Quaternion.identity);

        Enemy_Middle_Boss_FloorHazardOrb orbScript = orb.GetComponent<Enemy_Middle_Boss_FloorHazardOrb>();
        if (orbScript != null)
        {
            orbScript.Initialize(
                this,
                targetPosition,
                orbSpeed,
                orbArcHeight,
                floorHazardPrefab,
                floorHazardDamage,
                floorHazardDuration,
                floorHazardRadius
            );
        }
        else
        {
            Destroy(orb);
        }
    }

    public void OnOrbLanded(Vector3 position, GameObject hazardPrefab, float damage, float duration, float radius)
    {
        GameObject floorHazard = Instantiate(hazardPrefab, position, Quaternion.identity);

        Enemy_Middle_Boss_FloorHazard hazardScript = floorHazard.GetComponent<Enemy_Middle_Boss_FloorHazard>();
        if (hazardScript != null)
        {
            hazardScript.Initialize(damage, duration, radius, this);
        }

        currentFloorHazardCount++;
        StartCoroutine(DestroyFloorHazardAfterDuration(floorHazard, duration));
    }

    private Vector3 FindValidHazardPosition(List<Vector3> existingPositions)
    {
        const int maxAttempts = 10; // 시도 횟수 줄임

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * floorHazardRange;
            Vector3 candidatePosition = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            candidatePosition.y = 0;

            if (IsValidHazardPosition(candidatePosition, existingPositions))
            {
                return candidatePosition;
            }
        }

        return Vector3.zero;
    }

    private bool IsValidHazardPosition(Vector3 position, List<Vector3> existingPositions)
    {
        float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromPlayer) return false;

        float distanceToBoss = Vector3.Distance(position, transform.position);
        if (distanceToBoss < 1f) return false;

        foreach (Vector3 existingPos in existingPositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minDistanceBetweenHazards) return false;
        }

        return true;
    }

    private IEnumerator DestroyFloorHazardAfterDuration(GameObject hazard, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (hazard != null)
        {
            currentFloorHazardCount--;
            Destroy(hazard);
        }
    }

    public void OnFloorHazardDestroyed()
    {
        currentFloorHazardCount = Mathf.Max(0, currentFloorHazardCount - 1);
    }

    #endregion

    #region 오버라이드 메서드

    protected override void Die()
    {
        // 모든 상태 초기화
        isAnyAttackInProgress = false;
        isGrabbing = false;
        isShooting = false;
        isPlayerBeingPulled = false;
        isCreatingFloorHazard = false;
        isDashing = false;
        isMeleeAttacking = false;
        grabbedPlayer = null;
        currentFloorHazardCount = 0;

        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 300;
    }

    #endregion

    #region 퍼블릭 접근자

    public bool IsPerformingSpecialAttack() => isAnyAttackInProgress || isPlayerBeingPulled;

    #endregion
}