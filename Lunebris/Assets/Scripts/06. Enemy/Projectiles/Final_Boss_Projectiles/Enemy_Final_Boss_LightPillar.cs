using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스 빛 모드 - 빛 기둥 공격
/// </summary>
public class Enemy_Final_Boss_LightPillar : MonoBehaviour
{
    [Header("빛 기둥 설정")]
    [SerializeField] private float pillarRadius = 4f;
    [SerializeField] private float pillarHeight = 20f;

    [Header("공격 설정")]
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float activeDuration = 2f;

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

    private IEnumerator PillarSequence()
    {
        yield return StartCoroutine(WarningPhase());
        yield return StartCoroutine(ActivePhase());
        Cleanup();
        Destroy(gameObject);
    }

    private IEnumerator WarningPhase()
    {
        Debug.Log("빛 기둥 경고 시작!");

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
        else
        {
            Debug.LogWarning("Warning Effect가 할당되지 않았습니다!");
        }

        yield return new WaitForSeconds(warningDuration);

        if (instantiatedWarningEffect != null)
        {
            Destroy(instantiatedWarningEffect);
        }
    }

    private IEnumerator ActivePhase()
    {
        Debug.Log("빛 기둥 활성화!");

        if (pillarEffect != null)
        {
            pillarEffect.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Pillar Effect가 할당되지 않았습니다!");
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
                playerComponent.DecreaseHP(damage);
                Debug.Log($"플레이어가 빛 기둥에 맞음! 데미지: {damage}");
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
}
