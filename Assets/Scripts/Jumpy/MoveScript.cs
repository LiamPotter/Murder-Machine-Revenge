using UnityEngine;
using System.Collections;
using TriTools;


public class MoveScript : MonoBehaviour {

    public float moveSpeed;

    public Camera cam;

    private Vector3 moveDirection;

    private Transform childTransform;

	// Use this for initialization
	void Start ()
    {
        childTransform = GetComponentInChildren<Transform>();
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        TriToolHub.CreateVector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), moveSpeed, TriToolHub.AxisPlane.XZ, cam.gameObject, out moveDirection);
        TriToolHub.SmoothLookAtDirection(childTransform.gameObject, moveDirection, 0, Vector3.up, true, 1);
        TriToolHub.AddForce(gameObject, childTransform.forward * Input.GetAxis("Vertical")*moveSpeed, Space.World, ForceMode.Acceleration);
	}
}
