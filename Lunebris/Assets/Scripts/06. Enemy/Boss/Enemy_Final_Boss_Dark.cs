using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 탄막 패턴 종류
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
/// 최종보스 - 어둠 모드 (끌어당김 탄환, 어둠 바닥, 보스 탄환) - 중간보스 패턴 적용
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Final_Boss_Dark : Enemy_Base
{
    [Header("끌어당김 탄환")]
    [SerializeField] private GameObject pullBulletPrefab;
    [SerializeField] private float pullBulletSpeed = 10f;
    [SerializeField] private int pullBulletSpawnCount = 16;
    [SerializeField] private float mapRadius = 20f;
    [SerializeField] private float pullBulletSpawnInterval = 8f;
    [SerializeField] private float pullBulletRange = 25f;

    [Header("어둠 바닥")]
    [SerializeField] private GameObject darkFloorHazardPrefab;
    [SerializeField] private float darkFloorDamage = 30f;
    [SerializeField] private float darkFloorCooldown = 5f;

    [Header("보스 탄환")]
    [SerializeField] private GameObject bossBulletPrefab;
    [SerializeField] private float bossBulletDamage = 50f;
    [SerializeField] private float bossBulletCooldown = 3f;
    [SerializeField] private int bossBulletCount = 8;
    [SerializeField] private float bossBulletRange = 15f;
    [SerializeField] private float bossBulletSpeed = 12f;

    [Header("탄막 패턴 설정")]
    [SerializeField] private BulletPattern[] bulletPatterns;
    [SerializeField] private float patternSwitchChance = 0.3f; // 30% 확률로 패턴 변경

    [Header("이펙트")]
    [SerializeField] private GameObject pullBulletChargeEffect;
    [SerializeField] private GameObject darkFloorEffect;

    // 상태 플래그 (중간보스 패턴)
    private bool isCreatingPullBullets = false;
    private bool isCreatingDarkFloor = false;
    private bool isShootingBossBullets = false;

    // 공격 타이밍 관리
    private float lastPullBulletTime = 0f;
    private float lastDarkFloorTime = 0f;
    private float lastBossBulletTime = 0f;

    // 활성 오브젝트 관리
    private List<GameObject> activePullBullets = new();
    private GameObject activeDarkFloorHazard;
    private Coroutine pullBulletSpawnRoutine;

    // 탄막 패턴 관리
    private BulletPattern currentBulletPattern = BulletPattern.Straight;
    private int consecutiveAttacks = 0;

    // 공개 프로퍼티 (Move 스크립트에서 참조 가능)
    public bool IsCreatingPullBullets => isCreatingPullBullets;
    public bool IsCreatingDarkFloor => isCreatingDarkFloor;
    public bool IsShootingBossBullets => isShootingBossBullets;

    protected override void Awake()
    {
        enemyType = EnemyType.FinalBoss;
        elementType = ElementType.Tenebris;
        primaryDamageType = DamageType.Magical;
        enemyName = "Dark Sovereign";

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

        // 주기적인 끌어당김 탄환 시스템 시작
        pullBulletSpawnRoutine = StartCoroutine(SpawnPullBulletsPeriodically());

        // 연속 공격 루틴 시작
        StartCoroutine(ContinuousAttackRoutine());
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 공격 우선순위 (거리별로 중간보스 패턴 적용)
        if (distanceToPlayer >= pullBulletRange * 0.8f && CanUsePullBullets())
        {
            // 원거리에서 끌어당김 탄환 사용
            StartCoroutine(CreatePullBulletsAttack());
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
    }

    protected override void UpdateMovement()
    {
        // Enemy_Final_Boss_Dark_Move에서 처리
    }

    protected override void PerformAttack()
    {
        // 기본 공격은 사용하지 않음
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
        return isCreatingPullBullets || isCreatingDarkFloor || isShootingBossBullets;
    }

    #endregion

    #region 탄환 생성 헬퍼 메서드

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
            CreateTempBossBullet(direction, speed);
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
            CreateTempHomingBullet(initialDirection);
        }
    }

    private void CreateTempBossBullet(Vector3 dir, float speed)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "TempBossBullet";
        bullet.transform.position = transform.position;
        bullet.transform.localScale = Vector3.one * 0.6f;

        var renderer = bullet.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"))
        {
            color = new Color(0.3f, 0f, 0.5f)
        };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.magenta);
        renderer.material = mat;

        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = dir * speed;

        bullet.GetComponent<Collider>().isTrigger = true;
        bullet.AddComponent<Enemy_Temp_Damage_Projectile>().Initialize(bossBulletDamage, "보스 탄환");

        Destroy(bullet, 5f);
    }

    private void CreateTempHomingBullet(Vector3 initialDirection)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "TempHomingBullet";
        bullet.transform.position = transform.position;
        bullet.transform.localScale = Vector3.one * 0.8f;

        var renderer = bullet.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"))
        {
            color = Color.red
        };
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 3f);
        renderer.material = mat;

        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = initialDirection * bossBulletSpeed * 0.8f;

        bullet.GetComponent<Collider>().isTrigger = true;
        bullet.AddComponent<Enemy_Temp_Damage_Projectile>().Initialize(bossBulletDamage * 1.5f, "유도 탄환");

        Destroy(bullet, 8f);
    }

    #endregion

    #region 주기적 끌어당김 탄환 시스템

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

    #region 끌어당김 탄환 공격

    private bool CanUsePullBullets()
    {
        return Time.time - lastPullBulletTime >= pullBulletSpawnInterval * 0.5f &&
               !IsPerformingAnyAttack();
    }

    private IEnumerator CreatePullBulletsAttack()
    {
        isCreatingPullBullets = true;
        lastPullBulletTime = Time.time;

        // 차징 이펙트
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

        yield return new WaitForSeconds(0.5f);

        CreateDarkFloorHazard();

        yield return new WaitForSeconds(0.5f);
        isCreatingDarkFloor = false;
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
            yield break;
        }

        // 패턴 선택 (랜덤하게 변경하거나 연속 공격 횟수에 따라)
        SelectBulletPattern();

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
            Debug.Log($"다크 보스 탄막 패턴 변경: {currentBulletPattern}");
        }
    }

    #region 다양한 탄막 패턴들

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

    #region 정리 및 오버라이드

    private void CleanupModeAttacks()
    {
        if (pullBulletSpawnRoutine != null)
        {
            StopCoroutine(pullBulletSpawnRoutine);
            pullBulletSpawnRoutine = null;
        }

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

    #endregion

    #endregion
}