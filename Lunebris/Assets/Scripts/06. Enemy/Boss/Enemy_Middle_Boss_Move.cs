using UnityEngine;

/// <summary>
/// 중간보스 이동 제어 (뱀서류 - 부드러운 추적)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy_Middle_Boss))]
public class Enemy_Middle_Boss_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float smoothMoveSpeed = 2f; // 부드러운 이동 속도
    [SerializeField] private float rotationSpeed = 3f; // 회전 속도
    [SerializeField] private float stopDistance = 1.5f; // 플레이어와의 최소 거리
    [SerializeField] private float acceleration = 5f; // 가속도
    [SerializeField] private float deceleration = 8f; // 감속도

    [Header("뱀서류 특성")]
    [SerializeField] private bool useSmoothMovement = true; // 부드러운 이동 사용
    [SerializeField] private float pathSmoothness = 0.8f; // 경로 부드러움 (0~1)
    [SerializeField] private float minMoveThreshold = 0.1f; // 최소 이동 임계값

    // 컴포넌트 참조
    private Enemy_Middle_Boss bossScript;
    private Rigidbody bossRigidbody;
    private Transform playerTransform;

    // 이동 상태
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetDirection = Vector3.zero;
    private Vector3 smoothDirection = Vector3.zero;
    private bool isMoving = false;
    private float currentSpeed = 0f;

    // 디버그
    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;

    public bool IsMoving => isMoving;

    #region Unity Lifecycle

    private void Awake()
    {
        // 컴포넌트 참조
        bossScript = GetComponent<Enemy_Middle_Boss>();
        bossRigidbody = GetComponent<Rigidbody>();

        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // Rigidbody 설정
        if (bossRigidbody != null)
        {
            bossRigidbody.freezeRotation = true; // 회전은 스크립트로 제어
            bossRigidbody.drag = 2f; // 적절한 저항력
        }
    }

    private void Start()
    {
        // 초기화 검증
        if (bossScript == null)
        {
            Debug.LogError($"{name}: Enemy_Middle_Boss 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (bossRigidbody == null)
        {
            Debug.LogWarning($"{name}: Rigidbody가 없어서 Transform 기반 이동을 사용합니다.");
        }

        if (playerTransform == null)
        {
            Debug.LogWarning($"{name}: 플레이어를 찾을 수 없습니다!");
        }

        Debug.Log($"{name}: 중간보스 이동 시스템 초기화 완료!");
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    #endregion

    #region 이동 제어

    private void UpdateMovement()
    {
        // 기본 조건 체크
        if (!CanMove())
        {
            StopMovement();
            return;
        }

        // 플레이어 방향 계산
        CalculateTargetDirection();

        // 이동 실행
        if (useSmoothMovement)
        {
            PerformSmoothMovement();
        }
        else
        {
            PerformDirectMovement();
        }

        // 회전 처리
        HandleRotation();

        // 이동 상태 업데이트
        UpdateMovingState();

        // 디버그 정보
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    private bool CanMove()
    {
        // 보스 스크립트가 없으면 이동 불가
        if (bossScript == null) return false;

        // 특수 공격 중이면 이동 불가
        if (bossScript.IsPerformingSpecialAttack()) return false;

        // 플레이어가 없으면 이동 불가
        if (playerTransform == null) return false;

        // 보스가 죽었으면 이동 불가
        if (bossScript.IsDead()) return false;

        return true;
    }

    private void CalculateTargetDirection()
    {
        if (playerTransform == null) return;

        // 플레이어와의 거리 계산
        Vector3 directionToPlayer = (playerTransform.position - transform.position);
        directionToPlayer.y = 0; // Y축 무시
        float distanceToPlayer = directionToPlayer.magnitude;

        // 너무 가까우면 이동하지 않음
        if (distanceToPlayer <= stopDistance)
        {
            targetDirection = Vector3.zero;
            return;
        }

        // 정규화된 방향 벡터
        targetDirection = directionToPlayer.normalized;
    }

    private void PerformSmoothMovement()
    {
        // 부드러운 방향 전환
        smoothDirection = Vector3.Slerp(smoothDirection, targetDirection,
            Time.fixedDeltaTime * pathSmoothness * 10f);

        // 속도 계산
        float targetSpeed = targetDirection.magnitude > minMoveThreshold ? smoothMoveSpeed : 0f;

        if (targetSpeed > currentSpeed)
        {
            // 가속
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
                acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // 감속
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
                deceleration * Time.fixedDeltaTime);
        }

        // 최종 속도 벡터
        Vector3 velocity = smoothDirection * currentSpeed;

        // 이동 적용
        ApplyMovement(velocity);
    }

    private void PerformDirectMovement()
    {
        // 직접적인 이동
        float targetSpeed = targetDirection.magnitude > minMoveThreshold ? smoothMoveSpeed : 0f;
        Vector3 velocity = targetDirection * targetSpeed;

        currentSpeed = targetSpeed;
        ApplyMovement(velocity);
    }

    private void ApplyMovement(Vector3 velocity)
    {
        if (bossRigidbody != null)
        {
            // Rigidbody 기반 이동
            Vector3 newVelocity = velocity;
            newVelocity.y = bossRigidbody.velocity.y; // Y축 속도는 유지 (중력)
            bossRigidbody.velocity = newVelocity;
        }
        else
        {
            // Transform 기반 이동
            transform.position += velocity * Time.fixedDeltaTime;
        }

        currentVelocity = velocity;
    }

    private void HandleRotation()
    {
        // 이동 중일 때만 회전
        if (currentSpeed > minMoveThreshold && smoothDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(smoothDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                Time.fixedDeltaTime * rotationSpeed);
        }
    }

    private void UpdateMovingState()
    {
        // 이동 상태 업데이트
        isMoving = currentSpeed > minMoveThreshold;
    }

    private void StopMovement()
    {
        // 이동 정지
        targetDirection = Vector3.zero;
        smoothDirection = Vector3.zero;

        // 부드러운 감속
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);

        if (bossRigidbody != null)
        {
            Vector3 velocity = bossRigidbody.velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            bossRigidbody.velocity = velocity;
        }

        currentVelocity = Vector3.zero;
        isMoving = false;
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 강제로 이동 정지
    /// </summary>
    public void ForceStop()
    {
        StopMovement();
    }

    /// <summary>
    /// 이동 속도 임시 변경
    /// </summary>
    public void SetTemporarySpeed(float newSpeed, float duration = -1f)
    {
        smoothMoveSpeed = newSpeed;

        if (duration > 0f)
        {
            Invoke(nameof(ResetSpeed), duration);
        }
    }

    /// <summary>
    /// 이동 속도 원래대로 복구
    /// </summary>
    public void ResetSpeed()
    {
        smoothMoveSpeed = 2f; // 기본값으로 복구
    }

    /// <summary>
    /// 특정 위치로 강제 이동 (짧은 시간)
    /// </summary>
    public void MoveToPosition(Vector3 targetPos, float duration = 1f)
    {
        StartCoroutine(MoveToPositionCoroutine(targetPos, duration));
    }

    #endregion

    #region 코루틴

    private System.Collections.IEnumerator MoveToPositionCoroutine(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 부드러운 보간
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

            if (bossRigidbody != null)
            {
                bossRigidbody.MovePosition(currentPos);
            }
            else
            {
                transform.position = currentPos;
            }

            yield return null;
        }
    }

    #endregion

    #region 디버그

    private void ShowDebugInfo()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        Debug.Log($"[{name}] 이동상태: {isMoving}, 속도: {currentSpeed:F2}, " +
                 $"플레이어 거리: {distanceToPlayer:F2}, 특수공격중: {bossScript.IsPerformingSpecialAttack()}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 정지 거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // 현재 이동 방향 표시
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, smoothDirection * 3f);
        }

        // 플레이어로의 직선 표시
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }

    #endregion
}