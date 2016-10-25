using UnityEngine;
using System.Collections;
using Rewired;

public class PlayerMovement : MoveToPoint {

    public int currentHealth;
    public LayerMask groundMask;

    public bool canMove;

	void Start ()
    {
        shouldMoveToPoint = true;
        pointToReach = transform.position;

    }

	void Update ()
    {
        if (!canMove)
            return;
        MoveToMethod();
        if(Input.GetMouseButtonDown(0))
        { ClickToPoint();}
	}
    public void ClickToPoint()
    {
        hasReachedPoint = false;
        //Debug.Log("Clickd");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,500, groundMask.value))
        {
           pointToReach = hit.point;
        }
    }
    public void TakeOneDamage()
    {
        currentHealth--;
    }
    public void HealOneHealth()
    {
        if (currentHealth <= 4)
            currentHealth++;
    }
}
