using UnityEngine;

public class DepthSorter : MonoBehaviour
{
    [Header("Config")]
    public Transform sortingPoint;
    public bool useSortingPoint = true;

    [Header("Performance")]
    public bool updateOnlyOnMove = true;
    public float updateThreshold = 0.01f;

    private SpriteRenderer[] renderers;
    private float lastY;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        UpdateSorting();
    }

    void LateUpdate()
    {
        float currentY = GetY();

        if (updateOnlyOnMove && Mathf.Abs(currentY - lastY) < updateThreshold)
            return;

        UpdateSorting();
        lastY = currentY;
    }

    float GetY()
    {
        if (useSortingPoint && sortingPoint != null)
            return sortingPoint.position.y;

        return transform.position.y;
    }

    void UpdateSorting()
    {
        int order = Mathf.RoundToInt(-GetY() * 100);

        foreach (var sr in renderers)
        {
            sr.sortingOrder = order;
        }
    }
}