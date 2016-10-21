using UnityEngine;
using System.Collections;

public class FlowerPopup : MonoBehaviour {

    public float speed;

    private float yPos;

    void Start()
    {
        yPos = transform.position.y + 10;
    }


	void Update ()
    {
        transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, yPos, speed * Time.deltaTime),transform.position.z);
	}
}
