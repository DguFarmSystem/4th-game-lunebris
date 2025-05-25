// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [SerializeField] private Image maskImage;
        private SkillList skillList;
        private float timer;
        private bool canUse;

        private void Start()
        {
            timer = 0f;
            canUse = true;

            skillList = transform.parent.GetComponent<SkillList>();
        }

        public void UseSkill(Skill _skill)
        {
            if (!canUse) return;

            skillList.SelectSkill(_skill);
            DeactivateSkill();
            StartCoroutine(CoolTimeCoroutine(_skill.coolTime));
        }

        private IEnumerator CoolTimeCoroutine(float _coolTime)
        {
            timer = 0f;

            while(timer < _coolTime)
            {
                timer += Time.deltaTime;
                maskImage.fillAmount -= Time.deltaTime / _coolTime;
                yield return null;

                // UI 변화 코드
            }
            ActivateSkill();
        }

        private void ActivateSkill()
        {
            canUse = true;
            maskImage.fillAmount = 0f;
            Debug.Log("스킬 활성화");
            // UI 활성화
        }

        private void DeactivateSkill()
        {
            canUse = false;
            maskImage.fillAmount = 1f;
            Debug.Log("스킬 비활성화");
            // UI 비활성화
        }
    }
}
