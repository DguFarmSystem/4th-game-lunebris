using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 최종보스 빛 모드 이동 제어 스크립트
/// 플레이어 추격, 거리 조절, 공격 상태별 이동 패턴 관리
/// Idle 상태일 때는 모든 이동 중지
/// </summary>
[RequireComponent(typeof(Enemy_Final_Boss_Light))]
public class Enemy_Final_Boss_Light_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float optimalCombatDistance = 6f;
    [SerializeField] private float circleDistance = 7f;
    [SerializeField] private float circleSpeed = 2f;

    [Header("대쉬 공격 설정")]
    [SerializeField] private float dashSpeed = 20f;         // 대쉬 속도
    [SerializeField] private float dashDuration = 0.5f;      // 대쉬 지속 시간
    [SerializeField] private float dashCooldown = 8f;        // 대쉬 쿨타임
    private bool isDashing = false;
    private float lastDashTime = 0f;

    [Header("고급 설정")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float maxDistanceFromPlayer = 20f;
    [SerializeField] private bool enableTeleportWhenTooFar = true;
    [SerializeField] private float teleportDistance = 8f;

    [Header("Idle 상태 설정")]
    [SerializeField] private bool stopMovementInIdle = true;
    [SerializeField] private bool stopRotationInIdle = true;

    // 컴포넌트 참조
    private Enemy_Final_Boss_Light bossScript;
    private Transform playerTransform;
    private Rigidbody rb;
    private Enemy_Base enemyBase;
    private Animator moveAnimator;

    // 이동 상태
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentMoveSpeed;
    private bool wasMovingLastFrame = false;

    // 원형 이동 관련
    private float circleAngle = 0f;
    private bool isCircling = false;
    private Vector3 circleCenter;

    void Awake()
    {
        bossScript = GetComponent<Enemy_Final_Boss_Light>();
        enemyBase = GetComponent<Enemy_Base>();
        rb = GetComponent<Rigidbody>();
        moveAnimator = GetComponent<Animator>();
        if (moveAnimator == null) moveAnimator = GetComponentInChildren<Animator>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        currentMoveSpeed = moveSpeed;
        targetPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (enemyBase == null || enemyBase.GetCurrentHp() <= 0) return;
        UpdateMovement();
    }

    void Update()
    {
        if (enemyBase == null || enemyBase.GetCurrentHp() <= 0) return;

        UpdateRotation();

        bool isCurrentlyMoving = IsMoving();
        if (isCurrentlyMoving != wasMovingLastFrame)
        {
            UpdateMovementAnimation(isCurrentlyMoving);
            wasMovingLastFrame = isCurrentlyMoving;
        }

        // 대쉬 쿨타임 체크
        if (!isDashing && Time.time - lastDashTime >= dashCooldown)
        {
            // 특정 조건(예: 플레이어와의 거리)에서 대쉬 시작
            if (Vector3.Distance(transform.position, playerTransform.position) > optimalCombatDistance * 1.5f)
            {
                StartCoroutine(PerformDashAttack());
            }
        }
    }

    private void UpdateMovement()
    {
        if (playerTransform == null) return;

        if (stopMovementInIdle && bossScript.IsInIdleState)
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (!bossScript.IsInIdleState && CheckMaxDistanceLimit(distanceToPlayer)) return;
        if (isDashing)
        {
            ApplyDashMovement();
            return;
        }

        if (ShouldStopMoving())
        {
            StopMovement();
        }
        else if (ShouldCirclePlayer(distanceToPlayer))
        {
            CircleAroundPlayer();
        }
        else if (ShouldChasePlayer(distanceToPlayer))
        {
            ChasePlayer();
        }
        else
        {
            ApproachPlayer();
        }

        ApplyMovement();
    }

    /// <summary>
    /// 대쉬 공격 코루틴
    /// </summary>
    private IEnumerator PerformDashAttack()
    {
        isDashing = true;
        isCircling = false;
        lastDashTime = Time.time;

        // 대쉬 방향 설정 (플레이어 위치를 향해)
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        currentVelocity = directionToPlayer * dashSpeed;

        // 대쉬 애니메이션 재생 (필요시)
        // PlayDashAnimation();

        // 대쉬 지속 시간만큼 대기
        yield return new WaitForSeconds(dashDuration);

        // 대쉬 종료
        isDashing = false;
        // 이동 속도를 일반 속도로 되돌림
        currentVelocity = currentVelocity.normalized * moveSpeed;
    }

    /// <summary>
    /// 대쉬 중일 때 이동을 적용
    /// </summary>
    private void ApplyDashMovement()
    {
        rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
    }

    private bool ShouldStopMoving()
    {
        return bossScript.IsInIdleState || bossScript.IsPerformingSpecialAttack;
    }

    private bool ShouldCirclePlayer(float distance)
    {
        return !bossScript.IsInIdleState &&
               distance >= optimalCombatDistance * 0.8f &&
               distance <= circleDistance &&
               !bossScript.IsPerformingSpecialAttack &&
               Random.value < 0.4f;
    }

    private bool ShouldChasePlayer(float distance)
    {
        return !bossScript.IsInIdleState && distance > optimalCombatDistance;
    }

    private bool CheckMaxDistanceLimit(float currentDistance)
    {
        if (currentDistance > maxDistanceFromPlayer)
        {
            if (enableTeleportWhenTooFar)
            {
                TeleportNearPlayer();
            }
            else
            {
                ForceApproachPlayer();
            }
            return true;
        }
        return false;
    }

    private void TeleportNearPlayer()
    {
        Vector3 playerPosition = playerTransform.position;
        Vector3 directionFromPlayer = (transform.position - playerPosition).normalized;

        if (directionFromPlayer == Vector3.zero)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            directionFromPlayer = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        }

        Vector3 teleportPosition = playerPosition + directionFromPlayer * teleportDistance;
        teleportPosition.y = playerPosition.y;
        SetPosition(teleportPosition);
    }

    private void ForceApproachPlayer()
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * (moveSpeed * 2f);
        currentVelocity = targetVelocity;
    }

    private void ChasePlayer()
    {
        if (bossScript.IsInIdleState)
        {
            StopMovement();
            return;
        }

        isCircling = false;
        targetPosition = playerTransform.position;
        currentMoveSpeed = moveSpeed * 1.1f;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * currentMoveSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void ApproachPlayer()
    {
        if (bossScript.IsInIdleState)
        {
            StopMovement();
            return;
        }

        isCircling = false;
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * (moveSpeed * 0.6f);
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void CircleAroundPlayer()
    {
        if (bossScript.IsInIdleState)
        {
            StopMovement();
            return;
        }

        if (!isCircling)
        {
            isCircling = true;
            circleCenter = playerTransform.position;
            Vector3 offset = transform.position - circleCenter;
            circleAngle = Mathf.Atan2(offset.z, offset.x);
        }

        circleCenter = Vector3.Lerp(circleCenter, playerTransform.position, Time.fixedDeltaTime * 2f);
        circleAngle += circleSpeed * Time.fixedDeltaTime;

        float targetX = circleCenter.x + Mathf.Cos(circleAngle) * circleDistance;
        float targetZ = circleCenter.z + Mathf.Sin(circleAngle) * circleDistance;
        targetPosition = new Vector3(targetX, transform.position.y, targetZ);

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 targetVelocity = directionToTarget * (moveSpeed * 0.8f);
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void StopMovement()
    {
        isCircling = false;
        targetPosition = transform.position;

        if (bossScript != null && bossScript.IsInIdleState)
        {
            currentVelocity = Vector3.zero;
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
        }
    }

    private void ApplyMovement()
    {
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);

        float maxSpeed = moveSpeed * 1.2f;
        if (rb.velocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    private void UpdateRotation()
    {
        if (playerTransform == null) return;
        if (stopRotationInIdle && bossScript.IsInIdleState) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateMovementAnimation(bool isMoving)
    {
        if (moveAnimator == null) return;

        bool shouldShowMoving = isMoving && !bossScript.IsInIdleState;
        moveAnimator.SetBool("isMoving", shouldShowMoving);
        moveAnimator.SetFloat("moveSpeed", shouldShowMoving ? currentMoveSpeed : 0f);
    }

    #region 공개 메서드

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
        rb.velocity = Vector3.zero;
        currentVelocity = Vector3.zero;
    }

    public void SetTemporaryMoveSpeed(float newSpeed, float duration)
    {
        StartCoroutine(TemporarySpeedChange(newSpeed, duration));
    }

    private IEnumerator TemporarySpeedChange(float newSpeed, float duration)
    {
        float originalSpeed = moveSpeed;
        moveSpeed = newSpeed;
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
    }

    public void ApplyKnockback(Vector3 force)
    {
        if (bossScript.IsInIdleState) return;
        rb.AddForce(force, ForceMode.Impulse);
    }

    public bool IsMoving()
    {
        if (bossScript != null && bossScript.IsInIdleState) return false;
        return currentVelocity.magnitude > 0.1f;
    }

    public void StartCircling()
    {
        if (bossScript.IsInIdleState) return;
        isCircling = true;
        circleCenter = playerTransform.position;
    }

    public void StopCircling()
    {
        isCircling = false;
    }

    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    #endregion
}