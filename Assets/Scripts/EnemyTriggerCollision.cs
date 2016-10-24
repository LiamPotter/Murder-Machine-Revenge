using UnityEngine;
using System.Collections;

public class EnemyTriggerCollision : MonoBehaviour {

    EnemyMovement eMovement;

    void Start()
    {
        eMovement = GetComponentInParent<EnemyMovement>();
    }
    void OnTriggerEnter(Collider coll)
    {
        if (coll.tag.Contains("Flower")&&!eMovement.rageMode)
        {
            SendMessageUpwards("InitiateRage");
            Destroy(coll.gameObject);
        }
        //Debug.Log(name + " hit " + coll.name);
        if(eMovement.rageMode)
        {
            if(coll.tag.Contains("Player"))
            {
                coll.transform.parent.SendMessage("TakeOneDamage");
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
