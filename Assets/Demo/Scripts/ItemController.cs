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
        // ��ײ�Ķ�����Player�Ž�����ز���
        if(other.gameObject.tag == "Player")
        {
            // �ı��������ϵ���Ч
            other.GetComponent<ChemistryController>().ChangeEffects(effectTag);
            AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.PickUp);
            // ���һ��ʰȡ��Ч
            // ����ʱ�ݻ�
            if (pickEffect != null)
            {
                Destroy(Instantiate(pickEffect, transform.position, transform.rotation), effectTime);
            }
            Destroy(gameObject);
        }
    }
}
