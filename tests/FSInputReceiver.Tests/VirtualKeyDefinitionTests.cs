using NUnit.Framework;
using UnityEngine;
using FlameStream;

namespace FSInputReceiver.Tests
{
    [TestFixture]
    public class VirtualKeyDefinitionTests
    {
        [Test]
        public void VirtualKeyDefinition_DefaultGlowRestingState_IsFalse()
        {
            var def = new VirtualKeyDefinition();
            Assert.IsFalse(def.IsGlowResting);
            Assert.AreEqual(0f, def.CurrentPressProgress, 0.0001f);
        }

        [Test]
        public void VirtualKeyDefinition_DestroyVisuals_ResetsRestingState()
        {
            var def = new VirtualKeyDefinition();
            def.IsGlowResting = true;
            def.DestroyVisuals();

            Assert.IsFalse(def.IsGlowResting);
            Assert.AreEqual(new Vector2(-1f, -1f), def.CurrentSize);
        }
    }
}
