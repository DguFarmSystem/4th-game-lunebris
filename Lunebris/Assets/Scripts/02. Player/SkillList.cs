// System
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Lux2 Skill Settings")]
        [SerializeField] private float lux2_ShieldAmount = 100f; // 부여할 쉴드 양
        [SerializeField] private GameObject lux2_ShieldVFX_Prefab; // 쉴드 시각 효과

        [Header("Lux3 Skill Settings")]
        [SerializeField] private float lux3_Range = 20f; // 레이저 사거리
        [SerializeField] private float lux3_Width = 3f;  // 레이저 폭
        [SerializeField] private float lux3_MinDamageMultiplier = 0.5f; // 가장자리 최소 데미지 배율 (50%)
        [SerializeField] private GameObject lux3_VFX_Prefab; // 레이저 시각 효과 프리팹

        [Header("Lux4 Skill Settings")]
        [SerializeField] private GameObject summonPrefab;
        [SerializeField] private Transform summonPoint;

        [Header("Tenebris1 Skill Settings")]
        [SerializeField] private float tenebris1_Radius = 7f; // 흡혈 범위
        [SerializeField] private float tenebris1_Duration = 2f; // 총 지속 시간
        [SerializeField] private int tenebris1_MaxTargets = 3; // 최대 대상 수
        [SerializeField] private float tenebris1_TickRate = 0.2f; // 데미지 및 흡혈 주기 (0.2초마다)



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
                        enemy.TakeDamage(totalSkillDamage, DamageType.Magical, ElementType.Lux);

                        if (hitVFX != null)
                        {
                            Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                        }
                        Debug.Log($"[[LUX1]]{hitCollider.name}에게 {totalSkillDamage}의 빛 속성 마법 데미지를 입혔습니다.");
                    }
                }
            }
        }
        private void Lux2(Skill _skill) //쉴드 로직은 player.cs에 있음.
        {
            Debug.Log("Skill Name : " + _skill.skillName);

            if (player != null)
            {
                player.AddShield(lux2_ShieldAmount, lux2_ShieldVFX_Prefab);
            }
        }

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

                        enemy.TakeDamage(finalDamage, DamageType.Magical, ElementType.Lux);

                        Debug.Log($"{enemy.name}에게 중심에서 {distanceFromCenter:F2}m 떨어져 {finalDamage:F1}의 데미지를 입혔습니다 (배율: {damageMultiplier:P0}).");

                        if (hitVFX != null)
                        {
                            Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                        }
                    }
                }
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + direction * lux3_Range;

            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * (lux3_Width / 2f);

            Debug.DrawLine(startPos, endPos, Color.yellow, 2f);               // 중심선
            Debug.DrawLine(startPos - right, endPos - right, Color.cyan, 2f); // 왼쪽 경계
            Debug.DrawLine(startPos + right, endPos + right, Color.cyan, 2f); // 오른쪽 경계
                                                                            

            if (lux3_VFX_Prefab != null)
            {
                Instantiate(lux3_VFX_Prefab, transform.position, orientation);
            }
        }
        private void Lux4(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);

            if (summonPrefab != null)
            {
                Vector3 direction = playerAttack.GetLookDirection();
                if (direction == Vector3.zero)
                {
                    direction = player.transform.forward;
                }

                Vector3 spawnPosition = player.transform.position + direction.normalized * 3f;

                Debug.Log("빛의 정령을 소환합니다!");
                Instantiate(summonPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogError("Lux4: 소환수 프리팹이 할당되지 않았습니다!");
            }
        }
        private void Tenebris1(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);

            // 주변의 모든 적을 탐지
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, tenebris1_Radius);

            List<Enemy_Base> validEnemies = new List<Enemy_Base>();
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Enemy"))
                {
                    Enemy_Base enemy = hitCollider.GetComponent<Enemy_Base>();
                    if (enemy != null && !enemy.IsDead())
                    {
                        validEnemies.Add(enemy);
                    }
                }
            }

            // 플레이어와 가장 가까운 적 순서로 정렬 후, 최대 대상 수만큼 선택
            List<Enemy_Base> finalTargets = validEnemies
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .Take(tenebris1_MaxTargets)
                .ToList();

            if (finalTargets.Count > 0)
            {
                // 실제 흡혈 로직은 코루틴에 위임
                StartCoroutine(Tenebris1_DrainCoroutine(_skill, finalTargets));
            }
            else
            {
                Debug.Log("[Tenebris1] 주변에 흡혈할 대상이 없습니다.");
            }
        }

        private IEnumerator Tenebris1_DrainCoroutine(Skill _skill, List<Enemy_Base> targets)
        {
            float elapsedTime = 0f;
            float tickTimer = 0f;

            Debug.Log($"[Tenebris1] {targets.Count}개의 대상을 향해 흡혈을 시작합니다.");

            while (elapsedTime < tenebris1_Duration && targets.Count > 0)
            {
                elapsedTime += Time.deltaTime;
                tickTimer += Time.deltaTime;

                // 틱 주기(tickRate)마다 데미지 및 흡혈 처리
                if (tickTimer >= tenebris1_TickRate)
                {
                    float totalHealAmount = 0f;

                    // 리스트를 역순으로 순회하여 중간에 제거해도 안전하도록 처리
                    for (int i = targets.Count - 1; i >= 0; i--)
                    {
                        Enemy_Base enemy = targets[i];

                        // 조건 체크: 적이 죽었거나 범위를 벗어났는지 확인
                        if (enemy == null || enemy.IsDead() || Vector3.Distance(transform.position, enemy.transform.position) > tenebris1_Radius)
                        {
                            targets.RemoveAt(i); // 대상 목록에서 제거
                            continue;
                        }

                        // 데미지 및 흡혈량 계산 (총 스킬 데미지를 틱 횟수로 나눔)
                        float damagePerTick = (_skill.damage + player.GetPlayerStat().Get(StatType.SkillDamage)) / (tenebris1_Duration / tenebris1_TickRate);
                        enemy.TakeDamage(damagePerTick, DamageType.Magical, ElementType.Tenebris); // Enemy_Base에 맞게 수정 필요

                        totalHealAmount += damagePerTick; // 우선 데미지만큼 흡혈하도록 설정 (흡혈 계수 추가 가능)

                        // 시각 효과: 디버그 라인으로 빨대 그리기
                        Debug.DrawLine(player.transform.position, enemy.transform.position, Color.magenta, tenebris1_TickRate);
                    }

                    if (totalHealAmount > 0)
                    {
                        player.IncreaseHP(totalHealAmount); // Player.cs의 체력 회복 메서드 호출
                    }

                    tickTimer = 0f; // 틱 타이머 초기화
                }

                yield return null; // 다음 프레임까지 대기
            }

            Debug.Log("[Tenebris1] 흡혈이 종료되었습니다.");
        }
        private void Tenebris2(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris3(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
        private void Tenebris4(Skill _skill) { Debug.Log("Skill Name : " + _skill.skillName); }
    }
}