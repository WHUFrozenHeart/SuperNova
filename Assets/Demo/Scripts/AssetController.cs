using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 负责动态加载和卸载资源
public class AssetController : SingletonController<AssetController>
{
    // 根据名称存储对应资源
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
                // 应该进行报错处理
                Debug.Log("未找到相关资源");
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
