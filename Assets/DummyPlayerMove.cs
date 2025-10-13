using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPlayerMove : MonoBehaviour
{
    // The speed at which the player moves. Can be adjusted in the Unity Inspector.
    public float moveSpeed = 5f;

    // A reference to the Rigidbody2D component for physics calculations.
    private Rigidbody2D rb;

    // A Vector2 to store the player's movement input.
    private Vector2 movement;

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        // Get and store the Rigidbody2D component attached to this player GameObject.
        // This is a common optimization to avoid calling GetComponent every frame.
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame. It's used for handling input.
    void Update()
    {
        // Get raw input from the horizontal and vertical axes (A/D/Left/Right and W/S/Up/Down).
        // "GetAxisRaw" provides an immediate -1, 0, or 1, resulting in responsive movement.
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Normalize the movement vector to ensure consistent speed in all directions.
        // Without this, diagonal movement would be faster.
        movement.Normalize();
    }

    // FixedUpdate is called on a fixed time interval. It's the best place for physics-related code.
    void FixedUpdate()
    {
        // Apply the movement to the Rigidbody2D's velocity.
        // We multiply the normalized input vector by our desired speed.
        rb.velocity = movement * moveSpeed;
    }
}
