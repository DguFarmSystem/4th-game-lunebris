// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// System
using System.Collections.Generic;
using System;

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
        HpRegen,
        MagicDefensivePower,
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
            stats[StatType.AttackDamage] = new Stat(100f);
            stats[StatType.AttackSpeed] = new Stat(0.5f);
            stats[StatType.SkillDamage] = new Stat(15f);
            stats[StatType.CoolDown] = new Stat(0f);
            stats[StatType.DefensivePower] = new Stat(5f);
            stats[StatType.MagicDefensivePower] = new Stat(5f);
            stats[StatType.MaxHp] = new Stat(1000f);
            stats[StatType.HpRegen] = new Stat(0f); //per second
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
        [Header("HP UI")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpTMP;

        [Header("Shield UI")]
        [SerializeField] private Slider shieldSlider;
        [SerializeField] private TextMeshProUGUI shieldTMP;

        [Header("EXP UI")]
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

        private bool hasInvincibilityItem = false;
        private float invincibilityCooldown = 15f;
        [SerializeField] private float invincibilityDuration = 3f;
        private float lastInvincibilityUseTime = -999f;
        private bool isInvincible = false;

        public void SetInvincibilityDuration(float duration)
        {
            invincibilityDuration = duration;
        }
        public void GiveInvincibilityItem(float customDuration = -1f)
        {
            hasInvincibilityItem = true;
            if (customDuration > 0)
                SetInvincibilityDuration(customDuration);

            Debug.Log($"무적 아이템을 획득했습니다! 지속 시간: {invincibilityDuration}초");
        }

        private void ActivateTimedInvincibility()
        {
            isInvincible = true;
            lastInvincibilityUseTime = Time.time;
            Debug.Log("무적 상태 시작");

            Invoke(nameof(ResetInvincibility), invincibilityDuration);
        }

        private void ResetInvincibility()
        {
            isInvincible = false;
            Debug.Log("무적 상태 종료");
        }

        //쉴드 관련 변수 추가
        private float currentShield;
        private float maxShield = 100f;
        private GameObject activeShieldVFX;

        private void Awake()
        {
            stat = new PlayerStat();
            csvReader = new CSVReader();
            expData = csvReader.ReadCSVFile<List<EXPData>>("EXPData");
        }

        private void Start()
        {
            currentHP = stat.Get(StatType.MaxHp);
            currentShield = 0f;

            level = 1;

            currentXP = 0;
            maxXP = expData[0].MaxEXP;
            UpdateXP();

            UpdateHP();
            UpdateShieldUI();

            StartCoroutine(HPRegenRoutine());
        }

        private System.Collections.IEnumerator HPRegenRoutine()
        {
            while (true)
            {
                float regen = stat.Get(StatType.HpRegen) * Time.deltaTime;
                if (currentHP < stat.Get(StatType.MaxHp))
                {
                    currentHP = Mathf.Min(currentHP + regen, stat.Get(StatType.MaxHp));
                    UpdateHP();
                }

                yield return null;
            }
        }

        private void Update()
        {
            // Test Code
            if (Input.GetKeyDown(KeyCode.Space))
                IncreaseXP(50);

            if (Input.GetKeyDown(KeyCode.Z))
            {
                Debug.Log("체력: " + stat.Get(StatType.MaxHp));
                Debug.Log("이속: " + stat.Get(StatType.MoveSpeed));
                Debug.Log("공격: " + stat.Get(StatType.AttackDamage));
                Debug.Log("공속: " + stat.Get(StatType.AttackSpeed));
                Debug.Log("스뎀: " + stat.Get(StatType.SkillDamage));
                Debug.Log("방어: " + stat.Get(StatType.DefensivePower));
                Debug.Log("마저: " + stat.Get(StatType.MagicDefensivePower));
                Debug.Log("쿨감: " + stat.Get(StatType.CoolDown));
            }

            UpdateHP();
        }

        private void OnTriggerEnter(Collider collision)
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Enemy"))
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
            currentHP = Mathf.Clamp(currentHP, 0, stat.Get(StatType.MaxHp));
            UpdateHP();
        }
        
        public void DecreaseHP(float _value)
        {
            if (isInvincible)
            {
                Debug.Log("무적 상태");
                return;
            }
            if (hasInvincibilityItem)
            {
                float timeSinceLastUse = Time.time - lastInvincibilityUseTime;
                if (timeSinceLastUse >= invincibilityCooldown)
                {
                    ActivateTimedInvincibility();
                    return;
                }
            }

            currentHP -= _value;
            UpdateHP();

            if (currentHP <= 0)
            {
                if (Inventory.instance.HasItem("Reborn"))
                {
                    Item rebornItem = Inventory.instance.GetItem("Reborn");
                    rebornItem.ApplyEffect();
                    Inventory.instance.Remove(rebornItem);
                }
            }
            
            float damage = _value;

            if (currentShield >= damage)
            {
                currentShield -= damage;
                Debug.Log($"쉴드가 데미지 {damage}를 흡수. 남은 쉴드: {currentShield}");
            }
            else
            {
                float remainingDamage = damage - currentShield;
                currentShield = 0;
                currentHP -= remainingDamage;
                Debug.Log($"쉴드 없음 체력에 {remainingDamage} 데미지.");
            }

            currentHP = Mathf.Max(currentHP, 0);

            // [추가] 쉴드가 0 이하로 떨어졌고, 활성화된 이펙트가 있다면 파괴
            if (currentShield <= 0 && activeShieldVFX != null)
            {
                Destroy(activeShieldVFX);
                activeShieldVFX = null; // 참조를 깨끗하게 비워줍니다.
            }

            UpdateHP();
            UpdateShieldUI();
        }

        private void UpdateHP()
        {
            hpSlider.value = currentHP / stat.Get(StatType.MaxHp);
            hpTMP.text = $"{currentHP:F0} / {stat.Get(StatType.MaxHp)}";
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

        public void IncreaseMoveSpeed(float _value)
        {
            stat.AddBonus(StatType.MoveSpeed, _value);
            Debug.Log("이속 증가!");
        }

        public void IncreaseAttackDamage(float _value)
        {
            stat.AddBonus(StatType.AttackDamage, _value);
            Debug.Log("공격력 증가!");
        }

        public void IncreaseAttackSpeed(float _value)
        {
            stat.AddBonus(StatType.AttackSpeed, _value);
            Debug.Log("공속 증가!");
        }

        public void IncreaseSkillDamage(float _value)
        {
            stat.AddBonus(StatType.SkillDamage, _value);
            Debug.Log("스킬데미지 증가!");
        }

        public void IncreaseCoolDown(float _value)
        {
            stat.AddBonus(StatType.CoolDown, _value);
            Debug.Log("쿨감 증가!");
        }

        public void IncreaseDefensivePower(float _value)
        {
            stat.AddBonus(StatType.DefensivePower, _value);
            Debug.Log("방어력 증가!");
        }

        public void IncreaseMagicDefensivePower(float _value)
        {
            stat.AddBonus(StatType.MagicDefensivePower, _value);
            Debug.Log("마저 증가!");
        }

        public void IncreaseMaxHp(float _value)
        {
            stat.AddBonus(StatType.MaxHp, _value);
            Debug.Log("체력 증가!");
        }

        public float GetMoveSpeed()
        {
            return stat.Get(StatType.MoveSpeed);
        }

        public float GetAttackDamage()
        {
            return stat.Get(StatType.AttackDamage);
        }

        public float GetAttackSpeed()
        {
            return stat.Get(StatType.AttackSpeed);
        }

        public float GetSkillDamage()
        {
            return stat.Get(StatType.SkillDamage);
        }

        public float GetCoolDown()
        {
            return stat.Get(StatType.CoolDown);
        }

        public float GetDefensivePower()
        {
            return stat.Get(StatType.DefensivePower);
        }

        public float GetMagicDefensivePower()
        {
            return stat.Get(StatType.MagicDefensivePower);
        }

        public float GetMaxHp()
        {
            return stat.Get(StatType.MaxHp);
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


        public float CurrentHP => currentHP;

        public void AddShield(float _value, GameObject shieldVFXPrefab)
        {
            if (currentShield <= 0)
            {
                if (activeShieldVFX != null)
                {
                    Destroy(activeShieldVFX);
                }

                // 새로운 쉴드 이펙트 생성 및 저장
                if (shieldVFXPrefab != null)
                {
                    activeShieldVFX = Instantiate(shieldVFXPrefab, this.transform.position, Quaternion.identity, this.transform);
                }
            }

            currentShield += _value;
            currentShield = Mathf.Clamp(currentShield, 0, maxShield);
            Debug.Log($"쉴드 {_value} 획득! [현재 쉴드: {currentShield}]");
            UpdateShieldUI();
        }
        
        private void UpdateShieldUI()
        {
            if (shieldSlider == null) return;
            shieldSlider.gameObject.SetActive(currentShield > 0);
            shieldSlider.maxValue = stat.Get(StatType.MaxHp);
            shieldSlider.value = currentShield;
            if (shieldTMP != null)
            {
                shieldTMP.text = currentShield > 0 ? $"{currentShield:F0}" : "";
            }
        }
        public void ConsumeHP(float _value)
        {
            // DecreaseHP와 달리 피격 판정이 아닌, 순수한 체력 소모를 처리
            currentHP -= _value;
            currentHP = Mathf.Max(currentHP, 0); // 체력이 0 밑으로 내려가지 않도록 보정
            UpdateHP(); // UI 업데이트
        }

        public float GetCurrentHP()
        {
            return currentHP;
        }

        #endregion
    }
}

