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

    [Header("360빔 공격")]
    [SerializeField] private GameObject beam360Prefab; // 이름 변경: swordBeamPrefab -> beam360Prefab
    [SerializeField] private float beam360Damage = 80f; // 이름 변경: swordBeamDamage -> beam360Damage

    [Header("빛 기둥")]
    [SerializeField] private GameObject lightPillarPrefab;
    [SerializeField] private float lightPillarDamage = 100f;
    [SerializeField] private float lightPillarInterval = 6f;
    [SerializeField] private GameObject warningEffectPrefab;
    private bool isPerformingAttack = false;
    private float lastRailgunTime = 0f;
    private float lastLightPillarTime = 0f;

    // Move 스크립트 호출용 프로퍼티
    public bool IsPerformingSpecialAttack => isPerformingAttack;

    protected override void Awake()
    {
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Lux;
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
            yield return StartCoroutine(Perform360BeamAttack()); // 이름 변경
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator PerformRailgunBeamAttack()
    {
        isPerformingAttack = true;
        Debug.Log("레일건 빔 공격!");

        yield return new WaitForSeconds(railgunChargeTime);

        if (playerTransform != null && railgunBeamPrefab != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Vector3 spawnPosition = transform.position;

            GameObject beam = Instantiate(railgunBeamPrefab, spawnPosition, Quaternion.LookRotation(direction));
            Enemy_Final_Boss_RailgunBeam beamScript = beam.GetComponent<Enemy_Final_Boss_RailgunBeam>();
            if (beamScript != null)
            {
                beamScript.Initialize(railgunDamage, direction, 30f);
            }
        }
        else
        {
            Debug.LogWarning("railgunBeamPrefab이 설정되지 않았습니다!");
        }

        isPerformingAttack = false;
    }

    private IEnumerator Perform360BeamAttack() // 메소드 이름 변경: PerformSwordBeamAttack -> Perform360BeamAttack
    {
        isPerformingAttack = true;
        Debug.Log("360빔 공격!"); // 로그 메시지 변경

        for (int i = 0; i < 5; i++)
        {
            if (playerTransform != null && beam360Prefab != null) // 변수명 변경
            {
                // 플레이어를 향하는 방향 벡터 계산 (Y축 무시)
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0f;
                direction.Normalize();

                // 각도 오프셋 (좌우 팬처리)
                float angleOffset = (i - 1) * 60f;

                // Y축 기준으로 회전 적용
                Quaternion yRotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
                Vector3 finalDirection = yRotation * direction;

                // 검기 방향으로 회전값 생성 (up 방향 고정!)
                Quaternion rotation = Quaternion.LookRotation(finalDirection, Vector3.up);

                GameObject beam360 = Instantiate(beam360Prefab, transform.position, rotation); // 변수명 변경
                Enemy_Final_Boss_360Beam beamScript = beam360.GetComponent<Enemy_Final_Boss_360Beam>(); // 컴포넌트명 변경
                if (beamScript != null)
                {
                    beamScript.Initialize(beam360Damage, finalDirection * 15f); // 변수명 변경
                }
            }
            else
            {
                Debug.LogWarning("beam360Prefab이 설정되지 않았습니다!"); // 메시지 변경
            }

            yield return new WaitForSeconds(0.3f);
        }

        isPerformingAttack = false;
    }

    private IEnumerator PerformLightPillarAttack()
    {
        Debug.Log("빛 기둥 공격!");

        if (playerTransform != null)
        {
            Vector3 pillarPosition = playerTransform.position;
            pillarPosition.y = 0;

            float warningTime = 1.5f;

            // 1️ 경고 이펙트 먼저 생성
            if (warningEffectPrefab != null)
            {
                GameObject warning = Instantiate(
                    warningEffectPrefab,
                    pillarPosition + Vector3.down * 0.1f,
                    Quaternion.identity
                );
                Destroy(warning, warningTime); // 일정 시간 후 자동 제거
            }

            // 2️ 경고 시간 동안 대기 (빛 기둥 생성은 아직 아님!)
            yield return new WaitForSeconds(warningTime);

            // 3️ 경고 끝난 뒤 빛기둥 생성
            if (lightPillarPrefab != null)
            {
                GameObject pillar = Instantiate(lightPillarPrefab, pillarPosition, Quaternion.identity);
                Enemy_Final_Boss_LightPillar pillarScript = pillar.GetComponent<Enemy_Final_Boss_LightPillar>();
                if (pillarScript != null)
                {
                    // 경고는 이미 끝났으므로 duration을 "기둥 활성화 시간만큼"만 넘긴다
                    pillarScript.StartLightPillar(lightPillarDamage, 2.0f); // 경고 없이 바로 활성화됨
                }
            }
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