using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController
{
    // new layer mask because colliderMask is for stopping object
    public LayerMask passengerMask;
    // to move platform around. Not needed if platform has set movement among waypoints
    // public Vector3 move;

    // positions relative to platform for platform to move between
    public Vector3[] localWaypoints;
    // positions use to actually travel between. 
    public Vector3[] globalWaypoints;

    // speed of platform
    public float speed;

    // specify whether want to be cyclic platform (after reach last waypoint, move to first)
    public bool cyclic;

    // wait time for platform to wait when it hits a waypoint
    public float waitTime;

    // set east amount. Should be 1 when no easing. Ease() adds 1 to this value
    [Range (0, 3)]
    public float easeAmount;

    // index of waypoint platform is moving away from
    private int fromWaypointIndex;
    // percentage of distance covered between waypoints. Value between 0 and 1
    private float percentBetweenWaypoints;
    // when to next move
    private float nextMoveTime;

    // list of passenger information
    private List<PassengerMovement> passengerMovement;
    // map passenger transform to its controller 2d
    // for reducing number of GetComponent calls
    private Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

    // holds passenger information
    // holds tranform, velocity
    // whether standing on platform
    // and if must move passenger before platform is moved
    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform transform, Vector3 velocity, bool standingOnPlatform, bool moveBeforePlatform)
        {
            this.transform = transform;
            this.velocity = velocity;
            this.standingOnPlatform = standingOnPlatform;
            this.moveBeforePlatform = moveBeforePlatform;
        }
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        // intialize global waypoints
        globalWaypoints = new Vector3[localWaypoints.Length];

        // loop through local waypoints and use values to assign to global waypoints elements
        for (int i = 0; i < localWaypoints.Length; ++i)
        {
            // global waypoints equal to local waypoints plus the position of object when loaded into scene
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();
        // how much to move object
        // can change x, y, z values
        //Vector3 velocity = move * Time.deltaTime;
        // use CalculatePlatformMovement for velocity
        Vector3 velocity = CalculatePlatformMovement();

        // move the passengers based on velocity of platform
        CalculatePassengerMovement(velocity);

        // move passengers that must be moved before platform
        MovePassengers(true);
        transform.Translate(velocity);
        // move passengers that must be moved after platform
        MovePassengers(false);
    }

    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            // add passenger to dictionary if passenger not in dictionary
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }

            // if passenger move before platform equals what the paramter calls for'
            // move those objects
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    // move any controller 2d being affected by platform
    void CalculatePassengerMovement(Vector3 velocity)
    {
        // multiple raycasts can hit a passenger multiple times,
        // causing passenger to move many times in a frame
        // instead store in dictionary and check if passenger has been moved
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        // the sign of the velocity in X and Y directions
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // vertically moving platform
        if (velocity.y != 0)
        {
            // account for skin with rays
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (uint i = 0; i < verticalRayCount; ++i)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                // don't want to move player if player is inside collision box of platform. Can happen on one sided platforms
                if (hit && hit.distance != 0)
                {
                    // only move passenger is passenger has not been added to hashset
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        // only comes into effect is platform has some x velocity
                        // if platform is moving up, making passenger velocity.x otherwise make it 0
                        float pushX = (directionY == 1) ? velocity.x : Vector2.zero.x;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        // add passenger movment to list
                        // if vertically moving, then cast rays up if player standing on platform
                        // if going down, it is casting down and seeing player below
                        // if directoinY is 1, passenger is moving up. So is standing on platform
                        // if passenger is on top and platform moving up, move passenger first
                        // same if passenger is below and platform moving down
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                                                directionY == 1, true));
                    }
                }
            }
        }

        // horizontally moving platforms
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for(uint i = 0; i < horizontalRayCount; ++i)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                // don't want to move player if player is inside collision box of platform. Can happen on one sided platforms
                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        // [scratch] push y is 0
                        // do not use 0 because when platform is pushing player
                        // passenger is never checking below itself and not aware it is on ground
                        // add small downward force to have this disappear
                        float pushY = -skinWidth;

                        // impossible that player is standing on platform. player pushed from side
                        // always want passenger to move before
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                                                false, true));
                    }
                }
            }
        }

        // passenger on top of moving platform
        // if platform is going down or platform is moving horizontally (y ==0, x !=0)
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            // one skinwidth to get to surface of platform, other to detect anything on top
            float rayLength = skinWidth * 2;

            for (uint i = 0; i < verticalRayCount; ++i)
            {
                // always want to start at top left for raycast
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                // want ray to point up always
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                // don't want to move player if player is inside collision box of platform. Can happen on one sided platforms
                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        // move at velocity of platform
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        // passenger is on top so true
                        // want platform to move first if going down, and don't care going horizontal
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                                                true, false));
                    }
                }
            }
        }
    }

    // for viewing waypoints while editing level
    void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            // make gizmos red
            Gizmos.color = Color.red;
            // for setting gizmo size
            float size = .3f;

            // go through each waypoint
            for (int i = 0; i < localWaypoints.Length; ++i)
            {
                // need to convert local position into a global position to draw gizmos
                // want gizmos to stay still when game is running but move with platform when game isn't running (e.g. use global waypoints if running, otherwise use local). 
                Vector3 globalWaypointPosition = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                // draw gizmo centered at global position. Going to draw cross
                Gizmos.DrawLine(globalWaypointPosition - Vector3.up * size, globalWaypointPosition + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPosition - Vector3.left * size, globalWaypointPosition + Vector3.left * size);

            }
        }
    }

    // equation to get easing when moving between two points
    // y = (x^a) / (x^a + (1-x)^a). The higher the value of a, the more easing when reaching points. When a = 1, movement is linear and thus like not easing.
    // value between 1 and around 3 is optimal. 
    private float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    private Vector3 CalculatePlatformMovement()
    {
        // want to know which waypoint platform is moving away from, towards, and % it has moved between the two

        // if time is less than nextMoveTime, do not move at all (return Vector3.zero)
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        fromWaypointIndex %= globalWaypoints.Length;

        // toWayPoint must be 1 from fromWaypoint index
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;

        // Vector3 Distance() method used to get distance between two vector3 points
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        // Time.deltaTime * speed increases at a constant rate. So if waypoints are further apart, the percentage between them will still increase at same speed
        // use distance and divide speed by distance to account for this
        percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        // clamp percent between 0 and 1
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoint = Ease(percentBetweenWaypoints);

        // vector3 stores new position. Use Vector3.Lerp to find point between fromWaypoint and toWaypoint based on eased percentage between waypoints
        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoint);

        // what if percent between waypoints is >= 1. Reached waypoint
        if (percentBetweenWaypoints >= 1)
        {
            // set it to 0
            percentBetweenWaypoints = 0;
            // increment fromWayPoint
            fromWaypointIndex++;

            if (!cyclic)
            {
                // if fromWayPointIndex now is outside bounds of array, set it to 0, and reverse the waypoints array
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    // reverse globalWaypoints array to go in opposite direction.
                    System.Array.Reverse(globalWaypoints);
                }
            }

            // next move time is equal to current time plus amount to wait
            nextMoveTime = Time.time + waitTime;
        }

        // return newPos - transform.position to get amount we want to move this frame
        return newPos - transform.position;
    }
}
