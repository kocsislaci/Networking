using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerColor
{
    [SerializeField] public string label;
    [SerializeField] public Material material;

    public static List<string> GetLabels(List<PlayerColor> colors)
    {
        var labels = new List<string>();
        foreach (var c in colors)
        {
            labels.Add(c.label);
        }

        return labels;
    }

    public static PlayerColor FindColor(List<PlayerColor> colors, string label)
    {
        foreach (var c in colors)
        {
            if (c.label == label)
            {
                return c;
            }
        }

        throw new MissingReferenceException();
    }
}
