using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Range(1f, 20f)] public float smoothSpeed = 6f;

    void LateUpdate()
    {
        if (PlayerToken.Instance == null) return;
        Vector3 target = PlayerToken.Instance.transform.position;
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target,
            smoothSpeed * Time.deltaTime);
    }
}
