// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Enemy
{
    /// <summary>
    /// 몬스터 스탯 타입 정의
    /// </summary>
    public enum EnemyStatType
    {
        MaxHp,
        PhysicalDamage,     // AD (물리 데미지)
        MagicalDamage,      // AP (마법 데미지)
        AttackSpeed,
        MoveSpeed,
        PhysicalDefense,    // 물리 방어력
        MagicalDefense,     // 마법 방어력
        AttackRange,
        DetectionRange
    }

    /// <summary>
    /// 몬스터 종류 정의
    /// </summary>
    public enum EnemyType
    {
        MeleeTanker,    // 근접 탱커형 (AD)
        MeleeAssassin,  // 근접 어쌔신형 (AD)
        RangedAD,       // 원거리 AD형 (물리)
        RangedAP,       // 원거리 AP형 (마법)
        MiddleBoss,     // 중간보스 (혼합)
        FinalBoss       // 최종보스 (혼합)
    }

    /// <summary>
    /// 데미지 타입 정의
    /// </summary>
    public enum DamageType
    {
        Physical,   // 물리 데미지 (AD)
        Magical     // 마법 데미지 (AP)
    }

    /// <summary>
    /// 기본 스탯 클래스 
    /// </summary>
    [System.Serializable]
    public class EnemyStat
    {
        public float Base { get; private set; }
        public float Bonus { get; private set; }

        public float Total => Base + Bonus;

        public EnemyStat(float baseValue)
        {
            Base = baseValue;
            Bonus = 0f;
        }

        public void SetBase(float value) => Base = value;
        public void AddBonus(float value) => Bonus += value;
        public void ResetBonus() => Bonus = 0f;
    }

    /// <summary>
    /// 몬스터 전체 스탯 관리 클래스
    /// </summary>
    [System.Serializable]
    public class EnemyStatSystem
    {
        private Dictionary<EnemyStatType, EnemyStat> stats = new();

        public EnemyStatSystem(EnemyType enemyType)
        {
            InitializeStats(enemyType);
        }

        private void InitializeStats(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.MeleeTanker:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(150f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(25f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(0f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(0.8f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(1.5f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(15f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(5f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(1.5f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(8f);
                    break;

                case EnemyType.MeleeAssassin:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(80f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(45f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(0f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(1.5f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(3f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(3f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(8f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(1.2f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(12f);
                    break;

                case EnemyType.RangedAD:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(60f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(35f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(0f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(1.2f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(2f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(2f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(5f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(8f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(10f);
                    break;

                case EnemyType.RangedAP:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(70f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(0f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(50f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(0.8f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(1.8f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(1f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(15f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(10f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(12f);
                    break;

                case EnemyType.MiddleBoss:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(500f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(40f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(40f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(0.6f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(2f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(20f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(20f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(6f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(15f);
                    break;

                case EnemyType.FinalBoss:
                    stats[EnemyStatType.MaxHp] = new EnemyStat(2000f);
                    stats[EnemyStatType.PhysicalDamage] = new EnemyStat(70f);
                    stats[EnemyStatType.MagicalDamage] = new EnemyStat(80f);
                    stats[EnemyStatType.AttackSpeed] = new EnemyStat(0.4f);
                    stats[EnemyStatType.MoveSpeed] = new EnemyStat(1f);
                    stats[EnemyStatType.PhysicalDefense] = new EnemyStat(35f);
                    stats[EnemyStatType.MagicalDefense] = new EnemyStat(35f);
                    stats[EnemyStatType.AttackRange] = new EnemyStat(8f);
                    stats[EnemyStatType.DetectionRange] = new EnemyStat(20f);
                    break;
            }
        }

        public float Get(EnemyStatType type) => stats.ContainsKey(type) ? stats[type].Total : 0f;
        public void SetBase(EnemyStatType type, float value) => stats[type].SetBase(value);
        public void AddBonus(EnemyStatType type, float value) => stats[type].AddBonus(value);
        public void ResetBonus(EnemyStatType type) => stats[type].ResetBonus();
        public void ResetAllBonus()
        {
            foreach (var stat in stats.Values)
                stat.ResetBonus();
        }
    }

    /// <summary>
    /// 데미지 계산 시스템
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 몬스터가 플레이어에게 주는 데미지 계산
        /// </summary>
        public static float CalculateDamageToPlayer(
            EnemyStatSystem attackerStats,
            ElementType attackerElement,
            DamageType damageType,
            Player.PlayerStat playerStats,
            ElementType playerElement = ElementType.Neutral)
        {
            // 1. 기본 데미지 가져오기
            float baseDamage = damageType == DamageType.Physical
                ? attackerStats.Get(EnemyStatType.PhysicalDamage)
                : attackerStats.Get(EnemyStatType.MagicalDamage);

            // 2. 플레이어 방어력 가져오기 (플레이어 스탯에서)
            float playerDefense = damageType == DamageType.Physical
                ? playerStats.Get(Player.StatType.DefensivePower) // 물리 방어력
                : playerStats.Get(Player.StatType.DefensivePower) * 0.8f; // 마법 방어력 (임시)

            // 3. 속성 상성 계산
            float elementMultiplier = CalculateElementalMultiplier(attackerElement, playerElement);

            // 4. 데미지 타입별 추가 계산
            float typeMultiplier = damageType == DamageType.Magical ? 1.1f : 1.0f; // 마법이 약간 더 강함

            // 5. 최종 데미지 계산
            float finalDamage = (baseDamage * elementMultiplier * typeMultiplier) - playerDefense;

            // 6. 최소 데미지 보장
            return Mathf.Max(1f, finalDamage);
        }

        /// <summary>
        /// 플레이어가 몬스터에게 주는 데미지 계산
        /// </summary>
        public static float CalculateDamageToEnemy(
            Player.PlayerStat playerStats,
            ElementType playerElement,
            DamageType damageType,
            EnemyStatSystem enemyStats,
            ElementType enemyElement)
        {
            // 1. 플레이어 기본 데미지
            float baseDamage = damageType == DamageType.Physical
            ? playerStats.Get(Player.StatType.AttackDamage)
            : playerStats.Get(Player.StatType.SkillDamage);
            // 2. 몬스터 방어력
            float enemyDefense = damageType == DamageType.Physical
                ? enemyStats.Get(EnemyStatType.PhysicalDefense)
                : enemyStats.Get(EnemyStatType.MagicalDefense);

            // 3. 속성 상성 계산
            float elementMultiplier = CalculateElementalMultiplier(playerElement, enemyElement);

            // 4. 최종 데미지 계산
            float finalDamage = (baseDamage * elementMultiplier) - enemyDefense;

            // 5. 최소 데미지 보장
            return Mathf.Max(1f, finalDamage);
        }

        /// <summary>
        /// 속성 상성 배율 계산
        /// </summary>
        private static float CalculateElementalMultiplier(ElementType attackerElement, ElementType defenderElement)
        {
            // 빛 vs 어둠 상성
            if (attackerElement == ElementType.Lux && defenderElement == ElementType.Tenebris)
            {
                return 1.5f; // 빛이 어둠에게 1.5배 데미지
            }
            else if (attackerElement == ElementType.Tenebris && defenderElement == ElementType.Lux)
            {
                return 1.5f; // 어둠이 빛에게 1.5배 데미지
            }
            else if (attackerElement == defenderElement && attackerElement != ElementType.Neutral)
            {
                return 0.7f; // 같은 속성끼리는 0.7배 데미지
            }
            else if (attackerElement == ElementType.Neutral || defenderElement == ElementType.Neutral)
            {
                return 1.0f; // 무속성은 기본 데미지
            }

            return 1.0f; // 기본 배율
        }
    }
}