using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class RandomColor : NetworkBehaviour
{
    [SerializeField] private Material[] colors;
    private Renderer[] renderers;
    private List<Renderer> rendererList = new List<Renderer>();

    private NetworkVariable<int> colorIndex = new NetworkVariable<int>(0);

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.name != "Name") // Otherwise text gets fucked!
            {
                rendererList.Add(renderer);
            }
        }

        if (SceneHandler.Instance.IsLocalGame)
        {
            colorIndex.Value = Random.Range(0, colors.Length);
            ApplyColor(colorIndex.Value); // ✅ Apply correct color when spawned
            colorIndex.OnValueChanged -= (oldValue, newValue) => ApplyColor(newValue);
            colorIndex.OnValueChanged += (oldValue, newValue) => ApplyColor(newValue);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) // ✅ Only the server picks a color
        {
            colorIndex.Value = Random.Range(0, colors.Length);
        }

        ApplyColor(colorIndex.Value); // ✅ Apply correct color when spawned
        colorIndex.OnValueChanged -= (oldValue, newValue) => ApplyColor(newValue);
        colorIndex.OnValueChanged += (oldValue, newValue) => ApplyColor(newValue);
    }

    public override void OnNetworkDespawn()
    {
        colorIndex.OnValueChanged -= (oldValue, newValue) => ApplyColor(newValue);
    }

    private void ApplyColor(int index)
    {
        if (index < 0 || index >= colors.Length) return;

        foreach (Renderer rend in rendererList)
        {
            rend.material = colors[index];
        }
    }
}
