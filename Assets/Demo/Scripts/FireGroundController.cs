using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGroundController : MonoBehaviour
{
    // 如果玩家持续待在同一块上面
    // 那么是会进行多次判定的
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
            // 进行相关反应
            ChemistryController chemistry = other.gameObject.GetComponent<ChemistryController>();
            ChemistryController.EffectName currentBuff = chemistry.GetCurrentBuff();
            if(currentBuff == ChemistryController.EffectName.NONE)
            {
                // 没有buff直接死亡
                other.gameObject.GetComponent<PlayerController>().PlayerDead("天干物燥，小心火烛。");
                AudioController.Instance.PlayAudioEffect(AudioController.AudioEffct.Fire);
            }
            else if (currentBuff == ChemistryController.EffectName.H2O)
            {
                // 水遇到火蒸发
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
                // 氢气遇到火变水
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
