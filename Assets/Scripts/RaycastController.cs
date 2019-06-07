﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Annotation useful when referencing a component
// Automatically adds required components and prevents them from
// being deleted in Unity editor
[RequireComponent (typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    // layer mask for collisions
    // create layers for object that will be collided with and assign layers to objects
    // do not leave player layer in collisionMask
    public LayerMask collisionMask;

    // used so rays can be cast even when on the ground or colliding with object
    public const float skinWidth = .035f;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    public const float distanceBetweenRays = .25f;

    // holds ray spacing for number of rays
    protected float horizontalRaySpacing;
    protected float verticalRaySpacing;

    public BoxCollider2D collider;

    protected RaycastOrigins raycastOrigins;

    // want to easily get four corners of game object
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }

    // must be run before Start so that camera can get collider bounds in its Start
    // method
    protected virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        CalculateRaySpacing();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2); // shrinks bounds by skinWidth

        // Set bounds values for raycast origins
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    protected void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2); // shrinks bounds by skinWidth

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        // must be at least one ray in each corner
        // at least two vertical and two horizontal
        // calculate based on width and height of object instead
        //horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        horizontalRayCount = Mathf.RoundToInt(boundsHeight / distanceBetweenRays);

        //verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / distanceBetweenRays);

        // calculate ray spacing
        // e.g. if 2 rays, then size/1 so space is essentially spacing out by whole width
        // keeps equal spacing and fence posting
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
}
