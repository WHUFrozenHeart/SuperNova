using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 单例类的父类
public class SingletonController<T> : MonoBehaviour where T : SingletonController<T>
{
    private static T instance = default;

    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                if(FindObjectsOfType<T>().Length > 1)
                {
                    // 对象池只能挂载在一个物体上
                    // 否则就不是单例模式
                    Debug.Log("singleton failed.");
                }
                instance = FindObjectOfType<T>();
                // 如果没有挂在
                // 那就自己创建一个物品进行挂载
                if(instance == null)
                {
                    GameObject gameObject = new GameObject();
                    gameObject.name = typeof(T).ToString();
                    instance = gameObject.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    public bool IsInstanced
    {
        get
        {
            return instance != null;
        }
    }

    protected virtual void Awake()
    {
        if(instance == null)
        {
            instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }
}
