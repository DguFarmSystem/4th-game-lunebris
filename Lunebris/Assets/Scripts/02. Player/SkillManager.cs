// Unity
using UnityEngine;
using UnityEngine.UI;

// System
using System.Collections.Generic;

namespace Player
{
    // Enum of Skill Type
    public enum SkillType
    {
        Lux1,
        Lux2,
        Lux3,
        Lux4,
        Tenebris1,
        Tenebris2,
        Tenebris3,
        Tenebris4
    }

    /// <summary>
    /// Skll Structure Class (individually skill)
    /// </summary>
    [System.Serializable]
    public class Skill
    {
        public int id;
        public string skillName;
        public float damage;
        public float coolTime;

        public Skill(int id, string skillName, float damage, float coolTime)
        {
            this.id = id;
            this.skillName = skillName;
            this.damage = damage;
            this.coolTime = coolTime;
        }
    }

    /// <summary>
    /// To Initialize Player's Skill
    /// </summary>
    [System.Serializable]
    public class PlayerSkill
    {
        private Dictionary<SkillType, Skill> skillDict = new();

        public PlayerSkill()
        {
            skillDict[SkillType.Lux1] = new Skill(0, "Lux1", 2f, 5f);
            skillDict[SkillType.Lux2] = new Skill(1, "Lux2", 2f, 5f);
            skillDict[SkillType.Lux3] = new Skill(2, "Lux3", 2f, 5f);
            skillDict[SkillType.Lux4] = new Skill(3, "Lux4", 2f, 5f);
            skillDict[SkillType.Tenebris1] = new Skill(4, "Tenebris1", 2f, 5f);
            skillDict[SkillType.Tenebris2] = new Skill(5, "Tenebris2", 2f, 5f);
            skillDict[SkillType.Tenebris3] = new Skill(6, "Tenebris3", 2f, 5f);
            skillDict[SkillType.Tenebris4] = new Skill(7, "Tenebris4", 2f, 5f);
        }

        // Get Skill
        public Skill Get(SkillType type) => skillDict[type];
    }

    [DisallowMultipleComponent]
    public class SkillManager : MonoBehaviour
    {
        [SerializeField] private SkillCaster[] caster;
        [SerializeField] private MapController map;
        private PlayerSkill playerSkill;
        private Skill skill;

        private void Awake()
        {
            playerSkill = new PlayerSkill();
        }

        private void Update()
        {
            SkillHandler();
        }

        public void SkillHandler()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if(map.GetCurrentAttribute() == "lux") skill = playerSkill.Get(SkillType.Lux1);
                else skill = playerSkill.Get(SkillType.Tenebris1);

                caster[skill.id].UseSkill(skill);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (map.GetCurrentAttribute() == "lux") skill = playerSkill.Get(SkillType.Lux2);
                else skill = playerSkill.Get(SkillType.Tenebris2);

                caster[skill.id].UseSkill(skill);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (map.GetCurrentAttribute() == "lux") skill = playerSkill.Get(SkillType.Lux3);
                else skill = playerSkill.Get(SkillType.Tenebris3);

                caster[skill.id].UseSkill(skill);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (map.GetCurrentAttribute() == "lux") skill = playerSkill.Get(SkillType.Lux4);
                else skill = playerSkill.Get(SkillType.Tenebris4);

                caster[skill.id].UseSkill(skill);
            }
        }
    }
}
