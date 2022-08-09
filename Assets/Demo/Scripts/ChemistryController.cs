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

    // 存储外界添加效果的变量
    public List<EffectGameObject> effectList;
    // 存储效果对象
    // 根据碰撞道具的标签决定使用的特效
    private Dictionary<EffectName, GameObject> effectDictionary;
    private GameObject currentEffect;
    // 当前的buff
    // 与地面和进行联动
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

    // 外界通过该函数改变玩家的buff和特效
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

    // 外界获取当前玩家的buff状态
    // 外界根据自己的条件决定改变玩家的buff状态
    public EffectName GetCurrentBuff()
    {
        return currentBuff;
    }
}
