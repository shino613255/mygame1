using System.Collections.Generic;
using UnityEngine;

public sealed class SkillVfxController : MonoBehaviour
{
    private readonly List<GameObject> effects = new List<GameObject>();
    public int ActiveEffectCount { get { effects.RemoveAll(e => e == null); return effects.Count; } }

    public GameObject Play(SkillData skill, Vector3 position)
    {
        if (skill == null || skill.effectPrefab == null) return null;
        position += (Vector3)skill.effectOffset; position.z = 0;
        GameObject effect = Instantiate(skill.effectPrefab, position, skill.effectPrefab.transform.rotation, transform);
        foreach (Renderer renderer in effect.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingLayerName = "Default"; renderer.sortingOrder = 10;
        }
        effects.Add(effect);
        Destroy(effect, Mathf.Max(.1f, skill.effectDuration));
        return effect;
    }

    public void Clear()
    {
        foreach (GameObject effect in effects) if (effect != null) Destroy(effect);
        effects.Clear();
    }

    private void OnDisable() => Clear();
}
