using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class EnemyMovement : MoveToPoint {

    public Transform rightFootR, leftFootR;

    public GameObject playerGameObject;

    public GameObject flowerToGet;

    private Transform rFootT, lFootT;

    public bool rageMode;

    private bool dead;

    private GameManager gm;

	// Use this for initialization
	void Start ()
    {
        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (rightFootR != null)
            rFootT = rightFootR.GetChild(0);
        if (leftFootR != null)
            lFootT = leftFootR.GetChild(0);
        pointToReach = playerGameObject.transform.position;

        gm = FindObjectOfType<GameManager>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (dead)
            return;
        if (!rageMode)
            FindClosestFlower();
        else pointToReach = playerGameObject.transform.position;
        //hasReachedPoint = false;
        MoveToMethod();
        //KeepFeetsStable();
	}
    void KeepFeetsStable()
    {
        rFootT.rotation = new Quaternion(0, rFootT.rotation.y, rFootT.rotation.z, rFootT.rotation.w);
        lFootT.rotation = new Quaternion(0, lFootT.rotation.y, lFootT.rotation.z, lFootT.rotation.w);
    }
    void FindClosestFlower()
    {
        List<GameObject> flowers = new List<GameObject>(); 
        foreach(GameObject flower in GameObject.FindGameObjectsWithTag("Flower"))
        {
            flowers.Add(flower);
        }
        flowers = flowers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
        flowerToGet = flowers[0];
        pointToReach = flowerToGet.transform.position;
    }
    public void Die()
    {
        Debug.Log("ded");
        dead = true;
        gm.gameScore++;
        Destroy(gameObject);
    }
}
