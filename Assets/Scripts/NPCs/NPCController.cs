using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NPCController : MonoBehaviour, IInteractable
{
    [SerializeField] Tilemap terrainTilemap;
    [SerializeField] LayerMask blockingLayers;
    [SerializeField] int moveChancePercent;
    [SerializeField] int maximumRadius;
    [SerializeField] int moveSpeed = 2;

    [Header("Interaction data")]
    [SerializeField] List<Dialog> dialogs;


    bool isBusy;
    Animator animator;
    GameObject colliderChild;
    Vector2 childWorldPos;
    Vector3Int origPos;


    private void Awake()
    {
        origPos = new((int)transform.position.x, (int)transform.position.y);
        animator = GetComponent<Animator>();
        gameObject.AddComponent<BoxCollider2D>();

        //add childObject to this object and add a BoxCollider2D
        colliderChild = new("CollisionChild")
        {
            layer = 8  //interactable layer
        };
        colliderChild.transform.SetParent(transform);
        colliderChild.transform.position = transform.position;
        colliderChild.AddComponent<BoxCollider2D>();

        InvokeRepeating(nameof(AttemptMovement), 1.0f, 1.0f);
    }

    void AttemptMovement()
    {
        if (!isBusy && !GameState.InBattle)
        {
            //check moveChance
            if (UnityEngine.Random.Range(0, 100) < moveChancePercent)
            {
                //get movement direction randomly then set to Vector3Int
                Vector2Int move = new();
                switch (UnityEngine.Random.Range(0, 4))
                {
                    case 0:
                        {
                            move.y = 1;
                            break;
                        }
                    case 1:
                        {
                            move.x = 1;
                            break;
                        }
                    case 2:
                        {
                            move.y = -1;
                            break;
                        }
                    case 3:
                        {
                            move.x = -1;
                            break;
                        }
                }
                Vector3Int targetPos = new((int)transform.position.x + move.x,
                    (int)transform.position.y + move.y);

                //if greater than radius, flip x or y direction to try to move the other way
                if (Mathf.Abs(targetPos.x - origPos.x) > maximumRadius)
                {
                    //Debug.Log("targetPos.x beyond maximumRadius, flipping x");
                    //reverse sign and add value * 2 (already moved by 1)
                    targetPos.x += move.x * -2;
                }
                else if (Mathf.Abs(targetPos.y - origPos.y) > maximumRadius)
                {
                    //Debug.Log("targetPos.y beyond maximumRadius, flipping y");
                    targetPos.y += move.y * -2;
                }

                if (CheckIsWalkable(targetPos))
                {
                    StartCoroutine(Move(targetPos));
                }
            }
        }
    }

    IEnumerator Move(Vector3Int targetPos)
    {
        //Debug.Log("Move called.");
        isBusy = true;

        //while difference between target and actual position is not almost exact
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            //move position toward targetPos by moveSpeed * elasped (delta) time (SMALL AMOUNT)
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            colliderChild.transform.position = childWorldPos;

            //only performs single movement, then waits until next frame to move further
            yield return null;
        }

        //finally, after movement complete, set to actual targetPos to avoid tiny mathematical errors
        transform.localPosition = targetPos;
        isBusy = false;
    }

    bool CheckIsWalkable(Vector3Int targetPos)
    {
        if (terrainTilemap.GetTile(targetPos) is CustomTile)
        {
            //can NEVER walk on stairs
            return false;
        }

        if (Physics2D.OverlapCircle((Vector3)targetPos, 0.3f, blockingLayers) != null)
        {
            return false;
        }

        //if true, set 2D collider to targetPos to prevent NPC collisions
        colliderChild.transform.position = childWorldPos = (Vector3)targetPos;
        return true;
    }

    IEnumerator IInteractable.Interact()
    {
        //return if currently moving (already isBusy)
        if (isBusy) { yield break; }

        isBusy = true;

        //wait until ShowDialog returns so it has time to set GameState.InDialog
        yield return DialogManager.Instance.ShowDialog(dialogs);

        //player dialog started here, so wait to reset isBusy until InDialog gamestate is over
        yield return new WaitUntil(() => !GameState.InDialog);
        isBusy = false;
    }
}
