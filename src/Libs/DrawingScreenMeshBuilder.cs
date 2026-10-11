using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlameStream
{
    public static class DrawingScreenMeshBuilder
    {
        /// <summary>
        /// Generates a flat 2D rounded rectangle body mesh on the X-Y plane with uniform corner radius and uniform bound offset.
        /// </summary>
        public static Mesh CreateRoundedRectMesh(float width, float height, float radius, float boundOffset = 0f, int cornerSegments = 8) {
            return CreateRoundedRectMesh(width, height, BorderVector4Extensions.Uniform(radius), BorderVector4Extensions.Uniform(boundOffset), cornerSegments);
        }

        /// <summary>
        /// Generates a flat 2D rounded rectangle body mesh on the X-Y plane with 4-corner radii (TL, TR, BR, BL) and 4-edge bound offsets (Top, Right, Bottom, Left).
        /// </summary>
        public static Mesh CreateRoundedRectMesh(float width, float height, Vector4 cornerRadii, Vector4 boundOffsets, int cornerSegments = 8) {
            var mesh = new Mesh { name = "DrawingScreen_BodyMesh" };
            cornerSegments = Mathf.Max(2, cornerSegments);

            var perimeter = GeneratePerimeterPoints(width, height, cornerRadii, boundOffsets, cornerSegments);
            int count = perimeter.Count;

            var vertices = new Vector3[count + 1];
            var uvs = new Vector2[count + 1];
            var normals = new Vector3[count + 1];

            // Center vertex
            float cx = (boundOffsets.y - boundOffsets.w) * 0.5f;
            float cy = (boundOffsets.x - boundOffsets.z) * 0.5f;
            vertices[0] = new Vector3(cx, cy, 0f);
            uvs[0] = new Vector2(cx / Mathf.Max(0.001f, width) + 0.5f, cy / Mathf.Max(0.001f, height) + 0.5f);
            normals[0] = Vector3.back;

            for (int i = 0; i < count; i++) {
                vertices[i + 1] = perimeter[i];
                uvs[i + 1] = new Vector2(perimeter[i].x / Mathf.Max(0.001f, width) + 0.5f, perimeter[i].y / Mathf.Max(0.001f, height) + 0.5f);
                normals[i + 1] = Vector3.back;
            }

            var triangles = new int[count * 3 * 2];
            for (int i = 0; i < count; i++) {
                int next = (i + 1) % count;
                // Front face (visible when looking towards +Z, camera at -Z)
                triangles[i * 6 + 0] = 0;
                triangles[i * 6 + 1] = next + 1;
                triangles[i * 6 + 2] = i + 1;

                // Back face (visible from behind)
                triangles[i * 6 + 3] = 0;
                triangles[i * 6 + 4] = i + 1;
                triangles[i * 6 + 5] = next + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a flat 2D hollow rounded rectangle border frame mesh on the X-Y plane with uniform radius, border width, and bound offset.
        /// </summary>
        public static Mesh CreateRoundedRectFrameMesh(float width, float height, float radius, float borderWidth, float boundOffset = 0f, int cornerSegments = 8) {
            return CreateRoundedRectFrameMesh(width, height, BorderVector4Extensions.Uniform(radius), BorderVector4Extensions.Uniform(borderWidth), BorderVector4Extensions.Uniform(boundOffset), cornerSegments);
        }

        /// <summary>
        /// Generates a flat 2D hollow rounded rectangle border frame mesh on the X-Y plane with 4-corner radii (TL, TR, BR, BL), 4-edge border widths (Top, Right, Bottom, Left), and 4-edge bound offsets (Top, Right, Bottom, Left).
        /// </summary>
        public static Mesh CreateRoundedRectFrameMesh(float width, float height, Vector4 cornerRadii, Vector4 borderWidths, Vector4 boundOffsets, int cornerSegments = 8) {
            var mesh = new Mesh { name = "DrawingScreen_FrameMesh" };
            cornerSegments = Mathf.Max(2, cornerSegments);

            var outerPerimeter = GeneratePerimeterPoints(width, height, cornerRadii, boundOffsets, cornerSegments);
            var innerPerimeter = GenerateInnerPerimeterPoints(width, height, cornerRadii, borderWidths, boundOffsets, cornerSegments);
            int count = outerPerimeter.Count;

            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var normals = new Vector3[count * 2];

            for (int i = 0; i < count; i++) {
                // Outer vertex: index i
                vertices[i] = outerPerimeter[i];
                uvs[i] = new Vector2(outerPerimeter[i].x / Mathf.Max(0.001f, width) + 0.5f, outerPerimeter[i].y / Mathf.Max(0.001f, height) + 0.5f);
                normals[i] = Vector3.back;

                // Inner vertex: index count + i
                vertices[count + i] = innerPerimeter[i];
                uvs[count + i] = new Vector2(innerPerimeter[i].x / Mathf.Max(0.001f, width) + 0.5f, innerPerimeter[i].y / Mathf.Max(0.001f, height) + 0.5f);
                normals[count + i] = Vector3.back;
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
                triangles[i * 12 + 1] = iNext;
                triangles[i * 12 + 2] = oNext;

                // Triangle 2: Front face
                triangles[i * 12 + 3] = oCurrent;
                triangles[i * 12 + 4] = iCurrent;
                triangles[i * 12 + 5] = iNext;

                // Triangle 1: Back face (visible from behind)
                triangles[i * 12 + 6] = oCurrent;
                triangles[i * 12 + 7] = oNext;
                triangles[i * 12 + 8] = iNext;

                // Triangle 2: Back face (visible from behind)
                triangles[i * 12 + 9] = oCurrent;
                triangles[i * 12 + 10] = iNext;
                triangles[i * 12 + 11] = iCurrent;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Updates the vertex positions of an existing body mesh in-place.
        /// </summary>
        public static void UpdateRoundedRectVertices(Mesh mesh, float width, float height, Vector4 cornerRadii, Vector4 boundOffsets, int cornerSegments = 8) {
            if (mesh == null) return;
            cornerSegments = Mathf.Max(2, cornerSegments);

            var perimeter = GeneratePerimeterPoints(width, height, cornerRadii, boundOffsets, cornerSegments);
            int count = perimeter.Count;

            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length != count + 1) {
                vertices = new Vector3[count + 1];
            }

            float cx = (boundOffsets.y - boundOffsets.w) * 0.5f;
            float cy = (boundOffsets.x - boundOffsets.z) * 0.5f;
            vertices[0] = new Vector3(cx, cy, 0f);

            for (int i = 0; i < count; i++) {
                vertices[i + 1] = perimeter[i];
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Updates the vertex positions of an existing border frame mesh in-place.
        /// </summary>
        public static void UpdateRoundedRectFrameVertices(Mesh mesh, float width, float height, Vector4 cornerRadii, Vector4 borderWidths, Vector4 boundOffsets, int cornerSegments = 8) {
            if (mesh == null) return;
            cornerSegments = Mathf.Max(2, cornerSegments);

            var outerPerimeter = GeneratePerimeterPoints(width, height, cornerRadii, boundOffsets, cornerSegments);
            var innerPerimeter = GenerateInnerPerimeterPoints(width, height, cornerRadii, borderWidths, boundOffsets, cornerSegments);
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

        public static List<Vector3> GeneratePerimeterPoints(float width, float height, Vector4 cornerRadii, Vector4 boundOffsets, int segmentsPerCorner) {
            var points = new List<Vector3>(segmentsPerCorner * 4);

            float hw = width * 0.5f;
            float hh = height * 0.5f;

            // boundOffsets: x = Top, y = Right, z = Bottom, w = Left
            float xMin = -hw - boundOffsets.w;
            float xMax = hw + boundOffsets.y;
            float yMin = -hh - boundOffsets.z;
            float yMax = hh + boundOffsets.x;

            float effWidth = Mathf.Max(0.001f, xMax - xMin);
            float effHeight = Mathf.Max(0.001f, yMax - yMin);
            float maxR = Mathf.Min(effWidth, effHeight) * 0.5f;

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
                        center.y + Mathf.Sin(angle) * radius,
                        0f
                    ));
                }
            }

            // Quadrant 1: Top-Right (0 to PI/2)
            AddArc(new Vector2(xMax - rTR, yMax - rTR), rTR, 0f, Mathf.PI * 0.5f);

            // Quadrant 2: Top-Left (PI/2 to PI)
            AddArc(new Vector2(xMin + rTL, yMax - rTL), rTL, Mathf.PI * 0.5f, Mathf.PI);

            // Quadrant 3: Bottom-Left (PI to 3*PI/2)
            AddArc(new Vector2(xMin + rBL, yMin + rBL), rBL, Mathf.PI, Mathf.PI * 1.5f);

            // Quadrant 4: Bottom-Right (3*PI/2 to 2*PI)
            AddArc(new Vector2(xMax - rBR, yMin + rBR), rBR, Mathf.PI * 1.5f, Mathf.PI * 2f);

            return points;
        }

        private static List<Vector3> GenerateInnerPerimeterPoints(float width, float height, Vector4 cornerRadii, Vector4 borderWidths, Vector4 boundOffsets, int segmentsPerCorner) {
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            float xMin = -hw - boundOffsets.w;
            float xMax = hw + boundOffsets.y;
            float yMin = -hh - boundOffsets.z;
            float yMax = hh + boundOffsets.x;

            float effWidth = Mathf.Max(0.001f, xMax - xMin);
            float effHeight = Mathf.Max(0.001f, yMax - yMin);

            // borderWidths: x = Top, y = Right, z = Bottom, w = Left
            float bTop = Mathf.Clamp(borderWidths.x, 0.0001f, effHeight * 0.5f - 0.0005f);
            float bRight = Mathf.Clamp(borderWidths.y, 0.0001f, effWidth * 0.5f - 0.0005f);
            float bBottom = Mathf.Clamp(borderWidths.z, 0.0001f, effHeight * 0.5f - 0.0005f);
            float bLeft = Mathf.Clamp(borderWidths.w, 0.0001f, effWidth * 0.5f - 0.0005f);

            float inXMin = xMin + bLeft;
            float inXMax = xMax - bRight;
            float inYMin = yMin + bBottom;
            float inYMax = yMax - bTop;

            float inEffW = Mathf.Max(0.0005f, inXMax - inXMin);
            float inEffH = Mathf.Max(0.0005f, inYMax - inYMin);
            float maxR = Mathf.Min(inEffW, inEffH) * 0.5f;

            // Compute inner corner radii
            float inTL = Mathf.Clamp(cornerRadii.x - (bTop + bLeft) * 0.5f, 0f, maxR);
            float inTR = Mathf.Clamp(cornerRadii.y - (bTop + bRight) * 0.5f, 0f, maxR);
            float inBR = Mathf.Clamp(cornerRadii.z - (bBottom + bRight) * 0.5f, 0f, maxR);
            float inBL = Mathf.Clamp(cornerRadii.w - (bBottom + bLeft) * 0.5f, 0f, maxR);

            var points = new List<Vector3>(segmentsPerCorner * 4);

            void AddArc(Vector2 center, float radius, float startAngleRad, float endAngleRad) {
                for (int i = 0; i < segmentsPerCorner; i++) {
                    float t = (float)i / segmentsPerCorner;
                    float angle = Mathf.Lerp(startAngleRad, endAngleRad, t);
                    points.Add(new Vector3(
                        center.x + Mathf.Cos(angle) * radius,
                        center.y + Mathf.Sin(angle) * radius,
                        0f
                    ));
                }
            }

            // Quadrant 1: Top-Right
            AddArc(new Vector2(inXMax - inTR, inYMax - inTR), inTR, 0f, Mathf.PI * 0.5f);

            // Quadrant 2: Top-Left
            AddArc(new Vector2(inXMin + inTL, inYMax - inTL), inTL, Mathf.PI * 0.5f, Mathf.PI);

            // Quadrant 3: Bottom-Left
            AddArc(new Vector2(inXMin + inBL, inYMin + inBL), inBL, Mathf.PI, Mathf.PI * 1.5f);

            // Quadrant 4: Bottom-Right
            AddArc(new Vector2(inXMax - inBR, inYMin + inBR), inBR, Mathf.PI * 1.5f, Mathf.PI * 2f);

            return points;
        }
    }
}
