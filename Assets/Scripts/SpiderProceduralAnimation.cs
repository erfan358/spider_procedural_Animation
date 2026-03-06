using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SpiderProceduralAnimation : MonoBehaviour
{
    [System.Serializable]
    public class LegIKData
    {
        public string name;
        public Transform ikTarget;
        public Transform raycastTransform;
        [HideInInspector] public Vector3 currentPos;
        [HideInInspector] public Vector3 oldPos;
        [HideInInspector] public Vector3 newPos;
        [HideInInspector] public float lerp = 0;
        [HideInInspector] public bool initialized;
        [HideInInspector] public bool isGrounded = true;
    }

    [SerializeField] private LegIKData[] legIKDataArray;
    [SerializeField] private LayerMask walkableLayers;
    [SerializeField] private float raycastDistance = 1.0f;
    [SerializeField] private Vector3 raycastDirection = Vector3.down;
    [SerializeField] private float stepDistance = 1.0f;
    [SerializeField] private float stepDuration = 1;
    [SerializeField] private float stepHeight = 1;
    [SerializeField] private float feetHeight = .4f;

    private void Start()
    {
        for (int i = 0; i < legIKDataArray.Length; i++)
        {
            legIKDataArray[i].currentPos = legIKDataArray[i].ikTarget.position;
            legIKDataArray[i].oldPos = legIKDataArray[i].ikTarget.position;
            legIKDataArray[i].newPos = legIKDataArray[i].ikTarget.position;
        }
    }

    private void Update()
    {
        for (int i = 0; i < legIKDataArray.Length; i++)
        {
            legIKDataArray[i].ikTarget.position = legIKDataArray[i].currentPos;

            if (FindGroundContactPoint(legIKDataArray[i].raycastTransform.position, out RaycastHit hitData))
            {
                legIKDataArray[i].ikTarget.rotation = Quaternion.LookRotation(-hitData.normal, Vector3.up);

                if (Vector3.Distance(legIKDataArray[i].newPos, hitData.point) > stepDistance && legIKDataArray[i].isGrounded)
                {
                    legIKDataArray[i].lerp = 0;
                    Vector3 dir = hitData.point - legIKDataArray[i].newPos;
                    dir.y = 0;
                    dir.Normalize();
                    legIKDataArray[i].newPos = hitData.point + (hitData.normal * feetHeight) + (dir * (stepDistance * .9f));                  
                    legIKDataArray[i].isGrounded = false;
                }
            }
            if (legIKDataArray[i].lerp < 1)
            {
                Vector3 footPosition = Vector3.Lerp(legIKDataArray[i].oldPos, legIKDataArray[i].newPos, legIKDataArray[i].lerp);
                footPosition.y += Mathf.Sin(legIKDataArray[i].lerp * Mathf.PI) * stepHeight;

                legIKDataArray[i].currentPos = footPosition;
                legIKDataArray[i].lerp += Time.deltaTime / stepDuration;
            }
            else
            {
                legIKDataArray[i].oldPos = legIKDataArray[i].newPos;
                legIKDataArray[i].isGrounded = true;
            }
        }


    }


    private bool FindGroundContactPoint(Vector3 raycastOrigin, out RaycastHit hitData) 
    {
        Ray ray = new Ray(raycastOrigin, raycastDirection);
        return Physics.Raycast(ray, out hitData, raycastDistance, walkableLayers);
    }

    private void OnDrawGizmos()
    {
        if (legIKDataArray != null) {
            foreach (var legData in legIKDataArray)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(legData.raycastTransform.position, 0.1f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(legData.raycastTransform.position, legData.raycastTransform.position + raycastDirection * raycastDistance);
                if (FindGroundContactPoint(legData.raycastTransform.position, out RaycastHit hitData))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(hitData.point, 0.1f);
                }
            }
        }
    }
}
