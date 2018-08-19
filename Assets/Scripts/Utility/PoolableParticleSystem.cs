using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoolableParticleSystem : MonoBehaviour
{
    private ParticleSystem[] _ps;

    void Awake()
    {
        _ps = GetComponentsInChildren<ParticleSystem>();
    }

    void Update()
    {
        var setInactive = false;
        for (var i = 0; i < _ps.Length; i++)
        {
            var ps = _ps[i];
            if (!ps.IsAlive())
            {
                ps.Clear();
                setInactive = true;
                continue;
            }

            if (!ps.isPlaying)
            {
                ps.Play();
            }
        }

        if (setInactive)
        {
            gameObject.SetActive(false);
        }
    }
}