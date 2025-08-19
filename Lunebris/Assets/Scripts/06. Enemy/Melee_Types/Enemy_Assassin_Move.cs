using UnityEngine;

/// <summary>
/// 대쉬 기능을 고려한 암살자 이동 처리 스크립트
/// 대쉬 중이나 대쉬 준비 중에는 일반 이동을 하지 않음
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Assassin_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float rotationSpeed = 5f; // 회전 속도
    [SerializeField] private float stoppingDistance = 0.5f; // 정지 거리

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Assassin assassin;

    private void Start()
    {
        // 플레이어 타겟 찾기
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        assassin = GetComponent<Enemy_Assassin>();

        if (assassin == null)
        {
            Debug.LogError($"{gameObject.name}: Enemy_Assassin 컴포넌트를 찾을 수 없습니다!");
        }
    }

    private void FixedUpdate()
    {
        if (assassin == null || target == null || assassin.IsDead())
        {
            StopMovement();
            return;
        }

        // **대쉬 중이면 Move 스크립트는 아무것도 하지 않음**
        if (assassin.IsDashing)
        {
            return; // 대쉬 중에는 Enemy_Assassin에서 이동을 담당
        }

        // **대쉬 준비 중이면 멈춤**
        if (assassin.IsPreparingDash)
        {
            StopMovement(); // 준비 중에만 멈춤
            return;
        }

        // 이동 가능한지 체크
        if (ShouldMove())
        {
            NormalMove();
        }
        else
        {
            StopMovement();
        }
    }

    /// <summary>
    /// 이동 가능 여부 판단
    /// </summary>
    private bool ShouldMove()
    {
        // 공격 중이면 이동 불가
        if (assassin.IsAttacking)
        {
            return false;
        }

        // 대쉬 중이면 이동 불가 (대쉬가 이동을 담당)
        if (assassin.IsDashing)
        {
            return false;
        }

        // 대쉬 준비 중이면 이동 불가
        if (assassin.IsPreparingDash)
        {
            return false;
        }

        // 타겟과의 거리가 너무 가까우면 이동 불가
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= stoppingDistance)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 일반 이동 (플레이어 추적)
    /// </summary>
    private void NormalMove()
    {
        Vector3 dirVector = target.position - transform.position;
        dirVector.y = 0; // Y축 이동 제거

        if (dirVector.magnitude > 0.1f) // 너무 가까우면 이동하지 않음
        {
            // 이동 속도 스탯 사용
            float moveSpeed = assassin.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
            Vector3 moveVector = dirVector.normalized * moveSpeed * Time.fixedDeltaTime;

            Vector3 newPosition = rigid.position + moveVector;
            newPosition.y = rigid.position.y; // Y축 위치 고정
            rigid.MovePosition(newPosition);

            // 플레이어 방향으로 회전
            LookAtTarget(dirVector);
        }
    }

    /// <summary>
    /// 목표 방향으로 회전
    /// </summary>
    private void LookAtTarget(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            // X, Z축 속도만 0으로 설정, Y축은 중력 유지
            rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
        }
    }

    /// <summary>
    /// 디버그용 정보 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 정지 거리 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 타겟으로의 방향 표시
        if (target != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    #region 상태 확인 메서드

    /// <summary>
    /// 현재 이동 중인지 확인
    /// </summary>
    public bool IsMoving()
    {
        if (rigid == null) return false;

        Vector3 horizontalVelocity = new Vector3(rigid.velocity.x, 0, rigid.velocity.z);
        return horizontalVelocity.magnitude > 0.1f;
    }

    /// <summary>
    /// 타겟과의 거리 반환
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (target == null) return float.MaxValue;
        return Vector3.Distance(transform.position, target.position);
    }

    /// <summary>
    /// 이동 가능 상태인지 확인
    /// </summary>
    public bool CanMove()
    {
        return ShouldMove();
    }

    #endregion
}