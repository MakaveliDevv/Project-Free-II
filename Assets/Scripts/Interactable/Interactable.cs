using Assets.Scripts.Player;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    private BoxMover selectedBox;
    private Color selectedOriginalColor;
    private Player player;

    private void Awake()
    {
        player = FindAnyObjectByType<Player>();
    }

    public void UpdateHighlight(BoxMover bm)
    {
        if (selectedBox == bm) return;
        ClearHighlight();
        if (bm == null) return;
        selectedBox = bm;
        if (bm.transform.GetChild(0).TryGetComponent<Renderer>(out var r))
        {
            selectedOriginalColor = r.material.color;
            r.material.color = Color.blue;
        }
    }

    public void ClearHighlight()
    {
        if (selectedBox == null) return;
        if (selectedBox.transform.GetChild(0).TryGetComponent<Renderer>(out var r)) r.material.color = selectedOriginalColor;
        selectedBox = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, player.interactionSettings.interactionRadius);
    }
}
