using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for SceneTransitionService — pure Application, no Unity runtime.</summary>
    [TestFixture]
    public sealed class SceneTransitionServiceTests
    {
        private sealed class FakeSceneLoader : ISceneLoader
        {
            public string ActiveSceneName => "M01_Alley";
            public readonly List<string> Loaded = new List<string>();
            public TaskCompletionSource<bool> Gate;
            public bool ShouldThrow;

            public async Task LoadSceneAsync(string sceneName)
            {
                Loaded.Add(sceneName);
                if (ShouldThrow)
                    throw new System.InvalidOperationException("boom");
                if (Gate != null)
                    await Gate.Task;
            }
        }

        private EventBus _bus;
        private FakeSceneLoader _loader;
        private SceneTransitionService _service;
        private List<SceneTransitionStarted> _started;
        private List<SceneTransitionCompleted> _completed;
        private List<SceneTransitionFailed> _failed;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _loader = new FakeSceneLoader();
            _service = new SceneTransitionService(_loader, _bus);
            _started = new List<SceneTransitionStarted>();
            _completed = new List<SceneTransitionCompleted>();
            _failed = new List<SceneTransitionFailed>();
            _bus.Subscribe<SceneTransitionStarted>(e => _started.Add(e));
            _bus.Subscribe<SceneTransitionCompleted>(e => _completed.Add(e));
            _bus.Subscribe<SceneTransitionFailed>(e => _failed.Add(e));
        }

        [Test]
        public async Task RequestTransition_ValidScene_LoadsAndCompletes()
        {
            bool ok = await _service.RequestTransitionAsync("M02_Motel");

            Assert.IsTrue(ok);
            Assert.AreEqual(SceneTransitionState.Idle, _service.State);
            Assert.IsFalse(_service.IsTransitioning);
            Assert.AreEqual(1, _loader.Loaded.Count);
            Assert.AreEqual("M02_Motel", _loader.Loaded[0]);
            Assert.AreEqual(1, _started.Count);
            Assert.AreEqual("M02_Motel", _started[0].SceneName);
            Assert.AreEqual(1, _completed.Count);
            Assert.AreEqual("M02_Motel", _completed[0].SceneName);
            Assert.AreEqual(0, _failed.Count);
        }

        [Test]
        public async Task RequestTransition_EmptyName_FailsWithoutLoading()
        {
            Assert.IsFalse(await _service.RequestTransitionAsync(""));
            Assert.IsFalse(await _service.RequestTransitionAsync(null));

            Assert.AreEqual(0, _loader.Loaded.Count);
            Assert.AreEqual(2, _failed.Count);
            Assert.AreEqual(SceneTransitionState.Idle, _service.State);
        }

        [Test]
        public async Task RequestTransition_WhileLoading_RejectsSecond()
        {
            _loader.Gate = new TaskCompletionSource<bool>();
            Task<bool> first = _service.RequestTransitionAsync("M02_Motel");

            Assert.IsTrue(_service.IsTransitioning);
            Assert.AreEqual("M02_Motel", _service.CurrentTarget);
            Assert.IsFalse(await _service.RequestTransitionAsync("M03_Other"));

            _loader.Gate.SetResult(true);
            Assert.IsTrue(await first);

            Assert.AreEqual(1, _loader.Loaded.Count);
            Assert.AreEqual(1, _failed.Count);
            Assert.AreEqual("M03_Other", _failed[0].SceneName);
            Assert.AreEqual(SceneTransitionState.Idle, _service.State);
        }

        [Test]
        public async Task RequestTransition_LoaderThrows_FailsAndReturnsToIdle()
        {
            _loader.ShouldThrow = true;

            Assert.IsFalse(await _service.RequestTransitionAsync("M02_Motel"));

            Assert.AreEqual(1, _loader.Loaded.Count);
            Assert.AreEqual(1, _failed.Count);
            Assert.AreEqual("M02_Motel", _failed[0].SceneName);
            Assert.IsFalse(string.IsNullOrEmpty(_failed[0].Reason));
            Assert.AreEqual(0, _completed.Count);
            Assert.AreEqual(SceneTransitionState.Idle, _service.State);
            Assert.IsFalse(_service.IsTransitioning);
        }

        [Test]
        public async Task RequestTransition_AfterFailure_ServiceIsReusable()
        {
            _loader.ShouldThrow = true;
            Assert.IsFalse(await _service.RequestTransitionAsync("M02_Motel"));

            _loader.ShouldThrow = false;
            Assert.IsTrue(await _service.RequestTransitionAsync("M02_Motel"));
            Assert.AreEqual(2, _loader.Loaded.Count);
        }

        [Test]
        public void Reset_ClearsTarget()
        {
            _service.Reset();
            Assert.AreEqual(SceneTransitionState.Idle, _service.State);
            Assert.AreEqual(string.Empty, _service.CurrentTarget);
        }
    }
}
