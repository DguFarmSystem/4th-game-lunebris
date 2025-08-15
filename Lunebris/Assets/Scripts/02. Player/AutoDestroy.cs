// AutoDestroy.cs
using UnityEngine;

// 이 스크립트는 ParticleSystem이 있는 오브젝트에만 붙일 수 있도록 강제합니다.
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroy : MonoBehaviour
{
    private ParticleSystem ps;

    public void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void Update()
    {
        // 파티클 시스템의 재생이 완전히 끝났는지 확인합니다.
        if (ps != null && !ps.IsAlive())
        {
            // 재생이 끝났다면, 이 스크립트가 붙어있는 게임 오브젝트를 파괴합니다.
            Destroy(gameObject);
        }
    }
}