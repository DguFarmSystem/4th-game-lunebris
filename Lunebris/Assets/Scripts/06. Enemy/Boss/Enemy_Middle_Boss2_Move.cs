using UnityEngine;

/// <summary>
/// 중간보스 2 이동 처리 스크립트
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Enemy_Middle_Boss2_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float stoppingDistance = 2f;

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Middle_Boss2 bossScript;
    private bool isDashingMovement = false;
    private bool isMoving = false; // 애니메이션용 이동 상태

    // 공개 프로퍼티
    public bool IsMoving => isMoving;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        bossScript = GetComponent<Enemy_Middle_Boss2>();

        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            rigid.drag = 5f;
            rigid.angularDrag = 5f;
        }

        Debug.Log($"{name}: 중간보스2 이동 시스템 초기화 완료!");
    }

    private void FixedUpdate()
    {
        if (bossScript == null || target == null || bossScript.IsDead())
        {
            StopMovement();
            return;
        }

        if (bossScript.IsDashing)
        {
            HandleDashMovement();
        }
        else if (ShouldMove())
        {
            HandleNormalMovement();
        }
        else
        {
            StopMovement();
        }

        if (!bossScript.IsDashing)
        {
            LookAtTarget();
        }
    }

    private bool ShouldMove()
    {
        return !bossScript.IsDashWarning &&
               !bossScript.IsShooting &&
               !bossScript.IsRailgunActive; // 레일건 사용 중에도 이동 제한
    }

    private void HandleNormalMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer > stoppingDistance)
        {
            MoveTowardsPlayer();
            isMoving = true;
        }
        else
        {
            CircleAroundPlayer();
            isMoving = true;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;

        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
        Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;

        rigid.MovePosition(rigid.position + moveVector);
    }

    private void CircleAroundPlayer()
    {
        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0;

        Vector3 circleDirection = new Vector3(-toPlayer.z, 0, toPlayer.x).normalized;

        // 랜덤하게 방향 전환
        if (Random.Range(0, 100) < 2)
        {
            circleDirection = -circleDirection;
        }

        float moveSpeed = bossScript.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed) * 0.8f;
        Vector3 moveVector = circleDirection * moveSpeed * Time.fixedDeltaTime;

        rigid.MovePosition(rigid.position + moveVector);
    }

    private void HandleDashMovement()
    {
        if (!isDashingMovement)
        {
            isDashingMovement = true;
        }

        Vector3 dashDirection = bossScript.DashDirection;
        dashDirection.y = 0;
        dashDirection = dashDirection.normalized;

        float dashSpeed = bossScript.DashSpeed;
        Vector3 moveVector = dashDirection * dashSpeed * Time.fixedDeltaTime;

        rigid.MovePosition(rigid.position + moveVector);

        // 대시 중에는 이동 방향으로 즉시 회전
        if (dashDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dashDirection);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }

        // 대시 중에는 이동 상태가 아님 (대시 애니메이션이 따로 있음)
        isMoving = false;
    }

    private void LookAtTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;

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

    private void StopMovement()
    {
        if (rigid != null)
        {
            Vector3 vel = rigid.velocity;
            vel.x = 0;
            vel.z = 0;
            rigid.velocity = vel;
        }

        if (isDashingMovement && !bossScript.IsDashing)
        {
            isDashingMovement = false;
        }

        isMoving = false;
    }

    #region 공개 메서드

    /// <summary>
    /// 강제로 이동 정지
    /// </summary>
    public void ForceStop()
    {
        StopMovement();
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

            if (rigid != null)
            {
                rigid.MovePosition(currentPos);
            }
            else
            {
                transform.position = currentPos;
            }

            yield return null;
        }
    }

    #endregion

    #region 충돌 처리

    private void OnTriggerEnter(Collider other)
    {
        if (bossScript.IsDashing)
        {
            bossScript.OnDashCollision(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (bossScript.IsDashing)
        {
            if (IsMonsterCollider(collision.collider)) return;

            if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
            {
                bossScript.OnDashCollision(collision.collider);
                StopMovement();
            }
        }
    }

    private bool IsMonsterCollider(Collider col)
    {
        if (col == null) return false;

        string[] monsterTags = { "Enemy", "Boss", "MiddleBoss" };
        foreach (string tag in monsterTags)
        {
            if (col.CompareTag(tag)) return true;
        }

        return false;
    }

    #endregion

    #region 디버그

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || target == null) return;

        // 정지 거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 플레이어로의 직선 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);

        // 이동 상태 표시
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }

    #endregion
}