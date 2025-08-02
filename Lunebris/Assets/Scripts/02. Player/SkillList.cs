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
        [SerializeField] private LayerMask enemyLayer;

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject skillAreaVFX; // 스킬 범위 효과 프리팹
        [SerializeField] private GameObject hitVFX;
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
            UnityEngine.Debug.Log("스킬 사용: " + _skill.skillName);

            float skillRadius = 5f; //스킬 범위

            if (skillAreaVFX != null)
            {
                GameObject vfx = Instantiate(skillAreaVFX, transform.position, Quaternion.identity);
                var mainModule = vfx.GetComponent<ParticleSystem>().main;
                mainModule.startSize = skillRadius * 2;
                Destroy(vfx, 1.0f);
            }

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillRadius, enemyLayer);

            foreach (var hitCollider in hitColliders)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    if (hitVFX != null)
                    {
                        GameObject hitVfx = Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                        Destroy(hitVfx, 0.5f);
                    }

                    enemy.Death();
                    UnityEngine.Debug.Log(hitCollider.name + "call death()");
                }
            }
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

