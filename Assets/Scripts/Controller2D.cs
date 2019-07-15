using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Controller2D : RaycastController
{
    public CollisionInfo collisions;
    // max angle character can climb
    public float maxSlopeAngle = 80;

    public Vector2 playerInput;

    // struct for information about collisions. Where collisions are occurring
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        
        // for slopes
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 velocityOld;

        // 1 means facing right, -1 means facing left. For wall jumping
        public int faceDirection;

        // indicates if player if falling through platform
        // used to moving platform doesn't catch player if not falling through fast enough
        public bool fallingThroughPlatform;

        public bool slidingDownMaxSlope;

        // for use in acceleration on a slope
        public Vector2 slopeNormal;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            slopeNormal = Vector2.zero;
        }
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        // initialize to facing right
        collisions.faceDirection = 1;
    }

    // Update is called once per frame
    void Update()
    {
    }

    // for moving platforms
    // change variable from velocity to move amount because we pass velocity * Time.deltaTime
    public void Move(Vector2 moveAmount, bool standingOnPlatform = false)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    // standing on platform bool useful for allowing jump on vertically moving platforms
    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        // set old velocity
        collisions.velocityOld = moveAmount;
        playerInput = input;

        //// if character has horizontal velocity, set face direction to sign of that velocity
        //if (moveAmount.x != 0)
        //{
        //    collisions.faceDirection = (int) Mathf.Sign(moveAmount.x);
        //}

        // only check descent if y is decreasing
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        // if character has horizontal velocity, set face direction to sign of that velocity
        // move this after descend slope, because slope may cause velocity to be different than direction of input
        if (moveAmount.x != 0)
        {
            collisions.faceDirection = (int)Mathf.Sign(moveAmount.x);
        }

        //if (velocity.x != 0)
        //{
        //    HorizontalCollisions(ref velocity);
        //}

        // check for horizontal collisions even if x = 0, for wall jump
        HorizontalCollisions(ref moveAmount);

        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);
        }

        transform.Translate(moveAmount);

        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    public void HorizontalCollisions(ref Vector2 moveAmount)
    {
        // if moving down, direction is -1, else 1
        //float directionX = Mathf.Sign(velocity.x);
        float directionX = collisions.faceDirection;
        // raylength is equal to abs value of velocity (forces positive) and offset by skinWidth
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        // use two skinwidths. First to move ray to edge of collider, second to add some distance to detect a wall
        if (Mathf.Abs(moveAmount.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (uint i = 0; i < horizontalRayCount; ++i)
        {
            // if moving left, want rays to start at bottomLeft, else from bottomRight
            // then offSet from bottomRight/bottomLeft
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            // no need to offset by velocity.x because no gravity being applied?
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            // Raycast and check for hit to right or left
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // draw rays
            // bottomLeft + vector2.right * vertical ray spacing * i to space them out by iteration
            // on number of rays
            //Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);
            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);


            // if hit, then set y velocity equal to amount need to move to get to current position to 
            // where ray hit an obstacle (ray distance)
            if (hit)
            {
                // calculations will be incorrect when inside a collisions (distance == 0)
                // could cause character to me a little in opposite direction of collisions.
                // stops platform from making it hard to move
                if (hit.distance == 0)
                {
                    continue;
                }

                // get angle of surface hit with raycast.
                // raycast stores surface's normal when it hits a surface. Can get angle
                // use Vector2.up as other direction.
                // gets the angle of upcoming slope
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // bottom most ray
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    // if we are descending slope
                    // actually not descending, we are climbing. 
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.velocityOld;
                    }

                    // raycast will hit slope before player collider box does
                    // get the distance to the slope
                    float distanceToSlopeStart = 0;
                    // if slope angle is not the same as old, e.g. new slope
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        // then distance is hit distance - skinWidth
                        distanceToSlopeStart = hit.distance - skinWidth;
                        // subtracting distance from velocity x so that when 
                        // we call climp slope it onyl uses velocity.x that 
                        // object has when it reaches slope
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    // add distance to velocity after climbing slope
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                // only wwant to check other rays if we are not climbing slope
                // also if the slopeAngle is > maxClimbAngle
                // if inside collision hit distance is 0
                // in this case , velocity x will be -skinWidth * directoinX,
                // resulting in small amount of movement opposire of direction
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;

                    // say moving downward and left hits a higher platform than right raycast
                    // change rayLength to the distance of hit so rightmost raycast distance is only as long as left
                    // what about when right most hit higher platform than left most?
                    rayLength = hit.distance;

                    // if climbing slope, then set velocity y so that it is still sitting on slope
                    // when we move with this amount of velocity x
                    // use tangent
                    // fixes skittering when hitting obstacle on slope
                    if (collisions.climbingSlope)
                    {
                        // use collisions.slopeAngle because this ray is one that climbs slope
                        // otheres may have updated slopeAngle
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    // set collisions info struct values
                    collisions.left = (directionX == -1);
                    collisions.right = (directionX == 1);
                }                
            }
        }
    }

    public void VerticalCollisions(ref Vector2 moveAmount)
    {
        // if moving down, direction is -1, else 1
        float directionY = Mathf.Sign(moveAmount.y);
        // raylength is equal to abs value of velocity (forces positive) and offset by skinWidth
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;
        for (uint i = 0; i < verticalRayCount; ++i)
        {
            // if moving down, want rays to start at bottomLeft
            // then offSet from topLeft/bottomLeft and also move with horizontal velocity
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);

            // Raycast and check for hit
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            // draw rays
            // bottomLeft + vector2.right * vertical ray spacing * i to space them out by iteration
            // on number of rays
            // change to rayOrigin, vector2.up*directoinY * rayLength to show rays being used for collision
            // changed direction of rays depending on direction of player
            //Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);


            // if hit, then set y velocity equal to amount need to move to get to current position to 
            // where ray hit an obstacle (ray distance)
            if (hit)
            {
                // for moving through one sided platform
                if (hit.collider.tag == "OneSidedPlatform")
                {
                    // check if hit.distance is 0 in case player gets close to edge of platform but doesn't quite make it. 
                    // without this the player will float up through the platform
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }

                    if (collisions.fallingThroughPlatform)
                    {
                        continue;
                    }

                    // if player is pressing down, fall through platform
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        // reset falling through plaftorm flag after half a second
                        Invoke("ResetFallingThroughPlatform", 0.25f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY;

                // say moving downward and left hits a higher platform than right raycast
                // change rayLength to the distance of hit so rightmost raycast distance is only as long as left
                // what about when right most hit higher platform than left most?
                rayLength = hit.distance;

                // tan = y/x
                // x= y / tan(theta)
                // fixes skittering when running into obstacle on slope
                if (collisions.climbingSlope)
                {
                    // use collisoins.slopeAngle because slopeAngle may have been modified 
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                // set collisions info struct values
                collisions.above = (directionY == 1);
                collisions.below = (directionY == -1);
            }

        }

        // if climbing slope
        // deal with getting caught on angles when moving to a newer slope
        // fire horizontal array from point of y axis where we will be once we move. To 
        // check if new slope
        if (collisions.climbingSlope)
        {
            // get directionX. Get ray length
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            // ray origin. Get x direction of player. Change origin depending on direction
            // cast from height (velocity.y)
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) 
                + Vector2.up * moveAmount.y;
            // ray cast
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // if hit something
            if (hit)
            {
                // get angle
                // if new slope angle is not equal to angle in collisions struct
                // then must recalculate for new slope
                // do same as horizontal collision
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    public void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        // how much distance object should move
        // treat it total distance up the slope we want to move
        // figure out what new velocity x and y should be
        // distance is hypotenuse
        float moveDistance = Mathf.Abs(moveAmount.x);
        // y (height) is distance * sign(theta)
        // use this because jump will reset y velocity
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // only do calculation is y velocity falls behind climbVelocity
        // this happens if we are not jumping
        if (moveAmount.y <= climbVelocityY)
        {
            moveAmount.y = climbVelocityY;
            // use cos for x. Move left or right depending of velocity
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            // moving up with slope, but when movign up colliders check the top of object not bottom
            // assign collisions.below to true to correct for this
            // also assign climbingSlope to true
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    public void DescendSlope(ref Vector2 moveAmount)
    {

        // new ray casts for detecting max slope angle
        // don't know which way slope is facing
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);

        // only slide down slope, if only one corner of box is detecting a collisoion
        // XOR because if same, it will be equal to 0x0 which is falsey
        if (maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        // only do descend down slope, if not on a slope greater than max slope
        if (!collisions.slidingDownMaxSlope)
        {
            // get x direction. 
            // have ray cast downwards. If moving left, start at bottom right, otherwise bottom left.
            // these are corners touching slope if descending
            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft);
            // ray cast
            // cast downward. Don't know how far down slope is so use infinity ray cast length
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);

            // if hit something
            if (hit)
            {
                // get angle
                // if new slope angle is not 0 (flat surface) and is less or equal to maxDescendAngle
                // hit.normal is direction perpendicular to slope. If look at x or normal than you know
                // direction of slope. Only care if moving in same direction as slope.
                // check if hit distance is less than tangent of slopeAngle. This is because infinte raycast
                // want to make sure we are close enough to the slope. This is how far we have to move on y axis
                // based on slopeangle and velocity on x axis. 
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                        {
                            // distance of movement
                            // similar to climbSlope
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendVelocityY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    // used to reset falling through platform flag after some time
    public void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }

    private void SlideDownMaxSlope (RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            // get angle
            // if new slope angle is not 0 (flat surface) and is less or equal to maxDescendAngle
            // hit.normal is direction perpendicular to slope. If look at x or normal than you know
            // direction of slope. Only care if moving in same direction as slope.
            // check if hit distance is less than tangent of slopeAngle. This is because infinte raycast
            // want to make sure we are close enough to the slope. This is how far we have to move on y axis
            // based on slopeangle and velocity on x axis. 
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                // trying to figure out how far it should move in x axis at end of frame. Know it is movign y distance
                // y value minus hit distance will give y direction falling
                // angle is angle of slope
                // so must use tan
                // x = (y - hitDistance) / tan(theta)
                // moveAmount should be positive if slope is slanting down in positive direction or vice versa
                // use the normal, if sloping down in positive direction, normal will have positive x value
                moveAmount.x = hit.normal.x * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }
        
}
