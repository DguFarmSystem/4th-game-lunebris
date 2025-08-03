using UnityEngine;
using System.Collections;

/// <summary>
/// 임시 데미지 처리 컴포넌트 - 보스 공격 시스템에서 공통 사용
/// </summary>
public class Enemy_Temp_Damage_Projectile : MonoBehaviour
{
    [Header("데미지 설정")]
    public float damage = 50f;
    public string projectileType = "공격";
    public bool continuousDamage = false; // 지속 데미지 여부

    private float lastDamageTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DealDamage(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (continuousDamage && other.CompareTag("Player"))
        {
            // 0.3초마다 지속 데미지
            if (Time.time - lastDamageTime >= 0.3f)
            {
                DealDamage(other);
                lastDamageTime = Time.time;
            }
        }
    }

    private void DealDamage(Collider playerCollider)
    {
        Player.Player playerScript = playerCollider.GetComponent<Player.Player>();
        if (playerScript != null)
        {
            playerScript.DecreaseHP(damage);
            Debug.Log($"{projectileType} 적중! {damage} 데미지!");
        }
    }

    /// <summary>
    /// 데미지 설정 초기화
    /// </summary>
    public void Initialize(float damageAmount, string attackType, bool isContinuous = false)
    {
        damage = damageAmount;
        projectileType = attackType;
        continuousDamage = isContinuous;
    }
}