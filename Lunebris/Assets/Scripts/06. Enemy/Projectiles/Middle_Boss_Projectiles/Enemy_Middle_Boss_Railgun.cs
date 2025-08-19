using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 보스 차징 레일건 공격 - 차징 범위 표시 기능 포함
/// </summary>
public class Enemy_Middle_Boss_Railgun : MonoBehaviour
{
    [Header("레일건 설정")]
    [SerializeField] private float damage = 120f;
    [SerializeField] private float maxRange = 50f;
    [SerializeField] private float beamWidth = 3f;
    [SerializeField] private float chargingTime = 1f;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private bool canPenetrate = true;
    [SerializeField] private bool enablePrediction = true;   // 예측 조준
    [SerializeField] private float predictionTime = 0.5f;    // 예측 시간

    [Header("데미지 속성")]
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private ElementType elementType = ElementType.Tenebris;

    [Header("차징 범위 표시")]
    [SerializeField] private bool showChargingRange = true;      // 범위 표시 활성화
    [SerializeField] private Color chargingRangeColor = Color.red; // 경고 색상
    [SerializeField] private float chargingRangeAlpha = 0.5f;    // 투명도
    [SerializeField] private float blinkSpeed = 3f;             // 점멸 속도

    [Header("컴포넌트")]
    [SerializeField] private LineRenderer railgunBeam;        // 중앙 빔
    [SerializeField] private LineRenderer leftBeam;           // 좌측 빔
    [SerializeField] private LineRenderer rightBeam;          // 우측 빔
    [SerializeField] private ParticleSystem chargingParticle;

    [Header("사운드")]
    [SerializeField] private AudioClip fireSound;

    [Header("충돌 이펙트")]
    [SerializeField] private GameObject playerHitEffect;      // 플레이어 충돌 이펙트
    [SerializeField] private GameObject wallHitEffect;       // 벽 충돌 이펙트
    [SerializeField] private AudioClip playerHitSound;       // 플레이어 충돌 사운드
    [SerializeField] private AudioClip wallHitSound;         // 벽 충돌 사운드
    [SerializeField] private float hitEffectDuration = 2f;   // 이펙트 지속 시간

    [Header("슬로우 효과")]
    [SerializeField] private bool enableSlowEffect = true;   // 슬로우 효과 활성화
    [SerializeField] private float slowDuration = 3f;       // 슬로우 지속 시간
    [SerializeField] private float slowIntensity = 0.5f;    // 슬로우 강도 (0.5 = 50% 속도)

    private Enemy_Base sourceBoss;
    private Transform firePoint;
    private AudioSource audioSource;
    private bool isFiring = false;

    // Enemy_Middle_Boss2와의 호환성을 위한 최소한의 추가 변수
    private Coroutine currentRailgunSequence;

    #region 초기화

    private void Start()
    {
        // 컴포넌트 자동 찾기
        if (railgunBeam == null)
            railgunBeam = GetComponent<LineRenderer>();
        if (chargingParticle == null)
            chargingParticle = GetComponent<ParticleSystem>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetupRailgunBeams();
        SetupChargingParticle();
    }

    public void Initialize(Enemy_Base boss, Transform shootPoint)
    {
        sourceBoss = boss;
        firePoint = shootPoint;
    }

    private void SetupRailgunBeams()
    {
        // 중앙 빔 설정
        SetupSingleBeam(railgunBeam);

        // 좌측 빔 설정
        SetupSingleBeam(leftBeam);

        // 우측 빔 설정
        SetupSingleBeam(rightBeam);
    }

    private void SetupSingleBeam(LineRenderer beam)
    {
        if (beam != null)
        {
            // 빔 기본 설정
            beam.startWidth = beamWidth * 2f;
            beam.endWidth = beamWidth * 1.2f;
            beam.enabled = false;
            beam.useWorldSpace = true;
            beam.positionCount = 2;

            // 빔 스타일 설정
            beam.textureMode = LineTextureMode.Stretch;
            beam.alignment = LineAlignment.View;
            beam.numCapVertices = 8;
            beam.numCornerVertices = 8;

            // 발사용 기본 그라데이션 (차징 경고와 구분)
            Gradient fireGradient = new Gradient();
            fireGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.cyan, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.3f, 1.0f)
                }
            );
            beam.colorGradient = fireGradient;
        }
    }

    private void SetupChargingParticle()
    {
        if (chargingParticle != null)
        {
            chargingParticle.Stop();

            var main = chargingParticle.main;
            main.playOnAwake = false;
            // 파티클 설정은 프리팹에서 미리 설정된 값 사용
        }
    }

    #endregion

    #region 차징 레일건

    public void FireInstantRailgun(Vector3 targetPosition)
    {
        if (isFiring) return;

        // 이전 코루틴이 실행 중이면 정지 (안전성을 위해서만 추가)
        if (currentRailgunSequence != null)
        {
            StopCoroutine(currentRailgunSequence);
        }

        currentRailgunSequence = StartCoroutine(ChargingRailgunSequence(targetPosition));
    }

    private IEnumerator ChargingRailgunSequence(Vector3 targetPosition)
    {
        isFiring = true;

        // 1. 차징 페이즈
        yield return StartCoroutine(ChargingPhase());

        // 2. 발사 페이즈
        FireRailgunBeam(targetPosition);
        PlayFireSound();

        // 3. 섬광 시간
        yield return new WaitForSeconds(flashDuration);

        // 4. 정리
        CleanupEffects();
        isFiring = false;
        currentRailgunSequence = null;
    }

    private IEnumerator ChargingPhase()
    {
        // 차징 파티클 시작
        if (chargingParticle != null && firePoint != null)
        {
            chargingParticle.transform.position = firePoint.position;
            chargingParticle.Play();
        }

        // 차징 범위 표시 시작
        if (showChargingRange)
        {
            StartCoroutine(ShowChargingRangeWarning());
        }

        // 차징 시간 대기
        yield return new WaitForSeconds(chargingTime);

        // 차징 범위 표시 종료
        if (showChargingRange)
        {
            HideChargingRangeWarning();
        }

        // 차징 완료 - 파티클 강제 정지
        if (chargingParticle != null)
        {
            chargingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void FireRailgunBeam(Vector3 targetDirection)
    {
        Vector3 startPos = firePoint.position;
        Vector3 finalTarget = targetDirection;

        // 예측 조준 시스템
        if (enablePrediction && sourceBoss != null)
        {
            finalTarget = PredictPlayerPosition(targetDirection);
        }

        Vector3 baseDirection = (finalTarget - startPos).normalized;

        // Y축 위치 고정 (수평 발사)
        baseDirection.y = 0;
        baseDirection = baseDirection.normalized;

        // 3개 레이 방향 계산
        Vector3 centerDirection = baseDirection;
        Vector3 leftDirection = RotateDirection(baseDirection, 15f);   // 좌측 15도
        Vector3 rightDirection = RotateDirection(baseDirection, -15f); // 우측 15도

        // 각 방향별로 레이캐스트 및 빔 표시
        Vector3 centerEnd = FireSingleBeam(startPos, centerDirection, railgunBeam);
        Vector3 leftEnd = FireSingleBeam(startPos, leftDirection, leftBeam);
        Vector3 rightEnd = FireSingleBeam(startPos, rightDirection, rightBeam);
    }

    private Vector3 FireSingleBeam(Vector3 startPos, Vector3 direction, LineRenderer beam)
    {
        Vector3 endPos = startPos + direction * maxRange;
        bool hitPlayer = false;

        // 레이캐스트로 충돌 검사
        RaycastHit[] hits = Physics.RaycastAll(startPos, direction, maxRange);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
                // 플레이어 충돌 이펙트 생성
                CreatePlayerHitEffect(hit.point, hit.normal);

                // 데미지 적용
                ApplyInstantDamage(hit.collider.GetComponent<Player.Player>(), hit.point);
                hitPlayer = true;

                if (!canPenetrate)
                {
                    endPos = hit.point;
                    break;
                }
            }
            else if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
            {
                // 벽 충돌 이펙트 생성
                CreateWallHitEffect(hit.point, hit.normal);

                if (!hitPlayer)
                {
                    endPos = hit.point;
                }
                break;
            }
        }

        // 빔 표시 (실제 발사용 색상으로 재설정)
        if (beam != null)
        {
            beam.enabled = true;
            beam.positionCount = 2;
            beam.SetPosition(0, startPos);
            beam.SetPosition(1, endPos);

            // 발사용 굵기로 재설정
            beam.startWidth = beamWidth * 2f;
            beam.endWidth = beamWidth * 1.2f;

            // 발사용 색상으로 재설정
            Gradient fireGradient = new Gradient();
            fireGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.cyan, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.3f, 1.0f)
                }
            );
            beam.colorGradient = fireGradient;
        }

        return endPos;
    }

    #endregion

    #region 차징 범위 표시

    private IEnumerator ShowChargingRangeWarning()
    {
        if (sourceBoss == null || firePoint == null) yield break;

        // 목표 위치 계산 (예측 포함)
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) yield break;

        Vector3 targetPos = playerObj.transform.position;
        if (enablePrediction)
        {
            targetPos = PredictPlayerPosition(targetPos);
        }

        Vector3 startPos = firePoint.position;
        Vector3 baseDirection = (targetPos - startPos).normalized;
        baseDirection.y = 0; // 수평 발사

        // 3개 방향 계산
        Vector3 centerDirection = baseDirection;
        Vector3 leftDirection = RotateDirection(baseDirection, 15f);
        Vector3 rightDirection = RotateDirection(baseDirection, -15f);

        // 경고용 빔 설정
        SetupWarningBeam(railgunBeam, startPos, centerDirection);
        SetupWarningBeam(leftBeam, startPos, leftDirection);
        SetupWarningBeam(rightBeam, startPos, rightDirection);

        // 점멸 효과
        float elapsed = 0f;
        while (elapsed < chargingTime)
        {
            elapsed += Time.deltaTime;

            // 점멸 계산 (sin으로 부드러운 점멸)
            float blinkAlpha = (Mathf.Sin(elapsed * blinkSpeed * Mathf.PI * 2) + 1f) * 0.5f;
            blinkAlpha = Mathf.Lerp(0.2f, chargingRangeAlpha, blinkAlpha);

            // 모든 경고 빔에 적용
            UpdateWarningBeamAlpha(railgunBeam, blinkAlpha);
            UpdateWarningBeamAlpha(leftBeam, blinkAlpha);
            UpdateWarningBeamAlpha(rightBeam, blinkAlpha);

            yield return null;
        }
    }

    private void SetupWarningBeam(LineRenderer beam, Vector3 startPos, Vector3 direction)
    {
        if (beam == null) return;

        Vector3 endPos = startPos + direction * maxRange;

        // 벽 충돌 체크
        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, maxRange))
        {
            if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
            {
                endPos = hit.point;
            }
        }

        // 빔 설정
        beam.enabled = true;
        beam.positionCount = 2;
        beam.SetPosition(0, startPos);
        beam.SetPosition(1, endPos);

        // 경고 색상으로 변경
        beam.startWidth = beamWidth * 0.5f; // 실제보다 얇게
        beam.endWidth = beamWidth * 0.3f;

        // 경고 색상 그라데이션 (원본 방식 유지)
        Gradient warningGradient = new Gradient();
        warningGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(chargingRangeColor, 0.0f),
                new GradientColorKey(chargingRangeColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(chargingRangeAlpha, 0.0f),
                new GradientAlphaKey(chargingRangeAlpha * 0.3f, 1.0f)
            }
        );
        beam.colorGradient = warningGradient;
    }

    private void UpdateWarningBeamAlpha(LineRenderer beam, float alpha)
    {
        if (beam == null || !beam.enabled) return;

        Gradient currentGradient = beam.colorGradient;
        GradientAlphaKey[] alphaKeys = currentGradient.alphaKeys;

        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i].alpha = alpha * (i == 0 ? 1f : 0.3f);
        }

        Gradient newGradient = new Gradient();
        newGradient.SetKeys(currentGradient.colorKeys, alphaKeys);
        beam.colorGradient = newGradient;
    }

    private void HideChargingRangeWarning()
    {
        // 모든 경고 빔 숨기기
        if (railgunBeam != null) railgunBeam.enabled = false;
        if (leftBeam != null) leftBeam.enabled = false;
        if (rightBeam != null) rightBeam.enabled = false;
    }

    #endregion

    #region 충돌 이펙트 시스템

    private void CreatePlayerHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 플레이어 충돌 파티클 이펙트
        if (playerHitEffect != null)
        {
            // 충돌 지점에서 법선 방향으로 이펙트 생성
            Quaternion effectRotation = Quaternion.LookRotation(hitNormal);
            GameObject hitEffect = Instantiate(playerHitEffect, hitPoint, effectRotation);

            // 파티클 시스템 자동 설정 (프리팹 설정 유지)
            ParticleSystem ps = hitEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 프리팹 설정 그대로 사용하되, 자동 파괴만 보장
                var main = ps.main;
                if (main.stopAction != ParticleSystemStopAction.Destroy)
                {
                    main.stopAction = ParticleSystemStopAction.Destroy;
                }
            }

            // 일정 시간 후 오브젝트 파괴 (안전장치)
            Destroy(hitEffect, hitEffectDuration);

            Debug.Log($"플레이어 충돌 이펙트 생성: {hitPoint}");
        }

        // 플레이어 충돌 사운드 재생
        if (playerHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(playerHitSound);
        }

        // 추가 효과들
        CreateHitShockwave(hitPoint);
        CreateScreenShake();

        // 슬로우 효과 적용
        if (enableSlowEffect)
        {
            ApplySlowEffectToPlayer();
        }
    }

    private void CreateWallHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 벽 충돌 파티클 이펙트
        if (wallHitEffect != null)
        {
            // 충돌 지점에서 법선 방향으로 이펙트 생성
            Quaternion effectRotation = Quaternion.LookRotation(hitNormal);
            GameObject hitEffect = Instantiate(wallHitEffect, hitPoint, effectRotation);

            // 파티클 시스템 자동 설정 (프리팹 설정 유지)
            ParticleSystem ps = hitEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                if (main.stopAction != ParticleSystemStopAction.Destroy)
                {
                    main.stopAction = ParticleSystemStopAction.Destroy;
                }
            }

            // 일정 시간 후 오브젝트 파괴 (안전장치)
            Destroy(hitEffect, hitEffectDuration);

            Debug.Log($"벽 충돌 이펙트 생성: {hitPoint}");
        }

        // 벽 충돌 사운드 재생
        if (wallHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallHitSound);
        }
    }

    private void CreateHitShockwave(Vector3 hitPoint)
    {
        // 충돌 지점에서 작은 폭발 효과 (선택사항)
        // 추가 파티클 이펙트나 링 모양 확산 효과 등을 여기에 구현 가능

        // 예시: 간단한 링 확산 효과 (LineRenderer 사용)
        StartCoroutine(CreateShockwaveRing(hitPoint));
    }

    private IEnumerator CreateShockwaveRing(Vector3 center)
    {
        // 간단한 충격파 링 효과
        float duration = 0.5f;
        float maxRadius = 3f;
        int segments = 32;

        // 임시 게임오브젝트와 LineRenderer 생성
        GameObject ringObj = new GameObject("ShockwaveRing");
        ringObj.transform.position = center;

        LineRenderer ring = ringObj.AddComponent<LineRenderer>();
        ring.positionCount = segments + 1;
        ring.startWidth = 0.2f;
        ring.endWidth = 0.1f;
        ring.useWorldSpace = true;

        // 주황색 정의 (RGB)
        Color orangeColor = new Color(1f, 0.5f, 0f, 1f);

        // 초기 색상 설정 (그라데이션 사용)
        Gradient initialGradient = new Gradient();
        initialGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow, 0.0f),
                new GradientColorKey(orangeColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        ring.colorGradient = initialGradient;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentRadius = Mathf.Lerp(0f, maxRadius, t);
            float alpha = Mathf.Lerp(1f, 0f, t);

            // 링 모양 좌표 계산
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                Vector3 pos = center + new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0.1f,
                    Mathf.Sin(angle) * currentRadius
                );
                ring.SetPosition(i, pos);
            }

            // 투명도 적용 (그라데이션 업데이트)
            Gradient fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.yellow, 0.0f),
                    new GradientColorKey(orangeColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha, 0.0f),
                    new GradientAlphaKey(alpha, 1.0f)
                }
            );
            ring.colorGradient = fadeGradient;

            yield return null;
        }

        // 링 제거
        Destroy(ringObj);
    }

    private void CreateScreenShake()
    {
        // 화면 흔들림 효과 (카메라 컨트롤러가 있을 경우)
        // 여기에 화면 흔들림 코드를 추가할 수 있음

        Debug.Log("플레이어 피격 - 화면 흔들림 효과 (구현 필요)");

        // 예시: 카메라 흔들림 시작
        // CameraShakeManager.Instance?.StartShake(0.3f, 0.2f);
    }

    private void ApplySlowEffectToPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("플레이어를 찾을 수 없어 슬로우 효과를 적용할 수 없습니다.");
            return;
        }

        Player.Player playerScript = playerObj.GetComponent<Player.Player>();
        if (playerScript == null)
        {
            Debug.LogWarning("플레이어 스크립트를 찾을 수 없어 슬로우 효과를 적용할 수 없습니다.");
            return;
        }

        // 슬로우 효과 시작
        StartCoroutine(ApplySlowCoroutine(playerScript));

        Debug.Log($"플레이어에게 슬로우 효과 적용: {slowIntensity * 100}% 속도로 {slowDuration}초간");
    }

    private IEnumerator ApplySlowCoroutine(Player.Player player)
    {
        if (player == null) yield break;

        // 현재 이동속도 계산
        float originalSpeed = player.GetPlayerStat().Get(Player.StatType.MoveSpeed);
        float slowAmount = originalSpeed * (1f - slowIntensity); // 감소시킬 양 계산

        // 슬로우 효과 적용 (음수 보너스로 속도 감소)
        player.GetPlayerStat().AddBonus(Player.StatType.MoveSpeed, -slowAmount);

        Debug.Log($"슬로우 적용: 원래 속도 {originalSpeed} → 현재 속도 {player.GetPlayerStat().Get(Player.StatType.MoveSpeed)}");

        // 슬로우 시각적 표시 (선택사항)
        StartCoroutine(ShowSlowEffect(player));

        // 슬로우 지속 시간 대기
        yield return new WaitForSeconds(slowDuration);

        // 슬로우 효과 해제 (보너스 제거)
        player.GetPlayerStat().AddBonus(Player.StatType.MoveSpeed, slowAmount);

        Debug.Log($"슬로우 해제: 속도 복구 {player.GetPlayerStat().Get(Player.StatType.MoveSpeed)}");
    }

    private IEnumerator ShowSlowEffect(Player.Player player)
    {
        // 플레이어 주변에 슬로우 시각 효과 (선택사항)
        GameObject slowEffectObj = null;

        // 슬로우 이펙트 프리팹이 있다면 생성
        if (playerHitEffect != null && player != null)
        {
            // 플레이어 발밑에 지속적인 슬로우 이펙트 생성
            slowEffectObj = new GameObject("SlowEffect");
            slowEffectObj.transform.position = player.transform.position;
            slowEffectObj.transform.SetParent(player.transform);

            // 간단한 파티클 효과 (파란색 전기 효과)
            LineRenderer slowRing = slowEffectObj.AddComponent<LineRenderer>();
            slowRing.positionCount = 16;
            slowRing.startWidth = 0.1f;
            slowRing.endWidth = 0.1f;
            slowRing.useWorldSpace = false;

            // 파란색 전기 효과 색상
            Gradient slowGradient = new Gradient();
            slowGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.cyan, 0.0f),
                    new GradientColorKey(Color.blue, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.8f, 1.0f)
                }
            );
            slowRing.colorGradient = slowGradient;

            // 원형 링 좌표 설정
            for (int i = 0; i < 16; i++)
            {
                float angle = (float)i / 15 * Mathf.PI * 2;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * 1.5f,
                    0.1f,
                    Mathf.Sin(angle) * 1.5f
                );
                slowRing.SetPosition(i, pos);
            }
        }

        // 슬로우 지속 시간 동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 슬로우 시각 효과 제거
        if (slowEffectObj != null)
        {
            Destroy(slowEffectObj);
        }
    }

    #endregion

    #region 유틸리티 메서드

    private Vector3 PredictPlayerPosition(Vector3 currentPlayerPos)
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return currentPlayerPos;

        // 예측된 위치도 Y축 고정
        Vector3 predictedPos = currentPlayerPos;

        // 플레이어의 이동 속도 계산
        Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 playerVelocity = playerRb.velocity;
            playerVelocity.y = 0; // Y축 속도 무시
            predictedPos = currentPlayerPos + playerVelocity * predictionTime;
        }
        else
        {
            // Rigidbody가 없으면 기본 예측
            Vector3 toPlayer = (currentPlayerPos - firePoint.position).normalized;
            toPlayer.y = 0; // Y축 방향 무시
            float distanceToPlayer = Vector3.Distance(firePoint.position, currentPlayerPos);
            float timeToReach = distanceToPlayer / 30f;

            predictedPos = currentPlayerPos + toPlayer * timeToReach * 2f;
        }

        // 예측 위치의 Y축도 보스와 같은 높이로 고정
        predictedPos.y = firePoint.position.y;

        return predictedPos;
    }

    private Vector3 RotateDirection(Vector3 direction, float angleInDegrees)
    {
        float radians = angleInDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector3(
            direction.x * cos - direction.z * sin,
            direction.y,
            direction.x * sin + direction.z * cos
        );
    }

    private void ApplyInstantDamage(Player.Player player, Vector3 hitPoint)
    {
        if (player == null) return;

        if (sourceBoss != null)
        {
            float calculatedDamage = DamageCalculator.CalculateDamageToPlayer(
                sourceBoss.GetEnemyStats(),
                elementType,
                damageType,
                player.GetPlayerStat(),
                ElementType.Neutral
            );

            // 레일건 보너스
            calculatedDamage *= 2.0f;

            player.DecreaseHP(calculatedDamage);
        }
        else
        {
            player.DecreaseHP(damage);
        }
    }

    #endregion

    #region 시각적 효과

    private void PlayFireSound()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }

    private void CleanupEffects()
    {
        // 3개 빔 모두 끄기
        if (railgunBeam != null)
            railgunBeam.enabled = false;
        if (leftBeam != null)
            leftBeam.enabled = false;
        if (rightBeam != null)
            rightBeam.enabled = false;

        if (chargingParticle != null)
            chargingParticle.Stop();

        // 충돌 이펙트는 자동으로 정리됨 (Destroy 타이머에 의해)
    }

    #endregion

    #region 퍼블릭 접근자 및 Enemy_Middle_Boss2 호환성

    public bool IsReady => !isFiring;

    public void SetChargingRailgunProperties(float newDamage, float newRange, float newChargingTime, DamageType newDamageType)
    {
        damage = newDamage;
        maxRange = newRange;
        chargingTime = newChargingTime;
        damageType = newDamageType;
    }

    // Enemy_Middle_Boss2와의 호환성을 위한 ForceStop 메서드 (최소한만 추가)
    public void ForceStop()
    {
        if (currentRailgunSequence != null)
        {
            StopCoroutine(currentRailgunSequence);
            currentRailgunSequence = null;
        }

        CleanupEffects();
        isFiring = false;

        Debug.Log("레일건 시스템 강제 정지됨");
    }

    #endregion
}