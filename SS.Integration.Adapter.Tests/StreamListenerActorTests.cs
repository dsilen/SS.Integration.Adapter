//Copyright 2017 Spin Services Limited

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Threading.Tasks;
using Akka.TestKit.NUnit;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SS.Integration.Adapter.Interface;
using SS.Integration.Adapter.Actors;
using SS.Integration.Adapter.Model;
using SS.Integration.Adapter.Model.Enums;
using SS.Integration.Adapter.Model.Interfaces;

namespace SS.Integration.Adapter.Tests
{
    /// <summary>
    /// TODO: REMOVE WAITS FROM EACH TEST AND REPLACE WITH MESSAGE REPLY FROM THE ACTOR
    /// </summary>
    [TestFixture]
    public class StreamListenerActorTests : TestKit
    {
        #region Attributes

        private Mock<ISettings> _settingsMock;
        private Mock<IAdapterPlugin> _pluginMock;
        private Mock<IServiceFacade> _serviceMock;
        private Mock<IEventState> _eventStateMock;
        private Mock<IStateManager> _stateManagerMock;
        private Mock<IStateProvider> _stateProvider;
        private Mock<ISuspensionManager> _suspensionManager;

        #endregion

        #region SetUp

        [SetUp]
        public void SetupTest()
        {
            _pluginMock = new Mock<IAdapterPlugin>();

            _settingsMock = new Mock<ISettings>();
            _settingsMock.Setup(x => x.ProcessingLockTimeOutInSecs).Returns(10);

            _serviceMock = new Mock<IServiceFacade>();

            _eventStateMock = new Mock<IEventState>();

            _stateManagerMock = new Mock<IStateManager>();

            AdapterActorSystem.Init(_settingsMock.Object, _serviceMock.Object, Sys, false);
        }

        #endregion

        #region Test Methods

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationStartStreamingAndProcessFirstSnapshotWhenMatchStatusNotReadyOrSetup()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_inplay_snapshot_1,
                /*resourceMatchStatus*/MatchStatus.InRunning,
                /*storedMatchStatus*/MatchStatus.InRunning,
                /*resourceSequence*/2,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Once);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), false),
                Times.Once);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Streaming, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationMoveToFinishedStateWhenResourceHasMatchOverStatus()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_inplay_snapshot_1,
                /*resourceMatchStatus*/MatchStatus.MatchOver,
                /*storedMatchStatus*/MatchStatus.InRunning,
                /*resourceSequence*/2,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Once);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), true),
                Times.Once);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Suspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id)), SuspensionReason.SUSPENSION),
                Times.Once);
            _stateManagerMock.Verify(a =>
                    a.ClearState(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Once);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Finished, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationMoveToFinishedStateWhenMatchOverWasAlreadyProcessed()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_inplay_snapshot_1,
                /*resourceMatchStatus*/MatchStatus.MatchOver,
                /*storedMatchStatus*/MatchStatus.MatchOver,
                /*resourceSequence*/2,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Never);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), true),
                Times.Never);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Suspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id)), SuspensionReason.SUSPENSION),
                Times.Never);
            _stateManagerMock.Verify(a =>
                    a.ClearState(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Finished, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationProcessFullSnapshotWhenCurrentMatchStatusIsDifferentThanStoredMatchStatus()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_inplay_snapshot_1,
                /*resourceMatchStatus*/MatchStatus.InRunning,
                /*storedMatchStatus*/MatchStatus.Prematch,
                /*resourceSequence*/2,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Once);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), false),
                Times.Once);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Suspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id)), SuspensionReason.SUSPENSION),
                Times.Never);
            _stateManagerMock.Verify(a =>
                    a.ClearState(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Streaming, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationAfterStartStreamingUnSuspendFixtureOnProcessSnapshotWhenSequenceHasNotChanged()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_inplay_snapshot_1,
                /*resourceMatchStatus*/MatchStatus.InRunning,
                /*storedMatchStatus*/MatchStatus.InRunning,
                /*resourceSequence*/1,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Never);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), false),
                Times.Never);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Once);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Once);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Streaming, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void OnInitializationMoveToReadyStateWhenMatchStatusReadyOrSetupWithNotAllowStreamingInSetupMode()
        {
            //
            //Arrange
            //
            Mock<IResourceFacade> resourceMock;
            SetupCommonMockObjects(
                /*fixtureData*/FixtureSamples.football_ready_snapshot_2,
                /*resourceMatchStatus*/MatchStatus.Ready,
                /*storedMatchStatus*/MatchStatus.Ready,
                /*resourceSequence*/2,
                /*storedSequence*/1,
                out resourceMock);

            //
            //Act
            //
            var actor = ActorOfAsTestActorRef(() =>
                new StreamListenerActor(
                    resourceMock.Object,
                    _pluginMock.Object,
                    _eventStateMock.Object,
                    _stateManagerMock.Object,
                    _settingsMock.Object));
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            //
            //Assert
            //
            resourceMock.Verify(a => a.GetSnapshot(), Times.Once);
            _pluginMock.Verify(a =>
                    a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), false),
                Times.Once);
            _pluginMock.Verify(a =>
                    a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
                Times.Never);
            _suspensionManager.Verify(a =>
                    a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
                Times.Never);
            Assert.AreEqual(StreamListenerActor.StreamListenerState.Ready, actor.UnderlyingActor.State);
        }

        [Test]
        [Category("StreamListenerActor")]
        public void ProcessSnapshotOnlyOnceWhenMovingFromReadyStateToStreamingState()
        {
            throw new NotImplementedException();
            //
            //Arrange
            //
            //Mock<IResourceFacade> resourceMock;
            //SetupCommonMockObjects(
            //    /*fixtureData*/FixtureSamples.football_ready_snapshot_2,
            //    /*resourceMatchStatus*/MatchStatus.Ready,
            //    /*storedMatchStatus*/MatchStatus.Ready,
            //    /*resourceSequence*/2,
            //    /*storedSequence*/1,
            //    out resourceMock);

            ////
            ////Act
            ////
            //var actor = ActorOfAsTestActorRef(() =>
            //    new StreamListenerActor(
            //        resourceMock.Object,
            //        _pluginMock.Object,
            //        _eventStateMock.Object,
            //        _stateManagerMock.Object,
            //        _settingsMock.Object));
            //Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            ////
            ////Assert
            ////
            //resourceMock.Verify(a => a.GetSnapshot(), Times.Once);
            //_pluginMock.Verify(a =>
            //        a.ProcessSnapshot(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id)), false),
            //    Times.Once);
            //_pluginMock.Verify(a =>
            //        a.UnSuspend(It.Is<Fixture>(f => f.Id.Equals(resourceMock.Object.Id))),
            //    Times.Never);
            //_suspensionManager.Verify(a =>
            //        a.Unsuspend(It.Is<string>(id => id.Equals(resourceMock.Object.Id))),
            //    Times.Never);
            //Assert.AreEqual(StreamListenerActor.StreamListenerState.Ready, actor.UnderlyingActor.State);
        }

        #endregion

        #region Private methods

        private void SetupCommonMockObjects(
            byte[] fixtureData,
            MatchStatus resourceMatchStatus,
            MatchStatus storedMatchStatus,
            int resourceSequence,
            int storedSequence,
            out Mock<IResourceFacade> resourceMock)
        {
            resourceMock = new Mock<IResourceFacade>();

            var snapshotJson = System.Text.Encoding.UTF8.GetString(fixtureData);
            var snapshot = FixtureJsonHelper.GetFromJson(snapshotJson);
            resourceMock.Setup(o => o.Id).Returns(snapshot.Id);
            resourceMock.Setup(o => o.MatchStatus).Returns(resourceMatchStatus);
            resourceMock.Setup(o => o.Content).Returns(new Summary { Sequence = resourceSequence });
            resourceMock.Setup(o => o.GetSnapshot())
                .Returns(snapshotJson);
            _stateManagerMock.Setup(o => o.CreateNewMarketRuleManager(It.Is<string>(id => id.Equals(snapshot.Id))))
                .Returns(new Mock<IMarketRulesManager>().Object);
            _stateProvider = new Mock<IStateProvider>();
            _suspensionManager = new Mock<ISuspensionManager>();
            _stateManagerMock.SetupGet(o => o.StateProvider)
                .Returns(_stateProvider.Object);
            _stateProvider.SetupGet(o => o.SuspensionManager)
                .Returns(_suspensionManager.Object);
            _eventStateMock.Setup(o => o.GetFixtureState(It.Is<string>(id => id.Equals(snapshot.Id))))
                .Returns(new FixtureState { Id = snapshot.Id, Sequence = storedSequence, MatchStatus = storedMatchStatus });
        }

        #endregion
    }
}
