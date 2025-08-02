using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Enemy;
using Player;

/// <summary>
/// 중간보스가 생성하는 장판 위험 지역
/// 플레이어가 들어가면 지속 데미지를 받음
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Middle_Boss_FloorHazard : MonoBehaviour
{
    [Header("장판 설정")]
    [SerializeField] private float damageInterval = 0.5f; // 데미지 간격
    [SerializeField] private float warningDuration = 1f; // 경고 시간
    [SerializeField] private LayerMask playerLayerMask = -1; // 플레이어 레이어

    [Header("이펙트")]
    [SerializeField] private GameObject warningEffect; // 경고 이펙트
    [SerializeField] private GameObject hazardEffect; // 장판 이펙트
    [SerializeField] private AudioClip warningSound; // 경고 사운드
    [SerializeField] private AudioClip activateSound; // 활성화 사운드

    // 장판 속성
    private float damage;
    private float duration;
    private float radius;
    private Enemy_Middle_Boss ownerBoss;

    // 상태 관리
    private bool isWarning = true; // 경고 단계
    private bool isActive = false; // 활성화 상태
    private float startTime;
    private SphereCollider triggerCollider;
    private AudioSource audioSource;

    // 플레이어 추적
    private List<Transform> playersInRange = new List<Transform>();
    private float lastDamageTime;

    /// <summary>
    /// 장판 초기화
    /// </summary>
    public void Initialize(float hazardDamage, float hazardDuration, float hazardRadius, Enemy_Middle_Boss boss)
    {
        damage = hazardDamage;
        duration = hazardDuration;
        radius = hazardRadius;
        ownerBoss = boss;
        startTime = Time.time;

        SetupComponents();
        StartWarningPhase();
    }

    private void SetupComponents()
    {
        // 트리거 콜라이더 설정
        triggerCollider = gameObject.GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;

        // 오디오 소스 설정
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.maxDistance = 20f;
        audioSource.volume = 0.7f;

        // 위치 조정 (지면에 고정)
        Vector3 pos = transform.position;
        pos.y = 0.1f; // 지면보다 살짝 위에
        transform.position = pos;
    }

    private void StartWarningPhase()
    {
        isWarning = true;
        isActive = false;

        // 경고 이펙트 활성화
        if (warningEffect != null)
        {
            warningEffect.SetActive(true);

            // 경고 이펙트 크기 조정
            warningEffect.transform.localScale = Vector3.one * radius * 2f;
        }

        // 위험 이펙트 비활성화
        if (hazardEffect != null)
        {
            hazardEffect.SetActive(false);
        }

        // 경고 사운드 재생
        if (warningSound != null && audioSource != null)
        {
            audioSource.clip = warningSound;
            audioSource.Play();
        }

        // 경고 시간 후 활성화
        StartCoroutine(ActivateAfterWarning());
    }

    private IEnumerator ActivateAfterWarning()
    {
        yield return new WaitForSeconds(warningDuration);
        ActivateHazard();
    }

    private void ActivateHazard()
    {
        isWarning = false;
        isActive = true;

        // 경고 이펙트 비활성화
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }

        // 위험 이펙트 활성화
        if (hazardEffect != null)
        {
            hazardEffect.SetActive(true);

            // 위험 이펙트 크기 조정
            hazardEffect.transform.localScale = Vector3.one * radius * 2f;
        }

        // 활성화 사운드 재생
        if (activateSound != null && audioSource != null)
        {
            audioSource.clip = activateSound;
            audioSource.Play();
        }

        // 지속시간 후 자동 파괴
        StartCoroutine(DestroyAfterDuration());
    }

    private IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration - warningDuration);
        DestroyHazard();
    }

    private void Update()
    {
        // 활성화 상태에서만 데미지 처리
        if (isActive && playersInRange.Count > 0)
        {
            ProcessDamage();
        }
    }

    private void ProcessDamage()
    {
        if (Time.time - lastDamageTime < damageInterval)
            return;

        lastDamageTime = Time.time;

        // 범위 내 모든 플레이어에게 데미지
        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            Transform player = playersInRange[i];

            if (player == null)
            {
                playersInRange.RemoveAt(i);
                continue;
            }

            // 플레이어가 정말 범위 내에 있는지 다시 확인
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= radius)
            {
                DealDamageToPlayer(player);
            }
            else
            {
                playersInRange.RemoveAt(i);
            }
        }
    }

    private void DealDamageToPlayer(Transform player)
    {
        // 플레이어 컴포넌트 찾기
        Player.Player playerComponent = player.GetComponent<Player.Player>();
        if (playerComponent == null)
        {
            playerComponent = player.GetComponentInChildren<Player.Player>();
        }

        if (playerComponent != null)
        {
            // Player 클래스의 DecreaseHP 메서드 사용
            playerComponent.DecreaseHP(damage);
        }
    }

    #region 트리거 이벤트

    private void OnTriggerEnter(Collider other)
    {
        // 경고 단계에서는 데미지 없음
        if (isWarning) return;

        // 플레이어 레이어 체크
        if (IsPlayer(other.gameObject))
        {
            Transform playerTransform = other.transform;
            if (!playersInRange.Contains(playerTransform))
            {
                playersInRange.Add(playerTransform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            Transform playerTransform = other.transform;
            playersInRange.Remove(playerTransform);
        }
    }

    private bool IsPlayer(GameObject obj)
    {
        // 레이어 마스크로 플레이어 판별
        return ((1 << obj.layer) & playerLayerMask) != 0 ||
               obj.CompareTag("Player") ||
               obj.GetComponent<Player.Player>() != null;
    }

    #endregion

    private void DestroyHazard()
    {
        // 보스에게 장판 파괴 알림
        if (ownerBoss != null)
        {
            ownerBoss.OnFloorHazardDestroyed();
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 혹시 모를 안전장치
        if (ownerBoss != null)
        {
            ownerBoss.OnFloorHazardDestroyed();
        }
    }

    #region 디버그

    private void OnDrawGizmos()
    {
        // 장판 범위 시각화
        Gizmos.color = isWarning ? Color.yellow : (isActive ? Color.red : Color.gray);
        Gizmos.DrawWireSphere(transform.position, radius);

        // 중심점 표시
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        // 선택했을 때 더 자세한 정보 표시
        if (isActive && playersInRange.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform player in playersInRange)
            {
                if (player != null)
                {
                    Gizmos.DrawLine(transform.position, player.position);
                }
            }
        }
    }

    #endregion
}