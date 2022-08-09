using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatController : MonoBehaviour
{
    public float speed;
    public float changeTime;
    private float nextTime;
    private bool isUp;

    private void Start()
    {
        isUp = true;
        nextTime = Time.time + changeTime;
    }

    void Update()
    {
        if(Time.time > nextTime)
        {
            isUp = !isUp;
            nextTime = Time.time + changeTime;
        }
        if(isUp)
        {
            transform.Translate(0.0f, speed * Time.deltaTime, 0.0f, Space.World);
        }
        else
        {
            transform.Translate(0.0f, -speed * Time.deltaTime, 0.0f, Space.World);
        }
    }
}
