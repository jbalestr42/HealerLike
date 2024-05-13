using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DestroyOnDone : MonoBehaviour
{
    IEnumerator Start()
    {
        VisualEffect visualEffect = GetComponentInChildren<VisualEffect>();

        yield return new WaitForSeconds(1f);

        while (visualEffect.aliveParticleCount > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }
}
