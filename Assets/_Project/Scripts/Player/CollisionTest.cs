using UnityEngine;

public class CollisionTest : MonoBehaviour
{
    void Start()
    {
        // 이 스크립트가 붙어있는 오브젝트의 이름과 레이어를 게임 시작 즉시 출력합니다.
        Debug.Log(gameObject.name + " 오브젝트가 시작되었습니다. 레이어: " + LayerMask.LayerToName(gameObject.layer), gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // 물리적인 충돌이 '발생했다면' 이 메시지가 출력됩니다.
        Debug.Log(gameObject.name + "이(가) " + collision.gameObject.name + "과(와) 충돌했습니다!", gameObject);
    }
}