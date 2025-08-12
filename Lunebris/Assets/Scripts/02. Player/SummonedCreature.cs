using UnityEngine;
using System.Collections;

public enum SummonState
{
    Following,
    Attacking,
    Idle
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SummonedCreature : MonoBehaviour
{
    [Header("AI 설정")]
    [SerializeField] private float idleDistance = 4f;
    [SerializeField] private float followStartDistance = 6f;
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float attackRange = 3f;

    [Header("소환수 스탯")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float summonDuration = 20f;
    [SerializeField] private float attackWindUpTime = 0.3f;

    [Header("시각 효과")]
    [SerializeField] private GameObject attackEffectPrefab;

    private Transform playerTransform;
    private Transform currentTarget;
    private Enemy_Base targetEnemyScript;
    private SummonState currentState;
    private Rigidbody rb;

    private float lastAttackTime;
    private bool isPerformingAttack = false;

    private Collider[] nearbyEnemies = new Collider[20];

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        GetComponent<Collider>().isTrigger = true;

        currentState = SummonState.Idle;

        Destroy(gameObject, summonDuration);
        StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            if (currentTarget == null)
            {
                CheckForEnemies();
            }

            switch (currentState)
            {
                case SummonState.Idle:
                    Idle();
                    break;
                case SummonState.Following:
                    FollowPlayer();
                    break;
                case SummonState.Attacking:
                    AttackTarget();
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Idle()
    {
        rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime * 10f);

        if (Vector3.Distance(transform.position, playerTransform.position) > followStartDistance)
        {
            currentState = SummonState.Following;
        }
    }

    private void FollowPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // [수정] 플레이어가 추적 시작 거리(파란 원) 안으로 들어오면 바로 이동을 멈춥니다.
        if (distanceToPlayer <= followStartDistance)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime * 10f);

            // [수정] 대기 거리(녹색 원) 안으로 완전히 들어왔을 때만 Idle 상태로 전환합니다.
            if (distanceToPlayer <= idleDistance)
            {
                currentState = SummonState.Idle;
            }
        }
        else // 플레이어가 추적 시작 거리 밖에 있을 때만 이동합니다.
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 이동할 때만 부드럽게 회전합니다.
            Quaternion targetRotation = Quaternion.LookRotation(playerTransform.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void CheckForEnemies()
    {
        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, nearbyEnemies);
        if (enemyCount > 0)
        {
            FindClosestEnemy(enemyCount);
            if (currentTarget != null)
            {
                currentState = SummonState.Attacking;
            }
        }
    }

    private void FindClosestEnemy(int count)
    {
        float closestDistance = float.MaxValue;
        Transform newTarget = null;
        for (int i = 0; i < count; i++)
        {
            if (nearbyEnemies[i].CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, nearbyEnemies[i].transform.position);
                if (distance < closestDistance)
                {
                    Enemy_Base enemy = nearbyEnemies[i].GetComponent<Enemy_Base>();
                    if (enemy != null && !enemy.IsDead())
                    {
                        closestDistance = distance;
                        newTarget = nearbyEnemies[i].transform;
                    }
                }
            }
        }
        if (newTarget != null)
        {
            currentTarget = newTarget;
            targetEnemyScript = currentTarget.GetComponent<Enemy_Base>();
        }
    }

    private void AttackTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || targetEnemyScript.IsDead() || Vector3.Distance(transform.position, currentTarget.position) > detectionRadius)
        {
            currentTarget = null;
            targetEnemyScript = null;
            currentState = SummonState.Idle;
            rb.velocity = Vector3.zero;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget > attackRange)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;
        }
        else
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime * 5f);
            if (Time.time > lastAttackTime + attackCooldown && !isPerformingAttack)
            {
                StartCoroutine(AttackSequence());
            }
        }
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private IEnumerator AttackSequence()
    {
        isPerformingAttack = true;

        yield return new WaitForSeconds(attackWindUpTime);

        if (currentTarget != null && !targetEnemyScript.IsDead() && Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
        {
            transform.LookAt(currentTarget);
            targetEnemyScript.TakeDamage(attackDamage, Enemy.DamageType.Magical, Enemy.ElementType.Light);

            if (attackEffectPrefab != null)
            {
                Collider targetCollider = currentTarget.GetComponent<Collider>();
                Vector3 effectPosition = targetCollider != null ? targetCollider.bounds.center : currentTarget.position;
                Instantiate(attackEffectPrefab, effectPosition, Quaternion.identity);
            }
        }

        lastAttackTime = Time.time;
        isPerformingAttack = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, idleDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, followStartDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}