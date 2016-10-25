using UnityEngine;
using System.Collections;

public class EnemyTriggerCollision : MonoBehaviour {

    EnemyMovement eMovement;

    bool doneDamage;

    void Start()
    {
        doneDamage = false;
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
                if (!doneDamage)
                {
                    coll.transform.parent.SendMessage("TakeOneDamage");
                    doneDamage = true;
                }
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
