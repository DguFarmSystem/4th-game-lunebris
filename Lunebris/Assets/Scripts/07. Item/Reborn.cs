using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class Reborn : Item
{
    private void Awake()
    {
        itemID = 3;
        itemName = "Reborn";
        applyCondition = true;
    }

    public override void DestroyAfterTime()//시간 지난 후 파괴
    {
        Invoke("DestroyObject", 30.0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool wasPickedUp = Inventory.instance.Add(this);
            if (wasPickedUp)
            {
                CancelInvoke("DestroyObject");
                gameObject.SetActive(false);
            }
        }
    }

    public override void ApplyEffect()//효과 적용
    {
        Player.Player player = GameObject.FindWithTag("Player").GetComponent<Player.Player>();
            
            Debug.Log("부활~~");

            player.StartCoroutine(HealOverTime(player, player.GetMaxHp(), 2f));
            Destroy(gameObject); // 아이템 오브젝트 제거
         
    }

    private IEnumerator HealOverTime(Player.Player player, float targetHP, float duration)
    {
        float startHP = player.CurrentHP;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float newHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, t));
            player.IncreaseHP(newHP - player.CurrentHP); // 차액만큼 증가

            yield return null;
        }

        player.IncreaseHP(targetHP - player.CurrentHP);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
