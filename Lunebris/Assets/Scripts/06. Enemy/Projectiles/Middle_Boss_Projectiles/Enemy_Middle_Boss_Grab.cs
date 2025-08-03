using UnityEngine;
using Enemy;

/// <summary>
/// 중간보스가 발사하는 그랩 투사체
/// 플레이어에게 맞으면 보스 쪽으로 끌어당기는 효과 발생
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Enemy_Middle_Boss_Grab : MonoBehaviour
{
    [Header("그랩 투사체 설정")]
    [SerializeField] private float pullForce = 8f;
    [SerializeField] private float pullDuration = 2f;
    [SerializeField] private float damage = 40f;
    [SerializeField] private float lifeTime = 5f;

    [Header("시각적 효과")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private GameObject grabEffect;

    private bool hasHit = false;
    private Enemy_Middle_Boss sourceBoss;

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        // 트리거로 설정
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(Enemy_Middle_Boss boss, float grabPullForce, float grabDuration, float grabDamage)
    {
        sourceBoss = boss;
        pullForce = grabPullForce;
        pullDuration = grabDuration;
        damage = grabDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null && sourceBoss != null)
            {
                ApplyGrabEffect(other.transform, player);
            }
            HitTarget();
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            HitTarget();
        }
    }

    private void ApplyGrabEffect(Transform playerTransform, Player.Player player)
    {
        if (sourceBoss != null)
        {
            float calculatedDamage = DamageCalculator.CalculateDamageToPlayer(
                sourceBoss.GetEnemyStats(),
                sourceBoss.GetElementType(),
                DamageType.Physical, // 그랩은 물리 데미지로 가정
                player.GetPlayerStat(),
                ElementType.Neutral
            );

            player.DecreaseHP(calculatedDamage);
        }
        
        // 그랩 이펙트 생성
        if (grabEffect != null)
        {
            GameObject effect = Instantiate(grabEffect, playerTransform.position, Quaternion.identity);
            Destroy(effect, pullDuration);
        }

        // 보스에게 그랩 성공 알림
        sourceBoss.OnGrabProjectileHit(playerTransform, pullForce, pullDuration);
    }

    private void HitTarget()
    {
        if (hasHit) return;
        hasHit = true;

        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (trail != null)
        {
            trail.autodestruct = true;
        }

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