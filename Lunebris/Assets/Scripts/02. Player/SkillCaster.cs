// System
using System.Collections;

// Unity
using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Player
{
    [DisallowMultipleComponent]
    public class SkillCaster : MonoBehaviour
    {
        [SerializeField] private Image maskImage;
        [SerializeField] private TextMeshProUGUI coolTMP;
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

                coolTMP.text = Mathf.Ceil(_coolTime - timer).ToString();

                yield return null;
            }
            ActivateSkill();
        }

        private void ActivateSkill()
        {
            canUse = true;
            maskImage.fillAmount = 0f;
            coolTMP.gameObject.SetActive(false);
        }

        private void DeactivateSkill()
        {
            canUse = false;
            maskImage.fillAmount = 1f;
            coolTMP.gameObject.SetActive(true);
        }
    }
}
