using UnityEngine;
using Enemy;

/// <summary>
/// 원거리 몬스터가 발사하는 투사체
/// </summary>
public class Enemy_Ranged_Bullet : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 3f;

    [Header("데미지 시스템")]
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private ElementType elementType = ElementType.Neutral;
    [SerializeField] private bool useAdvancedDamageSystem = true;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) return;

        if (other.CompareTag("Player"))
        {
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                if (useAdvancedDamageSystem)
                {
                    ApplyDamageSystem(player);
                }
                else
                {
                    player.DecreaseHP(damage);
                }
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamageSystem(Player.Player player)
    {
        EnemyStatSystem tempStats = new EnemyStatSystem(EnemyType.RangedAD);

        float calculatedDamage = DamageCalculator.CalculateDamageToPlayer(
            tempStats,
            elementType,
            damageType,
            player.GetPlayerStat(),
            ElementType.Neutral
        );

        // 계산된 데미지 적용 (수정됨)
        player.DecreaseHP(calculatedDamage);

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

    public void SetDamage(float newDamage) => damage = newDamage;
    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void SetDamageType(DamageType newDamageType)
    {
        damageType = newDamageType;
        useAdvancedDamageSystem = true;
    }
    public void SetElementType(ElementType newElementType)
    {
        elementType = newElementType;
        useAdvancedDamageSystem = true;
    }

    public DamageType GetDamageType() => damageType;
    public ElementType GetElementType() => elementType;
    public bool IsUsingAdvancedSystem() => useAdvancedDamageSystem;
    #endregion
}