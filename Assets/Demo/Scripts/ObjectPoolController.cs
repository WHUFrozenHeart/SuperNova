using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �����
// һ��������
// ���һ��������ҪƵ�������ɺ�����
// ��ô��ֱ�Ӷ���ؽ��з���
public class ObjectPoolController : SingletonController<ObjectPoolController>
{
    // ˮλ��
    // ���ȡ�߶���һ����Ʒ
    // ��ô����Ҫ��Ӧ��Ʒ���Ƽ�¼��һ
    // �������һ
    // ÿ��ȡ�ߺͻ���ʱ
    // ����һ���ķ�ʽ�����Ƿ���������Ӹ�����Ʒ
    // ���߼��ٳ����е���Ʒ
    private Dictionary<string, int> waterLines = new Dictionary<string, int>();

    // ����
    // ���ݶ������ƽ��д洢
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

    public GameObject GetGameObject(string objectName, bool isActive = true)
    {
        GameObject gotObject;
        // ��ȡ��Ӧ��Ʒ
        if(pools.ContainsKey(objectName) && pools[objectName].Count > 0)
        {
            gotObject = pools[objectName].Peek();
            pools[objectName].Dequeue();
            ++waterLines[objectName];
        }
        else
        {
            gotObject = PrefabController.Instance.CreateGameObjectByPrefab(objectName);
            // �涨����
            // ��Ȼ������޷���ȷ����
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
        // ȡ��Ʒ����ˮλ�����
        int addCount = waterLines[objectName];
        // �������ֵ
        // ��Ҫ��֤��ǰ�����е���Ʒ����������ͬ
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
        // ������Ʒ����ˮλ�߼���
        int subCount = waterLines[recycleName];
        // ���ݱ�����ص�ˮλ���㷨
        // ������Ԥ�ȷ����ڳ����е���Ʒ
        // ����ˮλ���ǲ�����ָ�ֵ��
        // ����������Ҫ������������д���
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
        // ���ܴ���û����Ʒ�����
        // ��ô����Ҫ��̬ж��
        if (pools[recycleName].Count == 0)
        {
            AssetController.Instance.UnloadUnusedAsset();
        }
    }
}
