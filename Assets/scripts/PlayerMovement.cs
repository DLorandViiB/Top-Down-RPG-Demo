using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    public bool canMove = true;

    public float maxSpeed = 5f;
    public float accelerationTime = 0.5f;

    private Vector2 direction = Vector2.zero;
    private float currentSpeed = 0f;
    private float accelerationRate;
    private Rigidbody2D rb;
    private List<Key> pressedKeys = new List<Key>();

    private Animator anim;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        accelerationRate = maxSpeed / accelerationTime;
    }

    private void Update()
    {
        if (canMove == false)
        {
            if (pressedKeys.Count > 0)
            {
                pressedKeys.Clear();
                UpdateDirection();
            }

            anim.SetFloat("moveX", 0);
            anim.SetFloat("moveY", 0);
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        HandleKey(kb.upArrowKey, Key.UpArrow);
        HandleKey(kb.downArrowKey, Key.DownArrow);
        HandleKey(kb.leftArrowKey, Key.LeftArrow);
        HandleKey(kb.rightArrowKey, Key.RightArrow);

        UpdateDirection();

        anim.SetFloat("moveX", direction.x);
        anim.SetFloat("moveY", direction.y);
    }

    private void HandleKey(UnityEngine.InputSystem.Controls.KeyControl keyControl, Key key)
    {
        if (keyControl.wasPressedThisFrame)
        {
            if (!pressedKeys.Contains(key))
                pressedKeys.Add(key);
        }
        else if (keyControl.wasReleasedThisFrame)
        {
            pressedKeys.Remove(key);
        }
    }

    private void UpdateDirection()
    {
        if (pressedKeys.Count == 0)
        {
            direction = Vector2.zero;
            return;
        }

        Key latest = pressedKeys[pressedKeys.Count - 1];
        switch (latest)
        {
            case Key.UpArrow: direction = Vector2.up; break;
            case Key.DownArrow: direction = Vector2.down; break;
            case Key.LeftArrow: direction = Vector2.left; break;
            case Key.RightArrow: direction = Vector2.right; break;
        }
    }

    private void FixedUpdate()
    {
        // If frozen, force physics to stop.
        if (canMove == false)
        {
            currentSpeed = 0;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (direction != Vector2.zero)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, accelerationRate * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, accelerationRate * Time.fixedDeltaTime);

        rb.linearVelocity = direction * currentSpeed;
    }

    // GameStatemanager will call this
    public void StopMovement()
    {
        // Clear all held keys.
        pressedKeys.Clear();

        UpdateDirection();

        currentSpeed = 0f;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;

        if (anim == null) anim = GetComponent<Animator>();
        anim.SetFloat("moveX", 0);
        anim.SetFloat("moveY", 0);
    }
}