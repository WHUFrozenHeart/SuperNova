using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundFrictionController : MonoBehaviour
{
    // 当前地形的摩擦力
    public float friction;
    public PlayerController.LocationSort location;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.ChangeFriction(friction);
            player.ChangeLocation(location);
        }
    }
}
