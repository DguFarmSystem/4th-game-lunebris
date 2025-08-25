using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class CooldownReduction : Item
{
    public float reduction = 0.1f;

    private void Awake()
    {
        itemID = 6;
        itemName = "CooldownReduction";
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
            }
        }
    }

    public override void ApplyEffect()//효과 적용
    {
        Player.Player player = GameObject.FindWithTag("Player").GetComponent<Player.Player>();
        player.IncreaseCoolDown(reduction);
        Debug.Log("현재 쿨감: " + player.GetPlayerStat().Get(StatType.CoolDown));
        Destroy(gameObject); // 아이템 오브젝트 제거
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
