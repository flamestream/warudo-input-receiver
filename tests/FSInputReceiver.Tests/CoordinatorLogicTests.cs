using NUnit.Framework;
using FlameStream;

namespace FSInputReceiver.Tests
{
    [TestFixture]
    public class CoordinatorLogicTests
    {
        [Test]
        public void CoordinatorHandState_EnumValues_AreDistinct()
        {
            Assert.AreNotEqual(CoordinatorHandState.Released, CoordinatorHandState.Keyboard);
            Assert.AreNotEqual(CoordinatorHandState.Keyboard, CoordinatorHandState.Mouse);
            Assert.AreNotEqual(CoordinatorHandState.Mouse, CoordinatorHandState.Pen);
        }

        [Test]
        public void MultiInputCoordinator_IsPointerDeviceActive_ReturnsFalse_WhenDisabled()
        {
            var coordinator = new MultiInputCoordinatorAsset();
            coordinator.IsEnabled = false;
            coordinator.EnableHands = true;

            var pointer = new PointerReceiverAsset();
            Assert.IsFalse(coordinator.IsPointerDeviceActive(pointer));
            Assert.IsFalse(coordinator.IsPointerInMouseState(pointer));
            Assert.IsFalse(coordinator.IsPointerInPenState(pointer));
        }

        [Test]
        public void MultiInputCoordinator_IsPointerDeviceActive_ReturnsFalse_WhenHandsDisabled()
        {
            var coordinator = new MultiInputCoordinatorAsset();
            coordinator.IsEnabled = true;
            coordinator.EnableHands = false;

            var pointer = new PointerReceiverAsset();
            Assert.IsFalse(coordinator.IsPointerDeviceActive(pointer));
        }

        [Test]
        public void MultiInputCoordinator_HasCharacterMismatch_ReturnsFalse_WhenCharacterIsNull()
        {
            var coordinator = new MultiInputCoordinatorAsset();
            coordinator.Character = null;
            Assert.IsFalse(coordinator.HasCharacterMismatch());
        }
    }
}
