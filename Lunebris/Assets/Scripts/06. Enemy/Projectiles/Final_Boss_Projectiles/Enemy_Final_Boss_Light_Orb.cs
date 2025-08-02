using UnityEngine;
using System.Collections;

/// <summary>
/// 최종보스의 빛 구체 - 레일건 공격 (프리팹 자체 관리)
/// </summary>
public class Enemy_Final_Boss_LightOrb : MonoBehaviour
{
    [Header("구체 설정")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float heightFixed = 3f;
    [SerializeField] private float minDistanceToPlayer = 8f;
    [SerializeField] private float maxDistanceToPlayer = 18f;
    [SerializeField] private float randomMoveInterval = 3f;

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
    private Transform player;
    private Vector3 currentTargetPosition;
    private float lastRandomMoveTime;
    private float lastRailgunTime;
    private bool isCharging = false;
    private bool isDestroyed = false;
    private GameObject currentChargingEffect;

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
    public void SetRailgunSettings(float damage, float chargeTime, float cooldown)
    {
        railgunDamage = damage;
        railgunChargeTime = chargeTime;
        railgunCooldown = cooldown;
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
        orbLight.intensity = 4f;
        orbLight.range = 12f;
    }

    private void SetupCollider()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<SphereCollider>();

        collider.radius = 1.5f; // 기존 2f에서 3f로 증가
        collider.isTrigger = true;

        // 추가 콜라이더 (일반 충돌용)
        SphereCollider solidCollider = gameObject.AddComponent<SphereCollider>();
        solidCollider.radius = 1.5f;
        solidCollider.isTrigger = false; // 일반 충돌
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

            // 목표 위치로 이동 (차징 중일 않을 때만)
            if (!isCharging)
            {
                Vector3 currentPos = transform.position;
                Vector3 targetPos = new Vector3(currentTargetPosition.x, heightFixed, currentTargetPosition.z);
                Vector3 moveDirection = (targetPos - currentPos).normalized;

                if (moveDirection != Vector3.zero)
                {
                    Vector3 newPosition = currentPos + moveDirection * moveSpeed * Time.deltaTime;
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
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            FireRailgun(direction);
            Debug.Log("빛 구체 레일건 발사!");
        }

        // 차징 이펙트 종료
        StopChargingEffect();
        isCharging = false;
    }

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
            float scale = 3f + Mathf.Sin(Time.time * 10f) * 0.5f;
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

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;
        Debug.Log($"빛 구체 피해: {damage}, 남은 체력: {health}");

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
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }

    private void DestroyOrb()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log("빛 구체 파괴됨!");

        // 차징 이펙트 정리
        StopChargingEffect();

        // 보스에게 알림
        if (ownerBoss != null)
            ownerBoss.OnLightOrbDestroyed();

        // 마지막 폭발 레일건
        StartCoroutine(DeathRailgunExplosion());

        Destroy(gameObject, 1f);
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

    private void OnTriggerEnter(Collider other)
    {
        // 다양한 플레이어 공격 태그 확인
        if (other.CompareTag("Attack") )
        {
            Debug.Log($"빛 구체가 {other.name} (태그: {other.tag})에 맞음!");
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

        // 플레이어와의 거리 체크 (차징 중이 아닐 때만)
        if (player != null && !isCharging)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer < minDistanceToPlayer)
            {
                Vector3 awayDirection = (transform.position - player.position).normalized;
                awayDirection.y = 0;
                transform.position += awayDirection * moveSpeed * Time.deltaTime * 0.5f;
            }
            else if (distanceToPlayer > maxDistanceToPlayer)
            {
                Vector3 towardsDirection = (player.position - transform.position).normalized;
                towardsDirection.y = 0;
                transform.position += towardsDirection * moveSpeed * Time.deltaTime * 0.5f;
            }
        }
    }


}