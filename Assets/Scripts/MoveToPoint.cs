using UnityEngine;
using System.Collections;
using TriTools;

public class MoveToPoint : MonoBehaviour {

    public Vector3 pointToReach;

    private Vector3 pointMinusY;

    public float speed;

    public float movementAccuracy;

    public float fowardRayLength;

    public bool shouldMoveToPoint;

    public bool hasReachedPoint;

	public void MoveToMethod()
    {
        if(shouldMoveToPoint)
        {
            if (!hasReachedPoint)
            {
                TriToolHub.SmoothLookAt(gameObject, pointToReach, transform.up, true, 7);
                pointMinusY = new Vector3(pointToReach.x, transform.position.y, pointToReach.z);
                transform.position = Vector3.MoveTowards(transform.position, pointMinusY, speed * Time.deltaTime);
               
            }
            if (TriToolHub.FastApproximately(Vector3.Distance(transform.position, pointMinusY), 0, movementAccuracy))
            {
                hasReachedPoint = true;
            }
            //else hasReachedPoint = false;
            RaycastHit rayInfo;
            if (Physics.SphereCast(transform.position, 5, pointMinusY, out rayInfo, fowardRayLength))
            {
                if (!rayInfo.collider.tag.Contains("Ground"))
                {
                    if (gameObject.tag.Contains("Player"))
                        if (!rayInfo.collider.tag.Contains("Player"))
                            hasReachedPoint = true;
                    //if (gameObject.tag.Contains("Enemy"))
                    //if (!rayInfo.collider.tag.Contains("Enemy"))
                    hasReachedPoint = true;
                }
            }
            else hasReachedPoint = false;
        }
    }
 
}
