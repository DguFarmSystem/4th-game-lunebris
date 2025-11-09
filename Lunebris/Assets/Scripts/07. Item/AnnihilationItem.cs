using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class AnnihilationItem : Item
{
    [Header("Effect")]
    public float radius = 8f; // 효과 반경
    public float damage = 9999f;         
    public LayerMask enemyLayer = ~0;  // 에디터에서 'Enemy' 레이어로 설정 권장
    public GameObject explosionVFX; // 폭발 이펙트

    private bool used = false;  

    void Awake()
    {
        itemID = 111;
        itemName = "AnnihilationItem";
    }

    public override void DestroyAfterTime()
    {
        Invoke(nameof(DestroyObject), 30.0f);
    }

    public override void ApplyEffect()
    {
        if (used) return; // 중복 실행 방지
        used = true;

        CancelInvoke(nameof(DestroyObject));

        Debug.Log($"{itemName} 효과 발동! 주변 적 몰살!");

        // 폭발 이펙트 생성
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // 반경 내 적 탐지
        Collider[] enemies = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        foreach (var hit in enemies)
        {
            if (hit == null) continue;

            // Enemy_Base 컴포넌트가 있으면
            var enemy = hit.GetComponentInParent<Enemy_Base>();
            if (enemy != null)
            {
                // 적 사망 처리 (프로젝트에 맞게 수정 가능)
                enemy.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
                continue;
            }

            // Enemy_Base가 없는 경우도 대비
            hit.gameObject.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;

        if (other.CompareTag("Player"))
        {
            ApplyEffect();
        }
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
