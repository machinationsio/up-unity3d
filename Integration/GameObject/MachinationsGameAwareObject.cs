using System;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.SyncAPI;
using UnityEngine;

namespace MachinationsUP.Integration.GameObject
{
    /// <summary>
    /// Extends <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> by
    /// adding Game Awareness via <see cref="MachinationsUP.GameEngineAPI.Game.IGameLifecycleSubscriber"/> and
    /// <see cref="MachinationsUP.GameEngineAPI.GameObject.IGameObjectLifecycleSubscriber"/>.
    /// </summary>
    public class MachinationsGameAwareObject : MachinationsGameObject, IGameLifecycleSubscriber, IGameObjectLifecycleSubscriber
    {

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="manifest">The Manifest that will be used to initialized this MachinationsGameAwareObject.</param>
        /// <param name="onBindersUpdated">When a <see cref="MachinationsGameObject"/> enrolls itself
        /// using <see cref="MachinationsGameLayer.EnrollGameObject"/>, this event WILL fire if the MachinationsGameLayer
        /// has been initialized. So, this is why it is allowed to send an EventHandler callback upon Construction.
        /// </param>
        public MachinationsGameAwareObject (MachinationsGameObjectManifest manifest, EventHandler onBindersUpdated = null) :
            base(manifest, onBindersUpdated)
        {
        }

        /// <summary>
        /// Updates Binders with the name Game / Game Object State.
        /// </summary>
        private void UpdateBinders ()
        {
            //Notify all Binders about the (possibly) new Game / Game Object State.
            foreach (ElementBinder binder in _binders.Values)
                binder.UpdateStates(CurrentGameState, CurrentGameObjectState);
        }

        /// <summary>
        /// Called by <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> when initialization is complete.
        /// For all <see cref="MachinationsUP.Integration.Binder.ElementBinder"/>, retrieves their
        /// required <see cref="MachinationsUP.Integration.Elements.ElementBase"/> from MGL, FOR EACH
        /// possible <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/>.
        /// Complete override from <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/>.
        /// </summary>
        override internal void MGLInitComplete (bool isRunningOffline = false)
        {
            //Go through all Binders and ask them to retrieve their ElementBase, For each StatesAssociation.
            foreach (string gameObjectPropertyName in _binders.Keys)
            foreach (StatesAssociation sa in _manifest.GetStatesAssociationsForPropertyName(gameObjectPropertyName))
                _binders[gameObjectPropertyName].CreateElementBaseForStateAssoc(sa, isRunningOffline, isRunningOffline);

            //Since this is a Game Aware Object, update its Game State.
            OnGameStateChanged(MachinationsGameLayer.GetGameState());

            //Notify any listeners of base.OnBindersUpdated.
            NotifyBindersUpdated();
        }

        /// <summary>
        /// Asks this Game-Aware Object to update one of its <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> usually because
        /// new values arrived from the Machinations Back-end.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="DiagramMapping"/> associated with this Binder.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> obtained by parsing the back-end update.</param>
        override internal void UpdateBinder (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //TODO: on update, shouldn't create new elements, but rather UPDATE the current element.
            //Ask the Binder to update exactly this desired StatesAssociation.
            _binders[diagramMapping.GameObjectPropertyName].CreateElementBaseForStateAssoc(diagramMapping.StatesAssoc, true);
            //Notify any listeners of base.OnBindersUpdated.
            NotifyBindersUpdated();
        }

        #region Implementation of IGameLifecycleSubscriber

        /// <summary>
        /// Returns the current Game State.
        /// </summary>
        public GameStates CurrentGameState { get; private set; }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber about a new Game State.
        /// </summary>
        /// <param name="newGameState">New Game State.</param>
        public void OnGameStateChanged (GameStates newGameState)
        {
            if (newGameState == CurrentGameState) return;
            Debug.Log(DebugContext() + " Game State Changed to " + newGameState);
            CurrentGameState = newGameState;
            UpdateBinders();
        }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber that a Game Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameEvent (string evnt)
        {
        }

        /// <summary>
        /// Machinations -> Game commands. Intended for Future use.
        /// </summary>
        /// <param name="command"></param>
        public void GameCommand (MachinationsCommands command)
        {
        }

        #endregion

        #region implementation of IGameObjectLifecycleSubscriber

        /// <summary>
        /// Returns the current Game Object State.
        /// </summary>
        public GameObjectStates CurrentGameObjectState { get; private set; }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber about a new Game Object State.
        /// </summary>
        /// <param name="newGameObjectState">New Game Object State.</param>
        public void OnGameObjectStateChanged (GameObjectStates newGameObjectState)
        {
            if (newGameObjectState == CurrentGameObjectState) return;
            Debug.Log(DebugContext() + " Game Object State Changed to " + newGameObjectState);
            CurrentGameObjectState = newGameObjectState;
            UpdateBinders();
        }

        /// <summary>
        /// Informs an IGameObjectLifecycleSubscriber that a Game Object Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        public void OnGameObjectEvent (string evnt)
        {
            //Emit events marked as "to emit".
            if (_manifest.EventsToEmit.Contains(evnt))
                MachinationsGameLayer.Instance.EmitEvent(this, evnt);
        }

        #endregion

        override protected string DebugContext ()
        {
            return "MachinationsGameObject '" + _gameObjectName + "' @ currentGameState: " + CurrentGameState +
                   " and currentGameObjectState: " + CurrentGameObjectState + ".";
        }

        override public string ToString ()
        {
            return DebugContext();
        }
        
    }
}