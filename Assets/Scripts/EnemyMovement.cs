using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriTools;
public class EnemyMovement : MoveToPoint {

    public Transform rightFootR, leftFootR;

    public GameObject playerGameObject;

    public GameObject flowerToGet;

    private Transform rFootT, lFootT;

    public bool rageMode;

    public float rageTime;
    private float pRageTime;

    private bool dead;

    public Material rageMaterial;

    public Vector3 rageScale;

    public float rageSpeed;

    private float normalSpeed;

    private Vector3 normalScale;

    private Material normalMaterial;

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
        normalMaterial = transform.GetComponentInChildren<Renderer>().material;
        normalScale = transform.localScale;
        gm = FindObjectOfType<GameManager>();
        pRageTime = rageTime;
        normalSpeed = speed;

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (dead)
            return;
        if (!rageMode)
        {
            if (IsThereFlowers())
            {
                hasReachedPoint = false;
                FindClosestFlower();
            }
            else pointToReach = transform.position;
            if (transform.GetComponentInChildren<Renderer>().material!=normalMaterial)
            {
                transform.GetComponentInChildren<Renderer>().material = normalMaterial;
            }
            if (transform.localScale != normalScale)
                transform.localScale = normalScale;
            if (pRageTime != rageTime)
                pRageTime = rageTime;
            speed = normalSpeed;
        }
        else
        {
            pointToReach = playerGameObject.transform.position;
            if (transform.GetComponentInChildren<Renderer>().material != rageMaterial&&pRageTime>rageTime/2)
            {
                transform.GetComponentInChildren<Renderer>().material = rageMaterial;
            }
            if (transform.localScale != rageScale&&pRageTime>rageTime/2)
                transform.localScale = rageScale;
            pRageTime -= Time.deltaTime;
            if(pRageTime<=rageTime/2)
            {
                float scaleDown = TriToolHub.ReturnPercentage(rageTime / 2, 0, pRageTime)/100; ;
                transform.localScale = Vector3.Lerp(rageScale, normalScale, scaleDown);
            }
            if (pRageTime <= 0)
                rageMode = false;
            speed = rageSpeed;
        }
        MoveToMethod();
	}
    void KeepFeetsStable()
    {
        rFootT.rotation = new Quaternion(0, rFootT.rotation.y, rFootT.rotation.z, rFootT.rotation.w);
        lFootT.rotation = new Quaternion(0, lFootT.rotation.y, lFootT.rotation.z, lFootT.rotation.w);
    }
    void FindClosestFlower()
    {
        List<GameObject> flowers = new List<GameObject>();
        foreach (GameObject flower in GameObject.FindGameObjectsWithTag("Flower"))
        {
            flowers.Add(flower);
        }
        flowers = flowers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
        flowerToGet = flowers[0];
        pointToReach = flowerToGet.transform.position;
        
    }   
    bool IsThereFlowers()
    {
        //Debug.Log(GameObject.FindGameObjectsWithTag("Flower").Count());
        if (GameObject.FindGameObjectsWithTag("Flower").Count() <= 0)
        {
            return false;
        }
        else return true;
    }
    public void InitiateRage()
    {
        rageMode = true;
     
    }
    public void Die()
    {
        Debug.Log("ded");
        dead = true;
        gm.gameScore++;
        Destroy(gameObject);
    }
}
