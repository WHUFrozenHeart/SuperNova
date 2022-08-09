using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float maxGap;

    private void Update()
    {
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        if(transform.gameObject.activeSelf)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Max(transform.position.z, player.position.z - maxGap));
        }
    }
}
