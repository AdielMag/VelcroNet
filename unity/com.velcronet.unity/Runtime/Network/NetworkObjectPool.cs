using UnityEngine;

namespace VelcroNet
{
    /// <summary>
    /// Pre-warmed pool of networked entity GameObjects.
    /// Eliminates Instantiate() spikes when the server spawns entities.
    /// GetComponent is called once per slot at pre-warm time — never during gameplay.
    /// </summary>
    public sealed class NetworkObjectPool
    {
        private readonly GameObject[]     _objects;
        private readonly VelcroRigidbody[] _rigidbodies;
        private readonly bool[]           _active;
        private readonly int              _capacity;

        public int Capacity => _capacity;

        public NetworkObjectPool(GameObject prefab, int capacity, Transform? parent = null)
        {
            _capacity    = capacity;
            _objects     = new GameObject[capacity];
            _rigidbodies = new VelcroRigidbody[capacity];
            _active      = new bool[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _objects[i]     = Object.Instantiate(prefab, parent);
                _rigidbodies[i] = _objects[i].GetComponent<VelcroRigidbody>();
                _objects[i].SetActive(false);
            }
        }

        /// <summary>
        /// Rent a pooled object. Pass the expected entityId for O(1) access,
        /// or -1 for a linear scan of the first free slot.
        /// </summary>
        public VelcroRigidbody? Rent(int preferredIndex = -1)
        {
            if (preferredIndex >= 0 && preferredIndex < _capacity && !_active[preferredIndex])
            {
                Activate(preferredIndex);
                return _rigidbodies[preferredIndex];
            }

            for (int i = 0; i < _capacity; i++)
            {
                if (!_active[i])
                {
                    Activate(i);
                    return _rigidbodies[i];
                }
            }
            return null; // pool exhausted
        }

        public void Return(int index)
        {
            if ((uint)index >= (uint)_capacity) return;
            _active[index] = false;
            _objects[index].SetActive(false);
        }

        public void ReturnAll()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (_active[i]) Return(i);
            }
        }

        private void Activate(int index)
        {
            _active[index] = true;
            _objects[index].SetActive(true);
        }
    }
}
