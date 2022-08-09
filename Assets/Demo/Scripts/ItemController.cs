using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    public ChemistryController.EffectName effectTag;
    public GameObject pickEffect;
    public float effectTime = 3.0f;

    private void OnTriggerEnter(Collider other)
    {
        // 碰撞的对象是Player才进行相关操作
        if(other.gameObject.tag == "Player")
        {
            // 改变人物身上的特效
            other.GetComponent<ChemistryController>().ChangeEffects(effectTag);
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.PickUp);
            // 添加一个拾取特效
            // 并定时摧毁
            if (pickEffect != null)
            {
                Destroy(Instantiate(pickEffect, transform.position, transform.rotation), effectTime);
            }
            Destroy(gameObject);
        }
    }
}
