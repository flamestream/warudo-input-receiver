using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlameStream
{
    public static class VirtualKeyMeshBuilder
    {
        /// <summary>
        /// Generates a flat 2D rounded rectangle mesh on the X-Z plane with uniform corner radius.
        /// </summary>
        public static Mesh CreateRoundedRectMesh(float width, float height, float radius, int cornerSegments = 8) {
            return CreateRoundedRectMesh(width, height, BorderVector4Extensions.Uniform(radius), cornerSegments);
        }

        /// <summary>
        /// Generates a flat 2D rounded rectangle mesh on the X-Z plane with independent 4-corner radii (TL, TR, BR, BL).
        /// </summary>
        public static Mesh CreateRoundedRectMesh(float width, float height, Vector4 cornerRadii, int cornerSegments = 8) {
            var mesh = new Mesh { name = "VirtualKey_BodyMesh" };

            cornerSegments = Mathf.Max(2, cornerSegments);
            var perimeter = GeneratePerimeterPoints(width, height, cornerRadii, cornerSegments);
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
                // Front face (visible from above)
                triangles[i * 6 + 0] = 0;
                triangles[i * 6 + 1] = i + 1;
                triangles[i * 6 + 2] = next + 1;
                // Back face (visible from underneath)
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
        /// Generates a flat 2D hollow rounded rectangle border frame mesh with uniform radius and border width.
        /// </summary>
        public static Mesh CreateRoundedRectFrameMesh(float width, float height, float radius, float borderWidth, int cornerSegments = 8) {
            return CreateRoundedRectFrameMesh(width, height, BorderVector4Extensions.Uniform(radius), BorderVector4Extensions.Uniform(borderWidth), cornerSegments);
        }

        /// <summary>
        /// Generates a flat 2D hollow rounded rectangle border frame mesh with 4-corner radii (TL, TR, BR, BL) and 4-edge border widths (Top, Right, Bottom, Left).
        /// </summary>
        public static Mesh CreateRoundedRectFrameMesh(float width, float height, Vector4 cornerRadii, Vector4 borderWidths, int cornerSegments = 8) {
            var mesh = new Mesh { name = "VirtualKey_FrameMesh" };
            cornerSegments = Mathf.Max(2, cornerSegments);

            var outerPerimeter = GeneratePerimeterPoints(width, height, cornerRadii, cornerSegments);
            var innerPerimeter = GenerateInnerPerimeterPoints(width, height, cornerRadii, borderWidths, cornerSegments);
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

                // Triangle 1: Back face (visible from underneath)
                triangles[i * 12 + 6] = oCurrent;
                triangles[i * 12 + 7] = iNext;
                triangles[i * 12 + 8] = oNext;

                // Triangle 2: Back face (visible from underneath)
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

        /// <summary>
        /// Updates the vertex positions of an existing body mesh in-place (uniform radius overload).
        /// </summary>
        public static void UpdateRoundedRectVertices(Mesh mesh, float width, float height, float radius, int cornerSegments = 8) {
            UpdateRoundedRectVertices(mesh, width, height, BorderVector4Extensions.Uniform(radius), cornerSegments);
        }

        /// <summary>
        /// Updates the vertex positions of an existing body mesh in-place with 4-corner radii.
        /// </summary>
        public static void UpdateRoundedRectVertices(Mesh mesh, float width, float height, Vector4 cornerRadii, int cornerSegments = 8) {
            if (mesh == null) return;
            cornerSegments = Mathf.Max(2, cornerSegments);

            var perimeter = GeneratePerimeterPoints(width, height, cornerRadii, cornerSegments);
            int count = perimeter.Count;

            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length != count + 1) {
                vertices = new Vector3[count + 1];
            }

            vertices[0] = Vector3.zero;
            for (int i = 0; i < count; i++) {
                vertices[i + 1] = perimeter[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Updates the vertex positions of an existing border frame mesh in-place (uniform overload).
        /// </summary>
        public static void UpdateRoundedRectFrameVertices(Mesh mesh, float width, float height, float radius, float borderWidth, int cornerSegments = 8) {
            UpdateRoundedRectFrameVertices(mesh, width, height, BorderVector4Extensions.Uniform(radius), BorderVector4Extensions.Uniform(borderWidth), cornerSegments);
        }

        /// <summary>
        /// Updates the vertex positions of an existing border frame mesh in-place with 4-corner radii and 4-edge border widths.
        /// </summary>
        public static void UpdateRoundedRectFrameVertices(Mesh mesh, float width, float height, Vector4 cornerRadii, Vector4 borderWidths, int cornerSegments = 8) {
            if (mesh == null) return;
            cornerSegments = Mathf.Max(2, cornerSegments);

            var outerPerimeter = GeneratePerimeterPoints(width, height, cornerRadii, cornerSegments);
            var innerPerimeter = GenerateInnerPerimeterPoints(width, height, cornerRadii, borderWidths, cornerSegments);
            int count = outerPerimeter.Count;

            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length != count * 2) {
                vertices = new Vector3[count * 2];
            }

            for (int i = 0; i < count; i++) {
                vertices[i] = outerPerimeter[i];
                vertices[count + i] = innerPerimeter[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        public static List<Vector3> GeneratePerimeterPoints(float width, float height, Vector4 cornerRadii, int segmentsPerCorner) {
            var points = new List<Vector3>(segmentsPerCorner * 4);

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float maxR = Mathf.Min(hw, hh);

            // cornerRadii: x = Top-Left, y = Top-Right, z = Bottom-Right, w = Bottom-Left
            float rTL = Mathf.Clamp(cornerRadii.x, 0f, maxR);
            float rTR = Mathf.Clamp(cornerRadii.y, 0f, maxR);
            float rBR = Mathf.Clamp(cornerRadii.z, 0f, maxR);
            float rBL = Mathf.Clamp(cornerRadii.w, 0f, maxR);

            void AddArc(Vector2 center, float radius, float startAngleRad, float endAngleRad) {
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
            AddArc(new Vector2(hw - rTR, hh - rTR), rTR, 0f, Mathf.PI * 0.5f);

            // Quadrant 2: Top-Left (PI/2 to PI)
            AddArc(new Vector2(-hw + rTL, hh - rTL), rTL, Mathf.PI * 0.5f, Mathf.PI);

            // Quadrant 3: Bottom-Left (PI to 3*PI/2)
            AddArc(new Vector2(-hw + rBL, -hh + rBL), rBL, Mathf.PI, Mathf.PI * 1.5f);

            // Quadrant 4: Bottom-Right (3*PI/2 to 2*PI)
            AddArc(new Vector2(hw - rBR, -hh + rBR), rBR, Mathf.PI * 1.5f, Mathf.PI * 2f);

            return points;
        }

        private static List<Vector3> GenerateInnerPerimeterPoints(float width, float height, Vector4 cornerRadii, Vector4 borderWidths, int segmentsPerCorner) {
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            // borderWidths: x = Top, y = Right, z = Bottom, w = Left
            float bTop = Mathf.Clamp(borderWidths.x, 0.0001f, height * 0.5f - 0.0005f);
            float bRight = Mathf.Clamp(borderWidths.y, 0.0001f, width * 0.5f - 0.0005f);
            float bBottom = Mathf.Clamp(borderWidths.z, 0.0001f, height * 0.5f - 0.0005f);
            float bLeft = Mathf.Clamp(borderWidths.w, 0.0001f, width * 0.5f - 0.0005f);

            float inHw = Mathf.Max(0.0005f, hw - (bLeft + bRight) * 0.5f);
            float inHh = Mathf.Max(0.0005f, hh - (bTop + bBottom) * 0.5f);
            float maxR = Mathf.Min(inHw, inHh);

            // Center offset of inner boundary if asymmetric
            float cxOffset = (bLeft - bRight) * 0.5f;
            float czOffset = (bBottom - bTop) * 0.5f;

            // Compute inner corner radii
            float inTL = Mathf.Clamp(cornerRadii.x - (bTop + bLeft) * 0.5f, 0f, maxR);
            float inTR = Mathf.Clamp(cornerRadii.y - (bTop + bRight) * 0.5f, 0f, maxR);
            float inBR = Mathf.Clamp(cornerRadii.z - (bBottom + bRight) * 0.5f, 0f, maxR);
            float inBL = Mathf.Clamp(cornerRadii.w - (bBottom + bLeft) * 0.5f, 0f, maxR);

            var basePoints = GeneratePerimeterPoints(inHw * 2f, inHh * 2f, new Vector4(inTL, inTR, inBR, inBL), segmentsPerCorner);
            if (Mathf.Abs(cxOffset) > 0.00001f || Mathf.Abs(czOffset) > 0.00001f) {
                for (int i = 0; i < basePoints.Count; i++) {
                    basePoints[i] = new Vector3(basePoints[i].x + cxOffset, 0f, basePoints[i].z + czOffset);
                }
            }
            return basePoints;
        }

        private static List<Vector3> GeneratePerimeterPoints(float width, float height, float radius, int segmentsPerCorner) {
            return GeneratePerimeterPoints(width, height, BorderVector4Extensions.Uniform(radius), segmentsPerCorner);
        }
    }
}
