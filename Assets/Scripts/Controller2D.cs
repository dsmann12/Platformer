using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Controller2D : RaycastController
{
    public CollisionInfo collisions;
    // max angle character can climb
    public float maxClimbAngle = 80;
    public float maxDescendAngle = 80;

    // struct for information about collisions. Where collisions are occurring
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        
        // for slopes
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector3 velocityOld;

        // 1 means facing right, -1 means facing left. For wall jumping
        public int faceDirection;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
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

    // standing on platform bool useful for allowing jump on vertically moving platforms
    public void Move(Vector3 velocity, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        // set old velocity
        collisions.velocityOld = velocity;

        // if character has horizontal velocity, set face direction to sign of that velocity
        if (velocity.x != 0)
        {
            collisions.faceDirection = (int) Mathf.Sign(velocity.x);
        }

        // only check descent if y is decreasing
        if (velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }



        //if (velocity.x != 0)
        //{
        //    HorizontalCollisions(ref velocity);
        //}

        // check for horizontal collisions even if x = 0, for wall jump
        HorizontalCollisions(ref velocity);

        if (velocity.y != 0)
        {
            VerticalCollisions(ref velocity);
        }

        transform.Translate(velocity);

        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    public void HorizontalCollisions(ref Vector3 velocity)
    {
        // if moving down, direction is -1, else 1
        //float directionX = Mathf.Sign(velocity.x);
        float directionX = collisions.faceDirection;
        // raylength is equal to abs value of velocity (forces positive) and offset by skinWidth
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        // use two skinwidths. First to move ray to edge of collider, second to add some distance to detect a wall
        if (Mathf.Abs(velocity.x) < skinWidth)
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
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

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
                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    // if we are descending slope
                    // actually not descending, we are climbing. 
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
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
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    // add distance to velocity after climbing slope
                    velocity.x += distanceToSlopeStart * directionX;
                }

                // only wwant to check other rays if we are not climbing slope
                // also if the slopeAngle is > maxClimbAngle
                // if inside collision hit distance is 0
                // in this case , velocity x will be -skinWidth * directoinX,
                // resulting in small amount of movement opposire of direction
                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;

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
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    // set collisions info struct values
                    collisions.left = (directionX == -1);
                    collisions.right = (directionX == 1);
                }                
            }
        }
    }

    public void VerticalCollisions(ref Vector3 velocity)
    {
        // if moving down, direction is -1, else 1
        float directionY = Mathf.Sign(velocity.y);
        // raylength is equal to abs value of velocity (forces positive) and offset by skinWidth
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        for (uint i = 0; i < verticalRayCount; ++i)
        {
            // if moving down, want rays to start at bottomLeft
            // then offSet from topLeft/bottomLeft and also move with horizontal velocity
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            // Raycast and check for hit
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            // draw rays
            // bottomLeft + vector2.right * vertical ray spacing * i to space them out by iteration
            // on number of rays
            // change to rayOrigin, vector2.up*directoinY * rayLength to show rays being used for collision
            // changed direction of rays depending on direction of player
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            // if hit, then set y velocity equal to amount need to move to get to current position to 
            // where ray hit an obstacle (ray distance)
            if (hit)
            {

                velocity.y = (hit.distance - skinWidth) * directionY;

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
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
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
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            // ray origin. Get x direction of player. Change origin depending on direction
            // cast from height (velocity.y)
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) 
                + Vector2.up * velocity.y;
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
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    public void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        // how much distance object should move
        // treat it total distance up the slope we want to move
        // figure out what new velocity x and y should be
        // distance is hypotenuse
        float moveDistance = Mathf.Abs(velocity.x);
        // y (height) is distance * sign(theta)
        // use this because jump will reset y velocity
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // only do calculation is y velocity falls behind climbVelocity
        // this happens if we are not jumping
        if (velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            // use cos for x. Move left or right depending of velocity
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            // moving up with slope, but when movign up colliders check the top of object not bottom
            // assign collisions.below to true to correct for this
            // also assign climbingSlope to true
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    public void DescendSlope(ref Vector3 velocity)
    {
        // get x direction. 
        // have ray cast downwards. If moving left, start at bottom right, otherwise bottom left.
        // these are corners touching slope if descending
        float directionX = Mathf.Sign(velocity.x);
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
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        // distance of movement
                        // similar to climbSlope
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }
        
}
