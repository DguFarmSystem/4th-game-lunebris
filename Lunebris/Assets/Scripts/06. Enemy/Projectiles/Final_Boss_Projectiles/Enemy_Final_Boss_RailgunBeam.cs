using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 라이트왕국에서 발사하는 5갈래 레일건 빔 (중간보스 스타일)
/// </summary>
public class Enemy_Final_Boss_RailgunBeam : MonoBehaviour
{
    [Header("레일건 설정")]
    [SerializeField] private float damage = 120f;
    [SerializeField] private float maxRange = 30f;
    [SerializeField] private float beamWidth = 0.5f;
    [SerializeField] private float chargingTime = 0.5f;
    [SerializeField] private float beamDuration = 1f;
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private ElementType elementType = ElementType.Lux;
    [SerializeField] private bool canPenetrate = false;

    [Header("5갈래 설정")]
    [SerializeField] private float[] beamAngles = { 0f, -15f, 15f, -30f, 30f }; // 중앙, 좌15, 우15, 좌30, 우30

    [Header("슬로우 효과 설정")]
    [SerializeField] private bool applySlowEffect = true;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowIntensity = 0.7f; // 70% 속도 감소

    [Header("컴포넌트")]
    [SerializeField] private LineRenderer centerBeam;
    [SerializeField] private LineRenderer leftBeam1;
    [SerializeField] private LineRenderer rightBeam1;
    [SerializeField] private LineRenderer leftBeam2;
    [SerializeField] private LineRenderer rightBeam2;
    [SerializeField] private ParticleSystem chargingParticle;

    [Header("사운드")]
    [SerializeField] private AudioClip fireSound;

    private AudioSource audioSource;
    private Vector3 direction;
    private bool isFiring = false;
    private LineRenderer[] allBeams;

    // 슬로우 효과 설정 (외부에서 설정 가능)
    private float externalSlowDuration = 0f;
    private float externalSlowIntensity = 0f;

    public void Initialize(float beamDamage, Vector3 beamDirection, float beamRange)
    {
        damage = beamDamage;
        direction = beamDirection.normalized;
        maxRange = beamRange;

        SetupComponents();
        StartCoroutine(FireSequence());
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

    private void SetupComponents()
    {
        // LineRenderer 배열 설정
        allBeams = new LineRenderer[5];
        allBeams[0] = centerBeam;
        allBeams[1] = leftBeam1;
        allBeams[2] = rightBeam1;
        allBeams[3] = leftBeam2;
        allBeams[4] = rightBeam2;

        // 모든 빔 초기화
        SetupAllBeams();

        // AudioSource 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ParticleSystem 설정
        if (chargingParticle != null)
        {
            SetupChargingParticle();
        }
    }

    private void SetupAllBeams()
    {
        for (int i = 0; i < allBeams.Length; i++)
        {
            if (allBeams[i] == null)
            {
                Debug.LogWarning($"Beam {i}가 할당되지 않았습니다! 프리팹을 확인해주세요.");
                continue;
            }

            SetupSingleBeam(allBeams[i], i);
        }
    }

    private void SetupSingleBeam(LineRenderer beam, int index)
    {
        if (beam == null) return;

        // 빔 기본 설정
        beam.startWidth = 0f;
        beam.endWidth = 0f;
        beam.enabled = false;
        beam.useWorldSpace = true;
        beam.positionCount = 2;

        // 빔 두께 (중앙이 가장 두껍고 바깥쪽이 얇음)
        float widthMultiplier = index == 0 ? 1f : 0.7f; // 중앙빔이 가장 두껍게
        beam.startWidth = beamWidth * widthMultiplier;
        beam.endWidth = beamWidth * widthMultiplier * 0.5f;

        // 빔 색상 설정 (중앙이 가장 밝고 바깥쪽이 어둡게)
        Gradient gradient = new Gradient();
        Color beamColor = index == 0 ? Color.white : Color.cyan;
        float alpha = index == 0 ? 1f : 0.8f - (index * 0.1f);

        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(beamColor, 0.0f),
                new GradientColorKey(Color.cyan, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alpha, 0.0f),
                new GradientAlphaKey(alpha * 0.3f, 1.0f)
            }
        );
        beam.colorGradient = gradient;

        // 빔 스타일링
        beam.textureMode = LineTextureMode.Stretch;
        beam.alignment = LineAlignment.View;
        beam.numCapVertices = 8;
        beam.numCornerVertices = 8;
    }

    private void SetupChargingParticle()
    {
        chargingParticle.Stop();
        var main = chargingParticle.main;
        main.playOnAwake = false;
        main.startColor = Color.cyan;
        main.startSize = 2f; // 5갈래라서 더 크게
        main.startLifetime = chargingTime;
    }

    private IEnumerator FireSequence()
    {
        isFiring = true;

        // 1. 차지 단계
        yield return StartCoroutine(ChargingPhase());

        // 2. 발사 단계
        yield return StartCoroutine(FiringPhase());

        // 3. 정리
        CleanupEffects();
        Destroy(gameObject);
    }

    private IEnumerator ChargingPhase()
    {
        Debug.Log("5갈래 레일건 차지 시작!");

        // 차지 파티클 시작
        if (chargingParticle != null)
        {
            chargingParticle.transform.position = transform.position;
            chargingParticle.Play();
        }

        // 차지 중 빔들을 점진적으로 표시
        float elapsed = 0f;
        while (elapsed < chargingTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / chargingTime;

            // 차지 중 얇은 빔들을 점진적으로 표시
            for (int i = 0; i < allBeams.Length; i++)
            {
                if (allBeams[i] != null)
                {
                    allBeams[i].enabled = true;
                    float chargingWidth = (beamWidth * 0.1f) * progress;
                    allBeams[i].startWidth = chargingWidth;
                    allBeams[i].endWidth = chargingWidth * 0.5f;

                    // 빔 위치 설정
                    Vector3 beamDirection = RotateDirection(direction, beamAngles[i]);
                    Vector3 startPos = transform.position;
                    Vector3 endPos = startPos + beamDirection * maxRange;

                    allBeams[i].SetPosition(0, startPos);
                    allBeams[i].SetPosition(1, endPos);
                }
            }

            yield return null;
        }

        // 차지 완료 - 파티클 정지
        if (chargingParticle != null)
        {
            chargingParticle.Stop();
        }
    }

    private IEnumerator FiringPhase()
    {
        Debug.Log("5갈래 레일건 발사!");

        // 사운드 재생
        PlayFireSound();

        // 5갈래 빔 동시 발사
        Fire5BeamRailgun();

        // 빔 지속 시간
        yield return new WaitForSeconds(beamDuration);
    }

    private void Fire5BeamRailgun()
    {
        Vector3 startPos = transform.position;

        // 5방향 계산 및 발사
        for (int i = 0; i < beamAngles.Length; i++)
        {
            Vector3 beamDirection = RotateDirection(direction, beamAngles[i]);
            FireSingleBeam(startPos, beamDirection, allBeams[i], i);
        }
    }

    private void FireSingleBeam(Vector3 startPos, Vector3 beamDirection, LineRenderer beam, int beamIndex)
    {
        Vector3 endPos = startPos + beamDirection * maxRange;
        bool hitPlayer = false;

        // 레이캐스트로 충돌 검사
        RaycastHit[] hits = Physics.RaycastAll(startPos, beamDirection, maxRange);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
                ApplyDamage(hit.collider.GetComponent<Player.Player>(), hit.point, beamIndex);
                hitPlayer = true;

                if (!canPenetrate)
                {
                    endPos = hit.point;
                    break;
                }
            }
            else if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
            {
                if (!hitPlayer)
                {
                    endPos = hit.point;
                }
                break;
            }
        }

        // 빔 렌더링
        if (beam != null)
        {
            beam.enabled = true;
            // 최종 두께 설정
            float finalWidth = beamWidth * (beamIndex == 0 ? 1f : 0.7f);
            beam.startWidth = finalWidth;
            beam.endWidth = finalWidth * 0.5f;
            beam.SetPosition(0, startPos);
            beam.SetPosition(1, endPos);
        }
    }

    private Vector3 RotateDirection(Vector3 baseDirection, float angleInDegrees)
    {
        // Y축 기준으로 회전 (수평 확산)
        float radians = angleInDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector3(
            baseDirection.x * cos - baseDirection.z * sin,
            baseDirection.y,
            baseDirection.x * sin + baseDirection.z * cos
        );
    }

    private void ApplyDamage(Player.Player player, Vector3 hitPoint, int beamIndex)
    {
        if (player == null) return;

        // 임시 보스 스탯 생성
        EnemyStatSystem tempEnemyStats = new EnemyStatSystem(EnemyType.MiddleBoss);

        // 정확한 데미지 계산
        float calculatedDamage = DamageCalculator.CalculateDamageToPlayer(
            tempEnemyStats,
            elementType,
            damageType,
            player.GetPlayerStat(),
            ElementType.Neutral
        );

        // 중앙빔이 가장 강하고 바깥쪽 빔이 약함
        float damageMultiplier = beamIndex == 0 ? 1f : 0.7f - (beamIndex * 0.1f);
        calculatedDamage *= damageMultiplier;

        player.DecreaseHP(calculatedDamage);
        Debug.Log($"플레이어가 레일건 빔 {beamIndex}에 맞음! 데미지: {calculatedDamage:F1}");

        // 슬로우 효과 적용
        ApplySlowEffectToPlayer(player);
    }

    /// <summary>
    /// 플레이어에게 슬로우 효과 적용
    /// </summary>
    private void ApplySlowEffectToPlayer(Player.Player player)
    {
        if (!applySlowEffect || player == null) return;

        // 외부에서 설정된 값이 있으면 우선 사용
        float finalSlowDuration = externalSlowDuration > 0 ? externalSlowDuration : slowDuration;
        float finalSlowIntensity = externalSlowIntensity > 0 ? externalSlowIntensity : slowIntensity;

        GameObject playerObj = player.gameObject;

        // 방법 1: Player 스크립트에 ApplySlowEffect 메서드가 있는 경우
        var slowMethod = player.GetType().GetMethod("ApplySlowEffect");
        if (slowMethod != null)
        {
            slowMethod.Invoke(player, new object[] { finalSlowDuration, finalSlowIntensity });
            Debug.Log($"레일건 빔 - 플레이어에게 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
            return;
        }

        // 방법 2: Player Movement 컴포넌트가 있는 경우
        var movementComponent = playerObj.GetComponent<MonoBehaviour>();
        if (movementComponent != null)
        {
            var moveSlowMethod = movementComponent.GetType().GetMethod("ApplySlowEffect");
            if (moveSlowMethod != null)
            {
                moveSlowMethod.Invoke(movementComponent, new object[] { finalSlowDuration, finalSlowIntensity });
                Debug.Log($"레일건 빔 - 플레이어 이동에 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
                return;
            }
        }

        // 방법 3: 직접 Rigidbody 제어 (임시 방법)
        var playerRb = playerObj.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            StartCoroutine(ApplyTemporarySlowEffect(playerRb, finalSlowDuration, finalSlowIntensity));
            Debug.Log($"레일건 빔 - 플레이어에게 임시 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
        }
    }

    /// <summary>
    /// 임시 슬로우 효과 (Rigidbody 직접 제어)
    /// </summary>
    private IEnumerator ApplyTemporarySlowEffect(Rigidbody playerRb, float duration, float intensity)
    {
        float originalDrag = playerRb.drag;
        float slowDrag = originalDrag + (intensity * 10f); // 드래그 증가로 슬로우 효과

        playerRb.drag = slowDrag;
        yield return new WaitForSeconds(duration);

        // Rigidbody가 아직 존재하는지 확인
        if (playerRb != null)
        {
            playerRb.drag = originalDrag;
        }
    }

    private void PlayFireSound()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }

    private void CleanupEffects()
    {
        // 모든 빔 끄기
        for (int i = 0; i < allBeams.Length; i++)
        {
            if (allBeams[i] != null)
                allBeams[i].enabled = false;
        }

        if (chargingParticle != null)
            chargingParticle.Stop();
    }

    // 디버그용 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // 5갈래 방향 표시
        for (int i = 0; i < beamAngles.Length; i++)
        {
            Vector3 beamDirection = RotateDirection(direction, beamAngles[i]);
            Gizmos.color = i == 0 ? Color.white : Color.cyan;
            Gizmos.DrawRay(transform.position, beamDirection * maxRange);
        }
    }

    // 공개 속성
    public bool IsReady => !isFiring;

    // 빔 각도 런타임 설정
    public void SetBeamAngles(float[] newAngles)
    {
        if (newAngles.Length == 5)
        {
            beamAngles = newAngles;
        }
    }
}