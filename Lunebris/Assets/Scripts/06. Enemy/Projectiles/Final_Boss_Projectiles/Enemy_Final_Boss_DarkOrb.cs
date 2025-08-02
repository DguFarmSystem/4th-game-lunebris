using UnityEngine;
using System.Collections;

/// <summary>
/// 최종보스의 어둠 구체 - 다크 불릿 공격 (프리팹 자체 관리)
/// </summary>
public class Enemy_Final_Boss_DarkOrb : MonoBehaviour
{
    [Header("구체 설정")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float heightFixed = 2f;
    [SerializeField] private float minDistanceToPlayer = 6f;
    [SerializeField] private float maxDistanceToPlayer = 15f;
    [SerializeField] private float randomMoveInterval = 2f;

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
    private Transform player;
    private Vector3 currentTargetPosition;
    private float lastRandomMoveTime;
    private float lastBulletTime;
    private bool isFiring = false;
    private bool isDestroyed = false;

    // 초기화 간소화 - 보스 참조와 기본 설정만
    public void Initialize(Enemy_Final_Boss_Neutral boss)
    {
        ownerBoss = boss;
        SetupComponents();
        StartBehavior();
    }

    // 런타임에서 설정 변경 가능한 메서드들
    public void SetHealth(float newHealth) => health = newHealth;
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;
    public void SetBulletSettings(float damage, int count, float speed, float cooldown)
    {
        darkBulletDamage = damage;
        darkBulletCount = count;
        darkBulletSpeed = speed;
        darkBulletCooldown = cooldown;
    }

    private void SetupComponents()
    {
        // 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 초기 위치 설정
        SetRandomTargetPosition();
        transform.position = new Vector3(currentTargetPosition.x, heightFixed, currentTargetPosition.z);

        // 리지드바디 설정
        SetupRigidbody();

        // 렌더러 설정
        SetupRenderer();

        // 조명 설정
        SetupLight();

        // 콜라이더 설정
        SetupCollider();
    }

    private void SetupRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ |
                        RigidbodyConstraints.FreezePositionY;
    }

    private void SetupRenderer()
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
    }

    private void SetupLight()
    {
        Light orbLight = GetComponent<Light>();
        if (orbLight == null)
            orbLight = gameObject.AddComponent<Light>();

        orbLight.type = LightType.Point;
        orbLight.color = orbColor;
        orbLight.intensity = 3f;
        orbLight.range = 10f;
    }

    private void SetupCollider()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<SphereCollider>();

        collider.radius = 1.5f; // 기존 2f에서 3f로 증가
        collider.isTrigger = true;
    }

    private void StartBehavior()
    {
        StartCoroutine(MovementRoutine());
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        while (!isDestroyed)
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
                float currentMoveSpeed = isFiring ? moveSpeed * 0.3f : moveSpeed;
                Vector3 newPosition = currentPos + moveDirection * currentMoveSpeed * Time.deltaTime;
                newPosition.y = heightFixed;
                transform.position = newPosition;
            }

            // 플레이어를 향해 회전
            if (player != null)
            {
                Vector3 lookDirection = (player.position - transform.position).normalized;
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
        if (player == null) return;

        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(minDistanceToPlayer, maxDistanceToPlayer);

        Vector3 randomOffset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
            0,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
        );

        currentTargetPosition = player.position + randomOffset;
    }

    private IEnumerator AttackRoutine()
    {
        while (!isDestroyed && player != null)
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

        if (player == null) yield break;

        // 플레이어 방향으로 집중 다크 불릿 (3발)
        for (int i = 0; i < 3; i++)
        {
            Vector3 baseDirection = (player.position - transform.position).normalized;
            baseDirection.y = 0;

            // 약간의 각도 변화
            float angleOffset = (i - 1) * 20f;
            Quaternion rotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 direction = rotation * baseDirection;

            CreateBullet(direction, darkBulletDamage * 1.2f, darkBulletSpeed * 1.3f);

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;
        Debug.Log($"어둠 구체 피해: {damage}, 남은 체력: {health}");

        StartCoroutine(HitEffect());

        if (health <= 0)
        {
            DestroyOrb();
        }
    }

    private IEnumerator HitEffect()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }

    private void DestroyOrb()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log("어둠 구체 파괴됨!");

        // 보스에게 알림
        if (ownerBoss != null)
            ownerBoss.OnDarkOrbDestroyed();

        // 마지막 다크 불릿 폭발
        StartCoroutine(DeathBulletExplosion());

        Destroy(gameObject, 1f);
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

    private void OnTriggerEnter(Collider other)
    {
        // 다양한 플레이어 공격 태그 확인
        if (other.CompareTag("Attack")) 
        {
            TakeDamage(50f);
        }
    }

    private void Update()
    {
        // Y축 위치 강제 고정
        if (transform.position.y != heightFixed)
        {
            Vector3 pos = transform.position;
            pos.y = heightFixed;
            transform.position = pos;
        }

        // 플레이어와의 거리 체크
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer < minDistanceToPlayer)
            {
                Vector3 awayDirection = (transform.position - player.position).normalized;
                awayDirection.y = 0;
                transform.position += awayDirection * moveSpeed * Time.deltaTime;
            }
            else if (distanceToPlayer > maxDistanceToPlayer)
            {
                Vector3 towardsDirection = (player.position - transform.position).normalized;
                towardsDirection.y = 0;
                transform.position += towardsDirection * moveSpeed * Time.deltaTime;
            }
        }
    }

    // 에디터에서 콜라이더 영역 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f); // Trigger 콜라이더

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f); // Solid 콜라이더
    }
}