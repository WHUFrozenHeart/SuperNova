using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGroundController : MonoBehaviour
{
    // �����ҳ�������ͬһ������
    // ��ô�ǻ���ж���ж���
    public float gapTime = 2.0f;
    public GameObject fromH2OToNone;
    public GameObject fromH2ToH2O;
    public Vector3 offset = new Vector3(0.0f, 0.5f, 0.0f);
    private bool ready;
    private float nextTime;

    private void Start()
    {
        ready = true;
    }

    private void Update()
    {
        if(ready == false && Time.time > nextTime)
        {
            ready = !ready;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(ready && other.gameObject.tag == "Player")
        {
            ready = false;
            nextTime = Time.time + gapTime;
            // ������ط�Ӧ
            ChemistryController chemistry = other.gameObject.GetComponent<ChemistryController>();
            ChemistryController.EffectName currentBuff = chemistry.GetCurrentBuff();
            if(currentBuff == ChemistryController.EffectName.NONE)
            {
                // û��buffֱ������
                other.gameObject.GetComponent<PlayerController>().PlayerDead("������С�Ļ���");
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Fire);
            }
            else if (currentBuff == ChemistryController.EffectName.H2O)
            {
                // ˮ����������
                chemistry.ChangeEffects(ChemistryController.EffectName.NONE);
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.FireH2O);
                UIController.Instance.AddScore(200);
                if (fromH2OToNone != null)
                {
                    Destroy(Instantiate(fromH2OToNone, other.gameObject.transform.position + offset, Quaternion.identity), 3.0f);
                }
            }
            else if(currentBuff == ChemistryController.EffectName.H2)
            {
                // �����������ˮ
                chemistry.ChangeEffects(ChemistryController.EffectName.H2O);
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.FireH2);
                UIController.Instance.AddScore(200);
                if (fromH2ToH2O)
                {
                    Destroy(Instantiate(fromH2ToH2O, other.gameObject.transform.position + offset, Quaternion.identity), 3.0f);
                }
            }
        }
    }
}
