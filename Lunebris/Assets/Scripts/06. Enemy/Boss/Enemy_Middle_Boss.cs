using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 중간보스 몬스터 
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Middle_Boss : Enemy_Base
{
    [Header("그랩 공격 설정")]
    [SerializeField] private float grabRange = 12f; // 그랩 투사체 발사 거리
    [SerializeField] private float grabDamage = 60f; // 그랩 데미지
    [SerializeField] private float grabCooldown = 6f; // 그랩 쿨다운
    [SerializeField] private float grabDuration = 1.5f; // 그랩 지속시간
    [SerializeField] private float pullForce = 8f; // 끌어당기는 힘
    [SerializeField] private float grabProjectileSpeed = 15f; // 그랩 투사체 속도
    [SerializeField] private Transform grabFirePoint; // 그랩 투사체 발사점

    [Header("총알 공격 설정")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint; // 총알 발사점
    [SerializeField] private float bulletSpeed = 10f; // 총알 속도
    [SerializeField] private int bulletCount = 6; // 총알 개수 (중간보스용으로 적게)
    [SerializeField] private float bulletCooldown = 4f; // 총알 쿨다운
    [SerializeField] private float bulletRange = 8f; // 총알 사용 거리

    [Header("장판 공격 설정")]
    [SerializeField] private GameObject floorHazardPrefab; // 장판 프리팹
    [SerializeField] private float floorHazardDamage = 40f; // 장판 데미지
    [SerializeField] private float floorHazardDuration = 4f; // 장판 지속시간
    [SerializeField] private float floorHazardCooldown = 8f; // 장판 쿨다운
    [SerializeField] private float floorHazardRadius = 3f; // 장판 반지름
    [SerializeField] private int maxFloorHazards = 6; // 최대 동시 장판 개수
    [SerializeField] private float floorHazardRange = 15f; // 장판 생성 가능 거리
    [SerializeField] private int hazardsPerCast = 3; // 한 번에 생성할 장판 개수
    [SerializeField] private float minDistanceFromPlayer = 2f; // 플레이어로부터 최소 거리
    [SerializeField] private float minDistanceBetweenHazards = 4f; // 장판 간 최소 거리

    [Header("이펙트 및 프리팹")]
    [SerializeField] private GameObject grabProjectilePrefab; // 그랩 투사체 프리팹

    // 보스 상태
    private bool isGrabbing = false; // 투사체 발사 중
    private bool isShooting = false; // 총알 발사 중
    private bool isCreatingFloorHazard = false; // 장판 생성 중

    // 공격 타이밍
    private float lastGrabTime;
    private float lastBulletTime;
    private float lastFloorHazardTime;

    // 현재 그랩된 플레이어 정보
    private Transform grabbedPlayer = null;
    private bool isPlayerBeingPulled = false; // 플레이어 끌어당기는 중
    private float pullStartTime;
    private float currentPullDuration;

    // 현재 활성화된 장판 개수 추적
    private int currentFloorHazardCount = 0;

    // Move 스크립트에서 참조할 수 있는 프로퍼티들
    public bool IsGrabbing => isGrabbing; // 투사체 발사 중
    public bool IsShooting => isShooting;
    public bool IsPlayerBeingPulled => isPlayerBeingPulled; // 플레이어 끌어당기는 중
    public bool IsCreatingFloorHazard => isCreatingFloorHazard; // 장판 생성 중

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

        // 총알 발사점이 없으면 자신의 위치 사용
        if (firePoint == null)
            firePoint = transform;

        // 그랩 발사점이 없으면 자신의 위치 사용
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

        // 총알 발사 중이면 이동 제한
        if (isShooting || isCreatingFloorHazard)
        {
            return;
        }

        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 거리에 따른 공격 패턴 선택 (중간보스는 단순)
        if (distanceToPlayer >= grabRange * 0.8f && CanUseGrab())
        {
            // 원거리에서 그랩 투사체 발사
            StartCoroutine(PerformGrabAttack());
        }
        else if (distanceToPlayer <= floorHazardRange && CanUseFloorHazard())
        {
            // 장판 공격 우선 사용 (더 위험한 공격)
            StartCoroutine(PerformFloorHazardAttack());
        }
        else if (distanceToPlayer <= bulletRange && CanUseBullets())
        {
            // 근거리에서 총알 공격
            StartCoroutine(PerformBulletAttack());
        }
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Boss_Move에서 처리
    }

    protected override void PerformAttack()
    {
        // 기본 공격은 사용하지 않음
    }

    #region 그랩 투사체 공격

    private bool CanUseGrab()
    {
        return Time.time - lastGrabTime >= grabCooldown && !isGrabbing && !isShooting && !isPlayerBeingPulled && !isCreatingFloorHazard;
    }

    private IEnumerator PerformGrabAttack()
    {
        isGrabbing = true;
        lastGrabTime = Time.time;

        // 그랩 투사체 발사 준비 (짧은 준비 시간)
        yield return new WaitForSeconds(0.3f);

        // 플레이어 방향으로 그랩 투사체 발사
        if (playerTransform != null)
        {
            FireGrabProjectile();
        }

        yield return new WaitForSeconds(0.2f);
        isGrabbing = false;
    }

    private void FireGrabProjectile()
    {
        if (grabProjectilePrefab == null)
        {
            return;
        }

        if (playerTransform == null) return;

        // 플레이어 방향으로 투사체 발사
        Vector3 direction = (playerTransform.position - grabFirePoint.position).normalized;
        direction.y = 0; // Y축 무시

        // 그랩 투사체 생성
        GameObject grabProjectile = Instantiate(grabProjectilePrefab, grabFirePoint.position, Quaternion.LookRotation(direction));

        // 투사체에 속도 적용
        Rigidbody projectileRb = grabProjectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.velocity = direction * grabProjectileSpeed;
        }

        // 투사체 초기화
        Enemy_Middle_Boss_Grab grabScript = grabProjectile.GetComponent<Enemy_Middle_Boss_Grab>();
        if (grabScript != null)
        {
            grabScript.Initialize(this, pullForce, grabDuration, grabDamage);
        }

    }

    /// <summary>
    /// 그랩 투사체가 플레이어에게 맞았을 때 호출되는 콜백
    /// </summary>
    public void OnGrabProjectileHit(Transform player, float force, float duration)
    {
        if (isPlayerBeingPulled) return; // 이미 끌어당기고 있으면 무시

        grabbedPlayer = player;
        isPlayerBeingPulled = true;
        pullStartTime = Time.time;
        currentPullDuration = duration;


        // 끌어당기기 코루틴 시작
        StartCoroutine(PullPlayerCoroutine(force, duration));
    }

    private IEnumerator PullPlayerCoroutine(float force, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && isPlayerBeingPulled && grabbedPlayer != null)
        {
            // 보스 쪽으로 플레이어를 끌어당기기 (Y축 고정)
            Vector3 directionToBoss = (transform.position - grabbedPlayer.position).normalized;
            directionToBoss.y = 0; // Y축 방향 제거

            Vector3 pullPosition = grabbedPlayer.position + directionToBoss * force * Time.deltaTime;
            pullPosition.y = grabbedPlayer.position.y; // Y축 위치 고정

            // 보스에게 너무 가까이 가지 않도록 제한 (최소 2m 거리 유지)
            float distanceToBoss = Vector3.Distance(new Vector3(pullPosition.x, 0, pullPosition.z),
                                                  new Vector3(transform.position.x, 0, transform.position.z));
            if (distanceToBoss > 2f)
            {
                grabbedPlayer.position = pullPosition;
            }

            // 0.5초마다 추가 데미지
            if (elapsed % 0.5f < Time.deltaTime && elapsed > 0.1f) // 첫 데미지는 투사체에서 주니까 제외
            {
                DealDamageToPlayer(DamageType.Magical);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 끌어당기기 종료
        isPlayerBeingPulled = false;
        grabbedPlayer = null;
    }

    private void UpdatePullBehavior()
    {
        // 끌어당기기 중에는 움직이지 않음
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
        return Time.time - lastBulletTime >= bulletCooldown && !isGrabbing && !isShooting && !isPlayerBeingPulled && !isCreatingFloorHazard;
    }

    private IEnumerator PerformBulletAttack()
    {
        isShooting = true;
        lastBulletTime = Time.time;

        // 시전 시간
        yield return new WaitForSeconds(0.5f);

        // 총알 발사
        FireBulletsInCircle();

        yield return new WaitForSeconds(0.5f);
        isShooting = false;
    }

    private void FireBulletsInCircle()
    {
        if (bulletPrefab == null)
        {
            return;
        }

        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 총알 생성
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

            // 총알에 속도 적용
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * bulletSpeed;
            }

            // 총알 데미지 설정
            Enemy_Middle_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Middle_Boss_Bullet>();
            if (bulletScript != null)
            {
                float bulletDamage = enemyStats.Get(EnemyStatType.MagicalDamage) * 0.4f;
                bulletScript.Initialize(bulletDamage, DamageType.Magical);
            }

            // 5초 후 총알 삭제
            Destroy(bullet, 5f);
        }

    }

    #endregion

    #region 장판 공격

    private bool CanUseFloorHazard()
    {
        return Time.time - lastFloorHazardTime >= floorHazardCooldown &&
               !isGrabbing && !isShooting && !isPlayerBeingPulled && !isCreatingFloorHazard &&
               currentFloorHazardCount + hazardsPerCast <= maxFloorHazards; // 한 번에 생성할 개수를 고려
    }

    private IEnumerator PerformFloorHazardAttack()
    {
        isCreatingFloorHazard = true;
        lastFloorHazardTime = Time.time;

        // 장판 생성 시전 시간 (여러 개 생성하므로 조금 더 길게)
        yield return new WaitForSeconds(1.2f);

        // 플레이어 위치 주변에 여러 장판 랜덤 생성
        if (playerTransform != null)
        {
            CreateFloorHazard();
        }

        yield return new WaitForSeconds(0.3f);
        isCreatingFloorHazard = false;
    }

    private void CreateFloorHazard()
    {
        if (floorHazardPrefab == null || playerTransform == null)
        {
            return;
        }

        // 현재 생성 가능한 장판 개수 계산
        int hazardsToCreate = Mathf.Min(hazardsPerCast, maxFloorHazards - currentFloorHazardCount);

        if (hazardsToCreate <= 0) return;

        List<Vector3> createdPositions = new List<Vector3>();

        for (int i = 0; i < hazardsToCreate; i++)
        {
            Vector3 hazardPosition = FindValidHazardPosition(createdPositions);

            if (hazardPosition != Vector3.zero) // 유효한 위치를 찾았다면
            {
                // 장판 생성
                GameObject floorHazard = Instantiate(floorHazardPrefab, hazardPosition, Quaternion.identity);

                // 장판 초기화
                Enemy_Middle_Boss_FloorHazard hazardScript = floorHazard.GetComponent<Enemy_Middle_Boss_FloorHazard>();
                if (hazardScript != null)
                {
                    hazardScript.Initialize(floorHazardDamage, floorHazardDuration, floorHazardRadius, this);
                }

                // 현재 장판 개수 증가
                currentFloorHazardCount++;

                // 생성된 위치 기록
                createdPositions.Add(hazardPosition);

                // 지속시간 후 자동 삭제
                StartCoroutine(DestroyFloorHazardAfterDuration(floorHazard, floorHazardDuration));
            }
        }
    }

    /// <summary>
    /// 유효한 장판 생성 위치 찾기
    /// </summary>
    private Vector3 FindValidHazardPosition(List<Vector3> existingPositions)
    {
        const int maxAttempts = 20; // 최대 시도 횟수

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 플레이어 중심으로 원형 범위 내에서 랜덤 위치 생성
            Vector2 randomCircle = Random.insideUnitCircle * floorHazardRange;
            Vector3 candidatePosition = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            candidatePosition.y = 0; // Y축은 지면에 고정

            // 유효성 검사
            if (IsValidHazardPosition(candidatePosition, existingPositions))
            {
                return candidatePosition;
            }
        }

        // 유효한 위치를 찾지 못한 경우
        Debug.LogWarning("유효한 장판 위치를 찾지 못했습니다.");
        return Vector3.zero;
    }

    /// <summary>
    /// 장판 위치가 유효한지 검사
    /// </summary>
    private bool IsValidHazardPosition(Vector3 position, List<Vector3> existingPositions)
    {
        // 1. 플레이어로부터 최소 거리 체크
        float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
        if (distanceToPlayer < minDistanceFromPlayer)
        {
            return false;
        }

        // 2. 보스로부터 너무 가깝지 않게 (최소 1m)
        float distanceToBoss = Vector3.Distance(position, transform.position);
        if (distanceToBoss < 1f)
        {
            return false;
        }

        // 3. 이미 생성된 장판들과의 거리 체크
        foreach (Vector3 existingPos in existingPositions)
        {
            float distance = Vector3.Distance(position, existingPos);
            if (distance < minDistanceBetweenHazards)
            {
                return false;
            }
        }

        // 4. 맵 경계 체크 (옵션: 필요시 활성화)
        // if (!IsWithinMapBounds(position))
        // {
        //     return false;
        // }

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

    /// <summary>
    /// 장판이 파괴될 때 호출되는 콜백 (외부에서 호출 가능)
    /// </summary>
    public void OnFloorHazardDestroyed()
    {
        currentFloorHazardCount = Mathf.Max(0, currentFloorHazardCount - 1);
    }

    #endregion

    #region 오버라이드 메서드

    protected override void Die()
    {

        // 모든 상태 초기화
        isGrabbing = false;
        isShooting = false;
        isPlayerBeingPulled = false;
        isCreatingFloorHazard = false;
        grabbedPlayer = null;
        currentFloorHazardCount = 0;

        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 300; // 중간보스니까 경험치도 적당히
    }

    #endregion

    #region 퍼블릭 접근자

    public bool IsPerformingSpecialAttack() => isGrabbing || isShooting || isPlayerBeingPulled || isCreatingFloorHazard;

    #endregion
}