using UnityEngine;

/// <summary>
/// 임시 보스 따라다니기 컴포넌트 - 어둠 장판 등에서 사용
/// </summary>
public class Enemy_Temp_Follow_Boss : MonoBehaviour
{
    [Header("따라다니기 설정")]
    public Transform target;
    public float followSpeed = 2f;
    public float yOffset = 0.05f; // 지면에 붙이기 위한 Y 오프셋

    private void Update()
    {
        if (target != null)
        {
            Vector3 targetPos = target.position;
            targetPos.y = yOffset; // 지면에 붙이기

            transform.position = Vector3.Lerp(transform.position, targetPos,
                followSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 따라다닐 대상과 속도 설정
    /// </summary>
    public void Initialize(Transform targetTransform, float speed = 2f, float yPos = 0.05f)
    {
        target = targetTransform;
        followSpeed = speed;
        yOffset = yPos;
    }
}