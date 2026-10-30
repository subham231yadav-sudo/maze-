using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Traps
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class TigerWallAligner : MonoBehaviour
    {
        [Header("Auto Alignment Settings")]
        [Tooltip("Automatically orient tiger face opposite to wall face when moved in Scene View")]
        [SerializeField] private bool autoAlignToWall = true;

        [Tooltip("Automatically snap position flush against the wall surface")]
        [SerializeField] private bool snapPositionToWall = true;

        [Tooltip("Distance from wall surface to mount the tiger head (meters)")]
        [SerializeField] private float wallOffset = 0.05f;

        [Tooltip("Maximum distance to search for a wall to snap to")]
        [SerializeField] private float searchRadius = 3.0f;

        [Tooltip("Default height above ground if placed near the floor")]
        [SerializeField] private float defaultMountHeight = 1.65f;

        [Tooltip("If true, retains current Y height when snapping. If false, uses defaultMountHeight")]
        [SerializeField] private bool preserveCustomHeight = true;

        [Tooltip("Flip facing direction 180 degrees")]
        [SerializeField] private bool flipDirection = false;

        private Vector3 lastRecordedPosition;
        private Quaternion lastRecordedRotation;

        public bool AutoAlignToWall
        {
            get => autoAlignToWall;
            set => autoAlignToWall = value;
        }

        private void OnEnable()
        {
            lastRecordedPosition = transform.position;
            lastRecordedRotation = transform.rotation;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Align on first drag-and-drop into scene
                EditorApplication.delayCall += () =>
                {
                    if (this != null && autoAlignToWall)
                    {
                        AlignToNearestWall(true);
                    }
                };
            }
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && autoAlignToWall)
            {
                // Only evaluate if position has meaningfully moved in editor
                if (Vector3.Distance(transform.position, lastRecordedPosition) > 0.02f)
                {
                    lastRecordedPosition = transform.position;
                    AlignToNearestWall(snapPositionToWall);
                }
            }
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && autoAlignToWall)
            {
                AlignToNearestWall(false);
            }
#endif
        }

        /// <summary>
        /// Detects the nearest wall, snaps flush against its surface, and rotates the tiger face
        /// so it looks directly OPPOSITE the wall face (out into the room / corridor).
        /// </summary>
        [ContextMenu("Snap to Wall & Face Opposite")]
        public bool AlignToNearestWall(bool snapPos = true)
        {
            Vector3 currentPos = transform.position;

            // 1. Find nearest wall in scene
            Collider nearestWallCol = null;
            Vector3 closestSurfacePt = Vector3.zero;
            float closestDist = float.MaxValue;

            // Search physics colliders nearby
            Collider[] hits = Physics.OverlapSphere(currentPos, searchRadius);
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                if (IsWallObject(hit.gameObject))
                {
                    Vector3 pt = hit.ClosestPoint(currentPos);
                    float d = Vector3.Distance(currentPos, pt);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        nearestWallCol = hit;
                        closestSurfacePt = pt;
                    }
                }
            }

            // Fallback: search all wall colliders if physics overlap returned nothing (common in edit mode)
            if (nearestWallCol == null)
            {
                var allCols = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var col in allCols)
                {
                    if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                    if (IsWallObject(col.gameObject))
                    {
                        Vector3 pt = col.ClosestPoint(currentPos);
                        float d = Vector3.Distance(currentPos, pt);
                        if (d < closestDist && d <= searchRadius * 1.5f)
                        {
                            closestDist = d;
                            nearestWallCol = col;
                            closestSurfacePt = pt;
                        }
                    }
                }
            }

            if (nearestWallCol == null)
            {
                return false;
            }

            Transform wallTr = nearestWallCol.transform;

            // 2. Determine wall outward normal (pointing out from the wall face toward corridor)
            Vector3 outwardNormal = Vector3.forward;

            if (nearestWallCol is BoxCollider box)
            {
                Vector3 localPos = wallTr.InverseTransformPoint(currentPos);
                Vector3 halfSize = Vector3.Scale(box.size * 0.5f, wallTr.lossyScale);

                // Check which face the tiger is closest to (X vs Z)
                float normZ = halfSize.z > 0.001f ? Mathf.Abs(localPos.z) / halfSize.z : 0f;
                float normX = halfSize.x > 0.001f ? Mathf.Abs(localPos.x) / halfSize.x : 0f;

                Vector3 localFaceNormal;
                if (normZ >= normX)
                {
                    localFaceNormal = new Vector3(0f, 0f, localPos.z >= 0f ? 1f : -1f);
                }
                else
                {
                    localFaceNormal = new Vector3(localPos.x >= 0f ? 1f : -1f, 0f, 0f);
                }

                outwardNormal = wallTr.TransformDirection(localFaceNormal);
                outwardNormal.y = 0f;
                outwardNormal.Normalize();

                // If point was embedded inside wall, re-project surface point to the outer face
                if (closestDist < 0.05f || Vector3.Distance(currentPos, closestSurfacePt) < 0.01f)
                {
                    Vector3 pushedLocal = localPos;
                    if (normZ >= normX)
                    {
                        pushedLocal.z = (localPos.z >= 0f ? 1f : -1f) * (box.size.z * 0.5f);
                    }
                    else
                    {
                        pushedLocal.x = (localPos.x >= 0f ? 1f : -1f) * (box.size.x * 0.5f);
                    }
                    closestSurfacePt = wallTr.TransformPoint(pushedLocal);
                }
            }
            else
            {
                // General collider normal estimation
                Vector3 diff = currentPos - wallTr.position;
                diff.y = 0f;

                float dotFwd = Vector3.Dot(diff, wallTr.forward);
                float dotRight = Vector3.Dot(diff, wallTr.right);

                if (Mathf.Abs(dotFwd) >= Mathf.Abs(dotRight))
                {
                    outwardNormal = dotFwd >= 0f ? wallTr.forward : -wallTr.forward;
                }
                else
                {
                    outwardNormal = dotRight >= 0f ? wallTr.right : -wallTr.right;
                }

                outwardNormal.y = 0f;
                outwardNormal.Normalize();
            }

            // Check if user requested flipped orientation
            if (flipDirection)
            {
                outwardNormal = -outwardNormal;
            }

            // 3. Compute target rotation: Tiger face points directly along outwardNormal
            Quaternion targetRotation = Quaternion.LookRotation(outwardNormal, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);

            // 4. Compute target position: flush against wall face
            Vector3 targetPosition = closestSurfacePt + outwardNormal * wallOffset;

            // Height handling
            if (preserveCustomHeight && currentPos.y > 0.5f)
            {
                targetPosition.y = currentPos.y;
            }
            else
            {
                targetPosition.y = defaultMountHeight;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(transform, "Auto Align Tiger to Wall");
            }
#endif

            transform.rotation = targetRotation;
            if (snapPos)
            {
                transform.position = targetPosition;
            }

            lastRecordedPosition = transform.position;
            lastRecordedRotation = transform.rotation;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
            }
#endif

            return true;
        }

        public void FlipFacingDirection()
        {
            flipDirection = !flipDirection;
            AlignToNearestWall(false);
        }

        private static bool IsWallObject(GameObject go)
        {
            if (go == null) return false;
            string n = go.name.ToLower();
            if (n.Contains("cube") || n.Contains("wall") || n.Contains("fence") || n.Contains("pillar"))
            {
                return true;
            }
            if (go.transform.parent != null && go.transform.parent.name.ToLower().Contains("maze"))
            {
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw outward facing arrow showing the flame spray direction
            Vector3 fireDir = transform.rotation * Quaternion.Euler(90f, 0f, 0f) * Vector3.forward;

            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.9f);
            Gizmos.DrawRay(transform.position, fireDir * 1.5f);
        }
    }
}
