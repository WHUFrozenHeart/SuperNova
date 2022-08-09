using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterGroundController : MonoBehaviour
{
    public GameObject drownEffect;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PlayerController>().PlayerDead("��ʹ�������ȣ�Ҳ������ˮҰӾ��");
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Drown);
            if (drownEffect != null)
            {
                Destroy(Instantiate(drownEffect, other.gameObject.transform.position, other.gameObject.transform.rotation), 3.0f);
            }
        }
    }
}
