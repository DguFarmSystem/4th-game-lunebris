using UnityEngine;
using System.Collections;
using Enemy; // Enemy 네임스페이스 추가

/// <summary>
/// 360도 빔 공격 시스템
/// 필요한 컴포넌트: Collider (IsTrigger = true), Rigidbody (UseGravity = false)
/// </summary>
public class Enemy_Final_Boss_360Beam : MonoBehaviour
{
    [Header("빔 설정")]
    [SerializeField] private GameObject beamPrefab; // Enemy_Final_Boss_360Beam 프리팹
    [SerializeField] private float beamDamage = 80f;
    [SerializeField] private float beamSpeed = 15f;
    [SerializeField] private float beamLifetime = 5f;

    [Header("부메랑 설정")]
    [SerializeField] private bool isBoomerang = false;        // 부메랑 모드 활성화
    [SerializeField] private float maxDistance = 10f;        // 최대 전진 거리
    [SerializeField] private float returnSpeed = 20f;        // 돌아오는 속도 (더 빠르게)
    [SerializeField] private bool destroyOnReturn = true;     // 원점 도달 시 제거

    [Header("360도 패턴 설정")]
    [SerializeField] private int beamCount = 2;             // 360도 안에서 발사할 개수
    [SerializeField] private bool fireAllAtOnce = false;     // true면 모든 방향 동시에 발사
    [SerializeField] private bool rotateWhileFiring = false; // true면 회전하며 순차 발사
    [SerializeField] private float delayBetweenBeams = 0.1f; // 순차 발사 시 지연시간
    [SerializeField] private float rotationSpeed = 60f;      // 회전 속도 (deg/sec)

    [Header("슬로우 효과 설정")]
    [SerializeField] private bool applySlowEffect = true;
    [SerializeField] private float slowDuration = 2.5f;
    [SerializeField] private float slowIntensity = 0.6f; // 60% 속도 감소

    [Header("🎵 오디오 설정 추가")] // ✨ 추가됨
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beamFireSound;   // 빔 발사 효과음
    [SerializeField] private AudioClip beamHitSound;    // 플레이어 피격 효과음
    [SerializeField] private AudioClip beamReturnSound; // 부메랑 복귀 효과음

    private bool isFiring = false;
    private bool isSingleBeam = false;
    private Vector3 beamVelocity;
    private float damage;

    // Enemy_Base 데미지 시스템 참고용
    private EnemyStatSystem creatorStats;
    private ElementType creatorElement = ElementType.Lux;
    private DamageType damageType = DamageType.Magical;

    // 부메랑 시스템 변수
    private bool isReturning = false;
    private Vector3 startPosition;
    private Vector3 targetPosition; // 돌아갈 위치 (보스 위치)
    private float traveledDistance = 0f;

    // 슬로우 효과 설정 (외부에서 설정 가능)
    private float externalSlowDuration = 0f;
    private float externalSlowIntensity = 0f;

    private void Start()
    {
        // ✨ 오디오소스 자동 연결
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 부메랑 모드 초기화
        if (isBoomerang && isSingleBeam)
        {
            startPosition = transform.position;
            // 보스 위치를 찾아서 타겟으로 설정
            GameObject boss = GameObject.FindGameObjectWithTag("Enemy");
            if (boss != null)
            {
                targetPosition = boss.transform.position;
            }
            else
            {
                targetPosition = startPosition; // 보스를 못 찾으면 시작 위치로
            }
        }

        // 단일 빔 모드가 아닌 경우 자동으로 360도 패턴 실행
        if (!isSingleBeam)
        {
            Fire360Beams();
        }
        else
        {
            // 단일 빔 모드인 경우 직접 이동 (부메랑 포함)
            StartCoroutine(MoveSingleBeam());
        }
    }

    // ✨ 효과음 재생 함수
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Enemy_Final_Boss_Light에서 호출하는 Initialize 메소드
    /// 단일 빔으로 동작하도록 설정
    /// </summary>
    public void Initialize(float beamDamage, Vector3 velocity)
    {
        this.damage = beamDamage;
        this.beamVelocity = velocity;
        this.isSingleBeam = true;

        // 이동 방향으로 회전
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    /// <summary>
    /// 부메랑 옵션과 함께 Initialize
    /// </summary>
    public void Initialize(float beamDamage, Vector3 velocity, bool enableBoomerang, float maxDist = 10f, Vector3 returnTarget = default)
    {
        this.damage = beamDamage;
        this.beamVelocity = velocity;
        this.isSingleBeam = true;
        this.isBoomerang = enableBoomerang;
        this.maxDistance = maxDist;

        if (returnTarget != default)
        {
            this.targetPosition = returnTarget;
        }

        // 이동 방향으로 회전
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    /// <summary>
    /// Enemy 스탯 시스템을 포함한 Initialize (더 정확한 데미지 계산용)
    /// </summary>
    public void Initialize(float beamDamage, Vector3 velocity, EnemyStatSystem enemyStats, ElementType elementType)
    {
        this.damage = beamDamage;
        this.beamVelocity = velocity;
        this.isSingleBeam = true;
        this.creatorStats = enemyStats;
        this.creatorElement = elementType;

        // 이동 방향으로 회전
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    /// <summary>
    /// 모든 옵션을 포함한 Complete Initialize
    /// </summary>
    public void Initialize(float beamDamage, Vector3 velocity, EnemyStatSystem enemyStats, ElementType elementType,
                          bool enableBoomerang, float maxDist = 10f, Vector3 returnTarget = default)
    {
        this.damage = beamDamage;
        this.beamVelocity = velocity;
        this.isSingleBeam = true;
        this.creatorStats = enemyStats;
        this.creatorElement = elementType;
        this.isBoomerang = enableBoomerang;
        this.maxDistance = maxDist;

        if (returnTarget != default)
        {
            this.targetPosition = returnTarget;
        }

        // 이동 방향으로 회전
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    /// <summary>
    /// 외부에서 슬로우 효과 설정 (Enemy_Final_Boss_Light에서 호출)
    /// </summary>
    public void SetSlowEffect(float duration, float intensity)
    {
        externalSlowDuration = duration;
        externalSlowIntensity = intensity;
        applySlowEffect = true;
    }

    /// <summary>
    /// 360도 모든 방향으로 빔 발사
    /// </summary>
    public void Fire360Beams()
    {
        if (isFiring) return;

        // ✨ 빔 발사 사운드 재생
        PlaySound(beamFireSound);

        if (fireAllAtOnce)
        {
            FireAllDirectionsAtOnce();
        }
        else if (rotateWhileFiring)
        {
            StartCoroutine(FireWhileRotating());
        }
        else
        {
            StartCoroutine(FireSequentially());
        }
    }

    private void FireAllDirectionsAtOnce()
    {
        Vector3 origin = transform.position;

        for (int i = 0; i < beamCount; i++)
        {
            float angle = (360f / beamCount) * i;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;

            FireSingleBeam(origin, direction * beamSpeed);
        }

        // 360도 패턴 발사 후 자신은 제거
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator FireSequentially()
    {
        isFiring = true;

        Vector3 origin = transform.position;

        for (int i = 0; i < beamCount; i++)
        {
            float angle = (360f / beamCount) * i;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;

            FireSingleBeam(origin, direction * beamSpeed);
            yield return new WaitForSeconds(delayBetweenBeams);
        }

        isFiring = false;
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator FireWhileRotating()
    {
        isFiring = true;

        float currentAngle = 0f;
        float angleStep = 360f / beamCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < beamCount; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, currentAngle, 0f);
            Vector3 direction = rotation * Vector3.forward;

            FireSingleBeam(origin, direction * beamSpeed);
            yield return new WaitForSeconds(delayBetweenBeams);
            currentAngle += angleStep;
        }

        isFiring = false;
        Destroy(gameObject, 0.5f);
    }

    private void FireSingleBeam(Vector3 position, Vector3 velocity)
    {
        GameObject beam = Instantiate(beamPrefab, position, Quaternion.LookRotation(velocity.normalized));
        var beamScript = beam.GetComponent<Enemy_Final_Boss_360Beam>();
        if (beamScript != null)
        {
            if (creatorStats != null)
            {
                beamScript.Initialize(beamDamage, velocity, creatorStats, creatorElement);
            }
            else
            {
                beamScript.Initialize(beamDamage, velocity);
            }

            if (applySlowEffect)
            {
                float finalSlowDuration = externalSlowDuration > 0 ? externalSlowDuration : slowDuration;
                float finalSlowIntensity = externalSlowIntensity > 0 ? externalSlowIntensity : slowIntensity;
                beamScript.SetSlowEffect(finalSlowDuration, finalSlowIntensity);
            }
        }
    }

    /// <summary>
    /// 단일 빔 이동 처리 (부메랑 포함)
    /// </summary>
    private IEnumerator MoveSingleBeam()
    {
        float timer = 0f;

        if (!isBoomerang)
        {
            while (timer < beamLifetime)
            {
                transform.position += beamVelocity * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
            yield break;
        }

        while (!isReturning && timer < beamLifetime)
        {
            transform.position += beamVelocity * Time.deltaTime;
            traveledDistance += beamVelocity.magnitude * Time.deltaTime;

            if (traveledDistance >= maxDistance)
            {
                // ✨ 복귀 사운드 재생
                PlaySound(beamReturnSound);
                yield return new WaitForSeconds(2.0f);
                isReturning = true;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (isReturning)
        {
            while (timer < beamLifetime)
            {
                GameObject boss = GameObject.FindGameObjectWithTag("Enemy");
                if (boss != null)
                {
                    targetPosition = boss.transform.position;
                }

                Vector3 returnDirection = (targetPosition - transform.position).normalized;
                transform.position += returnDirection * returnSpeed * Time.deltaTime;

                if (returnDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(returnDirection);
                }

                if (destroyOnReturn && Vector3.Distance(transform.position, targetPosition) < 1f)
                {
                    Destroy(gameObject);
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            // ✨ 플레이어 피격 사운드 재생
            PlaySound(beamHitSound);

            var playerScript = other.GetComponent<Player.Player>();
            if (playerScript != null)
            {
                float finalDamage = isSingleBeam ? damage : beamDamage;

                if (creatorStats != null)
                {
                    finalDamage = DamageCalculator.CalculateDamageToPlayer(
                        creatorStats,
                        creatorElement,
                        damageType,
                        playerScript.GetPlayerStat(),
                        ElementType.Neutral
                    );
                }

                playerScript.DecreaseHP(finalDamage);
                ApplySlowEffectToPlayer(playerScript);
            }

            if (isSingleBeam && (!isBoomerang || !isReturning))
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplySlowEffectToPlayer(Player.Player player)
    {
        if (!applySlowEffect || player == null) return;

        float finalSlowDuration = externalSlowDuration > 0 ? externalSlowDuration : slowDuration;
        float finalSlowIntensity = externalSlowIntensity > 0 ? externalSlowIntensity : slowIntensity;

        var slowMethod = player.GetType().GetMethod("ApplySlowEffect");
        if (slowMethod != null)
        {
            slowMethod.Invoke(player, new object[] { finalSlowDuration, finalSlowIntensity });
        }
    }
}
