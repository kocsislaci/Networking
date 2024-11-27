using UnityEditor;
using UnityEngine;
using System.Linq;

public class ListMaterialsInFolder : MonoBehaviour
{
    public static string[] ListMaterials()
    {
        Material[] materials = Resources.LoadAll<Material>("TankMaterials/");
        foreach (var material in materials)
        {
            Debug.Log($"Materials in folder '{material.name}':");
        }

        return materials.Select(obj => obj.name).ToArray(); ;
    }
}
