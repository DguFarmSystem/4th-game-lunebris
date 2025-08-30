using UnityEngine;
using System.Collections;
using Enemy; // Enemy 네임스페이스 추가

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

        if (distanceToPlayer <= followStartDistance)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime * 10f);

            if (distanceToPlayer <= idleDistance)
            {
                currentState = SummonState.Idle;
            }
        }
        else
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

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

    // [최종 수정] 적 탐지 로직을 GetComponentInParent를 사용하도록 전체 수정
    private void FindClosestEnemy(int count)
    {
        float closestDistanceSqr = float.MaxValue;
        Transform newTarget = null;
        Enemy_Base newTargetScript = null; // 찾은 스크립트를 임시 저장

        for (int i = 0; i < count; i++)
        {
            // 1. 콜라이더의 부모까지 거슬러 올라가 Enemy_Base 스크립트를 찾습니다.
            Enemy_Base enemy = nearbyEnemies[i].GetComponentInParent<Enemy_Base>();

            // 2. 스크립트가 있고, 살아있는 적인지 확인합니다.
            if (enemy != null && !enemy.IsDead())
            {
                // 3. 기존에 찾은 가장 가까운 적보다 더 가까운지 확인합니다.
                float distanceSqr = (transform.position - enemy.transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    newTarget = enemy.transform; // 타겟의 transform을 저장
                    newTargetScript = enemy;     // 타겟의 스크립트를 저장
                }
            }
        }

        // 4. 가장 가까운 적을 찾았다면, 최종 타겟으로 설정합니다.
        if (newTarget != null)
        {
            currentTarget = newTarget;
            targetEnemyScript = newTargetScript; // 저장해둔 스크립트를 할당 (GetComponent를 또 호출할 필요 없음)
        }
    }

    private void AttackTarget()
    {
        if (currentTarget == null || targetEnemyScript == null || !currentTarget.gameObject.activeInHierarchy || targetEnemyScript.IsDead() || Vector3.Distance(transform.position, currentTarget.position) > detectionRadius)
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
            // ElementType은 소환수의 속성에 맞게 Lux 또는 다른 것으로 지정해야 할 수 있습니다.
            targetEnemyScript.TakeDamage(attackDamage, DamageType.Magical, ElementType.Lux);

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
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, idleDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerTransform.position, followStartDistance);
        }

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