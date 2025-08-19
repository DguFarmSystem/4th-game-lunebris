using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class HpRegeneration : Item
{
    public float regenIncrease = 0.5f;
    private void Awake()
    {
        itemID = 4;
        itemName = "HpRegeneration";
        applyCondition = false;
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
                Debug.Log("아이템 습득");
            }
        }
    }

    public override void ApplyEffect()//효과 적용
    {
        Player.Player player = GameObject.FindWithTag("Player").GetComponent<Player.Player>();

        if (player != null)
        {
            player.GetPlayerStat().AddBonus(Player.StatType.HpRegen, regenIncrease);
            Debug.Log($"체력 재생력이 {regenIncrease} 만큼 증가했습니다! 현재 초당 회복량: {player.GetPlayerStat().Get(Player.StatType.HpRegen)}");
        }
            Destroy(gameObject); // 아이템 오브젝트 제거

    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
