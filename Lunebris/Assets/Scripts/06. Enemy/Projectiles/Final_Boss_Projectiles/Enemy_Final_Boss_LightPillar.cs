using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스 빛 모드 - 빛 기둥 공격 (단일 기둥 + 슬로우 효과)
/// </summary>
public class Enemy_Final_Boss_LightPillar : MonoBehaviour
{
    [Header("빛 기둥 설정")]
    [SerializeField] private float pillarRadius = 4f;
    [SerializeField] private float pillarHeight = 20f;

    [Header("공격 설정")]
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float activeDuration = 2f;

    [Header("슬로우 효과 설정")]
    [SerializeField] private float slowDuration = 3f;
    [SerializeField] private float slowIntensity = 0.7f; // 70% 속도 감소

    [Header("이펙트")]
    [SerializeField] private GameObject warningEffect;   // 프리팹
    [SerializeField] private GameObject pillarEffect;
    [SerializeField] private Light pillarLight;

    private GameObject instantiatedWarningEffect;
    private float damage;
    private bool isActive = false;
    private AudioSource audioSource;
    private CapsuleCollider damageCollider;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        damageCollider = GetComponent<CapsuleCollider>();

        if (damageCollider == null)
            damageCollider = gameObject.AddComponent<CapsuleCollider>();

        damageCollider.isTrigger = true;
        damageCollider.radius = pillarRadius;
        damageCollider.height = pillarHeight;
        damageCollider.center = new Vector3(0, pillarHeight * 0.5f, 0);
        damageCollider.enabled = false;
        damageCollider.material = null; // 물리 재질 제거

        if (pillarLight == null)
        {
            pillarLight = GetComponent<Light>();
            if (pillarLight == null)
                pillarLight = gameObject.AddComponent<Light>();
        }

        SetupPillarLight();
    }

    private void SetupPillarLight()
    {
        if (pillarLight == null) return;

        pillarLight.type = LightType.Point;
        pillarLight.color = Color.white;
        pillarLight.intensity = 0f;
        pillarLight.range = pillarRadius * 2f;
    }

    public void StartLightPillar(float pillarDamage, float duration = 3.5f)
    {
        damage = pillarDamage;
        warningDuration = duration * 0.4f;
        activeDuration = duration * 0.6f;

        StartCoroutine(PillarSequence());
    }

    /// <summary>
    /// 슬로우 효과 설정
    /// </summary>
    public void SetSlowEffect(float duration, float intensity)
    {
        slowDuration = duration;
        slowIntensity = intensity;
    }

    private IEnumerator PillarSequence()
    {
        yield return StartCoroutine(WarningPhase());
        yield return StartCoroutine(ActivePhase());
        Cleanup();
        Destroy(gameObject);
    }

    private IEnumerator WarningPhase()
    {
        if (warningEffect != null)
        {
            instantiatedWarningEffect = Instantiate(
                warningEffect,
                transform.position + Vector3.down * 0.1f,  // 바닥에 살짝 위치
                Quaternion.identity,
                transform // 부모를 이 오브젝트로 설정
            );
            instantiatedWarningEffect.SetActive(true);
        }

        yield return new WaitForSeconds(warningDuration);

        if (instantiatedWarningEffect != null)
        {
            Destroy(instantiatedWarningEffect);
        }
    }

    private IEnumerator ActivePhase()
    {
        if (pillarEffect != null)
        {
            pillarEffect.SetActive(true);
        }

        damageCollider.enabled = true;
        isActive = true;

        if (pillarLight != null)
        {
            pillarLight.intensity = 10f;
            pillarLight.color = Color.white;
        }

        yield return new WaitForSeconds(activeDuration);

        isActive = false;
        damageCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            Player.Player playerComponent = other.GetComponent<Player.Player>();
            if (playerComponent != null)
            {
                // 데미지 적용
                playerComponent.DecreaseHP(damage);

                // 슬로우 효과 적용
                ApplySlowEffectToPlayer(other.gameObject);

                Debug.Log($"플레이어가 빛 기둥에 맞음! 데미지: {damage}, 슬로우: {slowIntensity * 100}% 감속 {slowDuration}초");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            // 지속 데미지 로직은 필요시 구현
        }
    }

    /// <summary>
    /// 플레이어에게 슬로우 효과 적용
    /// </summary>
    private void ApplySlowEffectToPlayer(GameObject player)
    {
        // 방법 1: 보스 스크립트를 통해 슬로우 효과 적용 (가장 안전)
        Enemy_Final_Boss_Light boss = FindObjectOfType<Enemy_Final_Boss_Light>();
        if (boss != null)
        {
            boss.ApplySlowToPlayer(slowDuration, slowIntensity);
            return;
        }

        // 방법 2: 플레이어 스크립트에서 슬로우 메서드 직접 호출
        Player.Player playerScript = player.GetComponent<Player.Player>();
        if (playerScript != null)
        {
            // ApplySlowEffect 메서드가 있는지 확인
            var slowMethod = playerScript.GetType().GetMethod("ApplySlowEffect");
            if (slowMethod != null)
            {
                slowMethod.Invoke(playerScript, new object[] { slowDuration, slowIntensity });
                Debug.Log($"플레이어에게 직접 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
                return;
            }
        }

        // 방법 3: 플레이어 이동 컴포넌트에서 슬로우 메서드 호출
        var movementComponents = player.GetComponents<MonoBehaviour>();
        foreach (var component in movementComponents)
        {
            var moveSlowMethod = component.GetType().GetMethod("ApplySlowEffect");
            if (moveSlowMethod != null)
            {
                moveSlowMethod.Invoke(component, new object[] { slowDuration, slowIntensity });
                Debug.Log($"플레이어 이동 컴포넌트에 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
                return;
            }
        }

        // 방법 4: 직접 Rigidbody 제어 (백업용)
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            StartCoroutine(ApplyDirectSlowEffect(playerRb));
            Debug.Log($"Rigidbody 직접 제어로 슬로우 효과 적용: {slowIntensity * 100}% 감속, {slowDuration}초");
        }
        else
        {
            Debug.LogWarning("플레이어에게 슬로우 효과를 적용할 수 없습니다!");
        }
    }

    /// <summary>
    /// 직접 슬로우 효과 적용 (백업용)
    /// </summary>
    private IEnumerator ApplyDirectSlowEffect(Rigidbody playerRb)
    {
        float originalDrag = playerRb.drag;
        float slowDrag = originalDrag + (slowIntensity * 10f); // 드래그 증가로 슬로우 효과

        playerRb.drag = slowDrag;
        yield return new WaitForSeconds(slowDuration);

        // 원래 드래그 값으로 복원
        if (playerRb != null) // null 체크 (플레이어가 파괴될 수 있음)
        {
            playerRb.drag = originalDrag;
        }
    }

    private void Cleanup()
    {
        if (instantiatedWarningEffect != null)
            Destroy(instantiatedWarningEffect);

        if (pillarEffect != null)
            pillarEffect.SetActive(false);

        if (pillarLight != null)
            pillarLight.intensity = 0f;

        isActive = false;
        damageCollider.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position + Vector3.up * pillarHeight * 0.5f, pillarRadius);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 edgePoint = position + new Vector3(
                Mathf.Cos(angle) * pillarRadius,
                0,
                Mathf.Sin(angle) * pillarRadius
            );
            Gizmos.DrawLine(edgePoint, edgePoint + Vector3.up * pillarHeight);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position + Vector3.up * 0.1f, pillarRadius);

        for (int i = 0; i < 16; i++)
        {
            float angle1 = i * 22.5f * Mathf.Deg2Rad;
            float angle2 = (i + 1) * 22.5f * Mathf.Deg2Rad;
            Vector3 point1 = position + new Vector3(Mathf.Cos(angle1) * pillarRadius, 0.1f, Mathf.Sin(angle1) * pillarRadius);
            Vector3 point2 = position + new Vector3(Mathf.Cos(angle2) * pillarRadius, 0.1f, Mathf.Sin(angle2) * pillarRadius);
            Gizmos.DrawLine(point1, point2);
        }

        // 슬로우 효과 범위 표시 (시각적으로 구분)
        if (isActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position + Vector3.up * pillarHeight * 0.5f, pillarRadius * 1.2f);
        }
    }

    public void SetPillarSize(float radius, float height)
    {
        pillarRadius = radius;
        pillarHeight = height;

        if (damageCollider != null)
        {
            damageCollider.radius = radius;
            damageCollider.height = height;
            damageCollider.center = new Vector3(0, height * 0.5f, 0);
        }

        if (pillarLight != null)
        {
            pillarLight.range = radius * 2f;
        }
    }

    public bool IsWarning => Time.time < warningDuration && !isActive;
    public bool IsActiveState => isActive;
    public float SlowDuration => slowDuration;
    public float SlowIntensity => slowIntensity;
}