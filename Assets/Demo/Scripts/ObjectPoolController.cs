using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 对象池
// 一个单例类
// 如果一个物体需要频繁地生成和销毁
// 那么会直接对象池进行访问
public class ObjectPoolController : SingletonController<ObjectPoolController>
{
    // 水位线
    // 如果取走队列一个物品
    // 那么就需要对应物品名称记录加一
    // 回收则减一
    // 每次取走和回收时
    // 根据一定的方式决定是否向对象池添加更多物品
    // 或者减少池子中的物品
    private Dictionary<string, int> waterLines = new Dictionary<string, int>();

    // 池子
    // 根据对象名称进行存储
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

    public GameObject GetGameObject(string objectName, bool isActive = true)
    {
        GameObject gotObject;
        // 获取对应物品
        if(pools.ContainsKey(objectName) && pools[objectName].Count > 0)
        {
            gotObject = pools[objectName].Peek();
            pools[objectName].Dequeue();
            ++waterLines[objectName];
        }
        else
        {
            gotObject = PrefabController.Instance.CreateGameObjectByPrefab(objectName);
            // 规定名字
            // 不然对象池无法正确回收
            gotObject.name = objectName;
            if(pools.ContainsKey(objectName) == false)
            {
                pools.Add(objectName, new Queue<GameObject>());
                waterLines.Add(objectName, 1);
            }
            else
            {
                ++waterLines[objectName];
            }
        }
        // 取物品根据水位线添加
        int addCount = waterLines[objectName];
        // 如果是正值
        // 需要保证当前队列中的物品至少与其相同
        if(addCount > 0 && addCount > pools[objectName].Count)
        {
            addCount -= pools[objectName].Count;
            for(int i = 0;i < addCount;++i)
            {
                GameObject preObject = PrefabController.Instance.CreateGameObjectByPrefab(objectName);
                preObject.name = objectName;
                preObject.SetActive(false);
                pools[objectName].Enqueue(preObject);
            }
        }
        if(isActive)
        {
            gotObject.SetActive(true);
        }
        return gotObject;
    }

    public void RecycleGameObject(GameObject recycleObject)
    {
        recycleObject.SetActive(false);
        string recycleName = recycleObject.name;
        if (pools.ContainsKey(recycleName))
        {
            pools[recycleName].Enqueue(recycleObject);
            --waterLines[recycleName];
        }
        else
        {
            pools.Add(recycleName, new Queue<GameObject>());
            waterLines.Add(recycleName, -1);
        }
        // 回收物品根据水位线减少
        int subCount = waterLines[recycleName];
        // 根据本对象池的水位线算法
        // 除非是预先放置在场景中的物品
        // 否则水位线是不会出现负值的
        // 不过还是需要对两种情况进行处理
        if(subCount < 0)
        {
            subCount = Mathf.Min(-subCount, pools[recycleName].Count);
            waterLines[recycleName] += subCount;
            for (int i = 0; i < subCount; ++i)
            {
                Destroy(pools[recycleName].Dequeue());
            }
        }
        else if (subCount * 2 < pools[recycleName].Count)
        {
            subCount = pools[recycleName].Count - subCount;
            for (int i = 0; i < subCount; ++i)
            {
                Destroy(pools[recycleName].Dequeue());
            }
        }
        // 可能存在没有物品的情况
        // 那么就需要动态卸载
        if (pools[recycleName].Count == 0)
        {
            AssetController.Instance.UnloadUnusedAsset();
        }
    }
}
