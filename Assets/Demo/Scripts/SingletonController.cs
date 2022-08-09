using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ������ĸ���
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
                    // �����ֻ�ܹ�����һ��������
                    // ����Ͳ��ǵ���ģʽ
                    Debug.Log("singleton failed.");
                }
                instance = FindObjectOfType<T>();
                // ���û�й���
                // �Ǿ��Լ�����һ����Ʒ���й���
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
