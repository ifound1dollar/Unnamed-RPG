using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public LayerMask collisionLayer;
    public Tilemap terrainTilemap;
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

            //disable diagonal movement (prioritize x)
            if (inputPos.x != 0) { inputPos.y = 0; }

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
                float xDirection = inputPos.x;

                if (IsWalkable(targetPos))
                {
                    //store player and target positions as integers to check for CustomTile
                    Vector3Int playerPosInt = new((int)transform.position.x,
                        (int)transform.position.y, (int)targetPos.z);
                    Vector3Int targetPosInt = new((int)targetPos.x, (int)targetPos.y, (int)targetPos.z);
                    
                    //if CustomTile, then stairs, so do special behavior; else call base Move()
                    if (terrainTilemap.GetTile(playerPosInt) is CustomTile
                        || terrainTilemap.GetTile(targetPosInt) is CustomTile)
                    {
                        StartCoroutine(MoveStairs(targetPos, xDirection, playerPosInt, targetPosInt));
                    }
                    else
                    {
                        StartCoroutine(Move(targetPos));
                    }
                }
            }
        }

        //set isMoving of animator each Update() so it knows which animation to play
        animator.SetBool("isMoving", isMoving);        
    }

    IEnumerator Move(Vector3 targetPos, bool resetIsMoving = true, bool moveSlower = false)
    {
        isMoving = true;

        float localMoveSpeed = (moveSlower) ? moveSpeed * 0.75f : moveSpeed;

        //if the difference between targetPos and player position is greater than tiny value
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            //move player position toward targetPos by moveSpeed * elasped (delta) time (SMALL AMOUNT)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, localMoveSpeed * Time.deltaTime);

            //move camera position toward x and y (along with player x and y movement), not adjusting z
            playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position,
                new Vector3(transform.position.x, transform.position.y, playerCamera.transform.position.z),
                localMoveSpeed * Time.deltaTime);

            //only performs single movement, then waits until next Update() call (frame) to move further
            yield return null;
        }

        //finally, after movement complete, set to actual targetPos to avoid tiny mathematical errors
        transform.position = targetPos;
        isMoving = !resetIsMoving;
    }
    IEnumerator MoveStairs(Vector3 targetPos, float xMovement, Vector3Int playerPosInt, Vector3Int targetPosInt)
    {
        //if xDirection = 0, then moved up or down, so do nothing with x (directly MoveTowards)
        if (xMovement == 0)
        {
            StartCoroutine(Move(targetPos));
            yield break;
        }

        if (terrainTilemap.GetTile(playerPosInt) is CustomTile tilePlr)
        {
            //FIRST, if standing on stairs, should adjust targetTile since 0.5 rounding errors
            float yToCheck = targetPos.y;
            if (tilePlr.StairDirection == CustomTile.StairType.Right)
            {
                yToCheck += (targetPos.x > transform.position.x) ? 0.4f : -0.4f;
            }
            else
            {
                yToCheck += (targetPos.x > transform.position.x) ? -0.4f : 0.4f;
            }
            targetPosInt = new((int)targetPos.x, Mathf.RoundToInt(yToCheck), (int)targetPos.z);


            //CURRENT AND TARGET ARE STAIRS
            if (terrainTilemap.GetTile(targetPosInt) is CustomTile tileTar)
            {
                if (tileTar.StairDirection == CustomTile.StairType.Right)
                {
                    //if also moving right, move up; else move down
                    targetPos.y += (xMovement == 1.0f) ? 1.0f : -1.0f;
                }
                else
                {
                    //if NOT also moving left, move down; else move up
                    targetPos.y += (xMovement == 1.0f) ? -1.0f : 1.0f;
                }
                StartCoroutine(Move(targetPos, moveSlower: true));
            }
            //ONLY CURRENT IS STAIRS
            else
            {
                //FIRST, move only halfway to x and up/down 0.5 y (wait for completion)
                targetPos.x -= xMovement / 2;
                if (tilePlr.StairDirection == CustomTile.StairType.Right)
                {
                    //if also moving right, move up; else move down
                    targetPos.y += (xMovement == 1.0f) ? 0.5f : -0.5f;
                }
                else
                {
                    //if NOT also moving left, move down; else move up
                    targetPos.y += (xMovement == 1.0f) ? -0.5f : 0.5f;
                }
                yield return Move(targetPos, resetIsMoving: false, moveSlower: true);

                //SECOND, set target to rest of the way to x, then move and auto reset IsMoving
                targetPos.x += xMovement / 2;
                StartCoroutine(Move(targetPos));
            }
        }
        //ELSE ONLY TARGET IS STAIRS
        else if (terrainTilemap.GetTile(targetPosInt) is CustomTile tileTar)
        {
            //FIRST, ensure that player is trying to enter from valid direction
            if (playerPosInt.x < targetPosInt.x)
            {
                //player moving to right, ensure not left-only from direction
                if (tileTar.FromDirection == CustomTile.EnterDirection.Left) { yield break; }
            }
            else
            {
                //else player moving to left, so ensure not right-only from direction
                if (tileTar.FromDirection == CustomTile.EnterDirection.Right) { yield break; }
            }


            //SECOND, move only halfway to x and don't move y yet (wait for completion)
            targetPos.x -= xMovement / 2;
            yield return Move(targetPos, resetIsMoving: false);

            //THIRD, move other half of x and up/down 0.5 y
            if (tileTar.StairDirection == CustomTile.StairType.Right)
            {
                //stairs right & move right, so move up
                targetPos.y += (xMovement == 1.0f) ? 0.5f : -0.5f;
            }
            else
            {
                //if NOT also moving left, move down; else move up
                targetPos.y += (xMovement == 1.0f) ? -0.5f : 0.5f;
            }
            targetPos.x += xMovement / 2;
            StartCoroutine(Move(targetPos, moveSlower: true));
        }
    }

    bool IsWalkable(Vector3 targetPos)
    {
        //if player is at y = n.5, then is on stairs, so should check if above or below is walkable
        if (Mathf.Approximately(transform.position.y % 0.5f, 0)
            && !Mathf.Approximately(transform.position.y % 1.0f, 0))
        {
            //player will always be between two stair tiles here, so cast to int is fine
            Vector3Int playerPosInt = new((int)transform.position.x,
                (int)transform.position.y, (int)targetPos.z);

            //if ONLY moving up/down, adjust y +- 0.5 to check collision without rounding errors
            if (targetPos.x == transform.position.x)
            {
                targetPos.y += (targetPos.y > transform.position.y) ? 0.5f : -0.5f;
            }
            //else moving left/right, so move y +- 0.5 to align with corresponding stair tile
            else
            {
                if (terrainTilemap.GetTile(playerPosInt) is CustomTile tilePlr)
                {
                    if (tilePlr.StairDirection == CustomTile.StairType.Right)
                    {
                        //if both RIGHT, move y up; else move down
                        targetPos.y += (targetPos.x > transform.position.x) ? 0.5f : -0.5f;
                    }
                    else
                    {
                        //if both LEFT, move y up; else move down
                        targetPos.y += (targetPos.x < transform.position.x) ? 0.5f : -0.5f;
                    }
                }
            }
        }

        //finally, if targetPos overlaps a tile in the collision layer (radius 0.3), is not walkable
        if (Physics2D.OverlapCircle(targetPos, 0.3f, collisionLayer) != null)
        {
            return false;
        }
        return true;
    }
}
