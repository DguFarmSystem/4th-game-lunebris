using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스 빛 모드 - 빛 기둥 공격 (단일 기둥 + 슬로우 효과)
/// </summary>
public class Enemy_Final_Boss_LightPillar : MonoBehaviour
{
    [Header("빛 기둥 설정")]
    [SerializeField] private float pillarRadius = 4f;
    [SerializeField] private float pillarHeight = 20f;

    [Header("공격 설정")]
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float activeDuration = 2f;

    [Header("슬로우 효과 설정")]
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowIntensity = 0.7f; // 70% 속도 감소

    [Header("이펙트")]
    [SerializeField] private GameObject warningEffect;   // 프리팹
    [SerializeField] private GameObject pillarEffect;
    [SerializeField] private Light pillarLight;

    [Header("===== 효과음 설정 =====")]
    [Header("경고 단계 효과음")]
    [SerializeField] private AudioClip warningStartSound;      // 경고 시작 효과음
    [SerializeField] private AudioClip warningLoopSound;       // 경고 지속 효과음 (루프)
    [SerializeField] private AudioClip warningEndSound;        // 경고 종료 효과음

    [Header("활성화 단계 효과음")]
    [SerializeField] private AudioClip pillarActivateSound;    // 기둥 활성화 효과음
    [SerializeField] private AudioClip pillarActiveLoopSound;  // 기둥 활성 상태 지속음 (루프)
    [SerializeField] private AudioClip pillarDeactivateSound;  // 기둥 비활성화 효과음

    [Header("타격 효과음")]
    [SerializeField] private AudioClip playerHitSound;         // 플레이어 타격 효과음
    [SerializeField] private AudioClip slowApplySound;         // 슬로우 효과 적용 효과음

    [Header("빛 효과음")]
    [SerializeField] private AudioClip lightChargeSound;       // 빛 차징 효과음
    [SerializeField] private AudioClip lightBurstSound;        // 빛 폭발 효과음

    [Header("효과음 볼륨 설정")]
    [SerializeField] private float masterVolume = 1f;          // 전체 효과음 볼륨
    [SerializeField] private float warningVolume = 0.7f;       // 경고 효과음 볼륨
    [SerializeField] private float activeVolume = 1f;          // 활성화 효과음 볼륨
    [SerializeField] private float hitVolume = 0.9f;           // 타격 효과음 볼륨
    [SerializeField] private float ambientVolume = 0.5f;       // 지속 효과음 볼륨

    private GameObject instantiatedWarningEffect;
    private float damage;
    private bool isActive = false;
    private AudioSource audioSource;
    private AudioSource loopAudioSource;  // 루프 사운드용 별도 AudioSource
    private CapsuleCollider damageCollider;

    private void Awake()
    {
        // 기존 오디오소스 또는 새로 생성
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 오디오소스 설정
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 50f;
        audioSource.volume = masterVolume;

        // 루프용 오디오소스 추가
        loopAudioSource = gameObject.AddComponent<AudioSource>();
        loopAudioSource.playOnAwake = false;
        loopAudioSource.spatialBlend = 1f;
        loopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        loopAudioSource.minDistance = 5f;
        loopAudioSource.maxDistance = 50f;
        loopAudioSource.loop = true;
        loopAudioSource.volume = ambientVolume * masterVolume;

        // 콜라이더 설정
        damageCollider = GetComponent<CapsuleCollider>();
        if (damageCollider == null)
            damageCollider = gameObject.AddComponent<CapsuleCollider>();

        damageCollider.isTrigger = true;
        damageCollider.radius = pillarRadius;
        damageCollider.height = pillarHeight;
        damageCollider.center = new Vector3(0, pillarHeight * 0.5f, 0);
        damageCollider.enabled = false;
        damageCollider.material = null; // 물리 재질 제거

        if (pillarLight == null)
        {
            pillarLight = GetComponent<Light>();
            if (pillarLight == null)
                pillarLight = gameObject.AddComponent<Light>();
        }

        SetupPillarLight();
    }

    private void SetupPillarLight()
    {
        if (pillarLight == null) return;

        pillarLight.type = LightType.Point;
        pillarLight.color = Color.white;
        pillarLight.intensity = 0f;
        pillarLight.range = pillarRadius * 2f;
    }

    public void StartLightPillar(float pillarDamage, float duration = 3.5f)
    {
        damage = pillarDamage;
        warningDuration = duration * 0.4f;
        activeDuration = duration * 0.6f;

        StartCoroutine(PillarSequence());
    }

    /// <summary>
    /// 슬로우 효과 설정
    /// </summary>
    public void SetSlowEffect(float duration, float intensity)
    {
        slowDuration = duration;
        slowIntensity = intensity;
    }

    private IEnumerator PillarSequence()
    {
        yield return StartCoroutine(WarningPhase());
        yield return StartCoroutine(ActivePhase());
        Cleanup();
        Destroy(gameObject);
    }

    private IEnumerator WarningPhase()
    {
        // 경고 시작 효과음
        PlaySound(warningStartSound, warningVolume);

        // 빛 차징 효과음 (있을 경우)
        if (lightChargeSound != null)
        {
            PlaySoundDelayed(lightChargeSound, warningVolume * 0.8f, 0.2f);
        }

        // 경고 루프 효과음 시작
        PlayLoopSound(warningLoopSound, ambientVolume);

        if (warningEffect != null)
        {
            instantiatedWarningEffect = Instantiate(
                warningEffect,
                transform.position + Vector3.down * 0.1f,  // 바닥에 살짝 위치
                Quaternion.identity,
                transform // 부모를 이 오브젝트로 설정
            );
            instantiatedWarningEffect.SetActive(true);
        }

        // 경고 지속 시간의 90%까지 대기
        yield return new WaitForSeconds(warningDuration * 0.9f);

        // 경고 종료 효과음
        PlaySound(warningEndSound, warningVolume);

        // 루프 사운드 정지
        StopLoopSound();

        // 남은 10% 시간 대기
        yield return new WaitForSeconds(warningDuration * 0.1f);

        if (instantiatedWarningEffect != null)
        {
            Destroy(instantiatedWarningEffect);
        }
    }

    private IEnumerator ActivePhase()
    {
        // 기둥 활성화 효과음
        PlaySound(pillarActivateSound, activeVolume);

        // 빛 폭발 효과음
        if (lightBurstSound != null)
        {
            PlaySoundDelayed(lightBurstSound, activeVolume * 0.9f, 0.1f);
        }

        if (pillarEffect != null)
        {
            pillarEffect.SetActive(true);
        }

        damageCollider.enabled = true;
        isActive = true;

        // 활성 상태 루프 효과음 시작
        PlayLoopSound(pillarActiveLoopSound, ambientVolume);

        if (pillarLight != null)
        {
            // 빛 강도를 점진적으로 증가
            StartCoroutine(FadeLightIntensity(0f, 10f, 0.3f));
            pillarLight.color = Color.white;
        }

        // 활성 지속 시간의 90%까지 대기
        yield return new WaitForSeconds(activeDuration * 0.9f);

        // 기둥 비활성화 준비 효과음
        if (pillarDeactivateSound != null)
        {
            PlaySound(pillarDeactivateSound, activeVolume * 0.7f);
        }

        // 남은 10% 시간 대기
        yield return new WaitForSeconds(activeDuration * 0.1f);

        isActive = false;
        damageCollider.enabled = false;

        // 루프 사운드 페이드 아웃
        StartCoroutine(FadeOutLoopSound(0.5f));

        // 빛 강도 페이드 아웃
        if (pillarLight != null)
        {
            StartCoroutine(FadeLightIntensity(pillarLight.intensity, 0f, 0.5f));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            Player.Player playerComponent = other.GetComponent<Player.Player>();
            if (playerComponent != null)
            {
                // 타격 효과음 재생
                PlaySound(playerHitSound, hitVolume);

                // 데미지 적용
                playerComponent.DecreaseHP(damage);

                // 슬로우 효과 적용
                ApplySlowEffectToPlayer(other.gameObject);

                // 슬로우 적용 효과음
                PlaySoundDelayed(slowApplySound, hitVolume * 0.8f, 0.1f);

                Debug.Log($"플레이어가 빛 기둥에 맞음! 데미지: {damage}, 슬로우: {slowIntensity * 100}% 감속 {slowDuration}초");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            // 지속 데미지 로직은 필요시 구현
        }
    }

    /// <summary>
    /// 플레이어에게 슬로우 효과 적용
    /// </summary>
    private void ApplySlowEffectToPlayer(GameObject player)
    {
        // 방법 1: 보스 스크립트를 통해 슬로우 효과 적용 (가장 안전)
        Enemy_Final_Boss_Light boss = FindObjectOfType<Enemy_Final_Boss_Light>();
        if (boss != null)
        {
            boss.ApplySlowToPlayer(slowDuration, slowIntensity);
            return;
        }

        // 방법 2: 플레이어 스크립트에서 슬로우 메서드 직접 호출
        Player.Player playerScript = player.GetComponent<Player.Player>();
        if (playerScript != null)
        {
            // ApplySlowEffect 메서드가 있는지 확인
            var slowMethod = playerScript.GetType().GetMethod("ApplySlowEffect");
            if (slowMethod != null)
            {
                slowMethod.Invoke(playerScript, new object[] { slowDuration, slowIntensity });
                Debug.Log($"플레이어에게 직접 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
                return;
            }
        }

        // 방법 3: 플레이어 이동 컴포넌트에서 슬로우 메서드 호출
        var movementComponents = player.GetComponents<MonoBehaviour>();
        foreach (var component in movementComponents)
        {
            var moveSlowMethod = component.GetType().GetMethod("ApplySlowEffect");
            if (moveSlowMethod != null)
            {
                moveSlowMethod.Invoke(component, new object[] { slowDuration, slowIntensity });
                Debug.Log($"플레이어 이동 컴포넌트에 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
                return;
            }
        }

        // 방법 4: 직접 Rigidbody 제어 (백업용)
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            StartCoroutine(ApplyDirectSlowEffect(playerRb));
            Debug.Log($"Rigidbody 직접 제어로 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
        }
        else
        {
            Debug.LogWarning("플레이어에게 슬로우 효과를 적용할 수 없습니다!");
        }
    }

    /// <summary>
    /// 직접 슬로우 효과 적용 (백업용)
    /// </summary>
    private IEnumerator ApplyDirectSlowEffect(Rigidbody playerRb)
    {
        float originalDrag = playerRb.drag;
        float slowDrag = originalDrag + (slowIntensity * 10f); // 드래그 증가로 슬로우 효과

        playerRb.drag = slowDrag;
        yield return new WaitForSeconds(slowDuration);

        // 원래 드래그 값으로 복원
        if (playerRb != null) // null 체크 (플레이어가 파괴될 수 있음)
        {
            playerRb.drag = originalDrag;
        }
    }

    private void Cleanup()
    {
        if (instantiatedWarningEffect != null)
            Destroy(instantiatedWarningEffect);

        if (pillarEffect != null)
            pillarEffect.SetActive(false);

        if (pillarLight != null)
            pillarLight.intensity = 0f;

        isActive = false;
        damageCollider.enabled = false;

        // 모든 사운드 정지
        StopAllSounds();
    }

    #region 오디오 관련 메서드

    /// <summary>
    /// 일반 효과음 재생
    /// </summary>
    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume * masterVolume);
        }
    }

    /// <summary>
    /// 지연된 효과음 재생
    /// </summary>
    private void PlaySoundDelayed(AudioClip clip, float volume, float delay)
    {
        if (clip != null)
        {
            StartCoroutine(PlaySoundAfterDelay(clip, volume, delay));
        }
    }

    private IEnumerator PlaySoundAfterDelay(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip, volume);
    }

    /// <summary>
    /// 루프 효과음 재생
    /// </summary>
    private void PlayLoopSound(AudioClip clip, float volume = 1f)
    {
        if (loopAudioSource != null && clip != null)
        {
            loopAudioSource.clip = clip;
            loopAudioSource.volume = volume * masterVolume;
            loopAudioSource.Play();
        }
    }

    /// <summary>
    /// 루프 효과음 정지
    /// </summary>
    private void StopLoopSound()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }
    }

    /// <summary>
    /// 루프 사운드 페이드 아웃
    /// </summary>
    private IEnumerator FadeOutLoopSound(float fadeTime)
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            float startVolume = loopAudioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                loopAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            loopAudioSource.Stop();
            loopAudioSource.volume = ambientVolume * masterVolume; // 원래 볼륨으로 복원
        }
    }

    /// <summary>
    /// 모든 사운드 정지
    /// </summary>
    private void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (loopAudioSource != null)
        {
            loopAudioSource.Stop();
        }
    }

    /// <summary>
    /// 빛 강도 페이드
    /// </summary>
    private IEnumerator FadeLightIntensity(float startIntensity, float endIntensity, float duration)
    {
        if (pillarLight == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            pillarLight.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            yield return null;
        }

        pillarLight.intensity = endIntensity;
    }

    #endregion

    #region 볼륨 설정 메서드

    /// <summary>
    /// 마스터 볼륨 설정
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }

        if (loopAudioSource != null)
        {
            loopAudioSource.volume = ambientVolume * masterVolume;
        }
    }

    /// <summary>
    /// 경고 효과음 볼륨 설정
    /// </summary>
    public void SetWarningVolume(float volume)
    {
        warningVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 활성화 효과음 볼륨 설정
    /// </summary>
    public void SetActiveVolume(float volume)
    {
        activeVolume = Mathf.Clamp01(volume);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position + Vector3.up * pillarHeight * 0.5f, pillarRadius);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 edgePoint = position + new Vector3(
                Mathf.Cos(angle) * pillarRadius,
                0,
                Mathf.Sin(angle) * pillarRadius
            );
            Gizmos.DrawLine(edgePoint, edgePoint + Vector3.up * pillarHeight);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position + Vector3.up * 0.1f, pillarRadius);

        for (int i = 0; i < 16; i++)
        {
            float angle1 = i * 22.5f * Mathf.Deg2Rad;
            float angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
            Vector3 point1 = position + new Vector3(Mathf.Cos(angle1) * pillarRadius, 0.1f, Mathf.Sin(angle1) * pillarRadius);
            Vector3 point2 = position + new Vector3(Mathf.Cos(angle2) * pillarRadius, 0.1f, Mathf.Sin(angle2) * pillarRadius);
            Gizmos.DrawLine(point1, point2);
        }

        // 슬로우 효과 범위 표시 (시각적으로 구분)
        if (isActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position + Vector3.up * pillarHeight * 0.5f, pillarRadius * 1.2f);
        }
    }

    public void SetPillarSize(float radius, float height)
    {
        pillarRadius = radius;
        pillarHeight = height;

        if (damageCollider != null)
        {
            damageCollider.radius = radius;
            damageCollider.height = height;
            damageCollider.center = new Vector3(0, height * 0.5f, 0);
        }

        if (pillarLight != null)
        {
            pillarLight.range = radius * 2f;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 모든 사운드 정지
        StopAllSounds();
    }

    public bool IsWarning => Time.time < warningDuration && !isActive;
    public bool IsActiveState => isActive;
    public float SlowDuration => slowDuration;
    public float SlowIntensity => slowIntensity;
}