using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;
using Player;

/// <summary>
/// 탄환 패턴 종류
/// </summary>
public enum BulletPattern
{
    Straight,   // 직선 공격
    Circular,   // 원형 공격  
    Spiral,     // 나선형 공격
    Fan,        // 부채꼴 공격
    Homing,     // 유도탄 공격
    Burst       // 폭발 공격
}

/// <summary>
/// 최종보스 - 어둠 모드 (끌어당기기 탄환, 어둠 바닥, 보스 탄환, 대시, 근거리 공격) - 중간보스 패턴 적용
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Final_Boss_Dark : Enemy_Base
{
    [Header("끌어당기기 탄환")]
    [SerializeField] private GameObject pullBulletPrefab;
    [SerializeField] private float pullBulletSpeed = 10f;
    [SerializeField] private int pullBulletSpawnCount = 16;
    [SerializeField] private float mapRadius = 15f; // 맵 반경 감소
    [SerializeField] private float pullBulletSpawnInterval = 10f; // 적절한 간격으로 조정
    [SerializeField] private float pullBulletRange = 18f; // 범위 감소 (이제 거리 체크에 사용 안됨)

    [Header("어둠 바닥")]
    [SerializeField] private GameObject darkFloorHazardPrefab;
    [SerializeField] private float darkFloorDamage = 30f;
    [SerializeField] private float darkFloorCooldown = 5f;

    [Header("보스 탄환")]
    [SerializeField] private GameObject bossBulletPrefab;
    [SerializeField] private float bossBulletDamage = 50f;
    [SerializeField] private float bossBulletCooldown = 3f;
    [SerializeField] private int bossBulletCount = 8;
    [SerializeField] private float bossBulletRange = 10f; // 범위 감소
    [SerializeField] private float bossBulletSpeed = 12f;

    [Header("대시 공격")]
    [SerializeField] private float dashSpeed = 20f; // 속도 감소
    [SerializeField] private float dashRange = 8f; // 대시 범위 감소
    [SerializeField] private float dashDamage = 80f;
    [SerializeField] private float dashCooldown = 6f;
    [SerializeField] private float dashDuration = 0.8f; // 대시 시간 감소
    [SerializeField] private GameObject dashChargeEffect;
    [SerializeField] private GameObject dashTrailEffect;

    [Header("근거리 공격")]
    [SerializeField] private float meleeRange = 4f; // 근접 범위 감소
    [SerializeField] private float meleeDamage = 100f;
    [SerializeField] private float meleeCooldown = 4f;
    [SerializeField] private GameObject meleeAttackEffect;
    [SerializeField] private float meleeAttackRadius = 5f; // 공격 반경 감소

    [Header("탄환 패턴 설정")]
    [SerializeField] private BulletPattern[] bulletPatterns;
    [SerializeField] private float patternSwitchChance = 0.3f; // 30% 확률로 패턴 변경

    [Header("이펙트")]
    [SerializeField] private GameObject pullBulletChargeEffect;
    [SerializeField] private GameObject darkFloorEffect;

    // 상태 플래그 (중간보스 패턴)
    private bool isCreatingPullBullets = false;
    private bool isCreatingDarkFloor = false;
    private bool isShootingBossBullets = false;
    private bool isDashing = false;
    private bool isMeleeAttacking = false;

    // 공격 타이밍 관리
    private float lastPullBulletTime = 0f;
    private float lastDarkFloorTime = 0f;
    private float lastBossBulletTime = 0f;
    private float lastDashTime = 0f;
    private float lastMeleeTime = 0f;

    // 활성 오브젝트 관리
    private List<GameObject> activePullBullets = new();
    private GameObject activeDarkFloorHazard;
    private Coroutine pullBulletSpawnRoutine;

    // 탄환 패턴 관리
    private BulletPattern currentBulletPattern = BulletPattern.Straight;
    private int consecutiveAttacks = 0;

    // 대시 관련
    private Vector3 dashStartPosition;
    private Vector3 dashTargetPosition;
    private float dashStartTime;

    // Move 스크립트 참조
    private Enemy_Final_Boss_Dark_Move moveScript;

    // 애니메이터 파라미터 이름들 (상수로 정의)
    private readonly string ANIM_IS_MOVING = "isMoving";
    private readonly string ANIM_MOVE_SPEED = "moveSpeed";
    private readonly string ANIM_IS_DEAD = "isDead";
    private readonly string ANIM_IS_CREATING_PULL_BULLETS = "isCreatingPullBullets";
    private readonly string ANIM_IS_CREATING_DARK_FLOOR = "isCreatingDarkFloor";
    private readonly string ANIM_IS_SHOOTING_BULLETS = "isShootingBullets";
    private readonly string ANIM_IS_DASHING = "isDashing";
    private readonly string ANIM_IS_MELEE_ATTACKING = "isMeleeAttacking";
    private readonly string ANIM_HIT_TRIGGER = "hit";
    private readonly string ANIM_DIE_TRIGGER = "die";
    private readonly string ANIM_DASH_ATTACK_TRIGGER = "dashAttack";
    private readonly string ANIM_MELEE_ATTACK_TRIGGER = "meleeAttack";
    private readonly string ANIM_BULLET_ATTACK_TRIGGER = "bulletAttack";
    private readonly string ANIM_PULL_BULLET_CAST_TRIGGER = "pullBulletCast";
    private readonly string ANIM_DARK_FLOOR_CAST_TRIGGER = "darkFloorCast";
    private readonly string ANIM_HEALTH_PERCENT = "healthPercent";
    private readonly string ANIM_ATTACK_INTENSITY = "attackIntensity";
    private readonly string ANIM_BULLET_PATTERN = "bulletPattern";
    private readonly string ANIM_CURRENT_PHASE = "currentPhase";

    // 애니메이터 참조
    private Animator bossAnimator;

    // 공개 프로퍼티 (Move 스크립트에서 참조 가능)
    public bool IsCreatingPullBullets => isCreatingPullBullets;
    public bool IsCreatingDarkFloor => isCreatingDarkFloor;
    public bool IsShootingBossBullets => isShootingBossBullets;
    public bool IsDashing => isDashing;
    public bool IsMeleeAttacking => isMeleeAttacking;

    protected override void Awake()
    {
        enemyType = EnemyType.FinalBoss;
        elementType = ElementType.Tenebris;
        primaryDamageType = DamageType.Magical;
        enemyName = "Dark Sovereign";

        moveScript = GetComponent<Enemy_Final_Boss_Dark_Move>();
        if (moveScript == null)
        {
            moveScript = gameObject.AddComponent<Enemy_Final_Boss_Dark_Move>();
        }

        // 애니메이터 컴포넌트 가져오기
        bossAnimator = GetComponent<Animator>();
        if (bossAnimator == null)
        {
            bossAnimator = GetComponentInChildren<Animator>();
        }

        if (bossAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator가 없습니다. 애니메이션이 재생되지 않습니다.");
        }

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();
        Debug.Log("어둠 모드 보스 활성화! 어둠의 힘을 사용합니다.");

        InitializeDarkMode();
    }

    public void SetHealth(float health)
    {
        currentHp = health;
        UpdateHpUI();
    }

    private void InitializeDarkMode()
    {
        // 어둠 바닥 해저드 생성
        CreateDarkFloorHazard();

        // 주기적인 끌어당기기 탄환 시스템 비활성화 (수동 생성만 사용)
        // pullBulletSpawnRoutine = StartCoroutine(SpawnPullBulletsPeriodically());

        // 연속 공격 루틴 시작
        StartCoroutine(ContinuousAttackRoutine());
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 공격 우선순위 (거리별로 중간보스 패턴 적용)
        if (distanceToPlayer <= meleeRange && CanUseMeleeAttack())
        {
            // 근거리에서 근접 공격 사용
            StartCoroutine(PerformMeleeAttack());
        }
        else if (distanceToPlayer <= dashRange && distanceToPlayer > meleeRange && CanUseDash())
        {
            // 중거리에서 대시 공격 사용
            StartCoroutine(PerformDashAttack());
        }
        else if (distanceToPlayer <= bossBulletRange && CanUseBossBullets())
        {
            // 중거리에서 보스 탄환 사용
            StartCoroutine(PerformBossBulletAttack());
        }
        else if (CanUseDarkFloor())
        {
            // 어둠 바닥 재생성
            StartCoroutine(RefreshDarkFloorAttack());
        }

        // 끌어당기기 탄환은 거리 상관없이 시간 기반으로만 체크
        if (CanUsePullBullets())
        {
            StartCoroutine(CreatePullBulletsAttack());
        }
    }

    protected override void UpdateMovement()
    {
        // Enemy_Final_Boss_Dark_Move에서 처리
    }

    protected override void PerformAttack()
    {
        // 기본 공격은 사용하지 않음
    }

    protected override void Update()
    {
        base.Update();

        // 주기적으로 애니메이터 파라미터 업데이트
        if (Time.frameCount % 10 == 0) // 10프레임마다 업데이트
        {
            UpdateAnimatorParameters();
        }
    }

    #region 연속 공격 시스템

    private IEnumerator ContinuousAttackRoutine()
    {
        while (currentHp > 0)
        {
            // 지속적인 보스 탄환 공격 (기본 공격)
            if (!IsPerformingAnyAttack() && playerTransform != null)
            {
                float distanceToPlayer = GetDistanceToPlayer();
                if (distanceToPlayer <= bossBulletRange)
                {
                    yield return StartCoroutine(PerformBossBulletAttack());
                }
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private bool IsPerformingAnyAttack()
    {
        return isCreatingPullBullets || isCreatingDarkFloor || isShootingBossBullets || isDashing || isMeleeAttacking;
    }

    #endregion

    #region 대시 공격

    private bool CanUseDash()
    {
        return Time.time - lastDashTime >= dashCooldown &&
               !IsPerformingAnyAttack() &&
               playerTransform != null;
    }

    private IEnumerator PerformDashAttack()
    {
        isDashing = true;
        lastDashTime = Time.time;

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_DASH_ATTACK_TRIGGER);
        }

        // 대시 준비 이펙트
        GameObject chargeEffect = null;
        if (dashChargeEffect != null)
        {
            chargeEffect = Instantiate(dashChargeEffect, transform.position, Quaternion.identity);
        }

        // 플레이어 위치 계산
        Vector3 playerPos = playerTransform.position;
        dashStartPosition = transform.position;

        // 플레이어 뒤쪽으로 대시 (오버슛 감소)
        Vector3 dashDirection = (playerPos - transform.position).normalized;
        dashTargetPosition = playerPos + dashDirection * 1.5f; // 오버슛 거리 감소
        dashTargetPosition.y = transform.position.y; // Y축 고정

        dashStartTime = Time.time;

        yield return new WaitForSeconds(0.8f); // 차지 시간

        if (chargeEffect != null)
        {
            Destroy(chargeEffect);
        }

        // 대시 트레일 이펙트
        GameObject trailEffect = null;
        if (dashTrailEffect != null)
        {
            trailEffect = Instantiate(dashTrailEffect, transform.position, Quaternion.identity);
            trailEffect.transform.SetParent(transform);
        }

        Debug.Log("다크 보스 대시 공격 시작!");

        // 대시 실행
        yield return StartCoroutine(ExecuteDash());

        if (trailEffect != null)
        {
            trailEffect.transform.SetParent(null);
            Destroy(trailEffect, 2f);
        }

        isDashing = false;
        UpdateAnimatorParameters(); // 애니메이터 상태 업데이트
    }

    private IEnumerator ExecuteDash()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;

            // 빠른 이동 (EaseInOutQuad)
            float smoothProgress = progress < 0.5f ?
                2f * progress * progress :
                -1f + (4f - 2f * progress) * progress;

            transform.position = Vector3.Lerp(startPos, dashTargetPosition, smoothProgress);

            // 대시 중 충돌 체크
            CheckDashCollision();

            yield return null;
        }

        transform.position = dashTargetPosition;
    }

    private void CheckDashCollision()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= 2f) // 충돌 거리 감소
        {
            // 플레이어에게 데미지 적용
            var playerComponent = playerTransform.GetComponent<Player.Player>();
            if (playerComponent != null)
            {
                playerComponent.DecreaseHP(dashDamage);
            }

            Debug.Log($"다크 보스 대시로 플레이어에게 {dashDamage} 데미지!");
        }
    }

    #endregion

    #region 근거리 공격

    private bool CanUseMeleeAttack()
    {
        return Time.time - lastMeleeTime >= meleeCooldown &&
               !IsPerformingAnyAttack();
    }

    private IEnumerator PerformMeleeAttack()
    {
        isMeleeAttacking = true;
        lastMeleeTime = Time.time;

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_MELEE_ATTACK_TRIGGER);
        }

        // 근접 공격 이펙트
        if (meleeAttackEffect != null)
        {
            GameObject effect = Instantiate(meleeAttackEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        yield return new WaitForSeconds(0.5f); // 공격 준비 시간

        // 범위 내 플레이어 찾기
        Collider[] hits = Physics.OverlapSphere(transform.position, meleeAttackRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Player 컴포넌트를 찾아서 데미지 적용
                var playerComponent = hit.GetComponent<Player.Player>();
                if (playerComponent != null)
                {
                    playerComponent.DecreaseHP(meleeDamage);
                }

                Debug.Log($"다크 보스 근접 공격으로 {meleeDamage} 데미지!");
                break;
            }
        }

        yield return new WaitForSeconds(0.5f); // 공격 후 딜레이
        isMeleeAttacking = false;
        UpdateAnimatorParameters(); // 애니메이터 상태 업데이트
    }

    #endregion

    #region 탄환 생성 핵심 메서드

    private void CreateBossBullet(Vector3 direction, float speed)
    {
        if (bossBulletPrefab != null)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, transform.position, Quaternion.LookRotation(direction));
            Enemy_Final_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Final_Boss_Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(bossBulletDamage, direction * speed);
            }
        }
        else
        {
            Debug.LogWarning("bossBulletPrefab이 설정되지 않았습니다!");
        }
    }

    private void CreateHomingBullet(Vector3 initialDirection)
    {
        if (bossBulletPrefab != null)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, transform.position, Quaternion.LookRotation(initialDirection));

            // 유도탄 스크립트 추가 (임시로 일반 탄환으로 대체)
            Enemy_Final_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Final_Boss_Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(bossBulletDamage * 1.5f, initialDirection * bossBulletSpeed * 0.8f);
            }

            // 유도탄 표시를 위해 색상 변경
            Renderer renderer = bullet.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                mat.color = Color.red;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.red * 2f);
            }
        }
        else
        {
            Debug.LogWarning("bossBulletPrefab이 설정되지 않았습니다!");
        }
    }

    #endregion

    #region 주기적 끌어당기기 탄환 시스템 (현재 비활성화)

    // 주기적 생성은 비활성화하고 거리 기반 수동 생성만 사용
    private IEnumerator SpawnPullBulletsPeriodically()
    {
        yield return new WaitForSeconds(pullBulletSpawnInterval);

        while (currentHp > 0)
        {
            if (!IsPerformingAnyAttack())
            {
                CleanupOldPullBullets();
                yield return CreatePullBullets();
            }

            yield return new WaitForSeconds(pullBulletSpawnInterval);
        }
    }

    private void CleanupOldPullBullets()
    {
        activePullBullets.RemoveAll(b => b == null);
        int overflow = activePullBullets.Count - pullBulletSpawnCount;

        for (int i = 0; i < overflow; i++)
        {
            if (activePullBullets[i] != null)
            {
                Destroy(activePullBullets[i]);
            }
        }

        if (overflow > 0)
            activePullBullets.RemoveRange(0, overflow);
    }

    #endregion

    #region 끌어당기기 탄환 공격

    private bool CanUsePullBullets()
    {
        return Time.time - lastPullBulletTime >= pullBulletSpawnInterval && // 시간 기반으로만 체크
               !IsPerformingAnyAttack(); // 확률 제한 제거
    }

    private IEnumerator CreatePullBulletsAttack()
    {
        isCreatingPullBullets = true;
        lastPullBulletTime = Time.time;

        Debug.Log("다크 보스: 끌어당기기 탄환 공격! (시간 기반 자동 발동)"); // 디버그 로그 수정

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_PULL_BULLET_CAST_TRIGGER);
        }

        // 차지 이펙트
        GameObject chargeEffect = null;
        if (pullBulletChargeEffect != null)
        {
            chargeEffect = Instantiate(pullBulletChargeEffect, transform.position + Vector3.up * 3f, Quaternion.identity);
        }

        yield return new WaitForSeconds(1f);

        if (chargeEffect != null)
        {
            Destroy(chargeEffect);
        }

        yield return CreatePullBullets();
        isCreatingPullBullets = false;
        UpdateAnimatorParameters(); // 애니메이터 상태 업데이트
    }

    private IEnumerator CreatePullBullets()
    {
        if (playerTransform == null || pullBulletPrefab == null) yield break;

        Vector3 playerPos = playerTransform.position;
        Vector3 bossPos = transform.position;

        // 보스 -> 플레이어 방향
        Vector3 bossToPlayer = (playerPos - bossPos).normalized;

        float angleStep = 120f / (pullBulletSpawnCount - 1);

        for (int i = 0; i < pullBulletSpawnCount; i++)
        {
            float angle = -60f + angleStep * i;

            // 플레이어 뒤쪽에 탄환 생성 (보스와 반대 방향)
            Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * bossToPlayer;
            Vector3 spawnPos = playerPos + rotatedDir * mapRadius;
            spawnPos.y = 2f;

            // 탄환이 보스 방향으로 날아가도록 설정
            Vector3 toBoss = (bossPos - spawnPos).normalized;

            GameObject bullet = Instantiate(pullBulletPrefab, spawnPos, Quaternion.LookRotation(toBoss));
            bullet.GetComponent<Enemy_Final_Boss_PullBullet>()?.Initialize(toBoss, pullBulletSpeed, 15f, bossPos);

            activePullBullets.Add(bullet);
            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region 어둠 바닥 해저드

    private bool CanUseDarkFloor()
    {
        return Time.time - lastDarkFloorTime >= darkFloorCooldown &&
               !IsPerformingAnyAttack() &&
               activeDarkFloorHazard == null;
    }

    private IEnumerator RefreshDarkFloorAttack()
    {
        isCreatingDarkFloor = true;
        lastDarkFloorTime = Time.time;

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_DARK_FLOOR_CAST_TRIGGER);
        }

        yield return new WaitForSeconds(0.5f);

        CreateDarkFloorHazard();

        yield return new WaitForSeconds(0.5f);
        isCreatingDarkFloor = false;
        UpdateAnimatorParameters(); // 애니메이터 상태 업데이트
    }

    private void CreateDarkFloorHazard()
    {
        if (darkFloorHazardPrefab == null) return;

        // 기존 어둠 바닥 제거
        if (activeDarkFloorHazard != null)
        {
            Destroy(activeDarkFloorHazard);
        }

        activeDarkFloorHazard = Instantiate(darkFloorHazardPrefab, transform.position, Quaternion.identity);

        Enemy_Final_Boss_DarkFloor darkFloorScript = activeDarkFloorHazard.GetComponent<Enemy_Final_Boss_DarkFloor>();
        if (darkFloorScript != null)
        {
            darkFloorScript.Initialize(darkFloorDamage, transform);
        }

        // 이펙트 생성
        if (darkFloorEffect != null)
        {
            GameObject effect = Instantiate(darkFloorEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    #endregion

    #region 보스 탄환 공격

    private bool CanUseBossBullets()
    {
        return Time.time - lastBossBulletTime >= bossBulletCooldown &&
               !IsPerformingAnyAttack();
    }

    private IEnumerator PerformBossBulletAttack()
    {
        isShootingBossBullets = true;
        lastBossBulletTime = Time.time;

        if (playerTransform == null)
        {
            isShootingBossBullets = false;
            UpdateAnimatorParameters();
            yield break;
        }

        // 패턴 선택 (랜덤하게 변경하거나 연속 공격 횟수에 따라)
        SelectBulletPattern();

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_BULLET_ATTACK_TRIGGER);
            bossAnimator.SetFloat(ANIM_BULLET_PATTERN, (float)currentBulletPattern); // Float으로 변경
        }

        // 선택된 패턴에 따라 공격 실행
        switch (currentBulletPattern)
        {
            case BulletPattern.Straight:
                yield return StraightBulletAttack();
                break;
            case BulletPattern.Circular:
                yield return CircularBulletAttack();
                break;
            case BulletPattern.Spiral:
                yield return SpiralBulletAttack();
                break;
            case BulletPattern.Fan:
                yield return FanBulletAttack();
                break;
            case BulletPattern.Homing:
                yield return HomingBulletAttack();
                break;
            case BulletPattern.Burst:
                yield return BurstBulletAttack();
                break;
        }

        consecutiveAttacks++;
        isShootingBossBullets = false;
        UpdateAnimatorParameters(); // 애니메이터 상태 업데이트
    }

    private void SelectBulletPattern()
    {
        // 체력이 낮을수록 더 위험한 패턴 사용
        float healthPercent = currentHp / enemyStats.Get(EnemyStatType.MaxHp);

        if (Random.value < patternSwitchChance || consecutiveAttacks >= 3)
        {
            if (healthPercent > 0.7f)
            {
                // 체력 70% 이상: 기본 패턴들
                currentBulletPattern = (BulletPattern)Random.Range(0, 3);
            }
            else if (healthPercent > 0.4f)
            {
                // 체력 40-70%: 중급 패턴들
                currentBulletPattern = (BulletPattern)Random.Range(1, 5);
            }
            else
            {
                // 체력 40% 이하: 모든 패턴 (위험!)
                currentBulletPattern = (BulletPattern)Random.Range(2, 6);
            }

            consecutiveAttacks = 0;
            Debug.Log($"다크 보스 탄환 패턴 변경: {currentBulletPattern}");
        }
    }

    #region 다양한 탄환 패턴들

    private IEnumerator StraightBulletAttack()
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;

        for (int i = 0; i < bossBulletCount; i++)
        {
            CreateBossBullet(dir, bossBulletSpeed);
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator CircularBulletAttack()
    {
        float angleStep = 360f / bossBulletCount;

        for (int i = 0; i < bossBulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            CreateBossBullet(direction, bossBulletSpeed);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator SpiralBulletAttack()
    {
        float totalBullets = bossBulletCount * 1.5f;
        float angleIncrement = 25f;
        float currentAngle = 0f;

        for (int i = 0; i < totalBullets; i++)
        {
            Vector3 direction = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );

            CreateBossBullet(direction, bossBulletSpeed * 0.8f);
            currentAngle += angleIncrement;
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator FanBulletAttack()
    {
        Vector3 playerDir = (playerTransform.position - transform.position).normalized;
        float fanAngle = 60f; // 부채꼴 각도
        float angleStep = fanAngle / (bossBulletCount - 1);

        for (int i = 0; i < bossBulletCount; i++)
        {
            float angle = -fanAngle * 0.5f + angleStep * i;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 direction = rotation * playerDir;

            CreateBossBullet(direction, bossBulletSpeed);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator HomingBulletAttack()
    {
        for (int i = 0; i < bossBulletCount * 0.6f; i++)
        {
            // 유도탄은 적게 발사하지만 위험
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;

            CreateHomingBullet(randomDir);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator BurstBulletAttack()
    {
        // 3번의 폭발적 발사
        for (int burst = 0; burst < 3; burst++)
        {
            // 각 폭발마다 원형으로 발사
            float burstCount = bossBulletCount * 0.7f;
            float angleStep = 360f / burstCount;

            for (int i = 0; i < burstCount; i++)
            {
                float angle = i * angleStep + (burst * 15f); // 약간의 회전 추가
                Vector3 direction = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                CreateBossBullet(direction, bossBulletSpeed * 1.2f);
            }

            yield return new WaitForSeconds(0.8f);
        }
    }

    #endregion

    #endregion

    #region 애니메이터 관리

    /// <summary>
    /// 모든 애니메이터 파라미터를 현재 상태에 맞게 업데이트
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        if (bossAnimator == null) return;

        // Bool 파라미터들
        bossAnimator.SetBool(ANIM_IS_MOVING, moveScript != null && moveScript.IsMoving());
        bossAnimator.SetBool(ANIM_IS_DEAD, isDead);
        bossAnimator.SetBool(ANIM_IS_CREATING_PULL_BULLETS, isCreatingPullBullets);
        bossAnimator.SetBool(ANIM_IS_CREATING_DARK_FLOOR, isCreatingDarkFloor);
        bossAnimator.SetBool(ANIM_IS_SHOOTING_BULLETS, isShootingBossBullets);
        bossAnimator.SetBool(ANIM_IS_DASHING, isDashing);
        bossAnimator.SetBool(ANIM_IS_MELEE_ATTACKING, isMeleeAttacking);

        // Float 파라미터들
        float currentSpeed = moveScript != null && moveScript.IsMoving() ? 5f : 0f;
        bossAnimator.SetFloat(ANIM_MOVE_SPEED, currentSpeed);

        float healthPercent = currentHp / enemyStats.Get(EnemyStatType.MaxHp);
        bossAnimator.SetFloat(ANIM_HEALTH_PERCENT, healthPercent);

        bossAnimator.SetFloat(ANIM_ATTACK_INTENSITY, GetAttackIntensity());

        // 페이즈를 Float으로 대체 (Unity 버전 호환성)
        bossAnimator.SetFloat(ANIM_CURRENT_PHASE, GetCurrentPhase());
    }

    /// <summary>
    /// 현재 페이즈 반환 (체력 기준)
    /// </summary>
    private int GetCurrentPhase()
    {
        float healthPercent = currentHp / enemyStats.Get(EnemyStatType.MaxHp);

        if (healthPercent > 0.7f) return 1;      // 1페이즈: 70% 이상
        else if (healthPercent > 0.4f) return 2; // 2페이즈: 40-70%
        else return 3;                           // 3페이즈: 40% 이하
    }

    /// <summary>
    /// 공격 강도 반환 (체력과 연속 공격 기준)
    /// </summary>
    private float GetAttackIntensity()
    {
        float healthPercent = currentHp / enemyStats.Get(EnemyStatType.MaxHp);
        float baseIntensity = 1f;

        // 체력이 낮을수록 강도 증가
        if (healthPercent <= 0.4f) baseIntensity = 3f;      // 분노 모드
        else if (healthPercent <= 0.7f) baseIntensity = 2f; // 강함
        else baseIntensity = 1f;                             // 보통

        // 연속 공격 시 강도 증가
        if (consecutiveAttacks >= 3) baseIntensity += 0.5f;

        return Mathf.Clamp(baseIntensity, 0f, 3f);
    }

    /// <summary>
    /// 피격 시 호출되는 메서드 (애니메이션 포함)
    /// </summary>
    public override void TakeDamage(float baseDamage, DamageType damageType = DamageType.Physical, ElementType attackerElement = ElementType.Neutral)
    {
        if (isDead) return;

        // 기본 피격 처리
        base.TakeDamage(baseDamage, damageType, attackerElement);

        // 피격 애니메이션
        if (bossAnimator != null && !isDead)
        {
            bossAnimator.SetTrigger(ANIM_HIT_TRIGGER);
        }

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
    }

    #endregion

    #region 정리 및 오버라이드

    private void CleanupModeAttacks()
    {
        // 주기적 스폰 루틴이 비활성화되어 있으므로 정리할 필요 없음
        // if (pullBulletSpawnRoutine != null)
        // {
        //     StopCoroutine(pullBulletSpawnRoutine);
        //     pullBulletSpawnRoutine = null;
        // }

        activePullBullets.ForEach(bullet => { if (bullet != null) Destroy(bullet); });
        activePullBullets.Clear();

        if (activeDarkFloorHazard != null)
        {
            Destroy(activeDarkFloorHazard);
            activeDarkFloorHazard = null;
        }
    }

    protected override void Die()
    {
        // 모든 상태 초기화
        isCreatingPullBullets = false;
        isCreatingDarkFloor = false;
        isShootingBossBullets = false;
        isDashing = false;
        isMeleeAttacking = false;

        // 애니메이터 파라미터 업데이트
        UpdateAnimatorParameters();
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger(ANIM_DIE_TRIGGER);
        }

        CleanupModeAttacks();

        Debug.Log("어둠의 힘이... 사라진다...");
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 1500; // 최종보스답게 높은 경험치
    }

    #endregion

    #region 퍼블릭 접근자 및 유틸리티

    public void SetPullBulletSpawnInterval(float interval) => pullBulletSpawnInterval = interval;

    public int GetActivePullBulletCount()
    {
        activePullBullets.RemoveAll(bullet => bullet == null);
        return activePullBullets.Count;
    }

    public bool IsPerformingSpecialAttack() => IsPerformingAnyAttack();

    // Move 스크립트에서 사용할 수 있는 추가 메서드들
    public float GetDashSpeed() => dashSpeed;
    public float GetMeleeRange() => meleeRange;
    public float GetDashRange() => dashRange;

    #endregion
}