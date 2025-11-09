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

    [Header("애니메이션 설정")]
    [SerializeField] protected Animator characterAnimator; // 자신 캐릭터의 Animator
    [SerializeField] protected bool usePhysicsMovement = true; // Rigidbody 사용 여부
    [SerializeField] protected float enemyRotationSpeed = 5f; // 회전 속도

    [Header("충돌 설정")]
    [SerializeField] protected bool ignoreEnemyCollisions = true; // Enemy끼리 충돌 무시 여부

    [Header("죽음 처리 설정")]
    [SerializeField] protected float destroyDelay = 2f; // 죽음 후 삭제까지 딜레이 (효과를 위해)

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

    // 물리 및 애니메이션 관련
    protected Rigidbody enemyRigidbody;
    protected Collider enemyCollider;

    // 애니메이션 파라미터 이름들
    protected readonly string ANIM_IS_MOVING = "isMoving";
    protected readonly string ANIM_MOVE_SPEED = "moveSpeed";
    protected readonly string ANIM_ATTACK_TRIGGER = "attack";
    protected readonly string ANIM_DIE_TRIGGER = "die";
    protected readonly string ANIM_IS_DEAD = "isDead";
    protected readonly string ANIM_HIT_TRIGGER = "hit";

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

        // 컴포넌트 참조들
        enemyRigidbody = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();

        // 자식에서 Animator 찾기
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        // 렌더러 컴포넌트 (피격 효과용) - 자식에서도 찾기
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (enemyRenderer != null)
        {
            // 머티리얼 복사해서 원본 보호
            enemyRenderer.material = new Material(enemyRenderer.material);
            originalColor = enemyRenderer.material.color;
        }

        killDetector = FindObjectOfType<EnemyKillDetector>();

        // 경고 메시지들
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator를 찾을 수 없습니다. 애니메이션이 재생되지 않습니다.");
        }

        if (usePhysicsMovement && enemyRigidbody == null)
        {
            Debug.LogWarning($"{name}: Rigidbody가 없어서 Transform 기반 이동을 사용합니다.");
            usePhysicsMovement = false;
        }
    }

    protected virtual void Start()
    {
        InitializeEnemy();
        UpdateHpUI();

        // Enemy끼리 충돌 무시 설정 (Start에서 실행 - 모든 Enemy가 생성된 후)
        if (ignoreEnemyCollisions)
        {
            SetupEnemyCollisionIgnoring();
        }
    }

    protected virtual void Update()
    {
        if (!isDead && playerTransform != null)
        {
            UpdateBehavior();
        }
    }

    protected virtual void OnEnable()
    {
        // 풀에서 재활성화될 때 호출
        if (isDead) // 죽음 상태에서 재활성화되는 경우
        {
            ResetForPooling();
        }
    }

    protected virtual void OnDisable()
    {
        // 비활성화될 때 모든 코루틴 정리
        StopAllCoroutines();
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

        // Rigidbody 설정
        if (usePhysicsMovement && enemyRigidbody != null)
        {
            enemyRigidbody.freezeRotation = true; // Y축 회전만 허용하도록 설정할 수도 있음
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

    #region Enemy 충돌 무시 시스템

    /// <summary>
    /// 다른 모든 Enemy와의 물리적 충돌을 무시하도록 설정
    /// </summary>
    protected virtual void SetupEnemyCollisionIgnoring()
    {
        if (enemyCollider == null) return;

        // 현재 씬의 모든 Enemy_Base를 상속받은 오브젝트 찾기
        Enemy_Base[] allEnemies = FindObjectsOfType<Enemy_Base>();

        foreach (Enemy_Base otherEnemy in allEnemies)
        {
            // 자기 자신은 제외
            if (otherEnemy == this || otherEnemy.enemyCollider == null) continue;

            // 물리적 충돌 무시 설정
            Physics.IgnoreCollision(enemyCollider, otherEnemy.enemyCollider, true);
        }

        Debug.Log($"{enemyName}: {allEnemies.Length - 1}개의 다른 Enemy와 충돌 무시 설정 완료");
    }

    /// <summary>
    /// 새로 생성된 Enemy와 충돌 무시 설정 (런타임에 Enemy가 스폰될 때 사용)
    /// </summary>
    public virtual void IgnoreCollisionWith(Enemy_Base otherEnemy)
    {
        if (otherEnemy == null || otherEnemy == this) return;
        if (enemyCollider == null || otherEnemy.enemyCollider == null) return;

        Physics.IgnoreCollision(enemyCollider, otherEnemy.enemyCollider, true);
    }

    /// <summary>
    /// 모든 기존 Enemy들과 새로 생성된 Enemy의 충돌 무시 설정
    /// (Enemy 스포너에서 호출할 수 있는 정적 메서드)
    /// </summary>
    public static void SetupCollisionIgnoringForNewEnemy(Enemy_Base newEnemy)
    {
        if (newEnemy == null || newEnemy.enemyCollider == null) return;

        Enemy_Base[] existingEnemies = FindObjectsOfType<Enemy_Base>();

        foreach (Enemy_Base existingEnemy in existingEnemies)
        {
            if (existingEnemy != newEnemy && existingEnemy.ignoreEnemyCollisions)
            {
                newEnemy.IgnoreCollisionWith(existingEnemy);
            }
        }
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
    /// 몬스터별 이동 패턴 (기본 구현 제공)
    /// </summary>
    protected virtual void UpdateMovement()
    {
        if (playerTransform == null || isDead) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        float moveSpeed = enemyStats.Get(EnemyStatType.MoveSpeed);

        // 실제 이동 처리
        bool isMoving = direction.magnitude > 0.1f;

        if (isMoving)
        {
            if (usePhysicsMovement && enemyRigidbody != null)
            {
                // Rigidbody를 사용한 물리 기반 이동
                Vector3 velocity = direction * moveSpeed;
                velocity.y = enemyRigidbody.velocity.y; // Y축 속도는 유지 (중력)
                enemyRigidbody.velocity = velocity;
            }
            else
            {
                // Transform 기반 이동
                transform.position += direction * moveSpeed * Time.deltaTime;
            }

            // 방향 전환
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    Time.deltaTime * enemyRotationSpeed);
            }
        }
        else if (usePhysicsMovement && enemyRigidbody != null)
        {
            // 이동하지 않을 때는 수평 속도를 0으로
            Vector3 velocity = enemyRigidbody.velocity;
            velocity.x = 0;
            velocity.z = 0;
            enemyRigidbody.velocity = velocity;
        }

        // 애니메이션 업데이트
        UpdateMovementAnimation(isMoving, moveSpeed);
    }

    #endregion

    #region 애니메이션 관리

    protected virtual void UpdateMovementAnimation(bool isMoving, float moveSpeed)
    {
        if (characterAnimator == null) return;

        // 이동 애니메이션 제어
        characterAnimator.SetBool(ANIM_IS_MOVING, isMoving);
        characterAnimator.SetFloat(ANIM_MOVE_SPEED, isMoving ? moveSpeed : 0f);
    }

    protected virtual void PlayAttackAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_ATTACK_TRIGGER);
    }

    protected virtual void PlayHitAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_HIT_TRIGGER);
    }

    protected virtual void PlayDeathAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_DIE_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DEAD, true);

        // 물리 효과 비활성화
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = true;
        }

        // 콜라이더 비활성화 (공격 받지 않도록)
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
    }

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
                attackerElement,
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
            return;
        }
    }

    protected virtual void OnDamaged()
    {
        // 피격 애니메이션 재생
        PlayHitAnimation();

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

    // MODIFIED: 풀링에 호환되는 죽음 처리
    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log($"{enemyName}: 죽음!");

        // 죽음 애니메이션 재생
        PlayDeathAnimation();

        // 경험치 지급
        GiveExperience();

        // 사망 효과
        PlayDeathEffects();

        // 킬 감지기 업데이트
        if (killDetector != null)
        {
            killDetector.UpdateKillPower(GetElementType());
        }

        // CHANGED: Destroy 대신 풀로 반환하는 코루틴 시작
        StartCoroutine(ReturnToPoolAfterDeath());

        Debug.Log($"{enemyName}: 풀로 반환 예정");
    }

    // NEW: 죽음 후 풀로 반환하는 코루틴
    protected virtual System.Collections.IEnumerator ReturnToPoolAfterDeath()
    {
        // 죽음 애니메이션 길이만큼 대기
        float deathAnimationLength = 1f; // 기본값

        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
        {
            // 실제 애니메이션 길이 가져오기
            AnimationClip[] clips = characterAnimator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("death") || clip.name.ToLower().Contains("die"))
                {
                    deathAnimationLength = clip.length;
                    break;
                }
            }
        }

        // 애니메이션이 완료될 때까지 대기
        yield return new WaitForSeconds(deathAnimationLength);

        // 추가 지연시간 (이펙트를 위해)
        yield return new WaitForSeconds(destroyDelay);

        // 풀링을 위한 상태 리셋
        ResetForPooling();

        // Destroy 대신 비활성화
        gameObject.SetActive(false);

        Debug.Log($"{enemyName} 풀로 반환 완료!");
    }

    // NEW: 풀링을 위한 상태 리셋
    protected virtual void ResetForPooling()
    {
        // 기본 상태 리셋
        isDead = false;
        currentHp = enemyStats.Get(EnemyStatType.MaxHp);

        // 물리 및 콜라이더 리셋
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.velocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        // 애니메이터 상태 리셋
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(ANIM_IS_DEAD, false);
            characterAnimator.SetBool(ANIM_IS_MOVING, false);
            characterAnimator.SetFloat(ANIM_MOVE_SPEED, 0f);
        }

        // 색상 리셋
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }

        // HP바 숨기기
        if (hpCanvas != null)
        {
            hpCanvas.gameObject.SetActive(false);
        }

        // 다음 사용을 위한 HP UI 업데이트
        UpdateHpUI();

        Debug.Log($"{enemyName} 상태 리셋 완료 - 재사용 준비됨");
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

        // 공격 애니메이션 재생
        PlayAttackAnimation();

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
        // Enemy 태그인 경우 충돌을 무시 (통과)
        if (other.CompareTag("Enemy") && ignoreEnemyCollisions)
        {
            return; // 아무 작업도 하지 않음
        }

        if (other.CompareTag("Attack"))
        {
            // BaseAttack 컴포넌트 가져오기
            Player.BaseAttack baseAttack = other.GetComponent<Player.BaseAttack>();

            if (baseAttack != null)
            {
                // 직접 데미지 적용
                float directDamage = baseAttack.GetBaseDamage();
                TakeDamage(directDamage, DamageType.Physical, ElementType.Neutral);

                // 스플래쉬 효과가 있다면 실행 (직접 맞은 적 제외)
                if (baseAttack.HasSplashEffect())
                {
                    baseAttack.ExecuteSplashEffect(transform.position, this);
                }
            }
            else
            {
                // 기존 로직 (BaseAttack이 없을 경우 대비)
                TakeDamage(10f, DamageType.Physical, ElementType.Neutral);
            }

            // 총알 비활성화
            other.gameObject.SetActive(false);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Enemy 태그인 경우 물리적 상호작용을 무시
        if (collision.gameObject.CompareTag("Enemy") && ignoreEnemyCollisions)
        {
            return; // 이미 Physics.IgnoreCollision으로 처리되어 있어야 함
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

    /// <summary>
    /// 외부에서 적을 강제로 죽일 때 사용하는 public 메서드
    /// </summary>
    public void Death()
    {
        Die();
    }

    /// <summary>
    /// 풀에서 재활성화될 때 호출되는 메서드
    /// </summary>
    public virtual void OnPoolReactivated()
    {
        // PoolManager에서 적을 재사용할 때 호출
        // 필요한 특별한 초기화가 있다면 여기에 추가

        // 다른 적들과의 충돌 무시 설정
        if (ignoreEnemyCollisions)
        {
            SetupCollisionIgnoringForNewEnemy(this);
        }

        Debug.Log($"{enemyName} 풀에서 재활성화됨");
    }

    #endregion
}