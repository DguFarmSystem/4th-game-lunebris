using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 최종보스 - 빛 모드 (레일건, 검기, 빛 기둥)
/// </summary>
public class Enemy_Final_Boss_Light : Enemy_Base
{
    [Header("레일건 빔")]
    [SerializeField] private GameObject railgunBeamPrefab;
    [SerializeField] private float railgunDamage = 120f;
    [SerializeField] private float railgunChargeTime = 2f;
    [SerializeField] private float railgunCooldown = 4f;

    [Header("검기 공격")]
    [SerializeField] private GameObject swordBeamPrefab;
    [SerializeField] private float swordBeamDamage = 80f;

    [Header("빛 기둥")]
    [SerializeField] private GameObject lightPillarPrefab;
    [SerializeField] private float lightPillarDamage = 100f;
    [SerializeField] private float lightPillarInterval = 6f;

    private bool isPerformingAttack = false;
    private float lastRailgunTime = 0f;
    private float lastLightPillarTime = 0f;

    // Move 스크립트 호환용 프로퍼티
    public bool IsPerformingSpecialAttack => isPerformingAttack;

    protected override void Awake()
    {
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Light;
        primaryDamageType = DamageType.Magical;
        enemyName = "Light Sovereign";

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();
        Debug.Log("빛 모드 보스 활성화! 빛의 힘을 사용합니다.");

        StartCoroutine(LightModeAttackRoutine());
    }

    public void SetHealth(float health)
    {
        currentHp = health;
    }

    protected override void UpdateBehavior()
    {
        if (isPerformingAttack) return;

        // 레일건 빔 공격
        if (Time.time - lastRailgunTime >= railgunCooldown)
        {
            StartCoroutine(PerformRailgunBeamAttack());
            lastRailgunTime = Time.time;
        }

        // 빛 기둥 공격
        if (Time.time - lastLightPillarTime >= lightPillarInterval)
        {
            StartCoroutine(PerformLightPillarAttack());
            lastLightPillarTime = Time.time;
        }
    }

    private IEnumerator LightModeAttackRoutine()
    {
        while (currentHp > 0)
        {
            yield return StartCoroutine(PerformSwordBeamAttack());
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator PerformRailgunBeamAttack()
    {
        isPerformingAttack = true;
        Debug.Log("레일건 빔 공격!");

        yield return new WaitForSeconds(railgunChargeTime);

        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Vector3 spawnPosition = transform.position;

            if (railgunBeamPrefab != null)
            {
                GameObject beam = Instantiate(railgunBeamPrefab, spawnPosition, Quaternion.LookRotation(direction));
                Enemy_Final_Boss_RailgunBeam beamScript = beam.GetComponent<Enemy_Final_Boss_RailgunBeam>();
                if (beamScript != null)
                {
                    beamScript.Initialize(railgunDamage, direction, 30f);
                }
            }
            else
            {
                CreateTempRailgunBeam(spawnPosition, direction);
            }
        }

        isPerformingAttack = false;
    }

    private void CreateTempRailgunBeam(Vector3 position, Vector3 direction)
    {
        GameObject tempBeam = new GameObject("TempRailgunBeam");
        tempBeam.transform.position = position;
        tempBeam.transform.rotation = Quaternion.LookRotation(direction);

        Enemy_Final_Boss_RailgunBeam beamScript = tempBeam.AddComponent<Enemy_Final_Boss_RailgunBeam>();
        beamScript.Initialize(railgunDamage, direction, 30f);
    }

    private IEnumerator PerformSwordBeamAttack()
    {
        isPerformingAttack = true;
        Debug.Log("검기 공격!");

        for (int i = 0; i < 3; i++)
        {
            if (playerTransform != null)
            {
                // 플레이어를 향하는 방향 벡터 계산 (Y축 무시)
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0f;
                direction.Normalize();

                // 각도 오프셋 (좌우 퍼짐)
                float angleOffset = (i - 1) * 15f;

                // Y축 기준으로 회전 적용
                Quaternion yRotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
                Vector3 finalDirection = yRotation * direction;

                // 검기 방향으로 회전값 생성 (up 방향 고정!)
                Quaternion rotation = Quaternion.LookRotation(finalDirection, Vector3.up);

                if (swordBeamPrefab != null)
                {
                    GameObject swordBeam = Instantiate(swordBeamPrefab, transform.position, rotation);
                    Enemy_Final_Boss_SwordBeam beamScript = swordBeam.GetComponent<Enemy_Final_Boss_SwordBeam>();
                    if (beamScript != null)
                    {
                        beamScript.Initialize(swordBeamDamage, finalDirection * 15f);
                    }
                }
                else
                {
                    // 디버깅용 임시 검기 생성
                    CreateTempSwordBeam(finalDirection, i);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        isPerformingAttack = false;
    }


    private void CreateTempSwordBeam(Vector3 direction, int index)
    {
        GameObject swordBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swordBeam.name = "TempSwordBeam_" + index;
        swordBeam.transform.position = transform.position;
        swordBeam.transform.localScale = new Vector3(0.5f, 0.5f, 4f);

        // Y축 고정 회전 적용
        swordBeam.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Renderer renderer = swordBeam.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.yellow;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.yellow * 3f);
        renderer.material = material;

        Rigidbody rb = swordBeam.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = direction * 15f;

        Collider collider = swordBeam.GetComponent<Collider>();
        collider.isTrigger = true;

        Enemy_Temp_Damage_Projectile damageScript = swordBeam.AddComponent<Enemy_Temp_Damage_Projectile>();
        damageScript.Initialize(swordBeamDamage, "검기");

        Destroy(swordBeam, 3f);
    }

    private IEnumerator PerformLightPillarAttack()
    {
        Debug.Log("빛 기둥 공격!");

        if (playerTransform != null)
        {
            Vector3 pillarPosition = playerTransform.position;
            pillarPosition.y = 0;

            // 경고 표시
            GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            warning.name = "LightPillarWarning";
            warning.transform.position = pillarPosition + Vector3.up * 0.1f;
            warning.transform.localScale = new Vector3(4f, 0.2f, 4f);

            Renderer warningRenderer = warning.GetComponent<Renderer>();
            Material warningMaterial = new Material(Shader.Find("Standard"));
            warningMaterial.color = Color.red;
            warningMaterial.EnableKeyword("_EMISSION");
            warningMaterial.SetColor("_EmissionColor", Color.red * 3f);
            warningRenderer.material = warningMaterial;

            StartCoroutine(BlinkWarning(warning));

            yield return new WaitForSeconds(2f);

            Destroy(warning);

            // 실제 빛 기둥 생성
            if (lightPillarPrefab != null)
            {
                GameObject pillar = Instantiate(lightPillarPrefab, pillarPosition, Quaternion.identity);
                Enemy_Final_Boss_LightPillar pillarScript = pillar.GetComponent<Enemy_Final_Boss_LightPillar>();
                if (pillarScript != null)
                {
                    pillarScript.StartLightPillar(lightPillarDamage, 3f);
                }
            }
            else
            {
                CreateTempLightPillar(pillarPosition);
            }
        }
    }

    private void CreateTempLightPillar(Vector3 position)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "TempLightPillar";
        pillar.transform.position = position + Vector3.up * 10f;
        pillar.transform.localScale = new Vector3(4f, 10f, 4f);

        Renderer pillarRenderer = pillar.GetComponent<Renderer>();
        Material pillarMaterial = new Material(Shader.Find("Standard"));
        pillarMaterial.color = Color.white;
        pillarMaterial.EnableKeyword("_EMISSION");
        pillarMaterial.SetColor("_EmissionColor", Color.white * 4f);
        pillarRenderer.material = pillarMaterial;

        Collider pillarCollider = pillar.GetComponent<Collider>();
        pillarCollider.isTrigger = true;

        Enemy_Temp_Damage_Projectile damageScript = pillar.AddComponent<Enemy_Temp_Damage_Projectile>();
        damageScript.Initialize(lightPillarDamage, "빛 기둥", true);

        Destroy(pillar, 5f);
    }

    private IEnumerator BlinkWarning(GameObject warning)
    {
        Renderer renderer = warning.GetComponent<Renderer>();
        float blinkTime = 0f;

        while (warning != null && blinkTime < 2f)
        {
            bool visible = Mathf.Sin(Time.time * 10f) > 0;
            renderer.enabled = visible;

            blinkTime += Time.deltaTime;
            yield return null;
        }
    }

    protected override void UpdateMovement()
    {
        // 보스는 움직이지 않음
    }

    protected override void PerformAttack()
    {
        // 기본 공격은 사용하지 않음
    }

    protected override void Die()
    {
        Debug.Log("빛의 힘이... 사라진다...");
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 1000;
    }
}