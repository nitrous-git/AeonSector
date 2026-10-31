using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AeonSector/Playable Mech Catalog")]
public sealed class PlayableMechCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Archetype
    {
        public string Id;
        public UnitStats Stats;
        public CombatUnit Prefab;
    }
    public List<Archetype> Mechs = new();
    private void OnValidate()
    {
        var ids = new HashSet<string>();
        foreach (var mech in Mechs)
            if (mech == null || string.IsNullOrWhiteSpace(mech.Id) || !ids.Add(mech.Id)
                || mech.Stats == null || mech.Prefab == null || mech.Prefab.Stats != mech.Stats)
                Debug.LogWarning($"{name}: catalog requires unique IDs and matching stats/prefab references.", this);
    }
}
