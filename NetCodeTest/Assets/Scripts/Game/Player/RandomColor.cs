using UnityEngine;
using Unity.Netcode;

public class RandomColor : NetworkBehaviour
{
    [SerializeField] private Material[] colors;
    private Renderer[] renderers;

    private NetworkVariable<int> colorIndex = new NetworkVariable<int>(0);

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
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

        foreach (Renderer rend in renderers)
        {
            rend.material = colors[index];
        }
    }
}
