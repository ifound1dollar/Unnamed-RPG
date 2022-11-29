using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
    public Transform playerCam;
    public float relativeMovement = 0.3f;

    //each frame, the tilemap this script is attached to will move along with the camera
    //  according to the ratio 'relativeMovement' defined in the editor (defaults here to 0.3)
    void Update()
    {
        transform.position = new Vector2(playerCam.position.x * relativeMovement,
            playerCam.position.y * relativeMovement);
    }
}
