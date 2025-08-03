using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 최종보스 적 바닥 - 보스 추적 상시 지속
/// </summary>
public class Enemy_Final_Boss_DarkFloor : MonoBehaviour
{
    [Header("지속 설정")]
    [SerializeField] private float floorRadius = 6f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float detectionInterval = 0.1f; // 플레이어 감지 주기

    [Header("이펙트")]
    [SerializeField] private GameObject floorEffect;
    [SerializeField] private Light floorLight;
    [SerializeField] private AudioClip ambientSound;

    [Header("시각적 설정")]
    [SerializeField] private float pulseIntensity = 0.3f;

    private float damage;
    private Transform bossTransform;
    private bool isActive = false;
    private Dictionary<Transform, float> lastDamageTime = new Dictionary<Transform, float>();
    private AudioSource audioSource;
    private List<Transform> playersInRange = new List<Transform>();
    private float lastDetectionTime = 0f;

    public void Initialize(float floorDamage, Transform boss)
    {
        damage = floorDamage;
        bossTransform = boss;

        SetupComponents();
        StartCoroutine(ActivateFloor());
    }

    private void SetupComponents()
    {
        // 오디오 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.loop = true;

        // 콜라이더 제거 - 순수 거리 계산 방식 사용

        // 지속 조명 설정
        if (floorLight == null)
            floorLight = gameObject.AddComponent<Light>();

        floorLight.type = LightType.Point;
        floorLight.color = Color.red;
        floorLight.intensity = 2f;
        floorLight.range = floorRadius * 2f;

        // 시각적 이펙트 설정
        if (floorEffect != null)
        {
            floorEffect.SetActive(false);
            floorEffect.transform.localScale = Vector3.one * floorRadius * 2f;
        }

        // 보스 위치로 이동
        if (bossTransform != null)
        {
            Vector3 pos = bossTransform.position;
            pos.y = 0.1f; // 지면보다 살짝 위
            transform.position = pos;
        }
    }

    private IEnumerator ActivateFloor()
    {
        isActive = true;

        // 이펙트 활성화
        if (floorEffect != null)
            floorEffect.SetActive(true);

        // 주변 사운드 시작
        if (ambientSound != null && audioSource != null)
        {
            audioSource.clip = ambientSound;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }

        // 지속 확산 애니메이션
        float expandTime = 1f;
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * floorRadius * 2f;

        while (elapsed < expandTime)
        {
            float progress = elapsed / expandTime;

            if (floorEffect != null)
            {
                floorEffect.transform.localScale = targetScale * progress;
            }

            if (floorLight != null)
            {
                floorLight.intensity = Mathf.Lerp(0f, 2f, progress);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 완전 활성화
        if (floorEffect != null)
            floorEffect.transform.localScale = targetScale;
    }

    private void Update()
    {
        if (!isActive) return;

        FollowBoss();
        UpdateVisualEffects();
        DetectPlayersInRange(); // 거리 기반 플레이어 감지
        ProcessDamage();
    }

    private void FollowBoss()
    {
        if (bossTransform == null) return;

        Vector3 targetPosition = bossTransform.position;
        targetPosition.y = 0.1f; // Y축 고정

        // 부드럽게 보스 따라가기
        Vector3 newPosition = Vector3.Lerp(transform.position, targetPosition,
            followSpeed * Time.deltaTime);

        transform.position = newPosition;
    }

    private void DetectPlayersInRange()
    {
        // 감지 주기 제어 (성능 최적화)
        if (Time.time - lastDetectionTime < detectionInterval)
            return;

        lastDetectionTime = Time.time;

        // 현재 범위 내 플레이어들 체크
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        List<Transform> currentPlayersInRange = new List<Transform>();

        foreach (GameObject playerObj in players)
        {
            if (playerObj == null) continue;

            float distance = Vector3.Distance(transform.position, playerObj.transform.position);

            // 플레이어가 범위 내에 있는지 체크
            if (distance <= floorRadius)
            {
                currentPlayersInRange.Add(playerObj.transform);
            }
        }

        // 새로 들어온 플레이어들 처리
        foreach (Transform player in currentPlayersInRange)
        {
            if (!playersInRange.Contains(player))
            {
                playersInRange.Add(player);
                Debug.Log($"플레이어 {player.name}이 장판에 진입했습니다.");
            }
        }

        // 벗어난 플레이어들 처리
        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            Transform player = playersInRange[i];
            if (player == null || !currentPlayersInRange.Contains(player))
            {
                playersInRange.RemoveAt(i);
                if (lastDamageTime.ContainsKey(player))
                    lastDamageTime.Remove(player);

                if (player != null)
                    Debug.Log($"플레이어 {player.name}이 장판에서 벗어났습니다.");
            }
        }
    }

    private void UpdateVisualEffects()
    {
        // 맥동 효과
        if (floorLight != null)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * pulseIntensity + (2f - pulseIntensity);
            floorLight.intensity = pulse;
        }
    }

    private void ProcessDamage()
    {
        if (playersInRange.Count == 0) return;

        // 안전한 순회를 위해 리스트 복사
        List<Transform> playersToProcess = new List<Transform>(playersInRange);

        foreach (Transform playerTransform in playersToProcess)
        {
            if (playerTransform == null) continue;

            // 데미지 간격 체크
            if (!lastDamageTime.ContainsKey(playerTransform))
                lastDamageTime[playerTransform] = 0f;

            if (Time.time - lastDamageTime[playerTransform] >= damageInterval)
            {
                DealDamageToPlayer(playerTransform);
                lastDamageTime[playerTransform] = Time.time;
            }
        }
    }

    private void DealDamageToPlayer(Transform playerTransform)
    {
        Player.Player player = playerTransform.GetComponent<Player.Player>();
        if (player != null)
        {
            player.DecreaseHP(damage);
            StartCoroutine(DamageFlash());
        }
    }

    private IEnumerator DamageFlash()
    {
        // 데미지를 줄 때 지속이 더 밝아짐
        if (floorLight != null)
        {
            float originalIntensity = floorLight.intensity;
            Color originalColor = floorLight.color;

            floorLight.intensity = 4f;
            floorLight.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            floorLight.intensity = originalIntensity;
            floorLight.color = originalColor;
        }
    }

    public void DeactivateFloor()
    {
        if (!isActive) return;
        StartCoroutine(DeactivateSequence());
    }

    private IEnumerator DeactivateSequence()
    {
        isActive = false;

        // 사운드 정지
        if (audioSource != null)
            audioSource.Stop();

        // 축소 애니메이션
        float shrinkTime = 0.8f;
        float elapsed = 0f;
        Vector3 originalScale = floorEffect != null ? floorEffect.transform.localScale : Vector3.one;
        float originalIntensity = floorLight != null ? floorLight.intensity : 0f;

        while (elapsed < shrinkTime)
        {
            float progress = elapsed / shrinkTime;
            float scale = Mathf.Lerp(1f, 0f, progress);

            if (floorEffect != null)
                floorEffect.transform.localScale = originalScale * scale;

            if (floorLight != null)
                floorLight.intensity = Mathf.Lerp(originalIntensity, 0f, progress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 완전히 비활성화
        if (floorEffect != null)
            floorEffect.SetActive(false);

        if (floorLight != null)
            floorLight.enabled = false;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        playersInRange.Clear();
        lastDamageTime.Clear();
    }

    // 디버깅을 위한 Gizmos - 장판 범위 시각화
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, floorRadius);

        if (playersInRange != null && playersInRange.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform player in playersInRange)
            {
                if (player != null)
                    Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}