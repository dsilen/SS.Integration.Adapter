using System;
using System.Reflection;
using Akka.Actor;
using Akka.TestKit.NUnit;
using Moq;
using SS.Integration.Adapter.Interface;
using SS.Integration.Adapter.Model;
using SS.Integration.Adapter.Model.Enums;
using SS.Integration.Adapter.Model.Interfaces;

namespace SS.Integration.Adapter.Tests
{
    public class BaseTestKit : TestKit
    {
        #region Constants

        public const int ASSERT_WAIT_TIMEOUT = 5000 /*ms*/;
        public const int ASSERT_EXEC_INTERVAL = 200 /*ms*/;

        #endregion

        #region Attributes

        protected Mock<ISettings> SettingsMock;
        protected Mock<IAdapterPlugin> PluginMock;
        protected Mock<IServiceFacade> ServiceMock;
        protected Mock<IEventState> EventStateMock;
        protected Mock<IStateManager> StateManagerMock;
        protected Mock<IStateProvider> StateProvider;
        protected Mock<ISuspensionManager> SuspensionManager;
        protected Mock<IStreamValidation> StreamValidationMock;
        protected Mock<IFixtureValidation> FixtureValidationMock;

        #endregion

        #region Protected methods

        protected void SetupCommonMockObjects(
            string sport,
            byte[] fixtureData,
            dynamic storedData,
            out Fixture snapshot,
            out Mock<IResourceFacade> resourceFacadeMock,
            Action<Mock<IResourceFacade>, string> resourceGetSnapshotCallsSequence = null)
        {
            resourceFacadeMock = new Mock<IResourceFacade>();

            var snapshotJson = System.Text.Encoding.UTF8.GetString(fixtureData);
            snapshot = FixtureJsonHelper.GetFromJson(snapshotJson);
            var snapshotVar = snapshot;
            resourceFacadeMock.Setup(o => o.Id).Returns(snapshot.Id);
            resourceFacadeMock.Setup(o => o.Sport).Returns(sport);
            resourceFacadeMock.Setup(o => o.MatchStatus).Returns((MatchStatus)Convert.ToInt32(snapshot.MatchStatus));
            resourceFacadeMock.Setup(o => o.Content).Returns(new Summary
            {
                Id = snapshot.Id,
                Sequence = snapshot.Sequence,
                MatchStatus = Convert.ToInt32(snapshot.MatchStatus),
                StartTime = snapshot.StartTime?.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
            if (resourceGetSnapshotCallsSequence == null)
            {
                resourceFacadeMock.Setup(o => o.GetSnapshot()).Returns(snapshotJson);
            }
            else
            {
                resourceGetSnapshotCallsSequence(resourceFacadeMock, snapshotJson);
            }
            resourceFacadeMock.Setup(r => r.StartStreaming()).Raises(r => r.StreamConnected += null, EventArgs.Empty);
            resourceFacadeMock.Setup(r => r.StopStreaming()).Raises(r => r.StreamDisconnected += null, EventArgs.Empty);
            StateManagerMock.Setup(o => o.CreateNewMarketRuleManager(It.Is<string>(id => id.Equals(snapshotVar.Id))))
                .Returns(new Mock<IMarketRulesManager>().Object);

            StateManagerMock.SetupGet(o => o.StateProvider)
                .Returns(StateProvider.Object);
            StateProvider.SetupGet(o => o.SuspensionManager)
                .Returns(SuspensionManager.Object);

            var storedFixtureState = new FixtureState { Id = snapshot.Id };

            if (storedData != null)
            {
                storedFixtureState.Epoch = (int)storedData.Epoch;
                storedFixtureState.Sequence = (int)storedData.Sequence;
                storedFixtureState.MatchStatus = (MatchStatus)storedData.MatchStatus;
                EventStateMock.Setup(o => o.GetFixtureState(It.Is<string>(id => id.Equals(snapshotVar.Id))))
                    .Returns(storedFixtureState);
            }
            EventStateMock.Setup(o =>
                o.UpdateFixtureState(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<MatchStatus>(),
                    It.IsAny<int>())).Callback<string, string, int, MatchStatus, int>(
                (sportParam, fixtureId, sequence, matchStatus, epoch) =>
                {
                    storedFixtureState.Sport = sportParam;
                    storedFixtureState.Id = fixtureId;
                    storedFixtureState.Sequence = sequence;
                    storedFixtureState.MatchStatus = matchStatus;
                    storedFixtureState.Epoch = epoch;
                });
        }

        protected IActorRef GetChildActorRef(IActorRef anchorRef,string name)
        {
            return Sys.ActorSelection(anchorRef, name).ResolveOne(TimeSpan.FromSeconds(5)).Result;
        }

        protected TActor GetUnderlyingActor<TActor>(IActorRef actorRef) where TActor : ActorBase
        {
            var actorProp =
                typeof(ActorCell).GetProperty(
                    "Actor",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty);

            return actorProp?.GetValue((ActorCell)((LocalActorRef)actorRef)?.Underlying) as TActor;
        }

        #endregion
    }
}