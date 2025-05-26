// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        MaxHp
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
            stats[StatType.AttackSpeed] = new Stat(0.5f);
            stats[StatType.SkillDamage] = new Stat(15f);
            stats[StatType.CoolDown] = new Stat(0f);
            stats[StatType.DefensivePower] = new Stat(5f);
            stats[StatType.MaxHp] = new Stat(1000f);
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

    public class EXPData
    {
        public int MaxEXP { get; set; }
    }

    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpTMP;

        [SerializeField] private Slider xpSlider;
        [SerializeField] private TextMeshProUGUI xpTMP;

        [SerializeField] private TextMeshProUGUI levelTMP;

        private PlayerStat stat;
        private CSVReader csvReader;
        private List<EXPData> expData;

        private int level;

        private float currentHP;

        private int currentXP;
        private int maxXP;

        private void Awake()
        {
            stat = new PlayerStat();
            csvReader = new CSVReader();
            expData = csvReader.ReadCSVFile<List<EXPData>>("EXPData");
        }

        private void Start()
        {
            currentHP = stat.Get(StatType.MaxHp);

            level = 1;

            currentXP = 0;
            maxXP = expData[0].MaxEXP;
            UpdateXP();
        }

        private void Update()
        {
            // Test Code
            if (Input.GetKeyDown(KeyCode.Space))
                IncreaseXP(50);
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                DecreaseHP(50f);
            }
        }

        #region Methods
        public PlayerStat GetPlayerStat()
        {
            return stat;
        }

        public void IncreaseHP(float _value)
        {
            currentHP += _value;
            UpdateHP();
        }

        public void DecreaseHP(float _value)
        {
            currentHP -= _value;
            UpdateHP();
        }

        private void UpdateHP()
        {
            hpSlider.value = 1 / stat.Get(StatType.MaxHp) * currentHP;
            hpTMP.text = currentHP.ToString() + " / " + stat.Get(StatType.MaxHp);
        }

        public void IncreaseXP(int _value)
        {
            currentXP += _value;

            if (currentXP >= maxXP)
            {
                LevelUp(currentXP - maxXP);
            }

            UpdateXP();
        }

        private void LevelUp(int _remainXP)
        {
            level++;
            maxXP = expData[level - 1].MaxEXP;

            currentXP = _remainXP;
        }

        private void UpdateXP()
        {
            xpSlider.value = (float)currentXP / (float)maxXP;
            xpTMP.text = currentXP.ToString() + " / " + maxXP.ToString();

            levelTMP.text = "Lv." + level.ToString();
        }
        #endregion
    }
}

