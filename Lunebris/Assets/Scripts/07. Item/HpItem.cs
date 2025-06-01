using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HpItem : MonoBehaviour
{
    public float regenBoost = 2f;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player.Player>();
        if (player != null)
        {
            player.GetPlayerStat().AddBonus(Player.StatType.HpRegen, regenBoost);
            Destroy(gameObject);
        }
    }
}
