using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleController : MonoBehaviour
{
    public float minAlpha = 0.25f;
    public float maxAlpha = 1.0f;
    private float changeSpeed;
    private bool isDown = true;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        changeSpeed = maxAlpha - minAlpha;
    }

    private void Update()
    {
        if (isDown)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Max(image.color.a - Time.deltaTime * changeSpeed, minAlpha));
            if (image.color.a == minAlpha)
            {
                isDown = false;
            }
        }
        else
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Min(image.color.a + Time.deltaTime * changeSpeed, maxAlpha));
            if (image.color.a == maxAlpha)
            {
                isDown = true;
            }
        }
    }
}
