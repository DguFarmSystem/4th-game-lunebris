using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어를 느리게 만드는 장판 효과 - 사운드 포함
/// </summary>
public class Enemy_Tanker_SlowArea : MonoBehaviour
{
    [Header("장판 설정")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float slowAmount = 0.5f; // 이동속도 감소량 (0.5 = 50% 감소)

    [Header("사운드 설정")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip explosionSound; // 폭발 사운드 (장판 생성시)
    [SerializeField] private AudioClip enterSound; // 플레이어 진입 사운드
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f;
    [SerializeField] private bool useRandomPitch = true;
    [SerializeField][Range(0.8f, 1.2f)] private float minPitch = 0.9f;
    [SerializeField][Range(0.8f, 1.2f)] private float maxPitch = 1.1f;

    private Player.Player playerScript;
    private bool isPlayerInside = false;
    private bool hasAppliedSlow = false;

    private void Start()
    {
        // AudioSource 자동 설정
        SetupAudioSource();

        // 생성시 즉시 폭발 사운드 재생
        PlayExplosionSound();

        Debug.Log("슬로우 장판이 활성화되었습니다!");
    }

    /// <summary>
    /// AudioSource 자동 설정
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // AudioSource 기본 설정
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 20f;
            audioSource.minDistance = 2f;
        }
    }

    /// <summary>
    /// 폭발 사운드 재생 (장판 생성시)
    /// </summary>
    private void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            PlaySound(explosionSound);
            Debug.Log("슬로우 장판 폭발 사운드 재생");
        }
    }

    /// <summary>
    /// 플레이어 진입 사운드 재생
    /// </summary>
    private void PlayEnterSound()
    {
        if (enterSound != null)
        {
            PlaySound(enterSound);
            Debug.Log("플레이어 진입 사운드 재생");
        }
    }

    /// <summary>
    /// 사운드 재생 (공통 메서드)
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        // 볼륨 설정
        audioSource.volume = soundVolume;

        // 랜덤 피치 적용
        if (useRandomPitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        // 사운드 재생
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 탱커에서 폭발 사운드 정보 설정
    /// </summary>
    public void SetExplosionSound(AudioClip sound, float volume)
    {
        explosionSound = sound;
        soundVolume = volume;

        // 즉시 폭발 사운드 재생
        PlayExplosionSound();

        Debug.Log("SlowArea: 탱커로부터 폭발 사운드 정보 설정됨");
    }

    /// <summary>
    /// 장판 초기화
    /// </summary>
    /// <param name="areaRadius">장판 반지름</param>
    /// <param name="areaDuration">지속 시간</param>
    /// <param name="slowValue">슬로우 강도</param>
    public void Initialize(float areaRadius, float areaDuration, float slowValue)
    {
        radius = areaRadius;
        duration = areaDuration;
        slowAmount = slowValue;

        // 장판 크기 조정
        transform.localScale = new Vector3(radius * 2, transform.localScale.y, radius * 2);

        // 지속시간 후 자동 제거
        StartCoroutine(LifeTimeCoroutine());

        Debug.Log($"슬로우 장판 생성! 반지름: {radius}, 지속시간: {duration}초, 슬로우: {slowAmount * 100}%");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerScript = other.GetComponent<Player.Player>();
            if (playerScript != null)
            {
                isPlayerInside = true;
                ApplySlow();

                // 플레이어 진입 사운드 재생
                PlayEnterSound();

                Debug.Log("플레이어가 슬로우 장판에 진입!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            RemoveSlow();
            Debug.Log("플레이어가 슬로우 장판에서 벗어남!");
        }
    }

    /// <summary>
    /// 슬로우 효과 적용
    /// </summary>
    private void ApplySlow()
    {
        if (playerScript != null && !hasAppliedSlow)
        {
            // 플레이어의 이동속도 감소
            float slowValue = -playerScript.GetMoveSpeed() * slowAmount;
            playerScript.IncreaseMoveSpeed(slowValue);
            hasAppliedSlow = true;
            Debug.Log($"슬로우 효과 적용! 감소량: {slowValue}");
        }
    }

    /// <summary>
    /// 슬로우 효과 제거
    /// </summary>
    private void RemoveSlow()
    {
        if (playerScript != null && hasAppliedSlow)
        {
            // 감소했던 이동속도 복구
            float restoreValue = playerScript.GetMoveSpeed() * slowAmount / (1 - slowAmount);
            playerScript.IncreaseMoveSpeed(restoreValue);
            hasAppliedSlow = false;
            Debug.Log($"슬로우 효과 제거! 복구량: {restoreValue}");
        }
    }

    /// <summary>
    /// 장판 수명 관리
    /// </summary>
    private IEnumerator LifeTimeCoroutine()
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 페이드 아웃 효과 (선택사항)
            if (timer > duration * 0.7f) // 마지막 30% 시간에서 페이드
            {
                float fadeRatio = (duration - timer) / (duration * 0.3f);
                SetAlpha(fadeRatio);
            }

            yield return null;
        }

        // 장판 제거 전 슬로우 효과 해제
        if (isPlayerInside)
        {
            RemoveSlow();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 장판 투명도 조절
    /// </summary>
    private void SetAlpha(float alpha)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = renderer.material.color;
            color.a = Mathf.Clamp01(alpha * 0.3f); // 최대 투명도 30%
            renderer.material.color = color;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴시 슬로우 효과 정리
        if (isPlayerInside && hasAppliedSlow)
        {
            RemoveSlow();
        }

        Debug.Log("슬로우 장판이 제거되었습니다.");
    }
}