using NUnit.Framework;
using UnityEngine;
using FlameStream;

namespace FSInputReceiver.Tests
{
    [TestFixture]
    public class MathAndHelperTests
    {
        [Test]
        public void NormalizeRotationAngles_WrapsAnglesCorrectly()
        {
            var raw = new Vector3(370f, -400f, 180f);
            var normalized = Helper.NormalizeRotationAngles(raw);

            Assert.AreEqual(10f, normalized.x, 0.001f);
            Assert.AreEqual(-40f, normalized.y, 0.001f);
            Assert.AreEqual(180f, normalized.z, 0.001f);
        }

        [Test]
        public void BorderVector4Extensions_Uniform_CreatesIdenticalComponents()
        {
            var v = BorderVector4Extensions.Uniform(0.05f);
            Assert.AreEqual(0.05f, v.x, 0.0001f);
            Assert.AreEqual(0.05f, v.y, 0.0001f);
            Assert.AreEqual(0.05f, v.z, 0.0001f);
            Assert.AreEqual(0.05f, v.w, 0.0001f);
        }

        [Test]
        public void BorderVector4Extensions_EdgeAccessors_MapCorrectly()
        {
            var v = new Vector4(1f, 2f, 3f, 4f);
            Assert.AreEqual(1f, v.Top());
            Assert.AreEqual(2f, v.Right());
            Assert.AreEqual(3f, v.Bottom());
            Assert.AreEqual(4f, v.Left());
        }
    }
}
