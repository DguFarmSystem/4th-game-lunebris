// Unity
using UnityEngine;

// System
using System.Collections.Generic;

namespace Player
{
    /// <summary>
    /// Defein Player's Stat Type
    /// </summary>
    public enum StatType
    {
        MoveSpeed,
        AttackDamage,
        AttackSpeed,
        SkillDamage,
        CoolDown,
        DefensivePower,
        Hp
    }

    /// <summary>
    /// Define Base Stat Class
    /// </summary>
    [System.Serializable]
    public class Stat
    {
        public float Base { get; private set; }
        public float Bonus { get; private set; }

        public float Total => Base + Bonus;

        public Stat(float baseValue)
        {
            Base = baseValue;
            Bonus = 0f;
        }

        public void SetBase(float value) => Base = value;
        public void AddBonus(float value) => Bonus += value;
        public void ResetBonus() => Bonus = 0f;
    }

    /// <summary>
    /// Define Player Stat Class
    /// </summary>
    [System.Serializable]
    public class PlayerStat
    {
        private Dictionary<StatType, Stat> stats = new();

        public PlayerStat()
        {
            stats[StatType.MoveSpeed] = new Stat(5f);
            stats[StatType.AttackDamage] = new Stat(10f);
            stats[StatType.AttackSpeed] = new Stat(1f);
            stats[StatType.SkillDamage] = new Stat(15f);
            stats[StatType.CoolDown] = new Stat(0f);
            stats[StatType.DefensivePower] = new Stat(5f);
            stats[StatType.Hp] = new Stat(100f);
        }

        public float Get(StatType type) => stats[type].Total;
        public void SetBase(StatType type, float value) => stats[type].SetBase(value);
        public void AddBonus(StatType type, float value) => stats[type].AddBonus(value);
        public void ResetBonus(StatType type) => stats[type].ResetBonus();
        public void ResetAllBonus()
        {
            foreach (var stat in stats.Values)
                stat.ResetBonus();
        }
    }

    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        private PlayerStat stat;

        private void Start()
        {
            stat = new PlayerStat();
        }

        public PlayerStat GetPlayerStat()
        {
            return stat;
        }
    }
}

