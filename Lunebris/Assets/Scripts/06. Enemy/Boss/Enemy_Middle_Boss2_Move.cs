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
        return !bossScript.IsDashWarning && !bossScript.IsShooting;
    }

    private void HandleNormalMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer > stoppingDistance)
        {
            MoveTowardsPlayer();
        }
        else
        {
            CircleAroundPlayer();
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

        if (dashDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dashDirection);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
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
    }

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
}