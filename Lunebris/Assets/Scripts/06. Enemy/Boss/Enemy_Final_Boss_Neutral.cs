using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 최종보스 - 중립 모드 (오브 관리만) - 오브 재생성 방식으로 변경
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

    [Header("모드 전환 (사용 안함)")]
    [SerializeField] private GameObject lightModeBoss; // 빛 모드 보스 오브젝트
    [SerializeField] private GameObject darkModeBoss;  // 어둠 모드 보스 오브젝트

    [Header("보스 설정 (오브 난이도용)")]
    [SerializeField] private float orbHealthMultiplier = 1f;
    [SerializeField] private float orbSpeedMultiplier = 1f;

    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("오브 재생성 설정")]
    [SerializeField] private float orbRespawnDelay = 3f; // 오브 재생성 딜레이
    [SerializeField] private bool autoRespawnOrbs = true; // 자동 재생성 여부

    // 오브 관리
    private GameObject lightOrb;
    private GameObject darkOrb;
    private bool isTransitioning = false;

    // 상태 플래그
    private bool isSpawningOrbs = false;

    // 쿨다운 관리
    private float orbRespawnCooldown = 5f;
    private float lastOrbCheckTime = 0f;

    // 오브 파괴 시간 추적
    private float lightOrbDestroyTime = -1f;
    private float darkOrbDestroyTime = -1f;

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

        Debug.Log($"중립 모드 보스 초기화 - spawnOrbsOnStart: {spawnOrbsOnStart}");
        Debug.Log($"프리팹 상태 - 라이트: {lightOrbPrefab != null}, 다크: {darkOrbPrefab != null}");

        if (spawnOrbsOnStart)
        {
            Debug.Log("오브 생성 시작!");
            StartCoroutine(SpawnOrbsWithDelay());
        }
        else
        {
            Debug.Log("spawnOrbsOnStart가 false로 설정됨 - 수동으로 RespawnOrbs() 호출 필요");
        }

        Debug.Log("중립 모드 보스 등장! 구체들을 파괴하세요. 구체는 자동으로 재생성됩니다.");
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

        // 자동 재생성이 활성화되어 있으면 오브 상태 체크
        if (autoRespawnOrbs)
        {
            CheckAndRespawnOrbs();
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
        Debug.Log("오브 생성 루틴 시작!");

        // 빛 구체 먼저 생성 (CanSpawnOrb 체크 건너뛰기)
        Debug.Log("1초 후 빛 구체 생성...");
        yield return new WaitForSeconds(orbSpawnDelay);
        SpawnLightOrb(true); // 강제 생성 플래그

        // 어둠 구체 생성 (CanSpawnOrb 체크 건너뛰기)
        Debug.Log("1초 후 어둠 구체 생성...");
        yield return new WaitForSeconds(orbSpawnDelay);
        SpawnDarkOrb(true); // 강제 생성 플래그

        Debug.Log("모든 오브 생성 완료!");
        isSpawningOrbs = false;
    }

    private bool CanSpawnOrb()
    {
        return !isTransitioning && !isSpawningOrbs;
    }

    public void SpawnLightOrb(bool forceSpawn = false)
    {
        Debug.Log($"SpawnLightOrb 호출됨 - 기존 오브 존재: {lightOrb != null}, 강제생성: {forceSpawn}");

        if (lightOrb != null)
        {
            Debug.Log("이미 라이트 오브가 존재해서 생성하지 않음");
            return;
        }

        if (!forceSpawn && !CanSpawnOrb())
        {
            Debug.Log($"CanSpawnOrb() 실패 - isTransitioning: {isTransitioning}, isSpawningOrbs: {isSpawningOrbs}");
            return;
        }

        Vector3 spawnPos = orbSpawnPositions.Length > 0 ? orbSpawnPositions[0] : transform.position + Vector3.left * 8f;
        Debug.Log($"라이트 오브 스폰 위치: {spawnPos}");

        if (lightOrbPrefab != null)
        {
            Debug.Log("라이트 오브 프리팹으로 생성 시도...");
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

                Debug.Log("라이트 오브 프리팹 생성 및 초기화 완료!");
            }
            else
            {
                Debug.LogWarning("라이트 오브 프리팹에 Enemy_Final_Boss_LightOrb 스크립트가 없음!");
            }

            Debug.Log("빛 구체 생성됨!");
        }
        else
        {
            Debug.Log("라이트 오브 프리팹이 null - 임시 오브 생성");
            CreateTempLightOrb(spawnPos);
        }
    }

    public void SpawnDarkOrb(bool forceSpawn = false)
    {
        Debug.Log($"SpawnDarkOrb 호출됨 - 기존 오브 존재: {darkOrb != null}, 강제생성: {forceSpawn}");

        if (darkOrb != null)
        {
            Debug.Log("이미 다크 오브가 존재해서 생성하지 않음");
            return;
        }

        if (!forceSpawn && !CanSpawnOrb())
        {
            Debug.Log($"CanSpawnOrb() 실패 - isTransitioning: {isTransitioning}, isSpawningOrbs: {isSpawningOrbs}");
            return;
        }

        Vector3 spawnPos = orbSpawnPositions.Length > 1 ? orbSpawnPositions[1] : transform.position + Vector3.right * 8f;
        Debug.Log($"다크 오브 스폰 위치: {spawnPos}");

        if (darkOrbPrefab != null)
        {
            Debug.Log("다크 오브 프리팹으로 생성 시도...");
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

                Debug.Log("다크 오브 프리팹 생성 및 초기화 완료!");
            }
            else
            {
                Debug.LogWarning("다크 오브 프리팹에 Enemy_Final_Boss_DarkOrb 스크립트가 없음!");
            }

            Debug.Log("어둠 구체 생성됨!");
        }
        else
        {
            Debug.Log("다크 오브 프리팹이 null - 임시 오브 생성");
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

        // 임시 오브는 시각적 효과만 (체력 관리 없음)
        Debug.Log("임시 빛 구체 생성 (시각적 효과만)");
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

        // 임시 오브는 시각적 효과만 (체력 관리 없음)
        Debug.Log("임시 어둠 구체 생성 (시각적 효과만)");
    }

    private void CheckAndRespawnOrbs()
    {
        // 오브 생성 중일 때는 체크하지 않음
        if (isSpawningOrbs)
        {
            Debug.Log("오브 생성 중이므로 재생성 체크 건너뜀");
            return;
        }

        // 라이트 오브 재생성 체크
        if (lightOrb == null && lightOrbDestroyTime > 0 &&
            Time.time - lightOrbDestroyTime >= orbRespawnDelay)
        {
            Debug.Log("빛 구체 재생성!");
            SpawnLightOrb();
            lightOrbDestroyTime = -1f;
        }

        // 다크 오브 재생성 체크
        if (darkOrb == null && darkOrbDestroyTime > 0 &&
            Time.time - darkOrbDestroyTime >= orbRespawnDelay)
        {
            Debug.Log("어둠 구체 재생성!");
            SpawnDarkOrb();
            darkOrbDestroyTime = -1f;
        }

        // 모든 오브가 파괴되었고 파괴 시간도 기록되지 않은 경우만 즉시 재생성
        if (lightOrb == null && darkOrb == null &&
            lightOrbDestroyTime < 0 && darkOrbDestroyTime < 0)
        {
            Debug.Log("모든 오브가 파괴됨! 즉시 재생성합니다.");
            StartCoroutine(SpawnOrbsWithDelay());
        }
    }

    #endregion

    #region 오브 파괴 콜백 (모드 전환)

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
        Debug.Log($"{(isLightMode ? "빛" : "어둠")} 모드로 수동 전환 중...");

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
        lightOrbDestroyTime = -1f;
        darkOrbDestroyTime = -1f;
        StartCoroutine(SpawnOrbsWithDelay());
    }

    // 특정 오브만 재생성
    public void RespawnLightOrb()
    {
        if (lightOrb != null) Destroy(lightOrb);
        lightOrbDestroyTime = -1f;
        SpawnLightOrb(); // 기본 호출 (forceSpawn=false)
    }

    public void RespawnDarkOrb()
    {
        if (darkOrb != null) Destroy(darkOrb);
        darkOrbDestroyTime = -1f;
        SpawnDarkOrb(); // 기본 호출 (forceSpawn=false)
    }

    // 자동 재생성 토글
    public void SetAutoRespawn(bool enable)
    {
        autoRespawnOrbs = enable;
        Debug.Log($"오브 자동 재생성: {(enable ? "활성화" : "비활성화")}");
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
        public float lightOrbRespawnTime;
        public float darkOrbRespawnTime;
    }

    public OrbStatus GetOrbStatus()
    {
        return new OrbStatus
        {
            lightOrbActive = lightOrb != null,
            darkOrbActive = darkOrb != null,
            lightOrbPosition = lightOrb != null ? lightOrb.transform.position : Vector3.zero,
            darkOrbPosition = darkOrb != null ? darkOrb.transform.position : Vector3.zero,
            lightOrbRespawnTime = lightOrbDestroyTime > 0 ? orbRespawnDelay - (Time.time - lightOrbDestroyTime) : -1f,
            darkOrbRespawnTime = darkOrbDestroyTime > 0 ? orbRespawnDelay - (Time.time - darkOrbDestroyTime) : -1f
        };
    }

    public bool IsPerformingSpecialAction() => isTransitioning || isSpawningOrbs;

    // 한방에 죽는 버그 디버깅용
    public void DebugOrbHealth()
    {
        if (lightOrb != null)
        {
            Enemy_Final_Boss_LightOrb lightScript = lightOrb.GetComponent<Enemy_Final_Boss_LightOrb>();
            Debug.Log($"라이트 오브 - 메인 스크립트: {lightScript != null}");
        }

        if (darkOrb != null)
        {
            Enemy_Final_Boss_DarkOrb darkScript = darkOrb.GetComponent<Enemy_Final_Boss_DarkOrb>();
            Debug.Log($"다크 오브 - 메인 스크립트: {darkScript != null}");
        }
    }

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