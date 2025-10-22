using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    public float maxSpeed = 5f;
    public float accelerationTime = 0.5f;

    private Vector2 direction = Vector2.zero;
    private float currentSpeed = 0f;
    private float accelerationRate;
    private Rigidbody2D rb;
    private List<Key> pressedKeys = new List<Key>();

    private Animator anim; // --- ÚJ VÁLTOZÓ AZ ANIMATORNAK ---

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // --- EZT A SORT ADD HOZZÁ ---
        accelerationRate = maxSpeed / accelerationTime;
    }

    private void Update()
    {

        var kb = Keyboard.current;
        if (kb == null) return;

        HandleKey(kb.upArrowKey, Key.UpArrow);
        HandleKey(kb.downArrowKey, Key.DownArrow);
        HandleKey(kb.leftArrowKey, Key.LeftArrow);
        HandleKey(kb.rightArrowKey, Key.RightArrow);

        UpdateDirection();

        // --- EZT A KÉT SORT ADD HOZZÁ AZ UPDATE VÉGÉRE ---
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

        if (direction != Vector2.zero)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, accelerationRate * Time.fixedDeltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, accelerationRate * Time.fixedDeltaTime);

        rb.linearVelocity = direction * currentSpeed;
    }
}