using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChemistryController : MonoBehaviour
{
    public enum EffectName
    {
        NONE,
        H2O,
        H2
    }

    [System.Serializable]
    public struct EffectGameObject
    {
        public EffectName name;
        public GameObject effect;
    }

    // �洢������Ч���ı���
    public List<EffectGameObject> effectList;
    // �洢Ч������
    // ������ײ���ߵı�ǩ����ʹ�õ���Ч
    private Dictionary<EffectName, GameObject> effectDictionary;
    private GameObject currentEffect;
    // ��ǰ��buff
    // �����ͽ�������
    private EffectName currentBuff;

    private void Start()
    {
        effectDictionary = new Dictionary<EffectName, GameObject>();
        foreach(EffectGameObject effect in effectList)
        {
            effectDictionary[effect.name] = effect.effect;
        }
        currentEffect = null;
        currentBuff = EffectName.NONE;
    }

    // ���ͨ���ú����ı���ҵ�buff����Ч
    public void ChangeEffects(EffectName effectTag)
    {
        if(currentEffect != null)
        {
            Destroy(currentEffect.gameObject);
            currentEffect = null;
        }
        if(effectTag != EffectName.NONE)
        {
            currentEffect = Instantiate(effectDictionary[effectTag], transform);
            currentEffect.transform.position += GetComponent<CapsuleCollider>().center;
        }
        currentBuff = effectTag;
    }

    // ����ȡ��ǰ��ҵ�buff״̬
    // �������Լ������������ı���ҵ�buff״̬
    public EffectName GetCurrentBuff()
    {
        return currentBuff;
    }
}
