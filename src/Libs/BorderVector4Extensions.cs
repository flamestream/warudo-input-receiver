using System;
using UnityEngine;

namespace FlameStream
{
    public static class BorderVector4Extensions
    {
        // 4-Edge Border Widths: Vector4(x: Top, y: Right, z: Bottom, w: Left)
        public static float Top(this Vector4 v) => v.x;
        public static float Right(this Vector4 v) => v.y;
        public static float Bottom(this Vector4 v) => v.z;
        public static float Left(this Vector4 v) => v.w;

        // 4-Corner Roundness: Vector4(x: Top-Left, y: Top-Right, z: Bottom-Right, w: Bottom-Left)
        public static float TopLeft(this Vector4 v) => v.x;
        public static float TopRight(this Vector4 v) => v.y;
        public static float BottomRight(this Vector4 v) => v.z;
        public static float BottomLeft(this Vector4 v) => v.w;

        public static Vector4 Uniform(float value) => new Vector4(value, value, value, value);
    }
}
