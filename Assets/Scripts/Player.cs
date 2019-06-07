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
    // public float jumpHeight = 4;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;

    public float timeToJumpApex = .4f;
    public float accelerationTimeAirborne = .2f;
    public float accelerationTimeGrounded = .1f;
    public float wallSlideSpeedMax = 3;

    // introduce minimum jump height for variable jump height
    // will need to calculate jump force required to meet min jump height
    // if release jump button early, character's upward velocity will be set to minimum jump force
    // so wherever character currently is in jump, it will only travel min jump height further upwards
    // velocityFinal^2 = velocityInit ^ 2 + 2 * acceleration * displacement
    // minJumpForce = sqrt(2 * gravity * minJumpHeight)

    // store forces for each type of wall jump
    // climb up same wall, jump off wall, and leap off wall
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    // to help with wall leap
    public float wallStickTime = .25f;



    private float gravity;
    private Vector3 velocity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private float velocityXSmoothing;
    private Controller2D controller;
    private float timeToWallUnstick;

    private Vector2 directionalInput;
    // boolean to hold whether player is wall sliding
    // wall sliding is true if player is colliding with wall to left or right, is not touching ground, and is moving downwards
    private bool wallSliding = false;
    private int wallDirX;

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
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        // Know final velocity equal to initial vecloity + acceleration * time
        // think of apex. Initial velocity us 0. Acceleration is gravity
        // velocityFinal = velocityInitial + acceleration * time
        // jumpVelocity = gravity * timeToJumpApex
        // make sure use positive version of gravity
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
    }

    // Update is called once per frame
    void Update()
    {
        //Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // equals -1 if colliding with wall to left, and 1 if colliding with wall to right
        wallDirX = (controller.collisions.left) ? -1 : 1;

        //// this is moved to before velocity may be reset
        //float targetVelocityX = directionalInput.x * moveSpeed;

        //// Apply input to move in horizontal direction
        //// makes change of direction less abrupt, smooths it.
        //// if grounded use acceleration for grounded, otherwise use the one for airborn
        //velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing,
        //    (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        //// apply gravity to velocity
        //velocity.y += gravity * Time.deltaTime;

        CalculateVelocity();
        HandleWallSliding();

        //// equals -1 if colliding with wall to left, and 1 if colliding with wall to right
        //wallDirX = (controller.collisions.left) ? -1 : 1;

        //// boolean to hold whether player is wall sliding
        //// wall sliding is true if player is colliding with wall to left or right, is not touching ground, and is moving downwards
        //wallSliding = false;

        //if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        //{
        //    wallSliding = true;

        //    // constrain downward speeds to some wall slide speed limit
        //    if (velocity.y < -wallSlideSpeedMax)
        //    {
        //        velocity.y = -wallSlideSpeedMax;
        //    }

        //    // if time to wall unstick more than 0, want to start decreasing time to unstick by Time.DeltaTime
        //    // only want to do this if we are moving away from wall we are sliding down
        //    if (timeToWallUnstick > 0)
        //    {
        //        // reset if still can be stuck to wall. Do not want to move
        //        velocityXSmoothing = 0;
        //        velocity.x = 0;

        //        if (directionalInput.x != wallDirX && directionalInput.x != 0)
        //        {
        //            timeToWallUnstick -= Time.deltaTime;
        //        } else
        //        {
        //            timeToWallUnstick = wallStickTime;
        //        }
        //    } else
        //    {
        //        // timeToWallUnstick starts at 0, so do this in case
        //        timeToWallUnstick = wallStickTime;
        //    }
        //}

        //if (controller.collisions.above || controller.collisions.below)
        //{
        //    velocity.y = 0;
        //}

        //Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // no longer care if collisions below since can jump if on wall
        //if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below)
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
            
        //}

        //// if space button released
        //if (Input.GetKeyUp(KeyCode.Space))
        //{
           
        //}


        //float targetVelocityX = input.x * moveSpeed;

        //// Apply input to move in horizontal direction
        //// makes change of direction less abrupt, smooths it.
        //// if grounded use acceleration for grounded, otherwise use the one for airborn
        //velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing,
        //    (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        // apply gravity to velocity
        //velocity.y += gravity * Time.deltaTime;

        // invoke controller's move method to move object
        controller.Move(velocity * Time.deltaTime, directionalInput);

        // if on moving platform, it is potentially calling controller.Move which may be altering above and below values
        // want to make sure have values present after calling Move with own input
        if (controller.collisions.above || controller.collisions.below)
        {
            // stops velocity from being reset to 0 when sliding down a slope
            // offset y to normal * gravity for each frame
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            } else
            {
                velocity.y = 0;
            }
        }
                
    }

    public void SetDirectionalInput (Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        // if wall sliding, 
        if (wallSliding)
        {
            // if trying to move in same direction as wall we are facing
            if (wallDirX == directionalInput.x)
            {
                // want to jump away from wall and up
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
                // for jumping off the wall
            }
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
                // when have input opposite to wall direction, do wall leap
            }
            else
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }

        // do regular jump if touching ground
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                // compare direction of x input to direction of slope normal
                // no jumping against max slope
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;

                }
            } else
            {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    public void OnJumpInputUp()
    {
        // could be case that jumpVelocity is less than minJumpVelocity
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    private void CalculateVelocity()
    {
        // this is moved to before velocity may be reset
        float targetVelocityX = directionalInput.x * moveSpeed;

        // Apply input to move in horizontal direction
        // makes change of direction less abrupt, smooths it.
        // if grounded use acceleration for grounded, otherwise use the one for airborn
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing,
            (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        // apply gravity to velocity
        velocity.y += gravity * Time.deltaTime;
    }

    private void HandleWallSliding()
    {
        // equals -1 if colliding with wall to left, and 1 if colliding with wall to right
        wallDirX = (controller.collisions.left) ? -1 : 1;

        // boolean to hold whether player is wall sliding
        // wall sliding is true if player is colliding with wall to left or right, is not touching ground, and is moving downwards
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            // constrain downward speeds to some wall slide speed limit
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // if time to wall unstick more than 0, want to start decreasing time to unstick by Time.DeltaTime
            // only want to do this if we are moving away from wall we are sliding down
            if (timeToWallUnstick > 0)
            {
                // reset if still can be stuck to wall. Do not want to move
                velocityXSmoothing = 0;
                velocity.x = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                // timeToWallUnstick starts at 0, so do this in case
                timeToWallUnstick = wallStickTime;
            }
        }
    }
}
