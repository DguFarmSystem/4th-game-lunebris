// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        private string attribute;
        private string skillName;

        private int skillNumber;
        private int damage;

        private float coolTime;
        private float timer;

        private bool canUse;

        private void Start()
        {
            timer = 0f;
            canUse = true;
        }

        private void Init(string _attribute, string _skillName, int _skillNumber, int _damage, float _coolTime, float _timer)
        {
            // Initialize
            attribute = _attribute;
            skillName = _skillName;
            skillNumber = _skillNumber;
            damage = _damage;
            coolTime = _coolTime;
            timer = _timer;
        }

        public void UseSkill()
        {
            if (!canUse) return;

            DeactivateSkill();
            StartCoroutine(CoolTimeCoroutine());
        }

        private IEnumerator CoolTimeCoroutine()
        {
            while(timer < coolTime)
            {
                timer += Time.deltaTime;
                yield return null;

                // UI ��ȭ �ڵ�
            }
            ActivateSkill();
        }

        private void ActivateSkill()
        {
            canUse = true;
            // UI Ȱ��ȭ
        }

        private void DeactivateSkill()
        {
            canUse = false;
            // UI ��Ȱ��ȭ
        }
    }
}
