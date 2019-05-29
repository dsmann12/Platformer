using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Responsible for receiving input
// Sends input to controller script

// Require component annotation used when referencing other component
[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour
{
    public float moveSpeed = 6;
    // jump height and time to apex more intuitive for better control over jump
    // these determine what gravity and jumpVelocity are?
    // jump height is how many units character to jump
    // time is how long it takes to reach that point
    public float jumpHeight = 4;
    public float timeToJumpApex = .4f;
    public float accelerationTimeAirborne = .2f;
    public float accelerationTimeGrounded = .1f;
    


    private float gravity;
    private Vector3 velocity;
    private float jumpVelocity;
    private float velocityXSmoothing;
    private Controller2D controller;


    // Start is called before the first frame update    
    void Start()
    {
        controller = GetComponent<Controller2D>();

        // At apex, velocity is 0. Gravity is acceleration. jumpVelocity is final velocity when
        // hit the earth
        // deltaMovement = velocityInitial * time + ((acceleration * time^2)/2)
        // jumpHeight = gravity * timeToJumpApex^2/2
        // 2*jumpHeight/gravity = timeToJumpApex^2
        // use recipricol
        // gravity / 2*jumpHeight = 1/timeToJumpApex^2
        // gravity = 2*jumpHeight / timeToApex^2
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        // Know final velocity equal to initial vecloity + acceleration * time
        // think of apex. Initial velocity us 0. Acceleration is gravity
        // velocityFinal = velocityInitial + acceleration * time
        // jumpVelocity = gravity * timeToJumpApex
        // make sure use positive version of gravity
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print("Gravity: " + gravity + " Jump Velocity: " + jumpVelocity);
    }

    // Update is called once per frame
    void Update()
    {
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below)
        {
            velocity.y = jumpVelocity;
        }


        float targetVelocityX = input.x * moveSpeed;

        // Apply input to move in horizontal direction
        // makes change of direction less abrupt, smooths it.
        // if grounded use acceleration for grounded, otherwise use the one for airborn
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing,
            (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        // apply gravity to velocity
        velocity.y += gravity * Time.deltaTime;

        // invoke controller's move method to move object
        controller.Move(velocity * Time.deltaTime);
    }

    
}
