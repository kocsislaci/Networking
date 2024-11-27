using UnityEngine;
using System.Collections.Generic;

public class ListMaterialsInFolder : MonoBehaviour
{
    public static List<PlayerColor> ListMaterials()
    {
        Material[] materials = Resources.LoadAll<Material>("TankMaterials/");

        List<PlayerColor> playerColors = new List<PlayerColor>();

        foreach (var m in materials)
        {
            playerColors.Add(new PlayerColor(m));
        }

        return playerColors;
    }
}
