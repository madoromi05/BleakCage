/// <summary>
///ドラッグアンドドロップ線
///</summary>
using UnityEngine;
using UnityEngine.EventSystems;
public class DragLineRenderer : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lineRenderer; 
    public Camera mainCamera; 


    [Header("Behavior")]
    public bool useScreenToWorld2D = true;
    public bool clampToPlane = false;
    public float raycastPlaneY = 0f;


    [Header("Appearance")]
    public int segmentCount = 2;
    public float smoothFactor = 0.15f;


    Vector3 targetStartWorld;
    Vector3 targetEndWorld;
    Vector3 currentStartWorld;
    Vector3 currentEndWorld;
    bool dragging = false;


    void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = segmentCount;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
        }
    }

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.positionCount = segmentCount;
        lineRenderer.enabled = false;
    }


    void Update()
    {
        if (!dragging) return;

        currentStartWorld = Vector3.Lerp(currentStartWorld, targetStartWorld, smoothFactor);
        currentEndWorld = Vector3.Lerp(currentEndWorld, targetEndWorld, smoothFactor);

        if (lineRenderer != null)
        {
            if (!lineRenderer.enabled) lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, currentStartWorld);
            lineRenderer.SetPosition(1, currentEndWorld);
        }
    }

    public void BeginDragFromScreenPos(Vector2 screenPos)
    {
        Vector3 world = ScreenToWorld(screenPos);
        BeginDragFromWorldPos(world);
    }
    public void BeginDragFromWorldPos(Vector3 worldPos)
    {
        dragging = true;
        targetStartWorld = worldPos;
        targetEndWorld = worldPos;
        currentStartWorld = worldPos;
        currentEndWorld = worldPos;


        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, currentStartWorld);
            lineRenderer.SetPosition(1, currentEndWorld);
        }
    }

    public void UpdateDragFromScreenPos(Vector2 screenPos)
    {
        Vector3 world = ScreenToWorld(screenPos);
        UpdateDragToWorldPos(world);
    }

    public void UpdateDragToWorldPos(Vector3 worldPos)
    {
        if (!dragging) return;
        targetEndWorld = worldPos;
    }


    public void EndDrag()
    {
        dragging = false;
        if (lineRenderer != null) lineRenderer.enabled = false;
    }


    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (mainCamera == null) return Vector3.zero;


        if (useScreenToWorld2D || mainCamera.orthographic)
        {
            Vector3 sp = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z));
            Vector3 wp = mainCamera.ScreenToWorldPoint(sp);
            if (clampToPlane)
            {
                wp.y = raycastPlaneY;
            }
            return wp;
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (clampToPlane)
            {
                float t = (raycastPlaneY - ray.origin.y) / ray.direction.y;
                return ray.GetPoint(t);
            }
            else
            {
                return ray.GetPoint(10f);
            }
        }
    }
}

