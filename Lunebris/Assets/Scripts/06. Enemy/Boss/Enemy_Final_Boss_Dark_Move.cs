using UnityEngine;
using Enemy;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 최종보스 어둠 모드 이동 제어 스크립트
/// 플레이어 추격, 거리 조절, 공격 상태별 이동 패턴 관리
/// </summary>
[RequireComponent(typeof(Enemy_Final_Boss_Dark))]
public class Enemy_Final_Boss_Dark_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f; // 속도 감소
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float stoppingDistance = 2f; // 최소 거리 더 감소 (멀어지지 않도록)
    [SerializeField] private float maxChaseDistance = 25f; // 최대 추격 거리 증가

    [Header("고급 이동 설정")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float avoidanceRadius = 1f; // 장애물 회피 반경 최소화
    [SerializeField] private LayerMask obstacleLayerMask = 1; // 장애물 레이어

    [Header("거리별 행동 설정")]
    [SerializeField] private float optimalCombatDistance = 4f; // 이상적인 전투 거리 더 감소
    [SerializeField] private float repositionThreshold = 1f; // 재배치 임계값 더 감소
    [SerializeField] private float circleDistance = 5f; // 원형 이동 거리 대폭 감소
    [SerializeField] private float circleSpeed = 1.5f; // 원형 이동 속도 감소

    // 컴포넌트 참조
    private Enemy_Final_Boss_Dark bossScript;
    private Transform playerTransform;
    private Rigidbody rb;
    private Enemy_Base enemyBase;
    private Animator moveAnimator; // 애니메이터 참조 추가

    // 이동 상태
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentMoveSpeed;
    private bool isRepositioning = false;
    private bool wasMovingLastFrame = false; // 이전 프레임 이동 상태

    // 원형 이동 관련
    private float circleAngle = 0f;
    private bool isCircling = false;
    private Vector3 circleCenter;

    // 장애물 회피 관련
    private Vector3 avoidanceDirection = Vector3.zero;
    private float avoidanceTimer = 0f;

    void Awake()
    {
        bossScript = GetComponent<Enemy_Final_Boss_Dark>();
        enemyBase = GetComponent<Enemy_Base>();
        rb = GetComponent<Rigidbody>();

        // 애니메이터 컴포넌트 가져오기
        moveAnimator = GetComponent<Animator>();
        if (moveAnimator == null)
        {
            moveAnimator = GetComponentInChildren<Animator>();
        }

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void Start()
    {
        // 플레이어 참조 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

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

        // 이동 상태 변화 감지 및 애니메이터 업데이트
        bool isCurrentlyMoving = IsMoving();
        if (isCurrentlyMoving != wasMovingLastFrame)
        {
            UpdateMovementAnimation(isCurrentlyMoving);
            wasMovingLastFrame = isCurrentlyMoving;
        }
    }

    private void UpdateMovement()
    {
        if (playerTransform == null) return;

        // 대시 중에는 이동 제어하지 않음 (대시 로직이 직접 제어)
        if (bossScript.IsDashing) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 상태별 이동 로직 (멀어지는 패턴 제거)
        if (ShouldStopMoving())
        {
            // 특정 공격 중에는 이동 중지
            StopMovement();
        }
        else if (ShouldCirclePlayer(distanceToPlayer))
        {
            // 원형 이동 (중거리에서)
            CircleAroundPlayer();
        }
        else if (ShouldChasePlayer(distanceToPlayer))
        {
            // 플레이어 추격
            ChasePlayer();
        }
        else
        {
            // 기본적으로 플레이어에게 접근 (멀어지지 않음)
            ApproachPlayer();
        }

        // 장애물 회피 (최소화)
        HandleObstacleAvoidance();

        // 실제 이동 적용
        ApplyMovement();
    }

    private bool ShouldStopMoving()
    {
        // 정지 조건을 더 제한적으로 (보스가 더 활발하게 움직이도록)
        return bossScript.IsMeleeAttacking ||
               (bossScript.IsShootingBossBullets && Random.value < 0.5f) || // 50% 확률로 사격 중 정지
               (bossScript.IsCreatingDarkFloor && Random.value < 0.7f); // 70% 확률로 어둠 바닥 생성 중 정지
    }

    private bool ShouldCirclePlayer(float distance)
    {
        // 매우 가까운 거리에서만 원형 이동 (멀어지지 않도록)
        return distance > stoppingDistance &&
               distance <= circleDistance &&
               !bossScript.IsCreatingPullBullets &&
               Random.value < 0.3f; // 30% 확률로 원형 이동
    }

    private bool ShouldChasePlayer(float distance)
    {
        // 적절한 거리보다 멀 때 추격
        return distance > optimalCombatDistance;
    }

    private void ChasePlayer()
    {
        isCircling = false;
        targetPosition = playerTransform.position;
        currentMoveSpeed = moveSpeed * 1.2f; // 추격 시 더 빠르게

        // 플레이어 방향으로 이동
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * currentMoveSpeed;

        // 부드러운 가속
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void ApproachPlayer()
    {
        isCircling = false;

        // 항상 플레이어에게 천천히 접근 (멀어지지 않음)
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 targetVelocity = directionToPlayer * (moveSpeed * 0.6f); // 천천히 접근

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void CircleAroundPlayer()
    {
        if (!isCircling)
        {
            isCircling = true;
            circleCenter = playerTransform.position;

            // 현재 위치를 기준으로 각도 계산
            Vector3 offset = transform.position - circleCenter;
            circleAngle = Mathf.Atan2(offset.z, offset.x);
        }

        // 플레이어 위치 계속 업데이트 (멀어지지 않도록)
        circleCenter = Vector3.Lerp(circleCenter, playerTransform.position, Time.fixedDeltaTime * 2f);

        // 원형 이동 (매우 작은 원)
        circleAngle += circleSpeed * Time.fixedDeltaTime;

        float targetX = circleCenter.x + Mathf.Cos(circleAngle) * circleDistance;
        float targetZ = circleCenter.z + Mathf.Sin(circleAngle) * circleDistance;
        targetPosition = new Vector3(targetX, transform.position.y, targetZ);

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 targetVelocity = directionToTarget * (moveSpeed * 0.7f);

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void StopMovement()
    {
        isCircling = false;
        targetPosition = transform.position;

        // 부드러운 감속
        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
    }

    private void HandleObstacleAvoidance()
    {
        // 장애물 회피를 최소화 (멀어지지 않도록)
        Vector3 forward = transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, forward, out hit, avoidanceRadius, obstacleLayerMask))
        {
            // 장애물 발견 시 최소한의 회피만
            Vector3 avoidDirection = Vector3.Cross(forward, Vector3.up).normalized;

            // 좌우 중 더 적절한 방향 선택
            Vector3 leftCheck = transform.position + avoidDirection * avoidanceRadius;
            Vector3 rightCheck = transform.position - avoidDirection * avoidanceRadius;

            if (Physics.CheckSphere(leftCheck, 0.5f, obstacleLayerMask))
            {
                avoidanceDirection = -avoidDirection; // 오른쪽으로 회피
            }
            else
            {
                avoidanceDirection = avoidDirection; // 왼쪽으로 회피
            }

            avoidanceTimer = 0.5f; // 0.5초간만 회피 (시간 단축)
        }

        // 회피 적용 (약하게)
        if (avoidanceTimer > 0f)
        {
            currentVelocity += avoidanceDirection * (moveSpeed * 0.3f); // 회피 강도 감소
            avoidanceTimer -= Time.fixedDeltaTime;
        }
    }

    private void ApplyMovement()
    {
        // Y축 속도는 중력에 맡김
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        // 리지드바디에 속도 적용
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);

        // 최대 속도 제한 (속도 제한 감소)
        if (rb.velocity.magnitude > moveSpeed * 1.1f)
        {
            Vector3 limitedVelocity = rb.velocity.normalized * moveSpeed * 1.1f;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    private void UpdateRotation()
    {
        if (playerTransform == null) return;

        // 대시 중에는 회전하지 않음
        if (bossScript.IsDashing) return;

        // 플레이어 방향으로 회전
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f; // Y축 회전만

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 이동 애니메이션 업데이트
    /// </summary>
    private void UpdateMovementAnimation(bool isMoving)
    {
        if (moveAnimator == null) return;

        moveAnimator.SetBool("isMoving", isMoving);
        moveAnimator.SetFloat("moveSpeed", isMoving ? currentMoveSpeed : 0f);
    }

    #region 공개 메서드

    /// <summary>
    /// 특정 위치로 즉시 이동 (대시 등에서 사용)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
        rb.velocity = Vector3.zero;
        currentVelocity = Vector3.zero;
    }

    /// <summary>
    /// 이동 속도 일시적 변경
    /// </summary>
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

    /// <summary>
    /// 강제로 특정 방향으로 밀어내기 (넉백 등)
    /// </summary>
    public void ApplyKnockback(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

    /// <summary>
    /// 현재 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        return currentVelocity.magnitude > 0.1f;
    }

    /// <summary>
    /// 원형 이동 강제 시작
    /// </summary>
    public void StartCircling()
    {
        isCircling = true;
        circleCenter = playerTransform.position;
    }

    /// <summary>
    /// 원형 이동 중지
    /// </summary>
    public void StopCircling()
    {
        isCircling = false;
    }

    #endregion

    #region 디버그

    void OnDrawGizmosSelected()
    {
        // 정지 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 최적 전투 거리 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalCombatDistance);

        // 최대 추격 거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance);

        // 회피 반경 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, avoidanceRadius);

        // 원형 이동 표시
        if (isCircling && playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(circleCenter, circleDistance);
        }

        // 타겟 위치 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(targetPosition, 1f);

        // 현재 속도 벡터 표시
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, currentVelocity);

        // 애니메이터 상태 표시 (에디터에서만)
#if UNITY_EDITOR
        if (moveAnimator != null)
        {
            string movementState = "Unknown";
            float distance = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 0f;
            
            if (bossScript.IsDashing) movementState = "Dashing";
            else if (ShouldStopMoving()) movementState = "Stopped";
            else if (ShouldCirclePlayer(distance)) movementState = "Circling";
            else if (ShouldChasePlayer(distance)) movementState = "Chasing";
            else movementState = "Approaching"; // KeepDistance 대신 Approaching
            
            Handles.Label(transform.position + Vector3.up * 4f, 
                $"State: {movementState}\nDistance: {distance:F1}m\nSpeed: {currentMoveSpeed:F1}\nNever backs away!");
        }
#endif
    }

    #endregion
}
