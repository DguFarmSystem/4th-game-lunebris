using UnityEngine;
using Enemy;

/// <summary>
/// 중간 보스가 발사하는 투사체 스크립트
/// 플레이어와 충돌 시 데미지를 주고 사라짐
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Enemy_Middle_Boss_Bullet : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private ElementType elementType = ElementType.Dark;
    [SerializeField] private float lifeTime = 1f;

    [Header("시각적 효과")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private TrailRenderer trail;

    private bool hasHit = false;

    private void Start()
    {
        // 수명 설정
        Destroy(gameObject, lifeTime);

        // 트리거로 설정
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>
    /// 투사체 초기화 (보스에서 호출)
    /// </summary>
    public void Initialize(float bulletDamage, DamageType bulletDamageType, ElementType bulletElement = ElementType.Dark)
    {
        damage = bulletDamage;
        damageType = bulletDamageType;
        elementType = bulletElement;
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

        // 투사체 오브젝트 파괴
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
