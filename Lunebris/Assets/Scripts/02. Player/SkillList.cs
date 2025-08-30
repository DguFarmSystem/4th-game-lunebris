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
        [SerializeField] private GameObject hitVFX; // Lux1, Lux3 등 범용 피격 이펙트

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
        [SerializeField] private GameObject tenebris1_VFX_Prefab; //  vfx
        [SerializeField] private float tenebris1_Radius = 7f; // 흡혈 범위
        [SerializeField] private float tenebris1_Duration = 2f; // 총 지속 시간
        [SerializeField] private int tenebris1_MaxTargets = 3; // 최대 대상 수
        [SerializeField] private float tenebris1_TickRate = 0.2f; // 데미지 및 흡혈

        [Header("Tenebris2 Skill Settings")]
        [SerializeField] private GameObject tenebris2_VFX_Prefab;  // 블랙홀 시각효과 프리팹
        [SerializeField] private float tenebris2_Duration = 5f;      // 지속 시간
        [SerializeField] private float tenebris2_Radius = 8f;        // 효과 반경
        [SerializeField] private float tenebris2_PullForce = 50f;    // 끌어당기는 힘
        [SerializeField] private float tenebris2_DamagePerTick = 1f;     // 틱당 데미지
        [SerializeField] private float tenebris2_TickRate = 0.5f;

        [Header("Tenebris3 Skill Settings")]
        [SerializeField] private GameObject tenebris3_ClonePrefab; // 위에서 만든 분신 프리팹
        [SerializeField] private float tenebris3_Duration = 5f;      // 분신 지속 시간
        private GameObject activeClone; // 현재 활성화된 분신을 추적

        [Header("Tenebris4 Skill Settings")]
        [SerializeField] private GameObject tenebris4_CastVFX_Prefab;      // 스킬 '시전' 시 사용될 화면 전체 파동 이펙트
        [SerializeField] private GameObject tenebris4_HitVFX_Prefab;       // 각 적에게 '피격' 시 나타날 이펙트
        [SerializeField] private float tenebris4_HealthCostPercent = 0.2f; // 현재 체력의 20% 소모
        [SerializeField] private float tenebris4_DamageMultiplier = 1.5f;  // 최대 체력의 150% 만큼 피해
        [SerializeField] private float tenebris4_SafetyThreshold = 0.25f; // 체력이 25% 초과일 때만 사용 가능

        private void Awake()
        {
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
                // [최종 수정] GetComponentInParent를 사용하여 부모 오브젝트의 스크립트까지 탐색
                Enemy_Base enemy = hitCollider.GetComponentInParent<Enemy_Base>();
                if (enemy != null && !enemy.IsDead())
                {
                    // 데미지 공식 수정
                    float totalSkillDamage = _skill.damage + player.GetSkillDamage();
                    enemy.TakeDamage(totalSkillDamage, DamageType.Magical, ElementType.Lux);

                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                    }
                    Debug.Log($"[[LUX1]]{enemy.name}에게 {totalSkillDamage}의 빛 속성 마법 데미지를 입혔습니다.");
                }
            }
        }
        private void Lux2(Skill _skill)
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

            Vector3 startPoint = player.transform.position;
            Vector3 rawDirection = playerAttack.GetLookDirection();
            if (rawDirection == Vector3.zero) return;

            rawDirection.y = 0;
            Vector3 direction = rawDirection.normalized;
            Vector3 boxHalfExtents = new Vector3(lux3_Width / 2f, 2f, 0.1f);
            Quaternion orientation = Quaternion.LookRotation(direction);
            RaycastHit[] hits = Physics.BoxCastAll(startPoint, boxHalfExtents, direction, orientation, lux3_Range);

            foreach (var hit in hits)
            {
                // [최종 수정] GetComponentInParent를 사용하여 부모 오브젝트의 스크립트까지 탐색
                Enemy_Base enemy = hit.collider.GetComponentInParent<Enemy_Base>();
                if (enemy != null && !enemy.IsDead())
                {
                    Vector3 vectorToEnemy = enemy.transform.position - startPoint;
                    Vector3 projectedVector = Vector3.Project(vectorToEnemy, direction);
                    Vector3 closestPointOnLine = startPoint + projectedVector;
                    float distanceFromCenter = Vector3.Distance(enemy.transform.position, closestPointOnLine);

                    float damageMultiplier = Mathf.Lerp(1f, lux3_MinDamageMultiplier, Mathf.Clamp01(distanceFromCenter / (lux3_Width / 2f)));
                    float baseSkillDamage = _skill.damage + player.GetSkillDamage();
                    float finalDamage = baseSkillDamage * damageMultiplier;
                    enemy.TakeDamage(finalDamage, DamageType.Magical, ElementType.Lux);

                    if (hitVFX != null)
                    {
                        Instantiate(hitVFX, enemy.transform.position, Quaternion.identity);
                    }
                }
            }

            if (lux3_VFX_Prefab != null)
            {
                Vector3 endPoint = startPoint + direction * lux3_Range;
                StartCoroutine(Lux3_LineVFXCoroutine(startPoint, endPoint));
            }
        }

        private IEnumerator Lux3_LineVFXCoroutine(Vector3 startPoint, Vector3 endPoint)
        {
            GameObject vfxInstance = Instantiate(lux3_VFX_Prefab, startPoint, Quaternion.identity);
            LineRenderer lineRenderer = vfxInstance.GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                lineRenderer.startWidth = lux3_Width;
                lineRenderer.endWidth = lux3_Width;

                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, endPoint);
            }

            yield return new WaitForSeconds(0.2f);

            if (vfxInstance != null)
            {
                Destroy(vfxInstance);
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
                Debug.Log("소환수을 소환합니다!");
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
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, tenebris1_Radius);
            List<Enemy_Base> validEnemies = new List<Enemy_Base>();
            foreach (var hitCollider in hitColliders)
            {
                // [최종 수정] GetComponentInParent를 사용하여 부모 오브젝트의 스크립트까지 탐색
                Enemy_Base enemy = hitCollider.GetComponentInParent<Enemy_Base>();
                if (enemy != null && !enemy.IsDead())
                {
                    validEnemies.Add(enemy);
                }
            }
            List<Enemy_Base> finalTargets = validEnemies
              .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
              .Take(tenebris1_MaxTargets)
              .ToList();
            if (finalTargets.Count > 0)
            {
                StartCoroutine(Tenebris1_DrainCoroutine(_skill, finalTargets));
            }
            else
            {
                Debug.Log("[Tenebris1] 주변에 흡혈할 대상이 없습니다.");
            }
        }
        private IEnumerator Tenebris1_DrainCoroutine(Skill _skill, List<Enemy_Base> targets)
        {
            Dictionary<Enemy_Base, LineRenderer> lineRenderers = new Dictionary<Enemy_Base, LineRenderer>();
            if (tenebris1_VFX_Prefab != null)
            {
                foreach (var enemy in targets)
                {
                    GameObject vfxInstance = Instantiate(tenebris1_VFX_Prefab, transform.position, Quaternion.identity);
                    LineRenderer lr = vfxInstance.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        lr.useWorldSpace = true;
                        lineRenderers.Add(enemy, lr);
                    }
                    else
                    {
                        Debug.LogWarning("Tenebris1 VFX Prefab에 LineRenderer 컴포넌트가 없습니다!");
                        Destroy(vfxInstance);
                    }
                }
            }
            Debug.Log($"[Tenebris1] {targets.Count}개의 대상을 향해 흡혈을 시작합니다.");
            float elapsedTime = 0f;
            float tickTimer = 0f;
            while (elapsedTime < tenebris1_Duration && targets.Count > 0)
            {
                elapsedTime += Time.deltaTime;
                tickTimer += Time.deltaTime;
                foreach (var pair in lineRenderers)
                {
                    Enemy_Base enemy = pair.Key;
                    LineRenderer lr = pair.Value;
                    if (enemy != null && lr != null)
                    {
                        lr.SetPosition(0, player.transform.position);
                        lr.SetPosition(1, enemy.transform.position);
                    }
                }
                if (tickTimer >= tenebris1_TickRate)
                {
                    float totalHealAmount = 0f;
                    for (int i = targets.Count - 1; i >= 0; i--)
                    {
                        Enemy_Base enemy = targets[i];
                        if (enemy == null || enemy.IsDead() || Vector3.Distance(transform.position, enemy.transform.position) > tenebris1_Radius)
                        {
                            if (lineRenderers.ContainsKey(enemy))
                            {
                                Destroy(lineRenderers[enemy].gameObject);
                                lineRenderers.Remove(enemy);
                            }
                            targets.RemoveAt(i);
                            continue;
                        }

                        // 데미지 공식 수정
                        float totalDamage = _skill.damage + player.GetSkillDamage();
                        float damagePerTick = totalDamage / (tenebris1_Duration / tenebris1_TickRate);
                        enemy.TakeDamage(damagePerTick, DamageType.Magical, ElementType.Tenebris);
                        totalHealAmount += damagePerTick;
                    }
                    if (totalHealAmount > 0)
                    {
                        player.IncreaseHP(totalHealAmount);
                    }
                    tickTimer = 0f;
                }
                yield return null;
            }
            foreach (var lr in lineRenderers.Values)
            {
                if (lr != null)
                {
                    Destroy(lr.gameObject);
                }
            }
            lineRenderers.Clear();
            Debug.Log("[Tenebris1] 흡혈이 종료되었습니다.");
        }

        private void Tenebris2(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);
            Plane groundPlane = new Plane(Vector3.up, player.transform.position);
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 spawnPosition = ray.GetPoint(distance);
                StartCoroutine(Tenebris2_BlackHoleCoroutine(_skill, spawnPosition));
            }
            else
            {
                Debug.LogWarning("[Tenebris2] 마우스 위치를 바닥 평면에 투영할 수 없습니다.");
            }
        }

        private IEnumerator Tenebris2_BlackHoleCoroutine(Skill _skill, Vector3 center)
        {
            Debug.Log($"[Tenebris2] {center} 위치에 블랙홀 생성.");
            GameObject vfxInstance = null;
            if (tenebris2_VFX_Prefab != null)
            {
                vfxInstance = Instantiate(tenebris2_VFX_Prefab, center, Quaternion.identity);
                vfxInstance.transform.localScale = Vector3.one * tenebris2_Radius;
            }
            List<Enemy_Base> affectedEnemies = new List<Enemy_Base>();
            float elapsedTime = 0f;
            float tickTimer = 0f;
            while (elapsedTime < tenebris2_Duration)
            {
                elapsedTime += Time.deltaTime;
                tickTimer += Time.deltaTime;
                if (tickTimer >= tenebris2_TickRate)
                {
                    Collider[] colliders = Physics.OverlapSphere(center, tenebris2_Radius);
                    foreach (var col in colliders)
                    {
                        // [최종 수정] GetComponentInParent를 사용하여 부모 오브젝트의 스크립트까지 탐색
                        Enemy_Base enemy = col.GetComponentInParent<Enemy_Base>();
                        if (enemy != null && !enemy.IsDead())
                        {
                            Rigidbody enemyRb = col.GetComponent<Rigidbody>();
                            if (enemyRb != null)
                            {
                                Vector3 direction = (center - col.transform.position).normalized;
                                enemyRb.AddForce(direction * tenebris2_PullForce);
                            }

                            // 데미지 공식 수정
                            float totalSkillDamage = tenebris2_DamagePerTick + player.GetSkillDamage();
                            enemy.TakeDamage(totalSkillDamage, DamageType.Magical, ElementType.Tenebris);
                        }
                    }
                    tickTimer = 0f;
                }
                yield return null;
            }
            foreach (var enemy in affectedEnemies)
            {
            }
            if (vfxInstance != null)
            {
                Destroy(vfxInstance);
            }
            Debug.Log("[Tenebris2] 블랙홀 소멸.");
        }

        private void Tenebris3(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);
            if (activeClone != null)
            {
                Debug.Log("[Tenebris3] 분신과 위치를 교대합니다!");
                Vector3 playerPosition = player.transform.position;
                Vector3 clonePosition = activeClone.transform.position;
                PlayerMove playerMoveScript = player.GetComponent<PlayerMove>();
                if (playerMoveScript != null)
                {
                    playerMoveScript.enabled = false;
                }
                player.transform.position = clonePosition;
                activeClone.transform.position = playerPosition;
                if (playerMoveScript != null)
                {
                    playerMoveScript.enabled = true;
                }
            }
            else
            {
                Debug.Log("[Tenebris3] 분신을 소환합니다!");
                if (tenebris3_ClonePrefab != null)
                {
                    activeClone = Instantiate(tenebris3_ClonePrefab, player.transform.position, player.transform.rotation);
                    Clone cloneScript = activeClone.GetComponent<Clone>();
                    if (cloneScript != null)
                    {
                        cloneScript.Initialize(playerAttack, player.GetPlayerStat(), tenebris3_Duration);
                    }
                    StartCoroutine(TrackCloneLifetime());
                }
            }
        }
        private IEnumerator TrackCloneLifetime()
        {
            yield return new WaitUntil(() => activeClone == null);
            Debug.Log("[Tenebris3] 분신이 소멸한 것을 감지했습니다.");
        }

        private void Tenebris4(Skill _skill)
        {
            Debug.Log("Skill Name : " + _skill.skillName);
            float currentHealth = player.GetCurrentHP();
            float maxHealth = player.GetMaxHp();

            if (currentHealth > maxHealth * tenebris4_SafetyThreshold)
            {
                float healthToConsume = currentHealth * tenebris4_HealthCostPercent;
                player.ConsumeHP(healthToConsume);
                Debug.Log($"[Tenebris4] 스킬 발동, 체력 {healthToConsume:F0} 소모.");
                if (tenebris4_CastVFX_Prefab != null)
                {
                    Instantiate(tenebris4_CastVFX_Prefab, player.transform.position, Quaternion.identity);
                }

                // [참고] 이 스킬은 이미 Enemy_Base 타입으로 모든 적을 찾고 있으므로 수정할 필요가 없습니다.
                Enemy_Base[] allEnemies = FindObjectsOfType<Enemy_Base>();

                // 데미지 공식 수정
                float damage = (maxHealth * tenebris4_DamageMultiplier) + player.GetSkillDamage();

                foreach (Enemy_Base enemy in allEnemies)
                {
                    if (enemy != null && !enemy.IsDead())
                    {
                        enemy.TakeDamage(damage, DamageType.Magical, ElementType.Tenebris);
                        if (tenebris4_HitVFX_Prefab != null)
                        {
                            Instantiate(tenebris4_HitVFX_Prefab, enemy.transform.position, Quaternion.identity);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[Tenebris4] 체력이 부족하여 스킬을 사용할 수 없습니다.");
            }
        }
    }
}