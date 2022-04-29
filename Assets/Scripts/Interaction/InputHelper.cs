using UnityEngine;
using UnityEngine.EventSystems;

public static class InputHelper
{
    private static Camera _mainCamera;

    // Constructor
    private static Camera MainCamera
    {
        get
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            return _mainCamera;
        }
    }

    public static Vector2 MouseWorldPos => MainCamera.ScreenToWorldPoint(Input.mousePosition);

    public static bool MouseOverUIObject()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public static GameObject GetObjectUnderMouse2D(LayerMask mask)
    {
        var mouse = MouseWorldPos;
        var hit = Physics2D.GetRayIntersection(new Ray(new Vector3(mouse.x, mouse.y, -100), Vector3.forward),
            float.MaxValue, mask);
        if (hit.collider) return hit.collider.gameObject;
        return null;
    }

    public static bool AnyOfTheseKeysDown(params KeyCode[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (Input.GetKeyDown(keys[i]))
                return true;
        return false;
    }

    public static bool AnyOfTheseKeysHeld(params KeyCode[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (Input.GetKey(keys[i]))
                return true;
        return false;
    }
}