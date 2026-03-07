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
        public int[] opositeLegsIndex;
        [HideInInspector] public Vector3 currentPos;
        [HideInInspector] public Vector3 oldPos;
        [HideInInspector] public Vector3 newPos;
        [HideInInspector] public float lerp = 0;
        public bool needsUpdate = false;
        [HideInInspector] public bool isGrounded = true;
        [HideInInspector] public Vector3 lastFrameGroundNormal;
        [HideInInspector] public float lastSurfaceChangeTime = 0;
    }

    [SerializeField] private Transform bodyTransform;
    [SerializeField] private LegIKData[] legIKDataArray;
    [SerializeField] private LayerMask walkableLayers;
    [SerializeField] private float raycastDistance = 1.0f;
    [SerializeField] private Vector3 raycastDirection = Vector3.down;
    [SerializeField] private float stepDistance = 1.0f;
    [SerializeField] private float stepDuration = 1;
    [SerializeField] private float stepHeight = 1;
    [SerializeField] private float feetHeight = .4f;
    [SerializeField] private float bodyRotationSpeed = 5;
    [SerializeField] private float heightFromGround = 2;
    private float surfaceChangeCooldown = .2f;
    private float lastTimeSinceSurfaceChange = 0;

    private Vector3 velocity;
    private Vector3 lastFramePos;

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
        velocity = CalculateVelocity();
        Vector3 supportNormal = CalculateSupportNormal();

        for (int i = 0; i < legIKDataArray.Length; i++)
        {
            legIKDataArray[i].ikTarget.position = legIKDataArray[i].currentPos;

            if (FindGroundContactPoint(
                legIKDataArray[i].raycastTransform.position,
                legIKDataArray[i].raycastTransform,
                legIKDataArray[i],
                out RaycastHit hitData))
            {
                legIKDataArray[i].ikTarget.rotation = Quaternion.LookRotation(-hitData.normal, Vector3.up);

                bool isOpositeLegsGrounded = true;
                for (int j = 0; j < legIKDataArray[i].opositeLegsIndex.Length; j++)
                {
                    if (!legIKDataArray[legIKDataArray[i].opositeLegsIndex[j]].isGrounded) 
                    {
                        isOpositeLegsGrounded = false;
                        break;
                    }
                }

                if ((Vector3.Distance(legIKDataArray[i].newPos, hitData.point) > stepDistance && 
                    (legIKDataArray[i].lastSurfaceChangeTime + surfaceChangeCooldown < Time.time) || legIKDataArray[i].needsUpdate) 
                    && legIKDataArray[i].isGrounded && isOpositeLegsGrounded)
                {
                    if (legIKDataArray[i].needsUpdate == true)
                    {
                        legIKDataArray[i].lastSurfaceChangeTime = Time.time;
                        legIKDataArray[i].needsUpdate = false;
                    }
                    legIKDataArray[i].lerp = 0;

                    Vector3 dir = hitData.point - legIKDataArray[i].newPos;
                    if (velocity.sqrMagnitude > .1)
                        dir = velocity;
                    // Project the direction onto the surface plane
                    dir = Vector3.ProjectOnPlane(dir, hitData.normal);
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

        RotateBasedOnLegPlacement(supportNormal);
        AdjustBodyHeight(supportNormal);
    }

    private Vector3 CalculateVelocity()
    {
        Vector3 vel = Vector3.zero;
        Vector3 currentPos = transform.position;
        vel = currentPos - lastFramePos;
        lastFramePos = currentPos;
        return vel;
    }

    private bool FindGroundContactPoint(Vector3 origin, Transform raycastTransform, LegIKData legData, out RaycastHit closestHit)
    {
        closestHit = new RaycastHit();
        bool foundHit = false;
        float closestDistance = float.MaxValue;
        Vector3 closestGroundNormal = Vector3.zero;

        Vector3[] directions =
        {
            -raycastTransform.up,
            raycastTransform.forward,
            -raycastTransform.forward,
            raycastTransform.right,
            -raycastTransform.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Ray ray = new Ray(origin, directions[i]);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, walkableLayers))
            {
                if (hit.distance < closestDistance)
                {
                    closestGroundNormal = hit.normal;
                    closestDistance = hit.distance;
                    closestHit = hit;
                    foundHit = true;
                }
            }
        }

        if (legData.lastFrameGroundNormal != closestGroundNormal)
        {
            legData.needsUpdate = true;
            legData.lastFrameGroundNormal = closestGroundNormal;
        }

        return foundHit;
    }

    private void RotateBasedOnLegPlacement(Vector3 supportNormal) 
    {
        Quaternion targetRotation =
            Quaternion.FromToRotation(transform.up, supportNormal) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * bodyRotationSpeed
        );
    }

    private void AdjustBodyHeight(Vector3 supportNormal)
    {
        Vector3 averageLegPos = Vector3.zero;
        int groundedLegs = 0;

        for (int i = 0; i < legIKDataArray.Length; i++)
        {
            if (!legIKDataArray[i].isGrounded)
                continue;

            averageLegPos += legIKDataArray[i].currentPos;
            groundedLegs++;
        }

        if (groundedLegs == 0)
            return;

        averageLegPos /= groundedLegs;

        // Desired world body position
        Vector3 targetBodyPos = averageLegPos + supportNormal * heightFromGround;

        // Convert to local space relative to spider root
        Vector3 localTarget = transform.InverseTransformPoint(targetBodyPos);

        Vector3 localPos = bodyTransform.localPosition;

        // Only adjust Y
        localPos.y = Mathf.Lerp(
            localPos.y,
            localTarget.y,
            Time.deltaTime * bodyRotationSpeed
        );

        bodyTransform.localPosition = localPos;
    }

    private Vector3 CalculateSupportNormal()
    {
        Vector3 normalSum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < legIKDataArray.Length; i++)
        {
            if (!legIKDataArray[i].isGrounded)
                continue;

            Vector3 legPos = legIKDataArray[i].currentPos;

            for (int j = i + 1; j < legIKDataArray.Length; j++)
            {
                if (!legIKDataArray[j].isGrounded)
                    continue;

                Vector3 legPos2 = legIKDataArray[j].currentPos;

                for (int k = j + 1; k < legIKDataArray.Length; k++)
                {
                    if (!legIKDataArray[k].isGrounded)
                        continue;

                    Vector3 legPos3 = legIKDataArray[k].currentPos;

                    Vector3 normal = Vector3.Cross(legPos2 - legPos, legPos3 - legPos).normalized;

                    normalSum += normal;
                    count++;
                }
            }
        }

        if (count == 0)
            return transform.up;

        return (normalSum / count).normalized;
    }

    private void OnDrawGizmos()
    {
        if (legIKDataArray == null) return;

        foreach (var legData in legIKDataArray)
        {
            if (legData.raycastTransform == null) continue;

            Vector3 origin = legData.raycastTransform.position;

            // Draw leg origin
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(origin, 0.08f);

            Vector3[] directions =
            {
                -legData.raycastTransform.up,
                legData.raycastTransform.forward,
                -legData.raycastTransform.forward,
                legData.raycastTransform.right,
                -legData.raycastTransform.right
            };

            // Draw all rays
            Gizmos.color = Color.red;

            foreach (var dir in directions)
            {
                Gizmos.DrawLine(origin, origin + dir * raycastDistance);
            }

            // Draw the chosen closest hit
            if (FindGroundContactPoint(origin, legData.raycastTransform, legData, out RaycastHit closestHit))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(closestHit.point, 0.12f);

                // draw surface normal
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(closestHit.point, closestHit.point + closestHit.normal * 0.3f);
            }
        }
    }
}
