using UnityEngine;
using System.Collections;
using Enemy;

/// <summary>
/// 최종보스의 빛 구체 - 레일건 공격 (프리팹 자체 관리)
/// </summary>
public class Enemy_Final_Boss_LightOrb : Enemy_Base
{
    [Header("구체 설정")]
    [SerializeField] private float heightFixed = 3f;
    [SerializeField] private float minDistanceToPlayer = 8f;
    [SerializeField] private float maxDistanceToPlayer = 18f;
    [SerializeField] private float randomMoveInterval = 3f;
    [SerializeField] private float orbMoveSpeed = 3f; // 구체 전용 이동속도

    [Header("레일건 공격")]
    [SerializeField] private GameObject railgunBeamPrefab;  // 자체 관리
    [SerializeField] private float railgunDamage = 120f;
    [SerializeField] private float railgunChargeTime = 2f;
    [SerializeField] private float railgunCooldown = 4f;

    [Header("시각적 효과")]
    [SerializeField] private Material orbMaterial;
    [SerializeField] private Color orbColor = Color.white;
    [SerializeField] private float emissionIntensity = 0.5f;
    [SerializeField] private GameObject chargingEffect;  // 차징 이펙트

    private Enemy_Final_Boss_Neutral ownerBoss;
    private Vector3 currentTargetPosition;
    private float lastRandomMoveTime;
    private float lastRailgunTime;
    private bool isCharging = false;
    private GameObject currentChargingEffect;

    protected override void Awake()
    {
        // Enemy_Base의 Awake 호출 전에 기본값 설정
        enemyType = EnemyType.FinalBoss;
        elementType = ElementType.Neutral; // Light가 없으므로 Neutral 사용
        primaryDamageType = DamageType.Magical;
        enemyName = "빛 구체";

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

    public void SetRailgunSettings(float damage, float chargeTime, float cooldown)
    {
        railgunDamage = damage;
        railgunChargeTime = chargeTime;
        railgunCooldown = cooldown;
    }

    #region 구체 컴포넌트 설정

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
        orbLight.intensity = 4f;
        orbLight.range = 12f;
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

    #endregion

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

        // 플레이어와의 거리 체크 (차징 중이 아닐 때만)
        if (playerTransform != null && !isCharging)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer < minDistanceToPlayer)
            {
                Vector3 awayDirection = (transform.position - playerTransform.position).normalized;
                awayDirection.y = 0;
                transform.position += awayDirection * orbMoveSpeed * Time.deltaTime * 0.5f;
            }
            else if (distanceToPlayer > maxDistanceToPlayer)
            {
                Vector3 towardsDirection = (playerTransform.position - transform.position).normalized;
                towardsDirection.y = 0;
                transform.position += towardsDirection * orbMoveSpeed * Time.deltaTime * 0.5f;
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

    private void StartBehavior()
    {
        StartCoroutine(MovementRoutine());
        StartCoroutine(AttackRoutine());
    }

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

            // 목표 위치로 이동 (차징 중일 때는 이동하지 않음)
            if (!isCharging)
            {
                Vector3 currentPos = transform.position;
                Vector3 targetPos = new Vector3(currentTargetPosition.x, heightFixed, currentTargetPosition.z);
                Vector3 moveDirection = (targetPos - currentPos).normalized;

                if (moveDirection != Vector3.zero)
                {
                    Vector3 newPosition = currentPos + moveDirection * orbMoveSpeed * Time.deltaTime;
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
                            Quaternion.LookRotation(lookDirection), Time.deltaTime * 3f);
                    }
                }
            }

            // 부드러운 자전
            transform.Rotate(Vector3.up, 20f * Time.deltaTime);

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
            if (Time.time - lastRailgunTime >= railgunCooldown)
            {
                yield return StartCoroutine(PerformRailgunAttack());
                lastRailgunTime = Time.time;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator PerformRailgunAttack()
    {
        isCharging = true;
        Debug.Log("빛 구체 레일건 차징 시작!");

        // 차징 이펙트 시작
        StartChargingEffect();

        // 차징 시간
        yield return new WaitForSeconds(railgunChargeTime);

        // 레일건 발사
        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;

            FireRailgun(direction);
            Debug.Log("빛 구체 레일건 발사!");
        }

        // 차징 이펙트 종료
        StopChargingEffect();
        isCharging = false;
    }

    #endregion

    #region 레일건 시스템

    private void StartChargingEffect()
    {
        if (chargingEffect != null)
        {
            currentChargingEffect = Instantiate(chargingEffect, transform.position, transform.rotation);
            currentChargingEffect.transform.SetParent(transform);
        }
        else
        {
            // 임시 차징 이펙트
            CreateTempChargingEffect();
        }
    }

    private void CreateTempChargingEffect()
    {
        currentChargingEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        currentChargingEffect.name = "ChargingEffect";
        currentChargingEffect.transform.SetParent(transform);
        currentChargingEffect.transform.localPosition = Vector3.zero;
        currentChargingEffect.transform.localScale = Vector3.one * 3f;

        Renderer renderer = currentChargingEffect.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1f, 1f, 1f, 0.3f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.cyan * 2f);
        renderer.material = material;

        // 투명하게
        material.renderQueue = 3000;
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");

        // 콜라이더 제거
        DestroyImmediate(currentChargingEffect.GetComponent<Collider>());

        // 펄스 효과
        StartCoroutine(ChargingPulseEffect());
    }

    private IEnumerator ChargingPulseEffect()
    {
        while (currentChargingEffect != null && isCharging)
        {
            float scale = 1f + Mathf.Sin(Time.time * 10f) * 0.5f;
            currentChargingEffect.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    private void StopChargingEffect()
    {
        if (currentChargingEffect != null)
        {
            Destroy(currentChargingEffect);
            currentChargingEffect = null;
        }
    }

    private void FireRailgun(Vector3 direction)
    {
        if (railgunBeamPrefab != null)
        {
            GameObject beam = Instantiate(railgunBeamPrefab, transform.position,
                Quaternion.LookRotation(direction));

            Enemy_Final_Boss_RailgunBeam beamScript = beam.GetComponent<Enemy_Final_Boss_RailgunBeam>();
            if (beamScript != null)
            {
                beamScript.Initialize(railgunDamage, direction, 30f);
            }
        }
        else
        {
            // 임시 레일건 빔 생성
            CreateTempRailgunBeam(direction);
        }
    }

    private void CreateTempRailgunBeam(Vector3 direction)
    {
        GameObject tempBeam = new GameObject("TempRailgunBeam");
        tempBeam.transform.position = transform.position;
        tempBeam.transform.rotation = Quaternion.LookRotation(direction);

        // 빔 시각적 표현
        LineRenderer lineRenderer = tempBeam.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.white;
        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * 30f;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 데미지 처리를 위한 스크립트 추가
        Enemy_Final_Boss_RailgunBeam beamScript = tempBeam.AddComponent<Enemy_Final_Boss_RailgunBeam>();
        beamScript.Initialize(railgunDamage, direction, 30f);

        Destroy(tempBeam, 2f);
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

        Debug.Log("빛 구체 파괴됨!");

        // 차징 이펙트 정리
        StopChargingEffect();

        // 보스에게 알림
        if (ownerBoss != null)
            ownerBoss.OnLightOrbDestroyed();

        // 마지막 폭발 레일건
        StartCoroutine(DeathRailgunExplosion());

        // Enemy_Base의 Die 호출
        base.Die();
    }

    private IEnumerator DeathRailgunExplosion()
    {
        // 8방향으로 레일건 발사
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            FireRailgun(direction);

            // 약간의 딜레이
            yield return new WaitForSeconds(0.1f);
        }
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.8f); // Trigger 콜라이더 (축소된 크기)

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer); // 최소 거리
        Gizmos.DrawWireSphere(transform.position, maxDistanceToPlayer); // 최대 거리
    }
}