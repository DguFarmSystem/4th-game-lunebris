using UnityEngine;
using System.Collections;

/// <summary>
/// 최종보스 빛 모드의 검기 공격
/// </summary>
public class Enemy_Final_Boss_SwordBeam : MonoBehaviour
{
    [Header("검기 설정")]
    [SerializeField] private float beamLength = 3f;
    [SerializeField] private float beamWidth = 1.5f;
    [SerializeField] private float expandSpeed = 8f;
    [SerializeField] private float sustainTime = 0.3f;

    [Header("이펙트")]
    [SerializeField] private LineRenderer beamLine;
    [SerializeField] private Light beamLight;
    [SerializeField] private AudioClip slashSound;

    private float damage;
    private Vector3 velocity;
    private bool hasHitPlayer = false;
    private bool isExpanding = true;
    private float currentLength = 0f;
    private Rigidbody rb;
    private AudioSource audioSource;
    private BoxCollider damageCollider;

    public void Initialize(float beamDamage, Vector3 beamVelocity)
    {
        damage = beamDamage;
        velocity = beamVelocity;

        SetupComponents();
        StartCoroutine(BeamSequence());
    }

    private void SetupComponents()
    {
        // 리지드바디 설정
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = velocity;

        // 오디오 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        // 데미지 콜라이더 설정
        damageCollider = GetComponent<BoxCollider>();
        if (damageCollider == null)
            damageCollider = gameObject.AddComponent<BoxCollider>();

        damageCollider.isTrigger = true;
        damageCollider.size = new Vector3(beamWidth, beamWidth, beamLength);
        damageCollider.center = Vector3.forward * (beamLength * 0.5f);
        damageCollider.enabled = false;

        // 라인 렌더러 설정
        if (beamLine == null)
            beamLine = gameObject.AddComponent<LineRenderer>();

        beamLine.material = new Material(Shader.Find("Sprites/Default"));
        beamLine.material.color = Color.white;
        beamLine.startWidth = beamWidth;
        beamLine.endWidth = beamWidth * 0.7f;
        beamLine.positionCount = 2;

        // 검기 조명 설정
        if (beamLight == null)
            beamLight = gameObject.AddComponent<Light>();

        beamLight.type = LightType.Spot;
        beamLight.color = Color.white;
        beamLight.intensity = 4f;
        beamLight.range = beamLength * 2f;
        beamLight.spotAngle = 30f;

        // 이동 방향으로 회전
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        // 발사 사운드
        if (slashSound != null)
        {
            audioSource.clip = slashSound;
            audioSource.Play();
        }
    }

    private IEnumerator BeamSequence()
    {
        // 1단계: 검기 확장
        yield return StartCoroutine(ExpandBeam());

        // 2단계: 검기 지속
        yield return StartCoroutine(SustainBeam());

        // 3단계: 검기 축소 및 소멸
        yield return StartCoroutine(ShrinkBeam());

        Destroy(gameObject);
    }

    private IEnumerator ExpandBeam()
    {
        isExpanding = true;
        float targetLength = beamLength;

        while (currentLength < targetLength)
        {
            currentLength += expandSpeed * Time.deltaTime;
            currentLength = Mathf.Min(currentLength, targetLength);

            UpdateBeamVisuals();
            UpdateBeamCollider();

            yield return null;
        }

        damageCollider.enabled = true;
        isExpanding = false;
    }

    private IEnumerator SustainBeam()
    {
        yield return new WaitForSeconds(sustainTime);
        damageCollider.enabled = false;
    }

    private IEnumerator ShrinkBeam()
    {
        float shrinkSpeed = expandSpeed * 2f;

        while (currentLength > 0f)
        {
            currentLength -= shrinkSpeed * Time.deltaTime;
            currentLength = Mathf.Max(currentLength, 0f);

            UpdateBeamVisuals();

            if (beamLight != null)
            {
                beamLight.intensity = Mathf.Lerp(4f, 0f, 1f - (currentLength / beamLength));
            }

            yield return null;
        }
    }

    private void UpdateBeamVisuals()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + transform.forward * currentLength;

        // 라인 렌더러 업데이트
        if (beamLine != null)
        {
            beamLine.SetPosition(0, startPos);
            beamLine.SetPosition(1, endPos);

            if (isExpanding)
            {
                float progress = currentLength / beamLength;
                beamLine.material.color = Color.Lerp(Color.yellow, Color.white, progress);
            }
        }

        // 조명 범위 업데이트
        if (beamLight != null)
        {
            beamLight.range = currentLength;
            beamLight.transform.rotation = transform.rotation;
        }
    }

    private void UpdateBeamCollider()
    {
        if (damageCollider != null)
        {
            damageCollider.size = new Vector3(beamWidth, beamWidth, currentLength);
            damageCollider.center = Vector3.forward * (currentLength * 0.5f);
        }
    }

    private void Update()
    {
        // 벽이나 장애물과 충돌하면 정지
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f))
        {
            if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Environment"))
            {
                rb.velocity = Vector3.zero;
                transform.position = hit.point - transform.forward * 0.5f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitPlayer || !damageCollider.enabled) return;

        if (other.CompareTag("Player"))
        {
            hasHitPlayer = true;

            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                player.DecreaseHP(damage);
                StartCoroutine(HitEffect());
            }
        }
    }

    private IEnumerator HitEffect()
    {
        if (beamLine != null)
        {
            Color originalColor = beamLine.material.color;
            beamLine.material.color = Color.cyan;
            yield return new WaitForSeconds(0.1f);
            beamLine.material.color = originalColor;
        }

        if (beamLight != null)
        {
            float originalIntensity = beamLight.intensity;
            beamLight.intensity = 8f;
            yield return new WaitForSeconds(0.1f);
            beamLight.intensity = originalIntensity;
        }
    }
}