using UnityEngine;
using Player; // PlayerAttack, PlayerStat 등을 참조하기 위해 필요

public class Clone : MonoBehaviour
{
    private float lifeTime = 5f; // 분신의 지속시간
    private PlayerAttack playerAttack; // 공격 로직을 실행하기 위한 참조
    private PlayerStat playerStat; // 공격 속도 등 스탯을 참조하기 위함

    private float attackCooldown;
    private float attackTimer;

    // 외부(SkillList)에서 분신을 초기화하는 메서드
    public void Initialize(PlayerAttack attackRef, PlayerStat statRef, float duration)
    {
        this.playerAttack = attackRef;
        this.playerStat = statRef;
        this.lifeTime = duration;

        // 플레이어의 공격 속도를 가져와서 공격 주기(cooldown)를 계산
        this.attackCooldown = 1f / playerStat.Get(StatType.AttackSpeed);
        this.attackTimer = 0f; // 생성되자마자 바로 공격할 수 있도록

        // 1. 올블랙 머티리얼 적용
        ApplyBlackMaterial();

        // 2. 설정된 지속시간이 지나면 자동으로 파괴
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (playerAttack == null || playerStat == null) return;

        // 1. 항상 마우스 커서 방향을 바라보도록 함
        transform.rotation = playerAttack.transform.rotation;

        // 2. 공격 타이머 계산
        attackTimer += Time.deltaTime;

        // 3. 공격 주기가 되면 공격 실행if (attackTimer >= attackCooldown)
        {
            // --- [수정된 부분 시작] ---
            // 아래 주석을 풀고, 실제 PlayerAttack 스크립트에 있는 일반 공격 메서드를 호출합니다.
            // 메서드 이름이 'FireNormalAttack'이 아니라면 실제 사용하는 이름으로 바꿔주세요!
            if (playerAttack != null)
            {
                // playerAttack.FireNormalAttack(); // 예시 메서드 이름입니다.
            }
            // --- [수정된 부분 끝] ---

            Debug.Log("[Clone] 분신이 공격을 실행합니다!");
            attackTimer = 0f;
        }
        if (attackTimer >= attackCooldown)
        {
            // --- [수정된 부분 시작] ---
            // 아래 주석을 풀고, 실제 PlayerAttack 스크립트에 있는 일반 공격 메서드를 호출합니다.
            // 메서드 이름이 'FireNormalAttack'이 아니라면 실제 사용하는 이름으로 바꿔주세요!
            if (playerAttack != null)
            {
                // playerAttack.FireNormalAttack(); // 예시 메서드 이름입니다.
            }
            // --- [수정된 부분 끝] ---

            Debug.Log("[Clone] 분신이 공격을 실행합니다!");
            attackTimer = 0f;
        }
    }

    private void ApplyBlackMaterial()
    {
        // 분신 오브젝트와 그 자식들로부터 모든 Renderer를 찾음
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // 새로운 검은색 머티리얼 생성
            Material blackMaterial = new Material(Shader.Find("Standard"));
            blackMaterial.color = Color.black;

            // 찾은 모든 Renderer에 검은색 머티리얼 적용
            foreach (Renderer rend in renderers)
            {
                rend.material = blackMaterial;
            }
        }
    }
}