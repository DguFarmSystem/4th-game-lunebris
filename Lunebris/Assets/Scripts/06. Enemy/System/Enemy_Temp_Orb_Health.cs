using UnityEngine;
using System.Collections;

/// <summary>
/// 임시 구체 체력 시스템 - 프리팹이 없을 때 사용
/// </summary>
public class Enemy_Temp_Orb_Health : MonoBehaviour
{
    private Enemy_Final_Boss_Neutral boss;
    private float health;
    private bool isLightOrb;

    public void Initialize(Enemy_Final_Boss_Neutral ownerBoss, float orbHealth, bool lightOrb)
    {
        boss = ownerBoss;
        health = orbHealth;
        isLightOrb = lightOrb;

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttack") || other.CompareTag("Projectile") || other.CompareTag("Player"))
        {
            TakeDamage(50f);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        StartCoroutine(HitEffect());

        if (health <= 0)
        {
            if (boss != null)
            {
                if (isLightOrb)
                    boss.OnLightOrbDestroyed();
                else
                    boss.OnDarkOrbDestroyed();
            }
            Destroy(gameObject);
        }
    }

    private IEnumerator HitEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }
}