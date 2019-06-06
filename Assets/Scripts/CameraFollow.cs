using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Controller2D target;
    public Vector2 focusAreaSize;

    public float verticalOffset;

    // how much to look ahead in camera
    public float lookAheadDistanceX;
    // time to smooth movement when camera needs to stop
    public float lookSmoothTimeX;
    // time to smooth for when camera needs to stop
    public float verticalSmoothTime;

    private FocusArea focusArea;

    private float currentLookAheadX;
    // target we are smoothing toward
    private float targetLookAheadX;
    private float lookAheadDirectionX;
    private float smoothLookVelocityX;
    private float smoothVelocityY;

    // used to keep look ahead from looking too far ahead after player stops
    // so look ahead is not set every frame
    private bool lookAheadStopped;

    // the focus area is the area the player can move in without moving the camera
    // when the player pushes against the bounds of the focus area, the player moved the
    // camera. 
    struct FocusArea
    {
        public Vector2 center;
        public Vector2 velocity;
        private float left, right, top, bottom;

        public FocusArea (Bounds targetBounds, Vector2 size)
        {
            this.left = targetBounds.center.x - (size.x / 2);
            this.right = targetBounds.center.x + (size.x / 2);
            this.bottom = targetBounds.min.y;
            this.top = targetBounds.min.y + size.y;

            this.velocity = Vector2.zero;

            // midpoint between two points found by adding points and dividing by 2
            // midpoint between top and bottom and left and right is center
            center = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        // method for updating focus area's position when target moves against bounds
        public void Update(Bounds targetBounds)
        {
            // check if target is moving against either left or right edge
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }

            left += shiftX;
            right += shiftX;

            // check if target is moving against either bottom or top edge
            float shiftY = 0;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }

            bottom += shiftY;
            top += shiftY;

            // midpoint between two points found by adding points and dividing by 2
            // midpoint between top and bottom and left and right is center
            center = new Vector2((left + right) / 2, (top + bottom) / 2);

            // want to know how far focus area has moved in last frame
            // equal to the shift in x and y
            velocity = new Vector2(shiftX, shiftY);
        }

    }

    

    // Start is called before the first frame update
    void Start()
    {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    // used by cameras because takes place after movement in Update/FixedUpdate
    private void LateUpdate()
    {
        focusArea.Update(target.collider.bounds);

        // moving camera a little further from focus area
        Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

        // only look ahead if player is moving
        if (focusArea.velocity.x != 0)
        {
            // direction indicated by sign of focusArea's velocity
            lookAheadDirectionX = Mathf.Sign(focusArea.velocity.x);

            // player has some degree of smoothing. When player is moving left, if tries moving right
            // player will continue moving left as it slows down, then starts moving right
            // only want to set look ahead in x direction if camera is looking in same direction as
            // focus area is moving
            // Sign returns 1 for 0 so must check player input is not 0
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) &&
                target.playerInput.x != 0)
            {
                lookAheadStopped = false;
                // look ahead is distance multiplied by direction to move look in right direction
                targetLookAheadX = lookAheadDirectionX * lookAheadDistanceX;
            } else
            {
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    // don't have lookaheadx go much further but don't want to stop it daed
                    // add just a fraction of remaining distance to achieve this
                    targetLookAheadX = currentLookAheadX + (lookAheadDirectionX * lookAheadDistanceX - currentLookAheadX) / 4f;
                }
            }
        } 

        // smooth in x direction toward target look ahead position in time smoothing is set to take
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);
        // vertical smoothing
        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        focusPosition += Vector2.right * currentLookAheadX;

        // move camera
        // multiply by -10 to make sure camera is always in front of scene
        transform.position = (Vector3) focusPosition + Vector3.forward * -10;
    }

    // draw transparent red gizmo for focus area
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
