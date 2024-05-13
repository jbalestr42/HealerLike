using System.Collections;
using UnityEngine;

public class OrbeSkillSource : SkillSource
{
    [SerializeField] AnimationCurve _curve;
    [SerializeField] float _duration = 1f;
    [SerializeField] float _factor = 1f;
    Coroutine _animationCor;

    public override void OnUseSkill()
    {
        if (_animationCor == null)
        {
            _animationCor = StartCoroutine(StartAnimation());
        }
    }

    IEnumerator StartAnimation()
    {
        YieldInstruction yieldInstruction = new WaitForEndOfFrame();
        float timer = _duration;
        Vector3 originalScale = transform.localScale;
        while (timer >= 0f)
        {
            timer -= Time.deltaTime;
            transform.localScale = originalScale + Mathf.Clamp01(_curve.Evaluate(timer / _duration)) * Vector3.one * _factor;
            yield return yieldInstruction;
        }
        _animationCor = null;
    }
}