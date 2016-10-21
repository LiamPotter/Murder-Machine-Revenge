using UnityEngine;
using System.Collections;
using Rewired;

public class PlayerMovement : MoveToPoint {

    public LayerMask groundMask;
   
	void Start ()
    {
        shouldMoveToPoint = true;
        pointToReach = transform.position;

    }

	void Update ()
    {
        MoveToMethod();
        if(Input.GetMouseButtonDown(0))
        { ClickToPoint();}
	}
    public void ClickToPoint()
    {
        hasReachedPoint = false;
        Debug.Log("Clickd");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,500, groundMask.value))
        {
           pointToReach = hit.point;
        }
    }

}
