using UnityEngine;
using Enemy;

/// <summary>
/// 다크왕국에서 발사하는 단순한 탄환
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Enemy_Final_Boss_Bullet : MonoBehaviour
{
    [Header("탄환 설정")]
    [SerializeField] private float damage = 40f;
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private ElementType elementType = ElementType.Tenebris;
    [SerializeField] private float lifeTime = 5f;

    [Header("시각 효과")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private bool rotateWhileFlying = true;
    [SerializeField] private float rotationSpeed = 360f;

    private bool hasHit = false;
    private Enemy_Base sourceBoss;

    public void Initialize(float bulletDamage, Vector3 bulletVelocity)
    {
        damage = bulletDamage;

        // 속도 적용
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bulletVelocity;
        }

        SetupComponents();

        // 수명 설정
        Destroy(gameObject, lifeTime);
    }

    private void SetupComponents()
    {
        // 트리거로 설정
        GetComponent<Collider>().isTrigger = true;

        // 트레일 설정 (이미 프리팹에 설정되어 있을 수 있음)
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }

        if (trail != null)
        {
            SetupTrailRenderer();
        }
    }

    private void SetupTrailRenderer()
    {
        trail.time = 0.5f;
        trail.startWidth = 1f;
        trail.endWidth = 0.5f;
        trail.startColor = new Color(0.5f, 0f, 0.5f, 1f);
        trail.endColor = new Color(0.5f, 0f, 0.5f, 0f);

        // 머티리얼 설정 (선택사항)
        if (trail.material == null)
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void Update()
    {
        // 회전 효과
        if (rotateWhileFlying && !hasHit)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                ApplyDamageSystem(player);
            }
            HitTarget();
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            HitTarget();
        }
    }

    private void ApplyDamageSystem(Player.Player player)
    {
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

        // 계산된 데미지 적용
        player.DecreaseHP(calculatedDamage);
        Debug.Log($"플레이어가 다크볼릿에 맞음! 데미지: {calculatedDamage}");
    }

    private void HitTarget()
    {
        if (hasHit) return;
        hasHit = true;

        // 충돌 이펙트 생성
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 궤적 효과 정리
        if (trail != null)
        {
            trail.autodestruct = true;
        }

        // 탄환 오브젝트 파괴
        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        if (!hasHit)
        {
            Destroy(gameObject);
        }
    }
}