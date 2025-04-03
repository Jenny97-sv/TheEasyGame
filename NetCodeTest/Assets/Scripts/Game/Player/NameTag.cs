using TMPro;
using UnityEngine;
using Unity.Netcode;

public class NameTag : MonoBehaviour
{
    private TextMeshPro text;
    private Stats stats;
    private Transform localCameraTransform;

    private void Start()
    {
        text = GetComponent<TextMeshPro>();
        stats = GetComponentInParent<Stats>();

        localCameraTransform = Camera.main?.transform;
    }

    private void LateUpdate()
    {
        if (stats && stats.myName != null)
        {
            text.text = stats.myName.Value.ToString();
        }

        if (localCameraTransform != null)
        {
            Vector3 lookDir = localCameraTransform.position - transform.position;

            transform.rotation = Quaternion.LookRotation(-lookDir);

            Vector3 eulerAngles = transform.rotation.eulerAngles;
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }

        //foreach (GameObject player in GameManager.Instance.GetPlayers().Keys)
        //{
        //    TextMeshPro name = player.GetComponent<TextMeshPro>();
        //    Vector3 lookDir = name.transform.position - transform.position;

        //    // Create a rotation that looks in that direction
        //    transform.rotation = Quaternion.LookRotation(-lookDir);

        //    // Ensure text is upright (optional)
        //    Vector3 eulerAngles = transform.rotation.eulerAngles;
        //    eulerAngles.z = 0;
        //    player.GetComponentInChildren<TextMeshPro>().transform.rotation = Quaternion.Euler(eulerAngles);
        //}
    }
}