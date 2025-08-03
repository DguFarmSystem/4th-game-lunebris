using UnityEngine;

/// <summary>
/// 최종보스 이동 처리 스크립트 - 중간보스 패턴 적용
/// 모든 최종보스 모드에서 공통으로 사용 가능
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Final_Boss_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float rotationSpeed = 2f; // 회전 속도 (최종보스는 느리게)
    [SerializeField] private float stoppingDistance = 8f; // 정지 거리 (최종보스는 넓게)
    [SerializeField] private float circleRadius = 12f; // 원형 이동 반지름

    [Header("최종보스 전용 설정")]
    [SerializeField] private bool allowMovement = true; // 이동 허용 여부
    [SerializeField] private float moveSpeedMultiplier = 0.5f; // 이동속도 배율 (최종보스는 느리게)

    private Transform target;
    private Rigidbody rigid;

    // 최종보스 스크립트들 참조 (다형성 활용)
    private Enemy_Base bossScript;
    private Enemy_Final_Boss_Neutral neutralBoss;
    private Enemy_Final_Boss_Dark darkBoss;
    // lightBoss 참조 제거 - 기존 파일과 충돌 방지

    // 이동 패턴
    private Vector3 circleCenter;
    private float circleAngle = 0f;
    private bool isCircling = false;

    private void Start()
    {
        // 타겟 찾기 (플레이어)
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        bossScript = GetComponent<Enemy_Base>();

        // Rigidbody가 없으면 경고 메시지
        if (rigid == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Rigidbody 컴포넌트가 필요합니다!");
            return;
        }

        // 각 최종보스 타입별 스크립트 찾기
        neutralBoss = GetComponent<Enemy_Final_Boss_Neutral>();
        darkBoss = GetComponent<Enemy_Final_Boss_Dark>();
        // lightBoss 참조 제거 - 기존 파일과 충돌 방지

        if (bossScript == null)
        {
            Debug.LogError("Enemy_Base 스크립트를 찾을 수 없습니다!");
        }

        // Rigidbody 설정
        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            rigid.drag = 8f; // 최종보스는 높은 저항
            rigid.angularDrag = 8f;
        }

        // 원형 이동 중심점 설정
        circleCenter = transform.position;
    }

    private void FixedUpdate()
    {
        // 기본 조건 체크
        if (bossScript == null || target == null || bossScript.IsDead() || rigid == null)
        {
            if (rigid != null) StopMovement();
            return;
        }

        // 이동 허용 여부 체크
        if (!allowMovement)
        {
            StopMovement();
            LookAtTarget();
            return;
        }

        // 각 보스 타입별 이동 조건 체크
        if (ShouldMove())
        {
            HandleBossMovement();
        }
        else
        {
            StopMovement();
        }

        // 항상 플레이어를 바라보기
        LookAtTarget();
    }

    #region 이동 조건 체크

    /// <summary>
    /// 각 보스 타입별 이동 가능 여부 판단
    /// </summary>
    private bool ShouldMove()
    {
        // 중립 보스 (오브 관리)
        if (neutralBoss != null)
        {
            return !neutralBoss.IsTransitioning && !neutralBoss.IsSpawningOrbs;
        }

        // 어둠 보스
        if (darkBoss != null)
        {
            return !darkBoss.IsPerformingSpecialAttack();
        }

        // 라이트 보스는 기존 파일 사용으로 특별한 제한 없음
        // 또는 Enemy_Base의 기본 동작 사용

        // 기본적으로 이동 허용
        return true;
    }

    #endregion

    #region 보스 이동 패턴

    /// <summary>
    /// 최종보스 이동 처리 (중간보스보다 더 위엄있게)
    /// </summary>
    private void HandleBossMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer > stoppingDistance)
        {
            // 플레이어에게 접근 (최종보스는 천천히)
            MoveTowardsPlayer();
            isCircling = false;
        }
        else
        {
            // 가까이 있을 때는 원형 이동
            CircleAroundPlayer();
        }
    }

    /// <summary>
    /// 플레이어에게 천천히 접근
    /// </summary>
    private void MoveTowardsPlayer()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Y축 이동 제거

        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed) * moveSpeedMultiplier;
        Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;

        Vector3 newPosition = rigid.position + moveVector;
        newPosition.y = rigid.position.y; // Y축 위치 고정
        rigid.MovePosition(newPosition);
    }

    /// <summary>
    /// 플레이어 주변을 위엄있게 원형 이동
    /// </summary>
    private void CircleAroundPlayer()
    {
        if (!isCircling)
        {
            // 원형 이동 시작
            circleCenter = target.position;
            Vector3 toTarget = transform.position - circleCenter;
            toTarget.y = 0;
            circleAngle = Mathf.Atan2(toTarget.z, toTarget.x);
            isCircling = true;
        }

        // 원형 이동 계산
        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed) * moveSpeedMultiplier;
        float angularSpeed = moveSpeed / circleRadius;

        circleAngle += angularSpeed * Time.fixedDeltaTime;

        // 새로운 위치 계산
        Vector3 offset = new Vector3(
            Mathf.Cos(circleAngle) * circleRadius,
            0,
            Mathf.Sin(circleAngle) * circleRadius
        );

        Vector3 targetPosition = circleCenter + offset;
        targetPosition.y = transform.position.y; // Y축 위치 고정

        // 부드럽게 이동
        Vector3 moveDirection = (targetPosition - rigid.position).normalized;
        Vector3 moveVector = moveDirection * moveSpeed * Time.fixedDeltaTime;

        rigid.MovePosition(rigid.position + moveVector);

        // 원형 이동 중심점 업데이트 (플레이어가 움직일 경우)
        Vector3 newCenter = Vector3.Lerp(circleCenter, target.position, Time.fixedDeltaTime * 0.5f);
        circleCenter = newCenter;
    }

    #endregion

    #region 회전 처리

    /// <summary>
    /// 플레이어를 향해 천천히 회전 (최종보스다운 위엄)
    /// </summary>
    private void LookAtTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Y축 회전 제거

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            targetRotation = Quaternion.Euler(eulerAngles);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region 이동 제어

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            Vector3 vel = rigid.velocity;
            vel.x = 0;
            vel.z = 0;
            rigid.velocity = vel;
        }

        isCircling = false;
    }

    /// <summary>
    /// 특정 위치로 강제 이동 (특별한 경우에 사용)
    /// </summary>
    public void MoveTo(Vector3 targetPosition)
    {
        if (!allowMovement) return;

        StopMovement();
        StartCoroutine(MoveToPositionCoroutine(targetPosition));
    }

    private System.Collections.IEnumerator MoveToPositionCoroutine(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        targetPosition.y = startPos.y; // Y축 위치 고정
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, t);
            transform.position = currentPos;

            yield return null;
        }
    }

    public void StopMove()
    {
        StopMovement();
    }

    #endregion

    #region 설정 메서드

    /// <summary>
    /// 이동 허용/금지 설정
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        allowMovement = enabled;
        if (!enabled)
        {
            StopMovement();
        }
    }

    /// <summary>
    /// 이동속도 배율 설정
    /// </summary>
    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
    }

    #endregion

    #region 디버그

    private void OnDrawGizmos()
    {
        if (target != null)
        {
            // 플레이어와의 거리 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);

            // 정지 거리 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            // 원형 이동 반지름 시각화
            if (isCircling)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(circleCenter, circleRadius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 이동 영역 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, circleRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }

    #endregion
}