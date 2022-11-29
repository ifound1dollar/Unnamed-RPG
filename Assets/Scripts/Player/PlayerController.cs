using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public LayerMask collisionLayer;
    public Camera playerCamera;

    bool isMoving;
    Vector2 inputPos;

    Animator animator;

    private void Awake()
    {
        //initialize animator reference and move camera above player sprite
        animator = GetComponent<Animator>();
        playerCamera.transform.position = new Vector3(transform.position.x,
            transform.position.y, playerCamera.transform.position.z);
    }

    void Update()
    {
        //only allow movement input if not already moving
        if (!isMoving)
        {
            inputPos.x = Input.GetAxisRaw("Horizontal");
            inputPos.y = Input.GetAxisRaw("Vertical");

            //disable diagonal movement (prioritize y)
            if (inputPos.y != 0) { inputPos.x = 0; }

            //if there is actual input
            if (inputPos != Vector2.zero)
            {
                //set animator move parameters to x and y from input
                animator.SetFloat("moveX", inputPos.x);
                animator.SetFloat("moveY", inputPos.y);

                //raw input will be -1, 0, or 1, so can directly add inputPos
                var targetPos = transform.position;
                targetPos.x += inputPos.x;
                targetPos.y += inputPos.y;

                if (IsWalkable(targetPos))
                {
                    StartCoroutine(Move(targetPos));
                }
            }
        }

        //set isMoving of animator each Update() so it knows which animation to play
        animator.SetBool("isMoving", isMoving);        
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        //if the difference between targetPos and player position is greater than tiny value
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            //move player position toward targetPos by moveSpeed * elasped (delta) time (SMALL AMOUNT)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            //move camera position toward x and y (along with player x and y movement), not adjusting z
            playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position,
                new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z),
                moveSpeed * Time.deltaTime);

            //only performs single movement, then waits until next Update() call (frame) to move further
            yield return null;
        }

        //finally, after movement complete, set to actual targetPos to avoid tiny mathematical errors
        transform.position = targetPos;
        isMoving = false;
    }

    bool IsWalkable(Vector3 targetPos)
    {
        if (Physics2D.OverlapCircle(targetPos, 0.3f, collisionLayer) != null)
        {
            //if targetPos overlaps a tile in the collision layer (radius 0.3), is not walkable
            return false;
        }
        return true;
    }
}
