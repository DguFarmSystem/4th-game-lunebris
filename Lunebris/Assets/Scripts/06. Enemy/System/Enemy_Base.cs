using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Enemy;

[DisallowMultipleComponent]
public abstract class Enemy_Base : MonoBehaviour
{
    [Header("몬스터 기본 정보")]
    [SerializeField] protected EnemyType enemyType;
    [SerializeField] protected ElementType elementType;
    [SerializeField] protected DamageType primaryDamageType; // 주 데미지 타입 (AD/AP)
    [SerializeField] protected string enemyName = "Enemy";

    [Header("UI 요소")]
    [SerializeField] protected Slider hpSlider;
    [SerializeField] protected TextMeshProUGUI hpText;
    [SerializeField] protected Canvas hpCanvas; // HP바 캔버스
    [SerializeField] protected TextMeshProUGUI damageText; // 데미지 표시용

    [Header("시각적 효과")]
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected GameObject damageEffect; // 피격 이펙트

    // 스탯 시스템
    protected EnemyStatSystem enemyStats;
    protected float currentHp;
    protected bool isDead = false;

    // 플레이어 참조
    protected Transform playerTransform;
    protected Player.Player playerScript;

    // 피격 색상 효과
    protected Renderer enemyRenderer;
    protected Color originalColor;

    protected EnemyKillDetector killDetector;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // 스탯 초기화
        enemyStats = new EnemyStatSystem(enemyType);
        currentHp = enemyStats.Get(EnemyStatType.MaxHp);

        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<Player.Player>();
        }

        // 렌더러 컴포넌트 (피격 효과용)
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            // 머티리얼 복사해서 원본 보호
            enemyRenderer.material = new Material(enemyRenderer.material);
            originalColor = enemyRenderer.material.color;
        }

        killDetector = FindObjectOfType<EnemyKillDetector>();
    }

    protected virtual void Start()
    {
        InitializeEnemy();
        UpdateHpUI();
    }

    protected virtual void Update()
    {
        if (!isDead && playerTransform != null)
        {
            UpdateBehavior();
        }
    }

    #endregion

    #region 초기화

    protected virtual void InitializeEnemy()
    {
        // HP 캔버스 설정
        if (hpCanvas != null)
        {
            // World Space Canvas는 카메라 없이도 작동
            hpCanvas.worldCamera = null;
            hpCanvas.gameObject.SetActive(false); // 기본적으로 숨김
        }

        Debug.Log($"{enemyName} ({enemyType}, {elementType}, {primaryDamageType}) 초기화 완료!");
        LogEnemyStats();
    }

    protected virtual void LogEnemyStats()
    {
        Debug.Log($"=== {enemyName} 스탯 ===");
        Debug.Log($"HP: {currentHp}");
        Debug.Log($"물리 공격력: {enemyStats.Get(EnemyStatType.PhysicalDamage)}");
        Debug.Log($"마법 공격력: {enemyStats.Get(EnemyStatType.MagicalDamage)}");
        Debug.Log($"물리 방어력: {enemyStats.Get(EnemyStatType.PhysicalDefense)}");
        Debug.Log($"마법 방어력: {enemyStats.Get(EnemyStatType.MagicalDefense)}");
        Debug.Log($"이동속도: {enemyStats.Get(EnemyStatType.MoveSpeed)}");
    }

    #endregion

    #region 추상 메서드 (각 몬스터에서 구현)

    /// <summary>
    /// 몬스터별 고유 행동 패턴
    /// </summary>
    protected abstract void UpdateBehavior();

    /// <summary>
    /// 몬스터별 공격 패턴
    /// </summary>
    protected abstract void PerformAttack();

    /// <summary>
    /// 몬스터별 이동 패턴
    /// </summary>
    protected abstract void UpdateMovement();

    #endregion

    #region 데미지 시스템

    public virtual void TakeDamage(float baseDamage, DamageType damageType = DamageType.Physical, ElementType attackerElement = ElementType.Neutral)
    {
        if (isDead) return;

        // 플레이어 스탯 가져오기 (임시로 기본값 사용)
        Player.PlayerStat tempPlayerStats = new Player.PlayerStat();

        // 데미지 계산기를 사용한 정확한 데미지 계산
        float actualDamage = DamageCalculator.CalculateDamageToEnemy(
            tempPlayerStats,
            attackerElement,
            damageType,
            enemyStats,
            elementType
        );

        // 만약 플레이어 스크립트가 있다면 실제 스탯 사용
        if (playerScript != null)
        {
            actualDamage = DamageCalculator.CalculateDamageToEnemy(
                playerScript.GetPlayerStat(),
                ElementType.Neutral, // 플레이어 속성 (추후 확장 가능)
                damageType,
                enemyStats,
                elementType
            );
        }

        // HP 감소
        currentHp -= actualDamage;

        // 데미지 표시
        ShowDamageText(actualDamage, damageType);

        Debug.Log($"{enemyName}이 {actualDamage:F1} {damageType} 데미지를 받았습니다! (남은 HP: {currentHp:F1})");

        // HP바 표시
        ShowHpBar();
        UpdateHpUI();

        // 피격 효과
        OnDamaged();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void OnDamaged()
    {
        // 피격 색상 효과
        StartCoroutine(DamageColorEffect());

        // 피격 이펙트 생성
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    /// <summary>
    /// 피격시 색상 변화 효과
    /// </summary>
    protected virtual System.Collections.IEnumerator DamageColorEffect()
    {
        if (enemyRenderer != null)
        {
            // 피격시 빨간색으로 변경
            enemyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            // 원래 색상으로 복구
            enemyRenderer.material.color = originalColor;
        }
    }

    protected virtual void ShowDamageText(float damage, DamageType damageType)
    {
        if (damageText != null)
        {
            damageText.text = $"-{damage:F0}";
            damageText.color = Color.white; // 기본 색상으로 고정

            // 데미지 텍스트 애니메이션
            StartCoroutine(AnimateDamageText());
        }
    }

    protected virtual System.Collections.IEnumerator AnimateDamageText()
    {
        if (damageText != null)
        {
            Vector3 startPos = damageText.transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f;

            float duration = 1f;
            float elapsed = 0f;

            damageText.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                damageText.transform.position = Vector3.Lerp(startPos, endPos, t);
                damageText.color = new Color(1f, 1f, 1f, 1f - t); // 페이드 아웃

                yield return null;
            }

            damageText.gameObject.SetActive(false);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        
        // 경험치 지급
        GiveExperience();

        // 사망 효과
        PlayDeathEffects();

        // 오브젝트 비활성화 또는 파괴
        gameObject.SetActive(false);

        killDetector.IncreaseTenePower();
    }

    protected virtual void GiveExperience()
    {
        if (playerScript != null)
        {
            int expReward = GetExperienceReward();
            playerScript.IncreaseXP(expReward);
        }
    }

    protected virtual int GetExperienceReward()
    {
        return enemyType switch
        {
            EnemyType.MeleeTanker => 15,
            EnemyType.MeleeAssassin => 20,
            EnemyType.RangedAD => 18,
            EnemyType.RangedAP => 25, 
            EnemyType.MiddleBoss => 100,
            EnemyType.FinalBoss => 500,
            _ => 10
        };
    }

    #endregion

    #region 공격 시스템

    /// <summary>
    /// 플레이어에게 데미지를 주는 공통 메서드
    /// </summary>
    protected virtual void DealDamageToPlayer(DamageType damageType = DamageType.Physical)
    {
        if (playerScript == null) return;

        // 데미지 계산
        float damage = DamageCalculator.CalculateDamageToPlayer(
            enemyStats,
            elementType,
            damageType,
            playerScript.GetPlayerStat(),
            ElementType.Neutral // 플레이어 속성 
        );

        // 플레이어에게 데미지 적용
        playerScript.DecreaseHP(damage);

    }

    #endregion

    #region UI 관리

    protected virtual void ShowHpBar()
    {
        if (hpCanvas != null)
        {
            hpCanvas.gameObject.SetActive(true);
            // 3초 후 숨기기
            CancelInvoke(nameof(HideHpBar));
            Invoke(nameof(HideHpBar), 3f);
        }
    }

    protected virtual void HideHpBar()
    {
        if (hpCanvas != null)
        {
            hpCanvas.gameObject.SetActive(false);
        }
    }

    protected virtual void UpdateHpUI()
    {
        float maxHp = enemyStats.Get(EnemyStatType.MaxHp);

        if (hpSlider != null)
        {
            hpSlider.value = currentHp / maxHp;
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHp:F0} / {maxHp:F0}";
        }
    }

    #endregion

    #region 유틸리티 메서드

    protected virtual void PlayDeathEffects()
    {
        // 사망 이펙트
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 사망 사운드
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
    }

    protected virtual float GetDistanceToPlayer()
    {
        if (playerTransform != null)
        {
            return Vector3.Distance(transform.position, playerTransform.position);
        }
        return float.MaxValue;
    }

    protected virtual bool IsPlayerInRange(float range)
    {
        return GetDistanceToPlayer() <= range;
    }

    protected virtual bool IsPlayerInAttackRange()
    {
        return IsPlayerInRange(enemyStats.Get(EnemyStatType.AttackRange));
    }

    protected virtual bool IsPlayerInDetectionRange()
    {
        return IsPlayerInRange(enemyStats.Get(EnemyStatType.DetectionRange));
    }

    #endregion

    #region 충돌 처리

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            other.gameObject.SetActive(false);

            // 플레이어 공격은 기본적으로 물리 데미지로 처리
            TakeDamage(10f, DamageType.Physical, ElementType.Neutral);
        }
    }

    #endregion

    #region 퍼블릭 접근자

    public EnemyType GetEnemyType() => enemyType;
    public ElementType GetElementType() => elementType;
    public DamageType GetPrimaryDamageType() => primaryDamageType;
    public EnemyStatSystem GetEnemyStats() => enemyStats;
    public float GetCurrentHp() => currentHp;
    public bool IsDead() => isDead;

    /// <summary>
    /// 주 데미지 타입에 따른 공격력 반환
    /// </summary>
    public float GetMainDamage()
    {
        return primaryDamageType == DamageType.Physical
            ? enemyStats.Get(EnemyStatType.PhysicalDamage)
            : enemyStats.Get(EnemyStatType.MagicalDamage);
    }

    #endregion
}