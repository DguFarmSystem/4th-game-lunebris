using UnityEngine;

/// <summary>
/// 탱커가 던지는 슬로우 구체 투사체
/// </summary>
public class Enemy_Tanker_SlowProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float arcHeight = 2f; // 포물선 높이

    [Header("장판 설정")]
    [SerializeField] private GameObject slowAreaPrefab; // 슬로우 장판 프리팹
    [SerializeField] private float areaRadius = 3f;
    [SerializeField] private float slowDuration = 5f;
    [SerializeField] private float slowAmount = 0.5f; // 이동속도 50% 감소

    [Header("효과음 설정")]
    [SerializeField] private AudioClip explosionSound; // 폭발 효과음
    [SerializeField] private AudioClip destroySound; // 일반 삭제 효과음
    [SerializeField] private float soundVolume = 1f; // 효과음 볼륨

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 targetPosition;
    private bool hasExploded = false;
    private bool isLaunched = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // AudioSource 컴포넌트 확인 및 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // AudioSource 기본 설정
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D 사운드로 설정
        audioSource.volume = soundVolume;
    }

    private void Start()
    {
        // 수명 시간 후 자동 제거
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 발사됐는데 속도가 너무 느려지면 강제 폭발
        if (isLaunched && rb.velocity.magnitude < 1f)
        {
            CreateSlowArea();
        }
    }

    /// <summary>
    /// 오브젝트가 파괴되기 직전에 호출
    /// </summary>
    private void OnDestroy()
    {
        // 효과음 재생을 위한 임시 오브젝트 생성 (오브젝트가 삭제되어도 효과음이 끝까지 재생되도록)
        if (!hasExploded && destroySound != null && Application.isPlaying)
        {
            PlaySoundAtPosition(destroySound, transform.position, soundVolume);
        }
    }

    /// <summary>
    /// 투사체 발사
    /// </summary>
    /// <param name="direction">발사 방향</param>
    public void Launch(Vector3 direction)
    {
        isLaunched = true;

        // 포물선 궤도로 발사
        Vector3 launchVelocity = CalculateArcVelocity(direction, speed, arcHeight);

        if (rb != null)
        {
            rb.velocity = launchVelocity;
        }

        Debug.Log($"투사체 발사! 속도: {launchVelocity}");
    }

    /// <summary>
    /// 포물선 속도 계산
    /// </summary>
    private Vector3 CalculateArcVelocity(Vector3 direction, float speed, float height)
    {
        Vector3 horizontalVelocity = direction.normalized * speed;
        float verticalVelocity = Mathf.Sqrt(2 * Physics.gravity.magnitude * height);

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 폭발했으면 무시
        if (hasExploded) return;

        Debug.Log($"투사체 충돌: {collision.gameObject.name}");

        // 플레이어 아닌 모든 오브젝트와 충돌시 폭발
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            CreateSlowArea();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 폭발했으면 무시
        if (hasExploded) return;

        Debug.Log($"투사체 트리거: {other.name}");

        // 플레이어와 충돌시 즉시 폭발
        if (other.CompareTag("Player"))
        {
            CreateSlowArea();
        }
    }

    /// <summary>
    /// 슬로우 장판 생성
    /// </summary>
    private void CreateSlowArea()
    {
        if (hasExploded) return;

        hasExploded = true;

        Debug.Log("슬로우 장판 생성!");

        // 폭발 효과음 재생
        if (explosionSound != null)
        {
            PlaySoundAtPosition(explosionSound, transform.position, soundVolume);
        }

        // 장판 생성 위치 (지면에 맞춤)
        Vector3 areaPosition = transform.position;
        areaPosition.y = 0.1f; // 지면 살짝 위

        // 장판 프리팹이 있으면 생성
        if (slowAreaPrefab != null)
        {
            GameObject areaObj = Instantiate(slowAreaPrefab, areaPosition, Quaternion.identity);

            // 장판 스크립트 설정
            Enemy_Tanker_SlowArea slowArea = areaObj.GetComponent<Enemy_Tanker_SlowArea>();
            if (slowArea != null)
            {
                slowArea.Initialize(areaRadius, slowDuration, slowAmount);
            }
        }
        else
        {
            // 프리팹이 없으면 기본 장판 생성
            CreateDefaultSlowArea(areaPosition);
        }

        // 투사체 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// 기본 장판 생성 (프리팹이 없을 때)
    /// </summary>
    private void CreateDefaultSlowArea(Vector3 position)
    {
        // 기본 원형 오브젝트 생성
        GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        area.transform.position = position;
        area.transform.localScale = new Vector3(areaRadius * 2, 0.1f, areaRadius * 2);

        // 반투명 빨간색 머티리얼
        Renderer renderer = area.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0f, 0f, 0.3f);
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;

        // 콜라이더를 트리거로 설정
        Collider collider = area.GetComponent<Collider>();
        collider.isTrigger = true;

        // SlowArea 컴포넌트 추가
        Enemy_Tanker_SlowArea slowArea = area.AddComponent<Enemy_Tanker_SlowArea>();
        slowArea.Initialize(areaRadius, slowDuration, slowAmount);
    }

    /// <summary>
    /// 특정 위치에서 효과음 재생 (오브젝트가 삭제되어도 효과음이 계속 재생되도록)
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="position">재생 위치</param>
    /// <param name="volume">볼륨</param>
    private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        // 임시 오브젝트 생성
        GameObject tempAudioObject = new GameObject("TempAudio_" + clip.name);
        tempAudioObject.transform.position = position;

        // AudioSource 추가 및 설정
        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        tempAudioSource.clip = clip;
        tempAudioSource.volume = volume;
        tempAudioSource.spatialBlend = 1f; // 3D 사운드
        tempAudioSource.rolloffMode = AudioRolloffMode.Linear;
        tempAudioSource.minDistance = 5f;
        tempAudioSource.maxDistance = 50f;

        // 효과음 재생
        tempAudioSource.Play();

        // 효과음 재생이 끝나면 임시 오브젝트 삭제
        Destroy(tempAudioObject, clip.length);
    }

    /// <summary>
    /// 효과음 즉시 재생 (오브젝트가 아직 존재할 때)
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// 효과음 볼륨 설정
    /// </summary>
    /// <param name="volume">볼륨 값 (0~1)</param>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }
}