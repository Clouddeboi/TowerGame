using Game.Inventory.Config;
using Game.Inventory.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Inventory.Tests
{
    public class InventoryModeControllerTests
    {
        private InventoryModeConfig _config;
        private FakeGameplayInputPort _input;
        private FakeCursorStatePort _cursor;
        private InventoryModeController _controller;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<InventoryModeConfig>();
            _config.EditorSetValues(true, true, true, InventoryModeConfig.TimeScaleBehaviour.PauseCompletely, 0.2f);

            _input = new FakeGameplayInputPort();
            _cursor = new FakeCursorStatePort();
            _controller = new InventoryModeController(_config, _input, _cursor);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Time.timeScale = 1f;
        }

        [Test]
        public void EnterInventoryMode_DisablesInputAndShowsCursor()
        {
            _controller.EnterInventoryMode();

            Assert.That(_input.inputEnabled, Is.False);
            Assert.That(_cursor.cursorVisible, Is.True);
            Assert.That(_cursor.cursorLocked, Is.False);
            Assert.That(_controller.IsOpen, Is.True);
        }

        [Test]
        public void ExitInventoryMode_RestoresInputAndCursor()
        {
            _controller.EnterInventoryMode();
            _controller.ExitInventoryMode();

            Assert.That(_input.inputEnabled, Is.True);
            Assert.That(_cursor.cursorVisible, Is.False);
            Assert.That(_cursor.cursorLocked, Is.True);
            Assert.That(_controller.IsOpen, Is.False);
        }

        [Test]
        public void EnterInventoryMode_PauseCompletely_SetsTimeScaleToZero()
        {
            Time.timeScale = 1f;

            _controller.EnterInventoryMode();

            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void ExitInventoryMode_RestoresPreviousTimeScale()
        {
            Time.timeScale = 1f;
            _controller.EnterInventoryMode();

            _controller.ExitInventoryMode();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void EnterInventoryMode_CalledTwice_IsIdempotent()
        {
            _controller.EnterInventoryMode();
            _controller.EnterInventoryMode();

            Assert.That(_controller.IsOpen, Is.True);
        }

        [Test]
        public void DoNotChangeTimeScale_LeavesTimeScaleUntouched()
        {
            _config.EditorSetValues(true, true, true, InventoryModeConfig.TimeScaleBehaviour.DoNotChange, 0.2f);
            Time.timeScale = 1f;

            _controller.EnterInventoryMode();

            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}