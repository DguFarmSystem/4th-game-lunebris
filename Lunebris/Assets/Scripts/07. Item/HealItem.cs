using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public class HealItem : Item
{
    public int healAmount = 20;

    void Awake()
    {
        itemID = 101;
        itemName = "Health Potion";
    }

    public override void DestroyAfterTime()//시간 지난 후 파괴
    {
        Invoke("DestroyObject", 30.0f);
    }

    public override void ApplyEffect()
    {
        Debug.Log($"{itemName} 효과 발동! 체력 {healAmount} 회복!");
        // Player.instance.Heal(healAmount);
        Player.Player player = GameObject.FindWithTag("Player").GetComponent<Player.Player>();
        player.IncreaseHP(healAmount);
        Debug.Log("체력 증가, 현재 체력: " + player.GetCurrentHP());
        // 효과가 적용되면 즉시 파괴
        Destroy(gameObject);
    }
}