using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "StatusIconDatabase", menuName = "Data/StatusIconDatabase")]
public class StatusIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class StatusIconData
    {
        public StatusEffectType type;
        public Sprite icon;
    }

    [SerializeField] private List<StatusIconData> iconList = new List<StatusIconData>();

    public Sprite GetIcon(StatusEffectType type)
    {
        var data = iconList.FirstOrDefault(x => x.type == type);
        return data != null ? data.icon : null;
    }
}