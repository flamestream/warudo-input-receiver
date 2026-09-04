using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlameStream
{
    public static class VirtualKeyMeshBuilder
    {
        /// <summary>
        /// Generates a flat 2D rounded rectangle mesh on the X-Z plane with normal facing +Y.
        /// </summary>
        public static Mesh CreateRoundedRectMesh(float width, float height, float radius, int cornerSegments = 8) {
            var mesh = new Mesh { name = "VirtualKey_BodyMesh" };

            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);
            cornerSegments = Mathf.Max(2, cornerSegments);

            var perimeter = GeneratePerimeterPoints(width, height, radius, cornerSegments);
            int count = perimeter.Count;

            var vertices = new Vector3[count + 1];
            var uvs = new Vector2[count + 1];
            var normals = new Vector3[count + 1];

            // Center vertex
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            normals[0] = Vector3.up;

            for (int i = 0; i < count; i++) {
                vertices[i + 1] = perimeter[i];
                uvs[i + 1] = new Vector2(perimeter[i].x / width + 0.5f, perimeter[i].z / height + 0.5f);
                normals[i + 1] = Vector3.up;
            }

            var triangles = new int[count * 3 * 2];
            for (int i = 0; i < count; i++) {
                int next = (i + 1) % count;
                // Front face
                triangles[i * 6 + 0] = 0;
                triangles[i * 6 + 1] = i + 1;
                triangles[i * 6 + 2] = next + 1;
                // Back face (no backface culling)
                triangles[i * 6 + 3] = 0;
                triangles[i * 6 + 4] = next + 1;
                triangles[i * 6 + 5] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a flat 2D hollow rounded rectangle border frame mesh on the X-Z plane.
        /// </summary>
        public static Mesh CreateRoundedRectFrameMesh(float width, float height, float radius, float borderWidth, int cornerSegments = 8) {
            var mesh = new Mesh { name = "VirtualKey_FrameMesh" };

            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);
            borderWidth = Mathf.Clamp(borderWidth, 0.0005f, Mathf.Min(width, height) * 0.5f - 0.001f);
            cornerSegments = Mathf.Max(2, cornerSegments);

            float innerWidth = Mathf.Max(0.001f, width - 2f * borderWidth);
            float innerHeight = Mathf.Max(0.001f, height - 2f * borderWidth);
            float innerRadius = Mathf.Max(0f, radius - borderWidth);

            var outerPerimeter = GeneratePerimeterPoints(width, height, radius, cornerSegments);
            var innerPerimeter = GeneratePerimeterPoints(innerWidth, innerHeight, innerRadius, cornerSegments);
            int count = outerPerimeter.Count;

            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var normals = new Vector3[count * 2];

            for (int i = 0; i < count; i++) {
                // Outer vertex: index i
                vertices[i] = outerPerimeter[i];
                uvs[i] = new Vector2(outerPerimeter[i].x / width + 0.5f, outerPerimeter[i].z / height + 0.5f);
                normals[i] = Vector3.up;

                // Inner vertex: index count + i
                vertices[count + i] = innerPerimeter[i];
                uvs[count + i] = new Vector2(innerPerimeter[i].x / width + 0.5f, innerPerimeter[i].z / height + 0.5f);
                normals[count + i] = Vector3.up;
            }

            var triangles = new int[count * 6 * 2];
            for (int i = 0; i < count; i++) {
                int next = (i + 1) % count;

                int oCurrent = i;
                int oNext = next;
                int iCurrent = count + i;
                int iNext = count + next;

                // Triangle 1: Front face
                triangles[i * 12 + 0] = oCurrent;
                triangles[i * 12 + 1] = oNext;
                triangles[i * 12 + 2] = iNext;

                // Triangle 2: Front face
                triangles[i * 12 + 3] = oCurrent;
                triangles[i * 12 + 4] = iNext;
                triangles[i * 12 + 5] = iCurrent;

                // Triangle 1: Back face (no backface culling)
                triangles[i * 12 + 6] = oCurrent;
                triangles[i * 12 + 7] = iNext;
                triangles[i * 12 + 8] = oNext;

                // Triangle 2: Back face (no backface culling)
                triangles[i * 12 + 9] = oCurrent;
                triangles[i * 12 + 10] = iCurrent;
                triangles[i * 12 + 11] = iNext;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        private static List<Vector3> GeneratePerimeterPoints(float width, float height, float radius, int segmentsPerCorner) {
            var points = new List<Vector3>(segmentsPerCorner * 4);

            float hw = width * 0.5f;
            float hh = height * 0.5f;

            if (radius <= 0.0001f) {
                // Sharp rectangle
                points.Add(new Vector3(hw, 0f, hh));
                points.Add(new Vector3(-hw, 0f, hh));
                points.Add(new Vector3(-hw, 0f, -hh));
                points.Add(new Vector3(hw, 0f, -hh));
                return points;
            }

            float cx = hw - radius;
            float cz = hh - radius;

            // Helper to generate an arc around (center.x, center.z) from startAngle to endAngle
            void AddArc(Vector2 center, float startAngleRad, float endAngleRad) {
                for (int i = 0; i < segmentsPerCorner; i++) {
                    float t = (float)i / segmentsPerCorner;
                    float angle = Mathf.Lerp(startAngleRad, endAngleRad, t);
                    points.Add(new Vector3(
                        center.x + Mathf.Cos(angle) * radius,
                        0f,
                        center.y + Mathf.Sin(angle) * radius
                    ));
                }
            }

            // Quadrant 1: Top-Right (0 to PI/2)
            AddArc(new Vector2(cx, cz), 0f, Mathf.PI * 0.5f);

            // Quadrant 2: Top-Left (PI/2 to PI)
            AddArc(new Vector2(-cx, cz), Mathf.PI * 0.5f, Mathf.PI);

            // Quadrant 3: Bottom-Left (PI to 3*PI/2)
            AddArc(new Vector2(-cx, -cz), Mathf.PI, Mathf.PI * 1.5f);

            // Quadrant 4: Bottom-Right (3*PI/2 to 2*PI)
            AddArc(new Vector2(cx, -cz), Mathf.PI * 1.5f, Mathf.PI * 2f);

            return points;
        }
    }
}
