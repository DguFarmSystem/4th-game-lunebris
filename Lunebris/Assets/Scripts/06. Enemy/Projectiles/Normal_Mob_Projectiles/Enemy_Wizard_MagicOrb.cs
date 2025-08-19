using UnityEngine;
using Enemy;

/// <summary>
/// 마법사가 발사하는 마법 구슬
/// </summary>
public class Enemy_Wizard_MagicOrb : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float lifeTime = 4f;

    [Header("마법 구슬 속성")]
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private ElementType elementType = ElementType.Neutral;
    [SerializeField] private bool useAdvancedDamageSystem = true;

    [Header("유도 기능")]
    [SerializeField] private bool isHoming = true;
    [SerializeField] private float homingStrength = 2f;

    [Header("시각적 효과")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip impactSound;

    private Transform target;
    private bool hasHitTarget = false;

    private void Start()
    {
        // 플레이어 타겟 찾기
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }

        PlayLaunchSound();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        MoveMagicOrb();
    }

    private void MoveMagicOrb()
    {
        Vector3 moveDirection = transform.forward;

        if (isHoming && target != null && !hasHitTarget)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            moveDirection = Vector3.Slerp(moveDirection, directionToTarget, homingStrength * Time.deltaTime).normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            HitPlayer(other);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            HitObstacle();
        }
    }

    private void HitPlayer(Collider playerCollider)
    {
        Player.Player player = playerCollider.GetComponent<Player.Player>();
        if (player != null)
        {
            hasHitTarget = true;

            if (useAdvancedDamageSystem)
            {
                ApplyDamageSystem(player);
            }
            else
            {
                player.DecreaseHP(damage);
            }

        }

        CreateImpactEffect();
        Destroy(gameObject);
    }

    private void HitObstacle()
    {
        CreateImpactEffect();
        Destroy(gameObject);
    }

    private void ApplyDamageSystem(Player.Player player)
    {
        EnemyStatSystem tempWizardStats = new EnemyStatSystem(EnemyType.RangedAP);

        float calculatedDamage = DamageCalculator.CalculateDamageToPlayer(
            tempWizardStats,
            elementType,
            damageType,
            player.GetPlayerStat(),
            ElementType.Neutral
        );

        // 계산된 데미지 적용 (수정됨)
        player.DecreaseHP(calculatedDamage);

    }

    private void PlayLaunchSound()
    {
        if (launchSound != null)
        {
            AudioSource.PlayClipAtPoint(launchSound, transform.position);
        }
    }

    private void CreateImpactEffect()
    {
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }
    }

    #region 설정 메서드들
    public void Initialize(float newDamage, float newSpeed, DamageType newDamageType, ElementType newElement)
    {
        damage = newDamage;
        speed = newSpeed;
        damageType = newDamageType;
        elementType = newElement;
        useAdvancedDamageSystem = true;
    }

    public void SetHomingProperties(bool enableHoming, float strength = 2f)
    {
        isHoming = enableHoming;
        homingStrength = strength;
    }

    public void SetBasicProperties(float newDamage, float newSpeed)
    {
        damage = newDamage;
        speed = newSpeed;
    }
    #endregion
}
