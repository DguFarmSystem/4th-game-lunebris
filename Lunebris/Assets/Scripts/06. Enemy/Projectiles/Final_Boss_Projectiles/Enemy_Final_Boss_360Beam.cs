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

    /// <summary>
    /// Enemy 정보와 함께 360도 빔 발사 (더 정확한 데미지 계산용)
    /// </summary>
    public void Fire360Beams(EnemyStatSystem enemyStats, ElementType elementType)
    {
        this.creatorStats = enemyStats;
        this.creatorElement = elementType;
        Fire360Beams();
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
        // 360도 패턴 발사 후 자신은 제거
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

            // 실제 오브젝트도 회전시키고 싶다면 아래 주석 해제
            // transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);

            yield return new WaitForSeconds(delayBetweenBeams);

            currentAngle += angleStep;

            // 회전 속도 적용해보고 싶다면 아래 주석 해제
            // yield return RotateOverTime(angleStep);
        }

        isFiring = false;
        // 360도 패턴 발사 후 자신은 제거
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// 빔 1개 생성 (360도 패턴용)
    /// </summary>
    private void FireSingleBeam(Vector3 position, Vector3 velocity)
    {
        GameObject beam = Instantiate(beamPrefab, position, Quaternion.LookRotation(velocity.normalized));
        var beamScript = beam.GetComponent<Enemy_Final_Boss_360Beam>();
        if (beamScript != null)
        {
            // creatorStats가 있으면 정확한 데미지 계산, 없으면 기본 데미지 사용
            if (creatorStats != null)
            {
                beamScript.Initialize(beamDamage, velocity, creatorStats, creatorElement);
            }
            else
            {
                beamScript.Initialize(beamDamage, velocity);
            }

            // 슬로우 효과 전달
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

        // 부메랑 모드가 아닌 경우 기존 방식
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

        // 부메랑 모드 - 1단계: 전진
        while (!isReturning && timer < beamLifetime)
        {
            Vector3 movement = beamVelocity * Time.deltaTime;
            transform.position += movement;
            traveledDistance += movement.magnitude;

            // 최대 거리에 도달하면 돌아가기 시작
            if (traveledDistance >= maxDistance)
            {
                yield return new WaitForSeconds(2.0f); // 2초 동안 대기
                isReturning = true;
                Debug.Log("빔이 최대 거리에 도달! 돌아갑니다.");
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 부메랑 모드 - 2단계: 돌아가기
        if (isReturning)
        {
            while (timer < beamLifetime)
            {
                // 타겟 위치 업데이트 (보스가 움직일 수 있으므로)
                GameObject boss = GameObject.FindGameObjectWithTag("Enemy");
                if (boss != null)
                {
                    targetPosition = boss.transform.position;
                }

                // 돌아갈 방향 계산
                Vector3 returnDirection = (targetPosition - transform.position).normalized;
                Vector3 returnMovement = returnDirection * returnSpeed * Time.deltaTime;

                // 위치 업데이트
                transform.position += returnMovement;

                // 회전 업데이트 (돌아가는 방향으로)
                if (returnDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(returnDirection);
                }

                // 목표 지점에 충분히 가까워지면 제거
                if (destroyOnReturn && Vector3.Distance(transform.position, targetPosition) < 1f)
                {
                    Debug.Log("빔이 원점에 도달하여 제거됩니다.");
                    Destroy(gameObject);
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        // 시간 초과로 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 부드러운 회전이 필요한 경우 사용
    /// </summary>
    private IEnumerator RotateOverTime(float angle)
    {
        float rotated = 0f;

        while (rotated < angle)
        {
            float step = rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, step, 0f);
            rotated += step;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 보스와 충돌 시 무시 (물리적 밀어내기 방지)
        if (other.CompareTag("Enemy"))
        {
            return; // 아무것도 하지 않음
        }

        // 플레이어와 충돌 시 데미지 처리
        if (other.CompareTag("Player"))
        {
            // 플레이어 스크립트 찾기 (Enemy_Base 방식 참고)
            var playerScript = other.GetComponent<Player.Player>();
            if (playerScript != null)
            {
                float finalDamage = isSingleBeam ? damage : beamDamage;

                // Enemy_Base.cs의 DealDamageToPlayer 방식을 참고한 데미지 계산
                if (creatorStats != null)
                {
                    // DamageCalculator 사용 (Enemy_Base와 동일한 방식)
                    finalDamage = DamageCalculator.CalculateDamageToPlayer(
                        creatorStats,
                        creatorElement,
                        damageType,
                        playerScript.GetPlayerStat(),
                        ElementType.Neutral // 플레이어 속성
                    );
                }

                // 플레이어에게 데미지 적용 (Enemy_Base와 동일한 방식)
                playerScript.DecreaseHP(finalDamage);

                string phase = isReturning ? "돌아가는 중" : "전진 중";
                Debug.Log($"360빔이 {phase} 플레이어에게 {finalDamage:F1} {damageType} 데미지를 입혔습니다!");

                // 슬로우 효과 적용
                ApplySlowEffectToPlayer(playerScript);
            }

            // 부메랑 모드가 아니거나, 부메랑 모드에서 전진 중일 때만 제거
            // 돌아가는 중에는 제거하지 않음 (계속 데미지 가능)
            if (isSingleBeam && (!isBoomerang || !isReturning))
            {
                Destroy(gameObject);
            }
        }
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
            Debug.Log($"360빔 - 플레이어에게 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
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
                Debug.Log($"360빔 - 플레이어 이동에 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
                return;
            }
        }

        // 방법 3: 직접 Rigidbody 제어 (임시 방법)
        var playerRb = playerObj.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            StartCoroutine(ApplyTemporarySlowEffect(playerRb, finalSlowDuration, finalSlowIntensity));
            Debug.Log($"360빔 - 플레이어에게 임시 슬로우 효과 적용: {finalSlowIntensity * 100}% 감속, {finalSlowDuration}초 지속");
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
}