using UnityEngine;
using System.Collections;

public class EnemyMovement : MoveToPoint {

    public Transform rightFootR, leftFootR;

    public GameObject playerGameObject;

    private Transform rFootT, lFootT;

	// Use this for initialization
	void Start ()
    {
        if (rightFootR != null)
            rFootT = rightFootR.GetChild(0);
        if (leftFootR != null)
            lFootT = leftFootR.GetChild(0);
        pointToReach = playerGameObject.transform.position;
	}
	
	// Update is called once per frame
	void Update ()
    {
        pointToReach = playerGameObject.transform.position;
        //hasReachedPoint = false;
        MoveToMethod();
        //KeepFeetsStable();
	}
    void KeepFeetsStable()
    {
        rFootT.rotation = new Quaternion(0, rFootT.rotation.y, rFootT.rotation.z, rFootT.rotation.w);
        lFootT.rotation = new Quaternion(0, lFootT.rotation.y, lFootT.rotation.z, lFootT.rotation.w);
    }
}
