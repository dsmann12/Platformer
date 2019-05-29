using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController
{
    // new layer mask because colliderMask is for stopping object
    public LayerMask passengerMask;
    // to move platform around
    public Vector3 move;

    // list of passenger information
    List<PassengerMovement> passengerMovement;
    // map passenger transform to its controller 2d
    // for reducing number of GetComponent calls
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

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
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();
        // how much to move object
        // can change x, y, z values
        Vector3 velocity = move * Time.deltaTime;

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

                if (hit)
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

                if (hit)
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

                if (hit)
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
}
