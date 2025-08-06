// System
using System.Collections;
using System.Collections.Generic;
using Enemy;

// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class SkillList : MonoBehaviour
    {
        [SerializeField] private MapController map;
        [SerializeField] private Player player;
        [SerializeField] private PlayerAttack playerAttack; // 마우스 방향을 얻어오기 위한 참조

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject skillAreaVFX;
        [SerializeField] private GameObject hitVFX;

        [Header("Lux1 Skill Settings")]
        [SerializeField] private float lux1_Radius = 5f; // Lux1 스킬의 범위를 설정하는 변수

        [Header("Lux3 Skill Settings")]
        [SerializeField] private float lux3_Range = 20f; // 레이저 사거리
        [SerializeField] private float lux3_Width = 3f;  // 레이저 폭
        [SerializeField] private float lux3_MinDamageMultiplier = 0.5f; // 가장자리 최소 데미지 배율 (50%)
        [SerializeField] private GameObject lux3_VFX_Prefab; // 레이저 시각 효과 프리팹

        private void Awake()
        {
            // 필요한 컴포넌트들을 자동으로 찾아 할당합니다.
            if (player == null) player = GetComponentInParent<Player>();
            if (playerAttack == null) playerAttack = GetComponentInParent<PlayerAttack>();
        }

        public void SelectSkill(Skill _skill)
        {
            switch (_skill.id)
            {
                case 0: Lux1(_skill); break;
                case 1: Lux2(_skill); break;
                case 2: Lux3(_skill); break;
                case 3: Lux4(_skill); break;
                case 4: Tenebris1(_skill); break;
                case 5: Tenebris2(_skill); break;
                case 6: Tenebris3(_skill); break;
                case 7: Tenebris4(_skill); break;
                case 8: map.ConvertMap(); break;
            }
        }

        private void Lux1(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);

            if (skillAreaVFX != null)
            {
                GameObject vfx = Instantiate(skillAreaVFX, transform.position, Quaternion.identity);
                var mainModule = vfx.GetComponent<ParticleSystem>().main;
                mainModule.startSize = lux1_Radius * 2;
                Destroy(vfx, 1.0f);
            }

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, lux1_Radius);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Enemy"))
                {
                    Enemy_Base enemy = hitCollider.GetComponent<Enemy_Base>();
                    if (enemy != null && !enemy.IsDead())
                    {
                        float totalSkillDamage = _skill.damage + player.GetPlayerStat().Get(StatType.SkillDamage);
                        enemy.TakeDamage(totalSkillDamage, DamageType.Magical, ElementType.Light);

                        if (hitVFX != null)
                        {
                            Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                        }
                        Debug.Log($"[[LUX1]]{hitCollider.name}에게 {totalSkillDamage}의 빛 속성 마법 데미지를 입혔습니다.");
                    }
                }
            }
        }

        // in Scripts/02. Player/SkillList.cs

        private void Lux3(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);

            Vector3 direction = playerAttack.GetLookDirection();
            if (direction == Vector3.zero) return;

            Vector3 boxHalfExtents = new Vector3(lux3_Width / 2f, 2f, 0.1f);
            Quaternion orientation = Quaternion.LookRotation(direction);
            RaycastHit[] hits = Physics.BoxCastAll(transform.position, boxHalfExtents, direction, orientation, lux3_Range);

            Debug.Log($"[Lux3] 레이저 범위 내에서 {hits.Length}개의 대상을 탐지했습니다.");

            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    Enemy_Base enemy = hit.collider.GetComponent<Enemy_Base>();
                    if (enemy != null && !enemy.IsDead())
                    {
                        Vector3 laserCenterLineStart = transform.position;
                        Vector3 vectorToEnemy = enemy.transform.position - laserCenterLineStart;
                        Vector3 projectedVector = Vector3.Project(vectorToEnemy, direction);
                        Vector3 closestPointOnLine = laserCenterLineStart + projectedVector;
                        float distanceFromCenter = Vector3.Distance(enemy.transform.position, closestPointOnLine);

                        float damageMultiplier = Mathf.Lerp(1f, lux3_MinDamageMultiplier, Mathf.Clamp01(distanceFromCenter / (lux3_Width / 2f)));
                        float baseSkillDamage = _skill.damage + player.GetPlayerStat().Get(StatType.SkillDamage);
                        float finalDamage = baseSkillDamage * damageMultiplier;

                        enemy.TakeDamage(finalDamage, DamageType.Magical, ElementType.Light);

                        Debug.Log($"{enemy.name}에게 중심에서 {distanceFromCenter:F2}m 떨어져 {finalDamage:F1}의 데미지를 입혔습니다 (배율: {damageMultiplier:P0}).");

                        if (hitVFX != null)
                        {
                            Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                        }
                    }
                }
            }

            // --- 여기가 바로 수정된 디버그 라인 코드입니다 ---
            // 1. 중심선의 시작과 끝 지점을 정의합니다.
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + direction * lux3_Range;

            // 2. '벡터 외적'을 사용하여 바라보는 방향(direction)에 대한 완벽한 수직 벡터(right)를 구합니다.
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * (lux3_Width / 2f);

            // 3. 계산된 값으로 평행한 세 개의 선을 그립니다.
            Debug.DrawLine(startPos, endPos, Color.yellow, 2f);               // 중심선
            Debug.DrawLine(startPos - right, endPos - right, Color.cyan, 2f); // 왼쪽 경계선 (중심선에서 -right 만큼)
            Debug.DrawLine(startPos + right, endPos + right, Color.cyan, 2f); // 오른쪽 경계선 (중심선에서 +right 만큼)
                                                                              // ----------------------------------------------------

            if (lux3_VFX_Prefab != null)
            {
                Instantiate(lux3_VFX_Prefab, transform.position, orientation);
            }
        }

        // --- 나머지 스킬 메서드 (구현 대기) ---
        private void Lux2(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Lux4(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris1(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris2(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris3(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris4(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
    }
}