﻿using UnityEngine;

public class PlayerCameraController : MonoBehaviour 
{
    private const float LERP_SPEED = 2f;

    [Header("Tweaking Variables:")]
    [SerializeField]
    private Vector3 camera_Offset = new Vector3(0, 11.06f, -11.05f);

    [Tooltip("Only supports 2 anchors, first index = Bottomleft, second index = Topright.")]
    [SerializeField]
    private Vector2[] boundries;

    [Header("References:")]
    [SerializeField]
    private Transform player_Target;

    [SerializeField]
    private Transform enemy_Base;

    #region ### Properties ###
    public float ZoomSpeed { get; set; } = 16;
    #endregion

    #region ### Private Variables ###
    private float zoomOffset;

    private Vector3 current_Target_Pos;
    private Vector3 last_Pos;

    private readonly KeyCode spyBaseKey = KeyCode.O;
    private readonly KeyCode zoomInKey = KeyCode.PageUp;
    private readonly KeyCode zoomOutKey = KeyCode.PageDown;
    #endregion


    private static bool WithinYBoundries(float y_pos1, float y_pos2, float y_yourPos) => (y_yourPos > y_pos1 && y_yourPos < y_pos2);

    private static bool WithinXBoundries(float x_pos1, float x_pos2, float x_yourPos) => (x_yourPos > x_pos1 && x_yourPos < x_pos2);

    private void Start() => transform.position = last_Pos + camera_Offset;

    private void FixedUpdate() 
    {
        MoveCamera();
        Zoom();
    }

    /// <summary>
    /// Takes care of zooming in with the camera.
    /// </summary>
    private void Zoom() 
    {
        const int MIN_ZOOM = -5;
        const int MAX_ZOOM = 13;

        zoomOffset += (Input.GetKey(zoomOutKey) ? ZoomSpeed : (Input.GetKey(zoomInKey) ? -ZoomSpeed : 0)) * Time.deltaTime;
        zoomOffset = Mathf.Clamp(zoomOffset, MIN_ZOOM, MAX_ZOOM);
    }

    /// <summary>
    /// Takes care of smoothly moving the camera.
    /// </summary>
    private void MoveCamera() 
    {
        DecideForTarget();

        if (boundries.Length == 2) 
        {
            last_Pos.x = WithinXBoundries(boundries[0].x, boundries[1].x, player_Target.position.x) ? player_Target.position.x : last_Pos.x;
            last_Pos.y = player_Target.position.y;
            last_Pos.z = WithinYBoundries(boundries[0].y, boundries[1].y, player_Target.position.z) ? player_Target.position.z : last_Pos.z;

            transform.position = Vector3.Lerp(transform.position, current_Target_Pos + (camera_Offset + (transform.forward * zoomOffset)), LERP_SPEED * Time.deltaTime);
        }
        else 
        {
            Debug.LogWarning("[PlayerCamera] 2 boundry points need to be defined for the camera to work.");
        }
    }

    /// <summary>
    /// Check for input, to decide wether to check the enemy base, or the area around the player.
    /// </summary>
    private void DecideForTarget() 
    {
        if (player_Target == null || enemy_Base == null)
        {
            Debug.LogWarning("[PlayerCameraController] No playerTarget or reference to the enemy base has been assigned, please assign them and try again.");
            enabled = false;
        }

        current_Target_Pos = Input.GetKey(spyBaseKey) ? enemy_Base.position : last_Pos;
    }

    /// <summary>
    /// Used for when instantiating a player within a scene, use this function when you want to declare scene specific camera boundries.
    /// </summary>
    /// <param name="bottomLeftAnchor"></param>
    /// <param name="topRightAnchor"></param>
    public void SetCameraBoundries(Vector2 bottomLeftAnchor, Vector2 topRightAnchor) => boundries = new Vector2[]
    {
        bottomLeftAnchor,
        topRightAnchor
    };

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (boundries.Length > 0) 
        {
            foreach (Vector2 pos in boundries)
            {
                Gizmos.DrawWireSphere(new Vector3(pos.x, 0, pos.y), 0.5f);
            }
        }
    }
#endif
}
