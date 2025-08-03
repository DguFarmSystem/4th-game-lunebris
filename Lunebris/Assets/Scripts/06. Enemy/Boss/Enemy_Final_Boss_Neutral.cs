using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 최종보스 - 중립 모드 (오브 관리만) - 중간보스 패턴 적용
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Final_Boss_Neutral : Enemy_Base
{
    [Header("오브 프리팹")]
    [SerializeField] private GameObject lightOrbPrefab;
    [SerializeField] private GameObject darkOrbPrefab;

    [Header("오브 스폰 설정")]
    [SerializeField] private Vector3[] orbSpawnPositions;
    [SerializeField] private float orbSpawnDelay = 1f;
    [SerializeField] private bool spawnOrbsOnStart = true;

    [Header("모드 전환")]
    [SerializeField] private GameObject lightModeBoss; // 빛 모드 보스 오브젝트
    [SerializeField] private GameObject darkModeBoss;  // 어둠 모드 보스 오브젝트

    [Header("보스 설정 (오브 난이도용)")]
    [SerializeField] private float orbHealthMultiplier = 1f;
    [SerializeField] private float orbSpeedMultiplier = 1f;

    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 10f;

    // 오브 관리
    private GameObject lightOrb;
    private GameObject darkOrb;
    private bool isTransitioning = false;

    // 상태 플래그
    private bool isSpawningOrbs = false;

    // 쿨다운 관리
    private float orbRespawnCooldown = 5f;
    private float lastOrbCheckTime = 0f;

    // 공개 프로퍼티 (Move 스크립트에서 참조 가능)
    public bool IsTransitioning => isTransitioning;
    public bool IsSpawningOrbs => isSpawningOrbs;

    protected override void Awake()
    {
        enemyType = EnemyType.FinalBoss;
        elementType = ElementType.Neutral;
        primaryDamageType = DamageType.Magical;
        enemyName = "Dual Sphere Sovereign";

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();

        // 기본 오브 위치 설정
        SetupOrbSpawnPositions();

        if (spawnOrbsOnStart)
        {
            StartCoroutine(SpawnOrbsWithDelay());
        }

        Debug.Log("중립 모드 보스 등장! 구체들만 공격합니다.");
    }

    private void SetupOrbSpawnPositions()
    {
        if (orbSpawnPositions == null || orbSpawnPositions.Length == 0)
        {
            orbSpawnPositions = new Vector3[]
            {
                transform.position + Vector3.left * 8f + Vector3.up * 3f,   // 빛 구체
                transform.position + Vector3.right * 8f + Vector3.up * 2f   // 어둠 구체
            };
        }
    }

    protected override void UpdateBehavior()
    {
        // 전환 중이면 다른 행동 중지
        if (isTransitioning) return;

        // 중립 모드에서는 보스가 직접 공격하지 않음
        // 구체들만 공격

        // 보스 본체는 천천히 회전만
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 주기적으로 오브 상태 체크
        if (Time.time - lastOrbCheckTime >= orbRespawnCooldown)
        {
            CheckAndRespawnOrbs();
            lastOrbCheckTime = Time.time;
        }
    }

    protected override void UpdateMovement()
    {
        // 보스는 움직이지 않음 (고정)
    }

    protected override void PerformAttack()
    {
        // 보스는 직접 공격하지 않음
    }

    #region 오브 생성 및 관리

    private IEnumerator SpawnOrbsWithDelay()
    {
        isSpawningOrbs = true;

        // 빛 구체 먼저 생성
        yield return new WaitForSeconds(orbSpawnDelay);
        SpawnLightOrb();

        // 어둠 구체 생성
        yield return new WaitForSeconds(orbSpawnDelay);
        SpawnDarkOrb();

        Debug.Log("모든 오브 생성 완료!");
        isSpawningOrbs = false;
    }

    private bool CanSpawnOrb()
    {
        return !isTransitioning && !isSpawningOrbs;
    }

    public void SpawnLightOrb()
    {
        if (lightOrb != null) return; // 이미 존재하면 생성하지 않음

        Vector3 spawnPos = orbSpawnPositions.Length > 0 ? orbSpawnPositions[0] : transform.position + Vector3.left * 8f;

        if (lightOrbPrefab != null)
        {
            lightOrb = Instantiate(lightOrbPrefab, spawnPos, Quaternion.identity);
            Enemy_Final_Boss_LightOrb lightOrbScript = lightOrb.GetComponent<Enemy_Final_Boss_LightOrb>();

            if (lightOrbScript != null)
            {
                lightOrbScript.Initialize(this);

                // 난이도에 따른 설정 조정
                if (orbHealthMultiplier != 1f)
                    lightOrbScript.SetHealth(100f * orbHealthMultiplier);
                if (orbSpeedMultiplier != 1f)
                    lightOrbScript.SetMoveSpeed(3f * orbSpeedMultiplier);
            }

            Debug.Log("빛 구체 생성됨!");
        }
        else
        {
            CreateTempLightOrb(spawnPos);
        }
    }

    public void SpawnDarkOrb()
    {
        if (darkOrb != null) return; // 이미 존재하면 생성하지 않음

        Vector3 spawnPos = orbSpawnPositions.Length > 1 ? orbSpawnPositions[1] : transform.position + Vector3.right * 8f;

        if (darkOrbPrefab != null)
        {
            darkOrb = Instantiate(darkOrbPrefab, spawnPos, Quaternion.identity);
            Enemy_Final_Boss_DarkOrb darkOrbScript = darkOrb.GetComponent<Enemy_Final_Boss_DarkOrb>();

            if (darkOrbScript != null)
            {
                darkOrbScript.Initialize(this);

                // 난이도에 따른 설정 조정
                if (orbHealthMultiplier != 1f)
                    darkOrbScript.SetHealth(100f * orbHealthMultiplier);
                if (orbSpeedMultiplier != 1f)
                    darkOrbScript.SetMoveSpeed(3f * orbSpeedMultiplier);
            }

            Debug.Log("어둠 구체 생성됨!");
        }
        else
        {
            CreateTempDarkOrb(spawnPos);
        }
    }

    private void CreateTempLightOrb(Vector3 position)
    {
        lightOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightOrb.name = "TempLightOrb";
        lightOrb.transform.position = position;
        lightOrb.transform.localScale = Vector3.one * 2f;

        Renderer renderer = lightOrb.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.white * 2f);
        renderer.material = material;

        Enemy_Temp_Orb_Health healthScript = lightOrb.AddComponent<Enemy_Temp_Orb_Health>();
        healthScript.Initialize(this, 100f * orbHealthMultiplier, true);
    }

    private void CreateTempDarkOrb(Vector3 position)
    {
        darkOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        darkOrb.name = "TempDarkOrb";
        darkOrb.transform.position = position;
        darkOrb.transform.localScale = Vector3.one * 2f;

        Renderer renderer = darkOrb.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.red * 2f);
        renderer.material = material;

        Enemy_Temp_Orb_Health healthScript = darkOrb.AddComponent<Enemy_Temp_Orb_Health>();
        healthScript.Initialize(this, 100f * orbHealthMultiplier, false);
    }

    private void CheckAndRespawnOrbs()
    {
        // 오브들이 모두 파괴되었다면 재생성 (안전장치)
        if (lightOrb == null && darkOrb == null && !isTransitioning)
        {
            Debug.Log("모든 오브가 파괴됨! 재생성합니다.");
            StartCoroutine(SpawnOrbsWithDelay());
        }
    }

    #endregion

    #region 오브 파괴 콜백

    public void OnLightOrbDestroyed()
    {
        if (isTransitioning) return;

        Debug.Log("빛 구체 파괴! 어둠 모드로 전환!");
        lightOrb = null;
        SwitchToDarkMode();
    }

    public void OnDarkOrbDestroyed()
    {
        if (isTransitioning) return;

        Debug.Log("어둠 구체 파괴! 빛 모드로 전환!");
        darkOrb = null;
        SwitchToLightMode();
    }

    #endregion

    #region 모드 전환

    private void SwitchToLightMode()
    {
        if (lightModeBoss == null || isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(TransitionToMode(lightModeBoss, true));
    }

    private void SwitchToDarkMode()
    {
        if (darkModeBoss == null || isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(TransitionToMode(darkModeBoss, false));
    }

    private IEnumerator TransitionToMode(GameObject targetBoss, bool isLightMode)
    {
        Debug.Log($"{(isLightMode ? "빛" : "어둠")} 모드로 전환 중...");

        // 전환 이벤트 시간
        yield return new WaitForSeconds(1f);

        // 새로운 보스 생성
        GameObject newBoss = Instantiate(targetBoss, transform.position, transform.rotation);

        // 체력 전달
        if (isLightMode)
        {
            Enemy_Final_Boss_Light lightBoss = newBoss.GetComponent<Enemy_Final_Boss_Light>();
            if (lightBoss != null)
            {
                lightBoss.SetHealth(currentHp);
            }
        }
        else
        {
            Enemy_Final_Boss_Dark darkBoss = newBoss.GetComponent<Enemy_Final_Boss_Dark>();
            if (darkBoss != null)
            {
                darkBoss.SetHealth(currentHp);
            }
        }

        // 남은 구체 정리
        CleanupOrbs();

        // 자신 제거
        Destroy(gameObject);
    }

    #endregion

    #region 정리 및 유틸리티

    private void CleanupOrbs()
    {
        if (lightOrb != null)
        {
            Destroy(lightOrb);
            lightOrb = null;
        }
        if (darkOrb != null)
        {
            Destroy(darkOrb);
            darkOrb = null;
        }
    }

    protected override void Die()
    {
        CleanupOrbs();
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 1500; // 최종보스답게 높은 경험치
    }

    #endregion

    #region 런타임에서 오브 설정 변경 가능

    public void SetOrbDifficulty(float healthMultiplier, float speedMultiplier)
    {
        orbHealthMultiplier = healthMultiplier;
        orbSpeedMultiplier = speedMultiplier;

        // 기존 오브들에도 적용
        if (lightOrb != null)
        {
            Enemy_Final_Boss_LightOrb lightScript = lightOrb.GetComponent<Enemy_Final_Boss_LightOrb>();
            if (lightScript != null)
            {
                lightScript.SetHealth(100f * healthMultiplier);
                lightScript.SetMoveSpeed(3f * speedMultiplier);
            }
        }

        if (darkOrb != null)
        {
            Enemy_Final_Boss_DarkOrb darkScript = darkOrb.GetComponent<Enemy_Final_Boss_DarkOrb>();
            if (darkScript != null)
            {
                darkScript.SetHealth(100f * healthMultiplier);
                darkScript.SetMoveSpeed(3f * speedMultiplier);
            }
        }
    }

    // 수동으로 오브 재생성
    public void RespawnOrbs()
    {
        CleanupOrbs();
        StartCoroutine(SpawnOrbsWithDelay());
    }

    // 특정 오브만 재생성
    public void RespawnLightOrb()
    {
        if (lightOrb != null) Destroy(lightOrb);
        SpawnLightOrb();
    }

    public void RespawnDarkOrb()
    {
        if (darkOrb != null) Destroy(darkOrb);
        SpawnDarkOrb();
    }

    #endregion

    #region 디버그용 메서드들

    [System.Serializable]
    public class OrbStatus
    {
        public bool lightOrbActive;
        public bool darkOrbActive;
        public Vector3 lightOrbPosition;
        public Vector3 darkOrbPosition;
    }

    public OrbStatus GetOrbStatus()
    {
        return new OrbStatus
        {
            lightOrbActive = lightOrb != null,
            darkOrbActive = darkOrb != null,
            lightOrbPosition = lightOrb != null ? lightOrb.transform.position : Vector3.zero,
            darkOrbPosition = darkOrb != null ? darkOrb.transform.position : Vector3.zero
        };
    }

    public bool IsPerformingSpecialAction() => isTransitioning || isSpawningOrbs;

    #endregion

    #region 에디터에서 확인용

    private void OnDrawGizmosSelected()
    {
        if (orbSpawnPositions != null)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < orbSpawnPositions.Length; i++)
            {
                Vector3 worldPos = Application.isPlaying ? orbSpawnPositions[i] : transform.position + orbSpawnPositions[i];
                Gizmos.DrawWireSphere(worldPos, 1f);

                if (i == 0) Gizmos.color = Color.cyan;    // 빛 구체 위치
                else if (i == 1) Gizmos.color = Color.magenta; // 어둠 구체 위치
            }
        }
    }

    #endregion
}