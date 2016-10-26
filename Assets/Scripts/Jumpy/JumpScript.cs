using UnityEngine;
using System.Collections;
using TriTools;
public class JumpScript : MonoBehaviour {

    public AnimationCurve jumpCurve;

    private AnimationCurve finishCurve;

    public bool isJumping;

    public float jumpStrength;

    public float jumpTime;

    public float y;

    private float timer = 0.0f;

    public Vector3 gravityStrength;

    private Rigidbody rBody;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        jumpCurve.postWrapMode = WrapMode.PingPong;
    }

	void FixedUpdate ()
    {
        Physics.gravity = gravityStrength;
       
        if (Input.GetKey(KeyCode.Space))
            isJumping = true;
        else isJumping = false;
        if (isJumping)
        {
            if (timer <= jumpTime)
            {
                y = jumpCurve.Evaluate(Mathf.Clamp((timer/jumpTime)*2,0,2));
                //Debug.Log(y);
                timer += Time.deltaTime;
                //transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, y, transform.position.z),Time.deltaTime*2f);
                //transform.position = new Vector3(0, y, 0);
                TriToolHub.AddForce(gameObject, TriToolHub.XYZ.Y, y*jumpStrength, Space.World, ForceMode.Acceleration);
            }      
        }
        if(!isJumping)
        {
            timer = 0.0f;
        }
    }
}
