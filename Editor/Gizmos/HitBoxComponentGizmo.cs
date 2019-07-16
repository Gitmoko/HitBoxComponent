using System.Linq;
using UnityEditor;
using UnityEngine;

public class CubeMesh : MonoBehaviour
{

    public static Mesh GetSquareMesh()
    {
        var mesh = new Mesh();

        mesh.vertices = (
            from direction in new[] { Vector3.up, Vector3.right, Vector3.forward }

            from normal in new[] { direction, -direction }
            from binormal in new[] { new Vector3(normal.z, normal.x, normal.y) }
            from tangent in new[] { Vector3.Cross(normal, binormal) }

            from binormal2 in new[] { binormal, -binormal }
            from tangent2 in new[] { tangent, -tangent }

            from vec in new[] { normal + binormal2 + tangent2 }

            select vec
        ).ToArray();

        mesh.triangles = (
            from iterator in Enumerable.Range(0, mesh.vertices.Length / 4)
            from index in new[] { 0, 2, 1, 1, 2, 3 }
            select (iterator * 4 + index) % mesh.vertices.Length
        ).ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}


public class HitBoxComponentGizmo
{
    [DrawGizmo(GizmoType.NonSelected | GizmoType.Active)]
    private static void DrawHitBoxComponentGizmo(HitBoxComponent target, GizmoType gizmoType)
    {
        if (target.gizmoController.drawGizmoData == null)
        {
            return;
        }

        var color = new Color32(0, 0, 255, 210);
        //GizmoType.Active の時は赤色にする
        if ((gizmoType & GizmoType.Active) == GizmoType.Active)
        {
            color = new Color(Color.red.r, Color.red.g, Color.red.b, 210);
        }
        var transform = target.gameObject.transform;
        foreach (var keyframe in target.gizmoController.drawGizmoData)
        {
            foreach (var e in keyframe.colliders)
            {
                if (e.colliderParam is RectColliderParam)
                {
                    var rect = e.colliderParam as RectColliderParam;
                    var position = new Vector3(rect.rect.position.x + transform.position.x, rect.rect.position.y + transform.position.y, transform.position.z);
                    DrawRectGizmo(position, rect.rect.size, color);
                }
                else if (e.colliderParam is SphereColliderParam)
                {
                    var sphere = e.colliderParam as SphereColliderParam;
                    var position = new Vector3(sphere.position.x + transform.position.x, sphere.position.y + transform.position.y, transform.position.z);

                    DrawSphereGizmo(position, sphere.radius, color);
                }
                else if (e.colliderParam is CapsuleColliderParam)
                {
                    var capsule = e.colliderParam as CapsuleColliderParam;
                    var start = new Vector3(capsule.start.x + transform.position.x, capsule.start.y + transform.position.y, transform.position.z);
                    var end = new Vector3(capsule.end.x + transform.position.x, capsule.end.y + transform.position.y, transform.position.z);

                    DrawCapsuleGizmo(start, end, capsule.radius, color);
                }
            }
        }
    }

    //回転無し立方体
    private static void DrawRectGizmo(Vector3 position, Vector2 size, Color32? col = null)
    {
        if (col == null)
        {
            col = new Color32(0, 0, 0, 255);
        }
        Gizmos.color = col.Value;

        Mesh mesh = CubeMesh.GetSquareMesh();

        Vector3 scale = new Vector3(size.x / 2.0f, size.y / 2.0f, 0.1f);
        position.x += scale.x;
        position.y += scale.y;

        Gizmos.DrawWireMesh(mesh, position, Quaternion.identity, scale);

    }

    //回転無し球
    private static void DrawSphereGizmo(Vector3 position, float radius, Color32? col = null)
    {
        if (col == null)
        {
            col = new Color32(0, 0, 0, 255);
        }
        Gizmos.color = col.Value;

        Gizmos.DrawWireSphere(position, radius);

    }


    //回転ありCapsule
    private static void DrawCapsuleGizmo(Vector3 start, Vector3 end, float radius, Color32? col = null)
    {

        if (col == null)
        {
            col = new Color32(0, 0, 0, 255);
        }
        Gizmos.color = col.Value;

        var preMatrix = Gizmos.matrix;

        // カプセル空間（(0, 0)からZ軸方向にカプセルが伸びる空間）からワールド座標系への変換行列
        Gizmos.matrix = Matrix4x4.TRS(start, Quaternion.FromToRotation(Vector3.forward, end - start), Vector3.one);

        // 球体を描画
        var distance = (end - start).magnitude;
        var capsuleStart = Vector3.zero;
        var capsuleEnd = Vector3.forward * distance;
        Gizmos.DrawWireSphere(capsuleStart, radius);
        Gizmos.DrawWireSphere(capsuleEnd, radius);

        // ラインを描画
        var offsets = new Vector3[] { new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f) };
        for (int i = 0; i < offsets.Length; i++)
        {
            Gizmos.DrawLine(capsuleStart + offsets[i] * radius, capsuleEnd + offsets[i] * radius);
        }

        Gizmos.matrix = preMatrix;
    }

    //任意Mesh
    private static void DrawMeshGizmo(Mesh mesh, Vector3 position, Quaternion rot, Vector3 scale, Color32? col = null)
    {
        if (col == null)
        {
            col = new Color32(0, 0, 0, 255);
        }
        Gizmos.color = col.Value;

        Gizmos.DrawWireMesh(mesh, position, Quaternion.identity, scale);

    }
}