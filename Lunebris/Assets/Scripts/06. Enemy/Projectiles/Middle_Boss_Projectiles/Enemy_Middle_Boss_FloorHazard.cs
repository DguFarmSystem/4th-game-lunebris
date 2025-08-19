using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy;

/// <summary>
/// 중간보스가 생성하는 장판 (바닥 위험 지역)
/// 일정 시간동안 지속되며 플레이어에게 지속 데미지를 가함
/// </summary>
public class Enemy_Middle_Boss_FloorHazard : MonoBehaviour
{
    [Header("장판 설정")]
    [SerializeField] private float tickInterval = 0.5f; // 데미지 간격 (초)
    [SerializeField] private GameObject warningEffect; // 경고 이펙트
    [SerializeField] private GameObject activeEffect; // 활성화 이펙트
    [SerializeField] private AudioClip activationSound; // 활성화 사운드
    [SerializeField] private AudioClip damageSound; // 데미지 사운드

    // 장판 데이터
    private float damage;
    private float duration;
    private float radius;
    private Enemy_Middle_Boss parentBoss;

    // 상태 관리
    private bool isActive = false;
    private float activationTime;
    private float nextDamageTime;
    private HashSet<Collider> playersInRange = new HashSet<Collider>();

    // 컴포넌트 참조
    private SphereCollider hazardCollider;

    #region 초기화

    /// <summary>
    /// 장판 초기화
    /// </summary>
    public void Initialize(float hazardDamage, float hazardDuration, float hazardRadius, Enemy_Middle_Boss boss)
    {
        damage = hazardDamage;
        duration = hazardDuration;
        radius = hazardRadius;
        parentBoss = boss;

        // 콜라이더 설정
        SetupCollider();

        // 초기화 후 짧은 딜레이 후 활성화
        StartCoroutine(ActivateAfterDelay(0.5f));

        Debug.Log($"장판 생성! 데미지: {damage}, 지속시간: {duration}초, 반지름: {radius}m");
    }

    /// <summary>
    /// 콜라이더 설정
    /// </summary>
    private void SetupCollider()
    {
        // SphereCollider 가져오거나 추가
        hazardCollider = GetComponent<SphereCollider>();
        if (hazardCollider == null)
        {
            hazardCollider = gameObject.AddComponent<SphereCollider>();
        }

        hazardCollider.isTrigger = true;
        hazardCollider.radius = radius;

        Debug.Log($"장판 콜라이더 설정 완료. 반지름: {radius}");
    }

    #endregion

    #region 활성화 시스템

    /// <summary>
    /// 딜레이 후 장판 활성화
    /// </summary>
    private IEnumerator ActivateAfterDelay(float delay)
    {
        // 경고 이펙트 표시
        ShowWarningEffect();

        yield return new WaitForSeconds(delay);

        // 장판 활성화
        ActivateHazard();
    }

    /// <summary>
    /// 경고 이펙트 표시
    /// </summary>
    private void ShowWarningEffect()
    {
        if (warningEffect != null)
        {
            GameObject warning = Instantiate(warningEffect, transform.position, transform.rotation, transform);
            // 경고 이펙트는 활성화되면 제거
            Destroy(warning, 0.5f);
        }

        Debug.Log("장판 경고 이펙트 표시");
    }

    /// <summary>
    /// 장판 활성화
    /// </summary>
    private void ActivateHazard()
    {
        isActive = true;
        activationTime = Time.time;
        nextDamageTime = Time.time + tickInterval;

        // 활성화 이펙트 표시
        if (activeEffect != null)
        {
            GameObject effect = Instantiate(activeEffect, transform.position, transform.rotation, transform);
            Destroy(effect, duration); // 지속시간과 함께 제거
        }

        // 활성화 사운드
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }

        // 지속시간 후 자동 제거
        StartCoroutine(DestroyAfterDuration());

        Debug.Log($"장판 활성화! 위치: {transform.position}");
    }

    /// <summary>
    /// 지속시간 후 장판 제거
    /// </summary>
    private IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);

        // 보스에게 장판 제거 알림
        if (parentBoss != null)
        {
            parentBoss.OnFloorHazardDestroyed();
        }

        Debug.Log("장판 지속시간 종료. 제거합니다.");
        Destroy(gameObject);
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        if (!isActive) return;

        // 정기적으로 범위 내 플레이어에게 데미지
        if (Time.time >= nextDamageTime)
        {
            DealDamageToPlayersInRange();
            nextDamageTime = Time.time + tickInterval;
        }
    }

    #endregion

    #region 충돌 및 데미지 처리

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other);
            Debug.Log($"플레이어가 장판 범위에 진입: {other.name}");

            // 즉시 첫 데미지 (활성화된 상태라면)
            if (isActive)
            {
                DealDamageToPlayer(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Remove(other);
            Debug.Log($"플레이어가 장판 범위에서 벗어남: {other.name}");
        }
    }

    /// <summary>
    /// 범위 내 모든 플레이어에게 데미지
    /// </summary>
    private void DealDamageToPlayersInRange()
    {
        // null 체크를 위해 리스트 복사
        List<Collider> playersToRemove = new List<Collider>();

        foreach (Collider player in playersInRange)
        {
            if (player == null)
            {
                playersToRemove.Add(player);
                continue;
            }

            DealDamageToPlayer(player);
        }

        // null인 플레이어들 제거
        foreach (Collider player in playersToRemove)
        {
            playersInRange.Remove(player);
        }
    }

    /// <summary>
    /// 개별 플레이어에게 데미지
    /// </summary>
    private void DealDamageToPlayer(Collider player)
    {
        // 플레이어의 Player 컴포넌트를 찾아서 데미지 적용
        var playerScript = player.GetComponent<Player.Player>();
        if (playerScript != null)
        {
            playerScript.DecreaseHP(damage);

            // 데미지 사운드
            if (damageSound != null)
            {
                AudioSource.PlayClipAtPoint(damageSound, player.transform.position, 0.5f);
            }

            Debug.Log($"장판 데미지 적용: {player.name}에게 {damage} 데미지");
        }
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        // 장판 범위 표시
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 중심점 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 장판이 활성화되었는지 확인
    /// </summary>
    public bool IsActive => isActive;

    /// <summary>
    /// 남은 지속시간 반환
    /// </summary>
    public float GetRemainingDuration()
    {
        if (!isActive) return duration;
        return Mathf.Max(0f, duration - (Time.time - activationTime));
    }

    /// <summary>
    /// 강제로 장판 제거 (외부에서 호출 가능)
    /// </summary>
    public void ForceDestroy()
    {
        StopAllCoroutines();

        if (parentBoss != null)
        {
            parentBoss.OnFloorHazardDestroyed();
        }

        Destroy(gameObject);
    }

    #endregion
}