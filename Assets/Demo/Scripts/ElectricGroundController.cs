using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricGroundController : MonoBehaviour
{
    // 如果玩家持续待在同一块上面
    // 那么是会进行多次判定的
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
            // 进行相关反应
            ChemistryController chemistry = other.gameObject.GetComponent<ChemistryController>();
            ChemistryController.EffectName currentBuff = chemistry.GetCurrentBuff();
            if (currentBuff == ChemistryController.EffectName.H2O)
            {
                // 水遇到电发生电解水
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
                // 其他情况直接死亡
                other.gameObject.GetComponent<PlayerController>().PlayerDead("有电危险，请勿触摸！");
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Electric);
            }
        }
    }
}
