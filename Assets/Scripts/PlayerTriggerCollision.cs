using UnityEngine;
using System.Collections;

public class PlayerTriggerCollision : MonoBehaviour {

    void OnTriggerEnter(Collider coll)
    {
        if (coll.tag.Contains("Enemy"))
        {
            if (coll.transform.parent.GetComponent<EnemyMovement>() != null)
            {
                if (!coll.transform.parent.GetComponent<EnemyMovement>().rageMode)
                    coll.transform.parent.SendMessage("Die");
            }
        }
        if(coll.tag.Contains("Flower"))
        {
            SendMessageUpwards("HealOneHealth");
            Destroy(coll.gameObject);
        }
        //Debug.Log(coll.name);
    }
}
