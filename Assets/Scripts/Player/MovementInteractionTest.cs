// using UnityEngine;

// namespace Assets.Scripts.Player
// {
//     public class MovementInteractionTests
//     {
//         readonly MovementSettings settings;
//         readonly Player player;

//         readonly float halfYawArcDeg;
//         readonly float sliceYawDeg;
//         readonly float maxPitchDeg;

//         Vector3 snappedDir3D = Vector3.zero;
//         public Vector3 SnappedDir3D => snappedDir3D;

//         public MovementInteraction(Player player, MovementSettings settings)
//         {
//             this.player = player;
//             this.settings = settings;

//             halfYawArcDeg = settings.selectionArcDegrees / 2f;
//             // ← use directionCount2 from your Settings class
//             sliceYawDeg = settings.selectionArcDegrees / settings.directionCount2;
//             maxPitchDeg = settings.selectionPitchDegrees;

//             // rebuild your 360°↔label map if you need it
//             // player.movementController.advancedMovement.BuildLabelToAngleMap();
//         }

//         Vector3 GetSnappedYawDir(Vector2 stick)
//         {
//             if (stick.sqrMagnitude < settings.minStickMagnitude * settings.minStickMagnitude)
//                 return Vector3.zero;

//             float rawYaw = Mathf.Atan2(stick.x, stick.y) * Mathf.Rad2Deg;
//             float snapped = Mathf.Round(rawYaw / sliceYawDeg) * sliceYawDeg;
//             return (Quaternion.Euler(0f, snapped, 0f) * Vector3.forward).normalized;
//         }

//         /// <summary>
//         /// Call every frame. Combines:
//         /// - right‐stick → snapped yaw within ±halfYawArcDeg
//         /// - left‐stick Y → pitch ±maxPitchDeg
//         /// </summary>
//         public void Update()
//         {
//             Vector2 r = InputManager.RightStickInput;
//             Vector2 l = InputManager.LeftStickInput;

//             bool noYaw = r.sqrMagnitude < settings.minStickMagnitude * settings.minStickMagnitude;
//             bool noPitch = Mathf.Abs(l.y) < settings.minStickMagnitude;
//             if (noYaw && noPitch)
//             {
//                 snappedDir3D = Vector3.zero;
//                 return;
//             }

//             // 1) Yaw
//             Vector3 yawDir = GetSnappedYawDir(r);
//             if (yawDir == Vector3.zero)
//                 yawDir = player.transform.forward;

//             // clamp to front‐arc
//             float rawYaw = Mathf.Atan2(r.x, r.y) * Mathf.Rad2Deg;
//             rawYaw = Mathf.DeltaAngle(0f, rawYaw);
//             if (Mathf.Abs(rawYaw) > halfYawArcDeg)
//             {
//                 snappedDir3D = Vector3.zero;
//                 return;
//             }

//             // 2) Pitch
//             float pitchInput = Mathf.Clamp(l.y, -1f, 1f);
//             float pitchDeg = pitchInput * maxPitchDeg;
//             Vector3 rightAx = Vector3.Cross(Vector3.up, yawDir).normalized;
//             snappedDir3D = (Quaternion.AngleAxis(-pitchDeg, rightAx) * yawDir).normalized;
//         }

//         /// <summary>
//         /// Sphere‐casts out along snappedDir3D; distance = range * |rightStick|.
//         /// Returns the nearest BoxMover in front (Z>origin.z) and within groundBounds.
//         /// </summary>
//         public BoxMover SelectTargetCone(
//             Vector3 origin,
//             float range,
//             float coneRadius,
//             Collider groundBounds)
//         {
//             if (snappedDir3D == Vector3.zero) return null;
//             float mag = Mathf.Clamp01(InputManager.RightStickInput.magnitude);
//             float dist = range * mag;

//             var hits = Physics.SphereCastAll(origin, coneRadius, snappedDir3D, dist);
//             System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

//             foreach (var h in hits)
//             {
//                 var bm = h.collider.GetComponent<BoxMover>();
//                 if (bm == null) continue;
//                 var p = bm.transform.position;
//                 if (p.z <= origin.z) continue;
//                 if (groundBounds != null)
//                 {
//                     float zmin = groundBounds.bounds.min.z;
//                     float zmax = groundBounds.bounds.max.z;
//                     if (p.z < zmin || p.z > zmax) continue;
//                 }
//                 return bm;
//             }
//             return null;
//         }

//         /// <summary>
//         /// Draws a little sphere at the cone tip and the central ray.
//         /// </summary>
//         public void OnDrawGizmos(Vector3 origin, float range, float coneRadius)
//         {
//             Gizmos.color = Color.green;
//             var halfExt = player.intSettings.selectionBoxSize * 0.5f;
//             var center = origin + player.transform.forward * halfExt.z;
//             Gizmos.matrix = Matrix4x4.TRS(center, player.transform.rotation, Vector3.one);
//             Gizmos.DrawWireCube(Vector3.zero, player.intSettings.selectionBoxSize);
//             Gizmos.matrix = Matrix4x4.identity;

//             if (snappedDir3D == Vector3.zero) return;
//             float mag = Mathf.Clamp01(InputManager.RightStickInput.magnitude);
//             float dist = range * mag;

//             Gizmos.color = Color.green;
//             Gizmos.DrawWireSphere(origin + snappedDir3D * dist, coneRadius);

//             Gizmos.color = Color.cyan;
//             Gizmos.DrawLine(origin, origin + snappedDir3D * dist);
//         }
//     }
// }