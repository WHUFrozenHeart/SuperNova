using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 利用AssetController
// 创建对应的prefab
public class PrefabController : SingletonController<PrefabController>
{
    public GameObject CreateGameObjectByPrefab(string name)
    {
        GameObject prefab = AssetController.Instance.GetAsset<GameObject>("Prefabs/" + name);
        GameObject gameObject = Instantiate(prefab);
        return gameObject;
    }

    public GameObject CreateGameObjectByPrefab(string name, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetController.Instance.GetAsset<GameObject>("Prefabs/" + name);
        GameObject gameObject = Instantiate(prefab);
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        return gameObject;
    }

    public GameObject CreateGameObjectByPrefab(string name, Transform parent)
    {
        GameObject prefab = AssetController.Instance.GetAsset<GameObject>("Prefabs/" + name);
        GameObject gameObject = Instantiate(prefab);
        gameObject.transform.SetParent(parent);
        return gameObject;
    }

    public GameObject CreateGameObjectByPrefab(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject prefab = AssetController.Instance.GetAsset<GameObject>("Prefabs/" + name);
        GameObject gameObject = Instantiate(prefab);
        gameObject.transform.SetParent(parent);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = localRotation;
        return gameObject;
    }
}
