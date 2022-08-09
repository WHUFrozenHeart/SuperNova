using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricGroundController : MonoBehaviour
{
    // �����ҳ�������ͬһ������
    // ��ô�ǻ���ж���ж���
    public float gapTime = 2.0f;
    public GameObject fromH2OToH2;
    private bool ready;
    private float nextTime;

    private void Start()
    {
        ready = true;
    }

    private void Update()
    {
        if (ready == false && Time.time > nextTime)
        {
            ready = !ready;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (ready && other.gameObject.tag == "Player")
        {
            ready = false;
            nextTime = Time.time + gapTime;
            // ������ط�Ӧ
            ChemistryController chemistry = other.gameObject.GetComponent<ChemistryController>();
            ChemistryController.EffectName currentBuff = chemistry.GetCurrentBuff();
            if (currentBuff == ChemistryController.EffectName.H2O)
            {
                // ˮ�����緢�����ˮ
                chemistry.ChangeEffects(ChemistryController.EffectName.H2);
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.ElectricH2O);
                UIController.Instance.AddScore(200);
                if (fromH2OToH2 != null)
                {
                    Destroy(Instantiate(fromH2OToH2, other.gameObject.transform.position, other.gameObject.transform.rotation), 3.0f);
                }
            }
            else
            {
                // �������ֱ������
                other.gameObject.GetComponent<PlayerController>().PlayerDead("�е�Σ�գ���������");
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Electric);
            }
        }
    }
}
