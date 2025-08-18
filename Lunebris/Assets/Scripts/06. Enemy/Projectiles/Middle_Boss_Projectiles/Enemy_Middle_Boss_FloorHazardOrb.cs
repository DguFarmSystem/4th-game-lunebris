using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 중간보스가 던지는 장판 생성용 구체
/// 포물선을 그리며 날아가서 착탄 지점에 장판 생성
/// </summary>
public class Enemy_Middle_Boss_FloorHazardOrb : MonoBehaviour
{
    [Header("시각적 효과")]
    [SerializeField] private GameObject impactEffect; // 착탄 이펙트
    [SerializeField] private AudioClip impactSound; // 착탄 사운드
    [SerializeField] private TrailRenderer orbTrail; // 구체 궤적

    // 초기화 데이터
    private Enemy_Middle_Boss parentBoss;
    private Vector3 targetPosition;
    private float speed;
    private float arcHeight;
    private GameObject floorHazardPrefab;
    private float floorHazardDamage;
    private float floorHazardDuration;
    private float floorHazardRadius;

    // 이동 관련
    private Vector3 startPosition;
    private float journeyLength;
    private float journeyTime;
    private float elapsedTime = 0f;
    private bool hasLanded = false;

    #region 초기화

    /// <summary>
    /// 구체 초기화
    /// </summary>
    public void Initialize(Enemy_Middle_Boss boss, Vector3 target, float orbSpeed, float height,
                          GameObject hazardPrefab, float damage, float duration, float radius)
    {
        parentBoss = boss;
        targetPosition = target;
        speed = orbSpeed;
        arcHeight = height;
        floorHazardPrefab = hazardPrefab;
        floorHazardDamage = damage;
        floorHazardDuration = duration;
        floorHazardRadius = radius;

        startPosition = transform.position;
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyTime = journeyLength / speed;

        // Trail Renderer 활성화
        if (orbTrail != null)
        {
            orbTrail.enabled = true;
            orbTrail.Clear();
        }

        // 일정 시간 후 강제 착탄 (안전장치)
        Destroy(gameObject, journeyTime + 2f);

        Debug.Log($"장판 구체 발사! 목표: {targetPosition}, 비행시간: {journeyTime:F1}초");
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        if (hasLanded) return;

        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / journeyTime;

        if (progress >= 1f)
        {
            // 착탄!
            LandAtTarget();
        }
        else
        {
            // 포물선 이동
            UpdateParabolicMovement(progress);
        }
    }

    #endregion

    #region 이동 시스템

    /// <summary>
    /// 포물선 이동 업데이트
    /// </summary>
    private void UpdateParabolicMovement(float progress)
    {
        // 수평 이동 (선형 보간)
        Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, progress);

        // 수직 이동 (포물선 - 중간에 최고점)
        float heightOffset = arcHeight * 4f * progress * (1f - progress);

        // 최종 위치 계산
        Vector3 currentPosition = horizontalPosition + Vector3.up * heightOffset;
        transform.position = currentPosition;

        // 구체가 이동 방향을 바라보도록 회전
        Vector3 lastPosition = transform.position;
        Vector3 nextPosition = CalculateNextPosition(progress + 0.01f);
        Vector3 direction = (nextPosition - lastPosition).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// 다음 프레임 위치 계산 (회전용)
    /// </summary>
    private Vector3 CalculateNextPosition(float progress)
    {
        progress = Mathf.Clamp01(progress);
        Vector3 horizontalPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        float heightOffset = arcHeight * 4f * progress * (1f - progress);
        return horizontalPosition + Vector3.up * heightOffset;
    }

    #endregion

    #region 착탄 처리

    /// <summary>
    /// 목표 지점 착탄
    /// </summary>
    private void LandAtTarget()
    {
        if (hasLanded) return;

        hasLanded = true;

        // 정확한 착탄 위치로 이동
        transform.position = targetPosition;

        // 착탄 이펙트
        PlayImpactEffects();

        // 보스에게 착탄 알림 (장판 생성)
        if (parentBoss != null)
        {
            parentBoss.OnOrbLanded(targetPosition, floorHazardPrefab, floorHazardDamage, floorHazardDuration, floorHazardRadius);
        }

        Debug.Log($"구체 착탄! 위치: {targetPosition}");

        // 구체 제거 (약간의 딜레이 후)
        StartCoroutine(DestroyAfterImpact());
    }

    /// <summary>
    /// 착탄 이펙트 재생
    /// </summary>
    private void PlayImpactEffects()
    {
        // 착탄 이펙트 생성
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, targetPosition, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // 착탄 사운드
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, targetPosition);
        }

        // Trail Renderer 비활성화
        if (orbTrail != null)
        {
            orbTrail.enabled = false;
        }
    }

    /// <summary>
    /// 착탄 후 구체 제거
    /// </summary>
    private IEnumerator DestroyAfterImpact()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    #endregion

    #region 충돌 처리

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌 시 즉시 착탄
        if (other.CompareTag("Player") && !hasLanded)
        {
            Debug.Log("구체가 플레이어와 충돌! 즉시 착탄");
            LandAtTarget();
        }
        // 벽이나 장애물과 충돌 시에도 착탄
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Debug.Log("구체가 장애물과 충돌! 즉시 착탄");
            LandAtTarget();
        }
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && !hasLanded)
        {
            // 목표 지점 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);

            // 예상 궤도 그리기
            Gizmos.color = Color.yellow;
            Vector3 lastPos = startPosition;

            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, t);
                float height = arcHeight * 4f * t * (1f - t);
                Vector3 currentPos = horizontalPos + Vector3.up * height;

                Gizmos.DrawLine(lastPos, currentPos);
                lastPos = currentPos;
            }
        }
    }

    #endregion
}