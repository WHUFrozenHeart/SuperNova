using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    public float mass;
    public int score = 35;
    public GameObject pickEffect;
    public float effectTime = 3.0f;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            other.GetComponent<PlayerController>().ChangeMass(mass);
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.PickUp);
            UIController.Instance.AddScore(score);
            if(pickEffect != null)
            {
                Destroy(Instantiate(pickEffect, transform.position, transform.rotation), effectTime);
            }
            //Destroy(gameObject);
            // 使用对象池进行回收再利用
            ObjectPoolController.Instance.RecycleGameObject(gameObject);
        }
    }
}
