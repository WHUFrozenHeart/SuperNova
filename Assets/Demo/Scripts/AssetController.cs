using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����̬���غ�ж����Դ
public class AssetController : SingletonController<AssetController>
{
    // �������ƴ洢��Ӧ��Դ
    private Dictionary<string, Object> assetCache = new Dictionary<string, Object>();

    public virtual T GetAsset<T>(string path) where T : Object
    {
        if(assetCache.ContainsKey(path))
        {
            return assetCache[path] as T;
        }
        else
        {
            T asset = Resources.Load<T>(path);
            if(asset == null)
            {
                // Ӧ�ý��б�����
                Debug.Log("δ�ҵ������Դ");
            }
            assetCache.Add(path, asset);
            return asset;
        }
    }

    public void UnloadUnusedAsset()
    {
        Resources.UnloadUnusedAssets();
    }
}
