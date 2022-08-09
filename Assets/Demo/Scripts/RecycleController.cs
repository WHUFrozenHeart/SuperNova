using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecycleController : MonoBehaviour
{
    public float recycleDistance = 15.0f;
    public static Vector3 turnPoint;
    public static Vector3 turnDirection;
    public static bool isTurn = false;
    private int turnCount = 0;
    private Transform player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void FixedUpdate()
    {
        if(turnCount > 0)
        {
            transform.RotateAround(turnPoint, turnDirection, 90.0f * Time.deltaTime);
            --turnCount;
            if(turnCount == 0)
            {
                isTurn = false;
                PlayerController.isTurn = false;
                GameController.turnState = GameController.TurnState.None;
            }
        }
    }

    private void Update()
    {
        if (isTurn && turnCount == 0)
        {
            turnCount = 50;
        }
        if(player.position.z - transform.position.z > recycleDistance)
        {
            turnCount = 0;
            ObjectPoolController.Instance.RecycleGameObject(gameObject);
        }
    }
}
