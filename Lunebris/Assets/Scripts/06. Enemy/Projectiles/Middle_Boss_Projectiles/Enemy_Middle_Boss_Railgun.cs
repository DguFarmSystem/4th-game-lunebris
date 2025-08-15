// Enemy_Middle_Boss_Railgun.cs (명중률 개선 버전)
using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 보스 차징 레일건 공격 - 명중률 개선
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

    [Header("컴포넌트")]
    [SerializeField] private LineRenderer railgunBeam;        // 중앙 빔
    [SerializeField] private LineRenderer leftBeam;           // 좌측 빔
    [SerializeField] private LineRenderer rightBeam;          // 우측 빔
    [SerializeField] private ParticleSystem chargingParticle;

    [Header("사운드")]
    [SerializeField] private AudioClip fireSound;

    private Enemy_Base sourceBoss;
    private Transform firePoint;
    private AudioSource audioSource;
    private bool isFiring = false;

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

            // 그라디언트 색상
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.cyan, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.3f, 1.0f)
                }
            );
            beam.colorGradient = gradient;
        }
    }

    private void SetupChargingParticle()
    {
        if (chargingParticle != null)
        {
            chargingParticle.Stop();

            var main = chargingParticle.main;
            main.playOnAwake = false;
            main.startColor = Color.cyan;
            main.startSize = 1f;
            main.startLifetime = 1f;
        }
    }

    #endregion

    #region 차징 레일건

    public void FireInstantRailgun(Vector3 targetPosition)
    {
        if (isFiring) return;

        StartCoroutine(ChargingRailgunSequence(targetPosition));
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
    }

    private IEnumerator ChargingPhase()
    {
        // 차징 파티클 시작
        if (chargingParticle != null && firePoint != null)
        {
            chargingParticle.transform.position = firePoint.position;
            chargingParticle.Play();
        }

        // 차징 시간 대기
        yield return new WaitForSeconds(chargingTime);

        // 차징 완료 - 파티클 정지
        if (chargingParticle != null)
        {
            chargingParticle.Stop();
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

        // Y축 완전 고정 (수평 발사)
        baseDirection.y = 0;
        baseDirection = baseDirection.normalized;

        // 3갈래 방향 계산
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
                if (!hitPlayer)
                {
                    endPos = hit.point;
                }
                break;
            }
        }

        // 빔 표시
        if (beam != null)
        {
            beam.enabled = true;
            beam.positionCount = 2;
            beam.SetPosition(0, startPos);
            beam.SetPosition(1, endPos);
        }

        return endPos;
    }

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

            // 순간딜 보너스
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
    }

    #endregion

    #region 퍼블릭 접근자

    public bool IsReady => !isFiring;

    public void SetChargingRailgunProperties(float newDamage, float newRange, float newChargingTime, DamageType newDamageType)
    {
        damage = newDamage;
        maxRange = newRange;
        chargingTime = newChargingTime;
        damageType = newDamageType;
    }

    #endregion
}
