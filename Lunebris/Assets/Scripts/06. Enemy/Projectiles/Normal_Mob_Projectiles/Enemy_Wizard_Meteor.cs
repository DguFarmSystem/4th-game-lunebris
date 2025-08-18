using UnityEngine;
using Enemy;

/// <summary>
/// 마법사가 소환하는 메테오 오브젝트 스크립트
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Wizard_Meteor : MonoBehaviour
{
    [Header("메테오 설정")]
    [SerializeField] private float fallSpeed = 8f;
    [SerializeField] private float damage = 100f;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private float lifetime = 10f;        // 최대 생존 시간

    [Header("이펙트 설정")]
    public GameObject explosionEffect;                   // 폭발 이펙트 프리팹
    public AudioClip impactSound;                        // 충돌 사운드

    private Vector3 targetPosition;                      // 목표 위치 (바닥)
    private Enemy_Wizard casterWizard;                   // 시전한 마법사
    private bool hasExploded = false;                    // 폭발 여부
    private AudioSource audioSource;
    private GameObject warningInstance;                  // 마법사에서 전달받은 경고 표시기
    private ElementType meteorElement;                   // 마법사로부터 상속받은 속성

    private void Start()
    {
        // 오디오 소스 가져오기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 최대 생존 시간 후 자동 파괴
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasExploded) return;

        // 목표 위치로 낙하
        FallToTarget();

        // 바닥 도달 체크
        CheckGroundHit();
    }

    /// <summary>
    /// 메테오 초기화 (마법사에서 호출)
    /// </summary>
    public void Initialize(Vector3 target, float speed, float dmg, float radius, Enemy_Wizard wizard, GameObject warning = null, ElementType element = ElementType.Neutral)
    {
        targetPosition = target;
        fallSpeed = speed;
        damage = dmg;
        damageRadius = radius;
        casterWizard = wizard;
        meteorElement = element; // 마법사의 속성을 메테오가 상속

        // 마법사에서 전달받은 경고 표시기 참조 (메테오가 관리하지 않음)
        warningInstance = warning;
    }

    private void FallToTarget()
    {
        // 직선으로 목표 지점까지 낙하
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * fallSpeed * Time.deltaTime;

        // 메테오가 회전하며 떨어지는 효과
        transform.Rotate(Vector3.forward * 180f * Time.deltaTime);
    }

    private void CheckGroundHit()
    {
        // 목표 위치에 거의 도달했거나 바닥과 충돌했는지 체크
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f ||
            transform.position.y <= targetPosition.y + 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.Log($"메테오 폭발! 피해량: {damage}, 반경: {damageRadius}");

        // 폭발 이펙트 생성
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, targetPosition, Quaternion.identity);

            // 파티클 시스템의 지속 시간에 따라 자동 조정
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 파티클 시스템의 Duration + Start Lifetime을 고려해서 파괴
                float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(effect, Mathf.Max(totalDuration, 1f)); // 최소 1초는 유지
            }
            else
            {
                // 파티클 시스템이 없다면 1초 후 제거
                Destroy(effect, 1f);
            }
        }

        // 폭발 사운드 재생
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound);
        }

        // 범위 내 대상에게 피해 적용
        DealDamageToTargets();

        // 경고 표시기 제거 (마법사에서 관리하므로 참조만 해제)
        warningInstance = null;

        // 메테오 오브젝트 제거 (사운드 재생을 위해 약간 지연)
        Destroy(gameObject, 0.5f);
    }

    private void DealDamageToTargets()
    {
        // 범위 내의 모든 콜라이더 검색
        Collider[] hitTargets = Physics.OverlapSphere(targetPosition, damageRadius);

        foreach (Collider target in hitTargets)
        {
            // 플레이어에게 피해
            if (target.CompareTag("Player"))
            {
                var playerScript = target.GetComponent<Player.Player>();
                if (playerScript != null)
                {
                    // 거리에 따른 피해 감소 (중심에서 멀수록 피해 감소)
                    float distance = Vector3.Distance(target.transform.position, targetPosition);
                    float damageMultiplier = Mathf.Clamp01(1f - (distance / damageRadius));
                    float finalDamage = damage * damageMultiplier;

                    // 플레이어 DecreaseHP 메서드 사용
                    playerScript.DecreaseHP(finalDamage);
                    Debug.Log($"플레이어에게 메테오 피해: {finalDamage} (거리 배율: {damageMultiplier:F2})");
                }
            }

            // 다른 적들에게도 피해 (아군 피해)
            if (target.CompareTag("Enemy") && target.gameObject != casterWizard?.gameObject)
            {
                var enemyBase = target.GetComponent<Enemy_Base>();
                if (enemyBase != null)
                {
                    float distance = Vector3.Distance(target.transform.position, targetPosition);
                    float damageMultiplier = Mathf.Clamp01(1f - (distance / damageRadius));
                    float finalDamage = damage * 0.3f * damageMultiplier; // 아군 피해는 30%로 감소

                    // Enemy_Base의 TakeDamage 메서드 사용 (마법 피해, 설정된 속성)
                    enemyBase.TakeDamage(finalDamage, DamageType.Magical, meteorElement);
                    Debug.Log($"다른 적에게 메테오 피해: {finalDamage}");
                }
            }

            // 파괴 가능한 오브젝트 (환경 파괴) - 프로젝트에 DestructibleObject가 있다면 활성화
            /*
            var destructible = target.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(damage);
            }
            */
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 바닥이나 장애물과 충돌 시 즉시 폭발
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            targetPosition = transform.position;
            Explode();
        }
    }

    #region 기즈모 표시

    private void OnDrawGizmos()
    {
        // 메테오 현재 위치
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 목표 위치와 피해 반경
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // 주황색
            Gizmos.DrawWireSphere(targetPosition, damageRadius);

            // 낙하 경로
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }

    #endregion
}