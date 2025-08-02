using UnityEngine;

/// <summary>
/// 마법사 적의 이동 제어 스크립트 (완성된 버전)
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Wizard_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float speed = 1.8f;
    [SerializeField] private float optimalDistance = 8f;    // 공격 사거리

    [Header("마법사 행동 패턴")]
    [SerializeField] private float repositionTime = 3f;    // 위치 재조정 시간 간격

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Wizard enemyWizard;
    private Vector3 lastRepositionTime;
    private Vector3 targetPosition;
    private bool isRepositioning = false;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        enemyWizard = GetComponent<Enemy_Wizard>();

        targetPosition = transform.position;
        lastRepositionTime = Vector3.zero;
    }

    private void FixedUpdate()
    {
        // 기본 조건 체크
        if (enemyWizard == null || target == null || enemyWizard.IsDead())
        {
            StopMovement();
            return;
        }

        // 마법 시전 중에는 이동 중지
        if (enemyWizard.IsCasting)
        {
            StopMovement();
            return;
        }

        // 일반 이동 로직
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (target == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // 최적 거리보다 멀면 접근
        if (distanceToPlayer > optimalDistance)
        {
            MoveTowardsPlayer();
        }
        else if (ShouldReposition())
        {
            // 일정 시간마다 위치 재조정 (측면 이동)
            PerformRepositioning();
        }
        else
        {
            // 최적 거리에서는 천천히 이동하거나 정지
            SlowMovement();
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 dirVector = (target.position - transform.position).normalized;
        dirVector.y = 0;

        Vector3 moveVector = dirVector * speed * Time.fixedDeltaTime;
        Vector3 newPosition = rigid.position + moveVector;
        newPosition.y = rigid.position.y; // Y축 위치 고정
        rigid.MovePosition(newPosition);

        isRepositioning = false;
    }

    private bool ShouldReposition()
    {
        return Time.time > lastRepositionTime.x + repositionTime && !isRepositioning;
    }

    private void PerformRepositioning()
    {
        // 측면으로 이동할 위치 계산
        Vector3 playerToWizard = (transform.position - target.position).normalized;
        Vector3 rightDirection = Vector3.Cross(playerToWizard, Vector3.up);

        // 랜덤하게 좌우 선택
        float sideDirection = Random.value > 0.5f ? 1f : -1f;
        Vector3 sideOffset = rightDirection * sideDirection * Random.Range(3f, 6f);

        // 최적 거리 유지하면서 측면 이동
        Vector3 optimalPositionFromPlayer = target.position + playerToWizard * optimalDistance;
        targetPosition = optimalPositionFromPlayer + sideOffset;
        targetPosition.y = transform.position.y; // Y축 위치 고정

        isRepositioning = true;
        lastRepositionTime.x = Time.time;

        // 재조정 완료까지의 시간 설정
        Invoke(nameof(FinishRepositioning), 2f);
    }

    private void FinishRepositioning()
    {
        isRepositioning = false;
    }

    private void SlowMovement()
    {
        if (isRepositioning)
        {
            // 목표 위치로 이동
            Vector3 dirToTarget = (targetPosition - transform.position).normalized;
            dirToTarget.y = 0;

            Vector3 moveVector = dirToTarget * speed * 0.5f * Time.fixedDeltaTime;
            Vector3 newPosition = rigid.position + moveVector;
            newPosition.y = rigid.position.y; // Y축 위치 고정

            // 목표 위치에 거의 도달했으면 정지
            if (Vector3.Distance(transform.position, targetPosition) < 1f)
            {
                isRepositioning = false;
                return;
            }

            rigid.MovePosition(newPosition);
        }
        else
        {
            // 최적 거리에서는 거의 정지하거나 매우 천천히 이동
            Vector3 dirVector = (target.position - transform.position).normalized;
            dirVector.y = 0;

            Vector3 moveVector = dirVector * speed * 0.2f * Time.fixedDeltaTime;
            Vector3 newPosition = rigid.position + moveVector;
            newPosition.y = rigid.position.y; // Y축 위치 고정
            rigid.MovePosition(newPosition);
        }
    }

    private void StopMovement()
    {
        if (rigid != null)
        {
            // X, Z축 속도만 0으로 설정, Y축은 중력 유지
            rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
        }
    }

    #region 공개 메서드

    /// <summary>
    /// 외부에서 이동 속도 조정 (스탯 시스템 연동용)
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// 최적 거리 설정 (난이도 조정용)
    /// </summary>
    public void SetOptimalDistance(float distance)
    {
        optimalDistance = distance;
    }

    /// <summary>
    /// 강제 위치 재조정 (스킬 사용 후 등)
    /// </summary>
    public void ForceReposition()
    {
        lastRepositionTime.x = 0; // 즉시 재조정 가능하게
        isRepositioning = false;
    }

    #endregion

    #region 디버깅 기능들

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 wizardPos = transform.position;

        // 마법사 위치 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(wizardPos, 0.5f);

        // 최적 거리 (공격 사거리) 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(target.position, optimalDistance);

        // 목표 위치 표시 (재조정 중일 때)
        if (isRepositioning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPosition, 1f);
            Gizmos.DrawLine(wizardPos, targetPosition);
        }

        // 현재 플레이어와의 거리 표시
        Gizmos.color = Color.white;
        Gizmos.DrawLine(wizardPos, target.position);
    }

    #endregion
}