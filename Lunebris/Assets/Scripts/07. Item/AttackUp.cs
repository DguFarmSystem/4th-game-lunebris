using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class AttackUp : Item
{
    public float bonusAmount = 10f;

    private void Awake()
    {
        itemID = 1;
        itemName = "AttackUp";
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
        player.GetPlayerStat().AddBonus(StatType.AttackDamage, bonusAmount);
        Debug.Log("공격력 증가, 현재 공격력: " + player.GetPlayerStat().Get(StatType.AttackDamage));
        Destroy(gameObject); // 아이템 오브젝트 제거
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
