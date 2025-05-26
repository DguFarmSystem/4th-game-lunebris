// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class SkillList : MonoBehaviour
    {
        public void SelectSkill(Skill _skill)
        {
            switch (_skill.id)
            {
                case 0:
                    Lux1(_skill);
                    break;
                case 1:
                    Lux2(_skill);
                    break;
                case 2:
                    Lux3(_skill);
                    break;
                case 3:
                    Lux4(_skill);
                    break;
                case 4:
                    Tenebris1(_skill);
                    break;
                case 5:
                    Tenebris2(_skill);
                    break;
                case 6:
                    Tenebris3(_skill);
                    break;
                case 7:
                    Tenebris4(_skill);
                    break;
            }
        }

        private void Lux1(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Lux2(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Lux3(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Lux4(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Tenebris1(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Tenebris2(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Tenebris3(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }

        private void Tenebris4(Skill _skill)
        {
            Debug.Log("스킬 이름 : " + _skill.skillName);
            Debug.Log("스킬 데미지 : " + _skill.damage);
        }
    }
}

