using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void LateUpdate()
    {
        transform.position += transform.right * moveSpeed * Time.deltaTime;
    }
}
