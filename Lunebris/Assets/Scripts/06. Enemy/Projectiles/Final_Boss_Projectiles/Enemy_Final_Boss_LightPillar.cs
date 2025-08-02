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
    [SerializeField] private GameObject warningEffect;
    [SerializeField] private GameObject pillarEffect;
    [SerializeField] private Light pillarLight;

    private float damage;
    private bool isActive = false;
    private AudioSource audioSource;
    private CapsuleCollider damageCollider;

    private void Awake()
    {
        // 컴포넌트 초기화
        audioSource = GetComponent<AudioSource>();
        damageCollider = GetComponent<CapsuleCollider>();

        // 콜라이더 초기 설정
        if (damageCollider == null)
            damageCollider = gameObject.AddComponent<CapsuleCollider>();

        damageCollider.isTrigger = true;
        damageCollider.radius = pillarRadius;
        damageCollider.height = pillarHeight;
        damageCollider.center = new Vector3(0, pillarHeight * 0.5f, 0);
        damageCollider.enabled = false;

        // 조명 설정
        if (pillarLight == null)
            pillarLight = gameObject.AddComponent<Light>();

        pillarLight.type = LightType.Point;
        pillarLight.color = Color.white;
        pillarLight.intensity = 0f;
        pillarLight.range = pillarRadius * 2f;
    }

    public void StartLightPillar(float pillarDamage, float duration = 3.5f)
    {
        damage = pillarDamage;
        warningDuration = duration * 0.4f; // 40%는 경고 시간
        activeDuration = duration * 0.6f;  // 60%는 활성화 시간

        StartCoroutine(PillarSequence());
    }

    private IEnumerator PillarSequence()
    {
        // 1단계: 경고
        if (warningEffect != null)
            warningEffect.SetActive(true);

        // 경고 깜빡임 효과
        StartCoroutine(BlinkWarning());

        yield return new WaitForSeconds(warningDuration);

        // 2단계: 활성화
        if (warningEffect != null)
            warningEffect.SetActive(false);

        if (pillarEffect != null)
            pillarEffect.SetActive(true);

        // 데미지 콜라이더 활성화
        damageCollider.enabled = true;
        isActive = true;

        // 조명 강화
        if (pillarLight != null)
        {
            pillarLight.intensity = 10f;
        }

        yield return new WaitForSeconds(activeDuration);

        // 3단계: 종료 및 소멸
        isActive = false;
        Destroy(gameObject);
    }

    private IEnumerator BlinkWarning()
    {
        float blinkTime = 0f;

        while (blinkTime < warningDuration)
        {
            if (pillarLight != null)
            {
                // 빨간색으로 깜빡임
                bool visible = Mathf.Sin(Time.time * 10f) > 0;
                pillarLight.intensity = visible ? 3f : 0f;
                pillarLight.color = Color.red;
            }

            blinkTime += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            Player.Player playerComponent = other.GetComponent<Player.Player>();
            if (playerComponent != null)
            {
                playerComponent.DecreaseHP(damage);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 지속 데미지는 Enemy_Temp_Damage_Projectile에서 처리
        if (isActive && other.CompareTag("Player"))
        {
            Enemy_Temp_Damage_Projectile damageScript = GetComponent<Enemy_Temp_Damage_Projectile>();
            if (damageScript == null)
            {
                damageScript = gameObject.AddComponent<Enemy_Temp_Damage_Projectile>();
                damageScript.Initialize(damage * 0.3f, "빛 기둥", true); // 초당 30% 데미지
            }
        }
    }
}