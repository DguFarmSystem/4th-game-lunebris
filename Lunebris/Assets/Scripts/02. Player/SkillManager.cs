// Unity
using UnityEngine;

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
            skillDict[SkillType.Lux1] = new Skill(1, "Lux1", 2f, 5f);
        }

        // Get Skill
        public Skill Get(SkillType _skillType) => skillDict[_skillType];
    }

    [DisallowMultipleComponent]
    public class SkillManager : MonoBehaviour
    {
        private PlayerSkill skill;

        private void Awake()
        {
            skill = new PlayerSkill();
        }

        private void Update()
        {
            SkillHandler();
        }

        public void SkillHandler()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log(skill.Get(SkillType.Lux1));
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("스킬 2");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("스킬 3");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("스킬 4");
            }
        }
    }
}
