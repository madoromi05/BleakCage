using UnityEngine;
using UnityEngine.Pool;

public abstract class PoolManager<T> : MonoBehaviour where T : MonoBehaviour, IPooledObject<T>
{
    [SerializeField] private T pooledPrefab;
    protected IObjectPool<T> _objectPool;

    [SerializeField] private bool _collectionCheck = true;

    // プールの初期サイズ(Deckのsize)
    [SerializeField] private int _defaultCapacity = 42;
    [SerializeField] private int _maxSize = 100;

    public virtual void Initialize()
    {
        _objectPool = new ObjectPool<T>(Create, OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
            _collectionCheck, _defaultCapacity, _maxSize);
    }

    protected virtual T Create()
    {
        T instance = Instantiate(pooledPrefab, transform.position, Quaternion.identity, transform);
        instance.ObjectPool = _objectPool;
        return instance;
    }

    protected virtual void OnReleaseToPool(T pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }

    protected virtual void OnGetFromPool(T pooledObject)
    {
        pooledObject.gameObject.SetActive(true);
    }

    protected virtual void OnDestroyPooledObject(T pooledObject)
    {
        pooledObject.Deactivate();
        Destroy(pooledObject.gameObject);
    }
}

