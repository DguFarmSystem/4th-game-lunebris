using UnityEngine;
using System.Collections;
using Enemy;

/// <summary>
/// 최종보스의 어둠 구체 - 다크 불릿 공격 (프리팹 자체 관리)
/// </summary>
public class Enemy_Final_Boss_DarkOrb : Enemy_Base
{
    [Header("구체 설정")]
    [SerializeField] private float heightFixed = 2f;
    [SerializeField] private float minDistanceToPlayer = 6f;
    [SerializeField] private float maxDistanceToPlayer = 15f;
    [SerializeField] private float randomMoveInterval = 2f;
    [SerializeField] private float orbMoveSpeed = 3f; // 구체 전용 이동속도

    [Header("다크 불릿 공격")]
    [SerializeField] private GameObject darkBulletPrefab;  // 자체 관리
    [SerializeField] private float darkBulletDamage = 40f;
    [SerializeField] private int darkBulletCount = 12;
    [SerializeField] private float darkBulletSpeed = 8f;
    [SerializeField] private float darkBulletCooldown = 2f;

    [Header("시각적 효과")]
    [SerializeField] private Material orbMaterial;
    [SerializeField] private Color orbColor = Color.red;
    [SerializeField] private float emissionIntensity = 0.3f;

    private Enemy_Final_Boss_Neutral ownerBoss;
    private Vector3 currentTargetPosition;
    private float lastRandomMoveTime;
    private float lastBulletTime;
    private bool isFiring = false;

    protected override void Awake()
    {
        // Enemy_Base의 Awake 호출 전에 기본값 설정
        enemyType = EnemyType.FinalBoss;
        elementType = ElementType.Neutral; // Dark가 없으므로 Neutral 사용
        primaryDamageType = DamageType.Magical;
        enemyName = "어둠 구체";

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // 구체 특화 컴포넌트 설정
        SetupOrbComponents();
        StartBehavior();
    }

    // 초기화 간소화 - 보스 참조와 기본 설정만
    public void Initialize(Enemy_Final_Boss_Neutral boss)
    {
        ownerBoss = boss;

        // Enemy_Base에서 제공하는 현재 HP 설정
        if (enemyStats != null)
        {
            currentHp = enemyStats.Get(EnemyStatType.MaxHp);
            UpdateHpUI();
        }
    }

    // 런타임에서 설정 변경 가능한 메서드들
    public void SetHealth(float newHealth)
    {
        if (enemyStats != null)
        {
            // Enemy_Base의 currentHp 직접 설정
            currentHp = newHealth;
            UpdateHpUI();
        }
    }

    public void SetMoveSpeed(float newSpeed)
    {
        orbMoveSpeed = newSpeed; // 구체 전용 이동속도 사용
        if (enemyStats != null)
        {
            enemyStats.SetBase(EnemyStatType.MoveSpeed, newSpeed);
        }
    }

    public void SetBulletSettings(float damage, int count, float speed, float cooldown)
    {
        darkBulletDamage = damage;
        darkBulletCount = count;
        darkBulletSpeed = speed;
        darkBulletCooldown = cooldown;
    }

    private void SetupOrbComponents()
    {
        // 초기 위치 설정
        SetRandomTargetPosition();
        transform.position = new Vector3(currentTargetPosition.x, heightFixed, currentTargetPosition.z);

        // 리지드바디 설정 (Enemy_Base의 설정 덮어쓰기)
        SetupOrbRigidbody();

        // 렌더러 설정
        SetupOrbRenderer();

        // 조명 설정
        SetupOrbLight();

        // 콜라이더 설정
        SetupOrbCollider();
    }

    private void SetupOrbRigidbody()
    {
        if (enemyRigidbody == null)
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();

        enemyRigidbody.useGravity = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionY;
    }

    private void SetupOrbRenderer()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            // 기본 구체 생성
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 2f;
            renderer = sphere.GetComponent<Renderer>();
            enemyRenderer = renderer; // Enemy_Base의 렌더러 참조 업데이트
        }

        // 머티리얼 설정
        if (orbMaterial != null)
        {
            renderer.material = orbMaterial;
        }
        else
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = orbColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", orbColor * emissionIntensity);
            renderer.material = material;
        }

        originalColor = renderer.material.color;
    }

    private void SetupOrbLight()
    {
        Light orbLight = GetComponent<Light>();
        if (orbLight == null)
            orbLight = gameObject.AddComponent<Light>();

        orbLight.type = LightType.Point;
        orbLight.color = orbColor;
        orbLight.intensity = 3f;
        orbLight.range = 10f;
    }

    private void SetupOrbCollider()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<SphereCollider>();

        collider.radius = 0.8f; // 히트박스 크기 축소 (기존 1.5f에서 0.8f로)
        collider.isTrigger = true;
        enemyCollider = collider; // Enemy_Base의 콜라이더 참조 업데이트
    }

    private void StartBehavior()
    {
        StartCoroutine(MovementRoutine());
        StartCoroutine(AttackRoutine());
    }

    #region Enemy_Base 추상 메서드 구현

    protected override void UpdateBehavior()
    {
        // Y축 위치 강제 고정
        if (transform.position.y != heightFixed)
        {
            Vector3 pos = transform.position;
            pos.y = heightFixed;
            transform.position = pos;
        }

        // 플레이어와의 거리 체크
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer < minDistanceToPlayer)
            {
                Vector3 awayDirection = (transform.position - playerTransform.position).normalized;
                awayDirection.y = 0;
                transform.position += awayDirection * orbMoveSpeed * Time.deltaTime;
            }
            else if (distanceToPlayer > maxDistanceToPlayer)
            {
                Vector3 towardsDirection = (playerTransform.position - transform.position).normalized;
                towardsDirection.y = 0;
                transform.position += towardsDirection * orbMoveSpeed * Time.deltaTime;
            }
        }
    }

    protected override void PerformAttack()
    {
        // 공격은 AttackRoutine 코루틴에서 처리됨
        // 필요시 여기서 추가 공격 로직 구현
    }

    #endregion

    #region 이동 및 공격 루틴

    private IEnumerator MovementRoutine()
    {
        while (!isDead)
        {
            // 일정 시간마다 새로운 랜덤 위치 설정
            if (Time.time - lastRandomMoveTime >= randomMoveInterval)
            {
                SetRandomTargetPosition();
                lastRandomMoveTime = Time.time;
            }

            // 목표 위치로 이동 (공격 중일 때 느리게)
            Vector3 currentPos = transform.position;
            Vector3 targetPos = new Vector3(currentTargetPosition.x, heightFixed, currentTargetPosition.z);
            Vector3 moveDirection = (targetPos - currentPos).normalized;

            if (moveDirection != Vector3.zero)
            {
                float currentMoveSpeed = isFiring ? orbMoveSpeed * 0.3f : orbMoveSpeed;
                Vector3 newPosition = currentPos + moveDirection * currentMoveSpeed * Time.deltaTime;
                newPosition.y = heightFixed;
                transform.position = newPosition;
            }

            // 플레이어를 향해 회전
            if (playerTransform != null)
            {
                Vector3 lookDirection = (playerTransform.position - transform.position).normalized;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(lookDirection), Time.deltaTime * 2f);
                }
            }

            // 추가 회전 (구체 특성)
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);

            yield return null;
        }
    }

    private void SetRandomTargetPosition()
    {
        if (playerTransform == null) return;

        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(minDistanceToPlayer, maxDistanceToPlayer);

        Vector3 randomOffset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
            0,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
        );

        currentTargetPosition = playerTransform.position + randomOffset;
    }

    private IEnumerator AttackRoutine()
    {
        while (!isDead && playerTransform != null)
        {
            if (Time.time - lastBulletTime >= darkBulletCooldown)
            {
                yield return StartCoroutine(PerformBulletAttack());
                lastBulletTime = Time.time;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator PerformBulletAttack()
    {
        isFiring = true;
        Debug.Log("어둠 구체 다크 불릿 공격 시작!");

        // 차징 준비 시간
        yield return new WaitForSeconds(1f);

        // 다크 불릿 발사
        FireBulletPattern();

        // 다크 불릿 발사 완료
        isFiring = false;
    }

    private void FireBulletPattern()
    {
        Debug.Log("어둠의 다크 불릿 발사!");

        // 원형 다크 불릿 패턴
        float angleStep = 360f / darkBulletCount;

        for (int i = 0; i < darkBulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            CreateBullet(direction, darkBulletDamage, darkBulletSpeed);
        }

        // 추가 패턴: 플레이어 방향 집중 다크 불릿
        StartCoroutine(FireTargetedBullets());
    }

    private void CreateBullet(Vector3 direction, float damage, float speed)
    {
        if (darkBulletPrefab != null)
        {
            GameObject bullet = Instantiate(darkBulletPrefab, transform.position,
                Quaternion.LookRotation(direction));

            // 불릿 스크립트가 있다면 초기화
            Enemy_Final_Boss_Bullet bulletScript = bullet.GetComponent<Enemy_Final_Boss_Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(damage, direction * speed);
            }
            else
            {
                // 기본 Rigidbody 이동
                Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
                if (bulletRb != null)
                {
                    bulletRb.velocity = direction * speed;
                }
            }

            Destroy(bullet, 5f);
        }
        else
        {
            CreateTempBullet(direction, damage, speed);
        }
    }

    private void CreateTempBullet(Vector3 direction, float damage, float speed)
    {
        GameObject tempBullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tempBullet.name = "TempDarkBullet";
        tempBullet.transform.position = transform.position;
        tempBullet.transform.localScale = Vector3.one * 0.5f;

        // 어둠의 보라색
        Renderer renderer = tempBullet.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(0.5f, 0f, 0.5f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.magenta);
        renderer.material = material;

        Rigidbody rb = tempBullet.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = direction * speed;

        // 데미지 처리
        Collider collider = tempBullet.GetComponent<Collider>();
        collider.isTrigger = true;

        Enemy_Temp_Damage_Projectile damageScript = tempBullet.AddComponent<Enemy_Temp_Damage_Projectile>();
        damageScript.Initialize(damage, "어둠 다크불릿");

        Destroy(tempBullet, 5f);
    }

    private IEnumerator FireTargetedBullets()
    {
        yield return new WaitForSeconds(0.5f);

        if (playerTransform == null) yield break;

        // 플레이어 방향으로 집중 다크 불릿 (3발)
        for (int i = 0; i < 3; i++)
        {
            Vector3 baseDirection = (playerTransform.position - transform.position).normalized;
            baseDirection.y = 0;

            // 약간의 각도 변화
            float angleOffset = (i - 1) * 20f;
            Quaternion rotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 direction = rotation * baseDirection;

            CreateBullet(direction, darkBulletDamage * 1.2f, darkBulletSpeed * 1.3f);

            yield return new WaitForSeconds(0.2f);
        }
    }

    #endregion

    #region Enemy_Base 오버라이드

    protected override void OnDamaged()
    {
        base.OnDamaged(); // 기본 피격 효과 사용
    }

    protected override void Die()
    {
        if (isDead) return;

        Debug.Log("어둠 구체 파괴됨!");

        // 보스에게 알림
        if (ownerBoss != null)
            ownerBoss.OnDarkOrbDestroyed();

        // 마지막 다크 불릿 폭발
        StartCoroutine(DeathBulletExplosion());

        // Enemy_Base의 Die 호출
        base.Die();
    }

    private IEnumerator DeathBulletExplosion()
    {
        float angleStep = 360f / (darkBulletCount * 2);

        for (int i = 0; i < darkBulletCount * 2; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            CreateBullet(direction, darkBulletDamage * 0.7f, darkBulletSpeed * 1.5f);
        }

        yield return null;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // Enemy_Base의 기본 충돌 처리 사용
        base.OnTriggerEnter(other);
    }

    #endregion

    // 에디터에서 콜라이더 영역 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.8f); // Trigger 콜라이더 (축소된 크기)

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer); // 최소 거리
        Gizmos.DrawWireSphere(transform.position, maxDistanceToPlayer); // 최대 거리
    }
}