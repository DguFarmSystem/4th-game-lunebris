using UnityEngine;

/// <summary>
/// 중간보스 이동 처리 스크립트
/// 단순하게 플레이어를 추적하면서 공격 중에는 정지
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Enemy_Middle_Boss_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float rotationSpeed = 4f; // 회전 속도
    [SerializeField] private float stoppingDistance = 2f; // 정지 거리
    [SerializeField] private float circleRadius = 5f; // 원형 이동 반지름

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Middle_Boss bossScript;

    private void Start()
    {
        // 타겟 찾기 (플레이어)
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        bossScript = GetComponent<Enemy_Middle_Boss>();

        if (bossScript == null)
        {
            Debug.LogError("Enemy_Boss 스크립트를 찾을 수 없습니다!");
        }
    }

    private void FixedUpdate()
    {
        // 기본 조건 체크
        if (bossScript == null || target == null || bossScript.IsDead())
        {
            StopMovement();
            return;
        }

        // 보스 상태에 따른 이동 처리
        if (ShouldMove())
        {
            HandleNormalMovement();
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
    /// 이동 가능 여부 판단
    /// </summary>
    private bool ShouldMove()
    {
        // 그랩 투사체 발사 중이면 이동 불가
        if (bossScript.IsGrabbing)
        {
            return false;
        }

        // 총알 발사 중이면 이동 불가
        if (bossScript.IsShooting)
        {
            return false;
        }

        // 플레이어를 끌어당기는 중이면 이동 불가
        if (bossScript.IsPlayerBeingPulled)
        {
            return false;
        }

        // 장판 생성 중이면 이동 불가
        if (bossScript.IsCreatingFloorHazard)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region 일반 이동

    /// <summary>
    /// 일반 추적 이동 (중간보스 스타일)
    /// </summary>
    private void HandleNormalMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // 중간보스는 단순하게 행동
        if (distanceToPlayer > stoppingDistance)
        {
            // 플레이어에게 접근
            MoveTowardsPlayer();
        }
        else
        {
            // 가까이 있을 때는 천천히 원형 이동
            CircleAroundPlayer();
        }
    }

    /// <summary>
    /// 플레이어에게 접근
    /// </summary>
    private void MoveTowardsPlayer()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Y축 이동 제거

        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
        Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;

        Vector3 newPosition = rigid.position + moveVector;
        newPosition.y = rigid.position.y; // Y축 위치 고정
        rigid.MovePosition(newPosition);
    }

    /// <summary>
    /// 플레이어 주변을 원형으로 이동 (중간보스 스타일)
    /// </summary>
    private void CircleAroundPlayer()
    {
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0;

        // 플레이어 중심으로 원형 이동 (중간보스는 느리게)
        Vector3 circleDirection = new Vector3(-toPlayer.z, 0, toPlayer.x).normalized;

        // 가끔 방향 변환 (1% 확률)
        if (Random.Range(0, 100) < 1)
        {
            circleDirection = -circleDirection;
        }

        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed) * 0.7f; // 원형 이동은 느리게
        Vector3 moveVector = circleDirection * moveSpeed * Time.fixedDeltaTime;

        Vector3 newPosition = rigid.position + moveVector;
        newPosition.y = rigid.position.y; // Y축 위치 고정
        rigid.MovePosition(newPosition);
    }

    #endregion

    #region 회전 처리

    /// <summary>
    /// 플레이어를 향해 회전
    /// </summary>
    private void LookAtTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Y축 회전 제거

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            rigid.velocity = Vector3.zero;
        }
    }

    #endregion

    #region 디버그

    private void OnDrawGizmos()
    {
        if (target != null)
        {
            // 플레이어와의 거리 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);

            // 정지 거리 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            // 원형 이동 반지름 시각화
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
    }

    #endregion
}