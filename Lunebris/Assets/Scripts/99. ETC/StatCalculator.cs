// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

public class StatCalculator : MonoBehaviour
{
    [SerializeField] private float defensivePowerRate = 1f;
    [SerializeField] private float magicDefensivePowerRate = 1f;

    public float CalculateAttackDamage(float damage, float defensivePower)
    {
        return damage - (defensivePower * defensivePowerRate);
    }

    public float CalculateSkillDamage(float damage, float magicDefensivePower)
    {
        return damage - (magicDefensivePower * magicDefensivePowerRate);
    }

    public float CalculateSkillCoolTime(float coolTime, float coolDown)
    {
        return coolTime - coolDown;
    }
}
