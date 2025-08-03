using UnityEngine;
using Enemy;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 중간보스 2 - 대시 공격과 투사체 공격을 사용하는 보스
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Middle_Boss2 : Enemy_Base
{
    [Header("차징 레일건")]
    [SerializeField] private Enemy_Middle_Boss_Railgun railgunSystem;
    [SerializeField] private Transform railgunFirePoint;
    [SerializeField] private float railgunCooldown = 6f;    // 차징 시간 고려해서 조금 더 긺

    [Header("대시 공격")]
    [SerializeField] private float dashRange = 15f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDistance = 12f;
    [SerializeField] private float dashCooldown = 8f;
    [SerializeField] private float dashWarningTime = 1f;

    [Header("투사체 공격")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private int bulletCount = 8;
    [SerializeField] private float bulletCooldown = 5f;
    [SerializeField] private float bulletRange = 10f;

    [Header("이펙트")]
    [SerializeField] private GameObject dashWarningEffect;
    [SerializeField] private GameObject dashTrailEffect;

    [Header("몬스터 무시 설정")]
    [SerializeField] private float monsterIgnoreRadius = 20f;
    [SerializeField] private string[] monsterTags = { "Enemy", "Boss", "MiddleBoss" };

    // 상태
    private bool isDashing = false;
    private bool isDashWarning = false;
    private bool isShooting = false;

    // 타이머
    private float lastDashTime;
    private float lastBulletTime;
    private float lastRailgunTime = 0f;

    // 대시 관련
    private Vector3 dashDirection;
    private Vector3 dashStartPosition;
    private float dashStartTime;
    private GameObject currentDashTrail;

    // 컴포넌트
    private Rigidbody rigid;
    private Collider bossCollider;
    private List<Collider> ignoredMonsterColliders = new List<Collider>();

    // 프로퍼티
    public bool IsDashing => isDashing;
    public bool IsDashWarning => isDashWarning;
    public bool IsShooting => isShooting;
    public Vector3 DashDirection => dashDirection;
    public float DashSpeed => dashSpeed;

    protected override void Awake()
    {
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Neutral;
        primaryDamageType = DamageType.Physical;
        enemyName = "Berserker Champion";

        base.Awake();

        rigid = GetComponent<Rigidbody>();
        bossCollider = GetComponent<Collider>();

        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();

        if (firePoint == null)
            firePoint = transform;

        // 레일건 시스템 초기화
        if (railgunSystem != null)
        {
            railgunSystem.Initialize(this, railgunFirePoint);
        }
    }

    protected override void UpdateBehavior()
    {
        if (isDashing)
        {
            UpdateDashBehavior();
            return;
        }

        if (isDashWarning || isShooting || playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 공격 패턴 우선순위
        if (CanUseChargingRailgun())
        {
            UseChargingRailgun();
        }
        else if (distanceToPlayer >= dashRange * 0.8f && CanUseDash())
        {
            StartCoroutine(PerformDashAttack());
        }
        else if (distanceToPlayer <= bulletRange && CanUseBullets())
        {
            StartCoroutine(PerformBulletAttack());
        }
    }

    protected override void UpdateMovement()
    {
        // Enemy_Boss2_Move에서 처리
    }

    protected override void PerformAttack()
    {
        // 사용하지 않음
    }

    #region 대시 공격

    private bool CanUseDash()
    {
        return Time.time - lastDashTime >= dashCooldown && !isDashing && !isDashWarning && !isShooting;
    }

    private IEnumerator PerformDashAttack()
    {
        isDashWarning = true;
        lastDashTime = Time.time;

        if (playerTransform != null)
        {
            dashDirection = (playerTransform.position - transform.position).normalized;
            dashDirection.y = 0;
        }

        if (dashWarningEffect != null)
        {
            Vector3 warningPos = transform.position + dashDirection * dashDistance * 0.5f;
            GameObject warning = Instantiate(dashWarningEffect, warningPos, Quaternion.LookRotation(dashDirection));
            Destroy(warning, dashWarningTime);
        }

        yield return new WaitForSeconds(dashWarningTime);

        isDashWarning = false;
        ExecuteDash();
    }

    private void ExecuteDash()
    {
        if (playerTransform == null) return;

        isDashing = true;
        dashStartTime = Time.time;
        dashStartPosition = transform.position;

        if (dashTrailEffect != null)
        {
            currentDashTrail = Instantiate(dashTrailEffect, transform.position, Quaternion.identity);
            currentDashTrail.transform.SetParent(transform);
        }

        IgnoreMonsterCollisions(true);

        float dashDuration = dashDistance / dashSpeed;
        StartCoroutine(DashCoroutine(dashDuration));
    }

    private IEnumerator DashCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        EndDash();
    }

    private void UpdateDashBehavior()
    {
        float traveledDistance = Vector3.Distance(dashStartPosition, transform.position);
        if (traveledDistance >= dashDistance || Time.time - dashStartTime >= 3f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        isDashing = false;

        if (currentDashTrail != null)
        {
            currentDashTrail.transform.SetParent(null);
            Destroy(currentDashTrail, 1f);
            currentDashTrail = null;
        }

        IgnoreMonsterCollisions(false);
    }

    public void OnDashCollision(Collider other)
    {
        if (IsMonsterCollider(other)) return;

        if (other.CompareTag("Player") && isDashing)
        {
            if (playerScript != null)
            {
                DealDamageToPlayer(DamageType.Physical);
            }
        }

        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            EndDash();
        }
    }

    #endregion

    #region 투사체 공격

    private bool CanUseBullets()
    {
        return Time.time - lastBulletTime >= bulletCooldown && !isDashing && !isDashWarning && !isShooting;
    }

    private IEnumerator PerformBulletAttack()
    {
        isShooting = true;
        lastBulletTime = Time.time;

        yield return new WaitForSeconds(0.5f);
        FireBulletsInCircle();
        yield return new WaitForSeconds(0.5f);

        isShooting = false;
    }

    private void FireBulletsInCircle()
    {
        if (bulletPrefab == null) return;

        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                bulletRb.velocity = direction * bulletSpeed;
            }

            Enemy_Middle_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Middle_Boss_Bullet>();
            if (bulletScript != null)
            {
                float bulletDamage = enemyStats.Get(EnemyStatType.PhysicalDamage) * 0.4f;
                bulletScript.Initialize(bulletDamage, DamageType.Physical);
            }

            Destroy(bullet, 5f);
        }
    }

    #endregion

    #region 차징 레일건 시스템

    private bool CanUseChargingRailgun()
    {
        return railgunSystem != null &&
               railgunSystem.IsReady &&
               Time.time - lastRailgunTime >= railgunCooldown &&
               IsPlayerInDetectionRange() &&
               !isDashing && !isDashWarning && !isShooting;
    }

    private void UseChargingRailgun()
    {
        if (playerTransform != null)
        {
            lastRailgunTime = Time.time;

            // 플레이어 위치로 차징 후 발사
            Vector3 targetPos = playerTransform.position;
            railgunSystem.FireInstantRailgun(targetPos);
        }
    }

    #endregion

    #region 몬스터 충돌 무시

    private void IgnoreMonsterCollisions(bool ignore)
    {
        if (bossCollider == null) return;

        if (ignore)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, monsterIgnoreRadius);

            foreach (Collider col in nearbyColliders)
            {
                if (col == bossCollider || !IsMonsterCollider(col)) continue;

                if (!ignoredMonsterColliders.Contains(col))
                {
                    Physics.IgnoreCollision(bossCollider, col, true);
                    ignoredMonsterColliders.Add(col);
                }
            }
        }
        else
        {
            foreach (Collider col in ignoredMonsterColliders)
            {
                if (col != null && bossCollider != null)
                {
                    Physics.IgnoreCollision(bossCollider, col, false);
                }
            }
            ignoredMonsterColliders.Clear();
        }
    }

    private bool IsMonsterCollider(Collider col)
    {
        if (col == null) return false;

        foreach (string tag in monsterTags)
        {
            if (col.CompareTag(tag)) return true;
        }

        return col.GetComponent<Enemy_Base>() != null;
    }

    #endregion

    protected override void Die()
    {
        isDashing = false;
        isDashWarning = false;
        isShooting = false;

        if (currentDashTrail != null)
        {
            Destroy(currentDashTrail);
        }

        IgnoreMonsterCollisions(false);
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 400;
    }
}