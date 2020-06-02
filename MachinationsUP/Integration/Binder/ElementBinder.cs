using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using UnityEngine;

namespace MachinationsUP.Integration.Binder
{
    /// <summary>
    /// Binds a certain Game Object Property (Name) to the appropriate Machinations Element based
    /// on Game and Game Object State (via <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/>). There will be
    /// a separate <see cref="ElementBase"/> for each StatesAssociation.
    /// </summary>
    public class ElementBinder
    {

        /// <summary>
        /// Machinations Behavior (Game Object) owning this Binder.
        /// </summary>
        internal MachinationsGameObject ParentGameObject { get; }

        /// <summary>
        /// The <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> which defines how to retrieve & connect this Binder (and
        /// the Game Object Property Name it represents) to the Machinations Diagram.
        /// <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/>
        /// </summary>
        public DiagramMapping DiagMapping { get; }

        /// <summary>
        /// Current state of the Game. Updated via <see cref="UpdateGameState"/>
        /// </summary>
        private GameStates _currentGameState;

        /// <summary>
        /// /// Current state of the Game Object which owns this Binder. Updated via <see cref="UpdateGameObjectState"/>
        /// </summary>
        private GameObjectStates _currentGameObjectState;

        /// <summary>
        /// Maps <see cref="MachinationsUP.Integration.Elements.ElementBase"/> to <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/>.
        /// </summary>
        readonly private Dictionary<StatesAssociation, ElementBase> _elements =
            new Dictionary<StatesAssociation, ElementBase>();

        private ElementBase _currentElement;

        /// <summary>
        /// The ElementBase for the current StatesAssociation.
        /// </summary>
        public ElementBase CurrentElement => GetCurrentElementOrCreateFromDefaultIfAvailable();

        /// <summary>
        /// Current Integer Value of the Current Element.
        /// </summary>
        public int Value => GetCurrentElementOrCreateFromDefaultIfAvailable().CurrentValue;

        /// <summary>
        /// Base Integer Value of the Current Element.
        /// </summary>
        public int BaseValue => GetCurrentElementOrCreateFromDefaultIfAvailable().BaseValue;

        /// <summary>
        /// Returns the Game Object Property Name that this Binder is for.
        /// </summary>
        public string GameObjectPropertyName => DiagMapping.GameObjectPropertyName;

        /// <summary>
        /// <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/> to use for when there is no States Association selected.
        /// </summary>
        static readonly private StatesAssociation _noStatesAssociation = new StatesAssociation("N/A");

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="parentGameObject">MachinationsGameObject that owns this Binder.</param>
        /// <param name="diagramMapping">The <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> that specifies where this
        /// ElementBinder will retrieve its data from.</param>
        public ElementBinder (MachinationsGameObject parentGameObject, DiagramMapping diagramMapping)
        {
            ParentGameObject = parentGameObject;
            DiagMapping = diagramMapping;
        }

        /// <summary>
        /// Returns the currently selected <see cref="MachinationsUP.Integration.Elements.ElementBase"/>. If none is selected,
        /// returns the DefaultElementBase from this Binder's <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/>.
        /// If no such Default exists, throws an Exception.
        /// </summary>
        private ElementBase GetCurrentElementOrCreateFromDefaultIfAvailable ()
        {
            if (_currentElement != null)
            {
                return _currentElement;
            }

            //No Element exists? Clone one from the Diagram Base (if available),
            if (DiagMapping.DefaultElementBase != null)
            {
                Debug.Log("ElementBinder returning DefaultElementBase for " + GetFullName());
                _currentElement = DiagMapping.DefaultElementBase.Clone();
                return _currentElement;
            }

            throw new Exception("No Element selected for " + DebugContext() + " and no Default available.");
        }

        /// <summary>
        /// Chooses the correct <see cref="ElementBase"/> based on the current Game and GameObject State.
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void SelectCurrentElement ()
        {
            bool found = false;
            //Go through all registered StatesAssociations.
            foreach (StatesAssociation sa in _elements.Keys)
                //Search current StatesAssociation for the current Game / Game Object State.
                if ((_currentGameState == GameStates.Undefined || sa.gameStates.Contains(_currentGameState)) &&
                    (_currentGameObjectState == GameObjectStates.Undefined || sa.gameObjectStates.Contains(_currentGameObjectState)))
                {
                    _currentElement = _elements[sa];
                    found = true;
                }

            if (!found && !MachinationsGameLayer.IsInOfflineMode)
                throw new Exception("ElementBinder.SelectCurrentElement: Couldn't find ElementBase for " + DebugContext());
        }

        /// <summary>
        /// Updates this Binder with the current Game State.
        /// </summary>
        /// <param name="gameState">New Game State.</param>
        public void UpdateGameState (GameStates gameState)
        {
            if (gameState == _currentGameState) return;
            _currentGameState = gameState;
            SelectCurrentElement();
        }

        /// <summary>
        /// Updates this Binder with the current Game Object State.
        /// </summary>
        /// <param name="gameObjectState">New Game Object State.</param>
        public void UpdateGameObjectState (GameObjectStates gameObjectState)
        {
            if (gameObjectState == _currentGameObjectState) return;
            _currentGameObjectState = gameObjectState;
            SelectCurrentElement();
        }

        /// <summary>
        /// Updates this Binder with the current Game & Game Object State.
        /// </summary>
        /// <param name="gameState">New Game State.</param>
        /// <param name="gameObjectState">New Game Object State.</param>
        public void UpdateStates (GameStates gameState, GameObjectStates gameObjectState)
        {
            if (gameState == _currentGameState && gameObjectState == _currentGameObjectState) return;
            _currentGameState = gameState;
            _currentGameObjectState = gameObjectState;
            SelectCurrentElement();
        }

        /// <summary>
        /// Retrieves a <see cref="ElementBase"/> for the desired StatesAssociation.
        /// </summary>
        /// <param name="statesAssociation">OPTIONAL. The <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/> for which the Holder is to be created.
        /// If this is not provided, the default value of NULL means that the Holder will use "N/A" as Title
        /// in the <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> Init Request.</param>
        /// <param name="overwrite">TRUE: overwrite the value if it's already in the <see cref="_elements"/> Dictionary.</param>
        /// <param name="isRunningOffline">TRUE: the <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> is running in offline mode.</param>
        public void CreateElementBaseForStateAssoc (StatesAssociation statesAssociation = null, bool overwrite = false,
            bool isRunningOffline = false)
        {
            Debug.Log("CreateElementBaseForStateAssoc in ElementBinder [Hash: " + GetHashCode() + "] '" +
                      GetFullName() + "' @ statesAssociation: " + (statesAssociation != null ? statesAssociation.Title : "N/A"));
            //The MachinationsGameLayer is responsible for creating ElementBase.
            ElementBase newElement = MachinationsGameLayer.Instance.CreateElement(this, statesAssociation);
            //If no element was found & running offline, just letting it slide.
            if (newElement == null && isRunningOffline)
                return;
            //If the Element isn't already in the Dictionary.
            if (!_elements.ContainsKey(statesAssociation ?? _noStatesAssociation))
                _elements.Add(statesAssociation ?? _noStatesAssociation, newElement);
            //The Element is already there, but may need updating due to incoming Machinations data.
            else if (overwrite)
            {
                _elements[statesAssociation ?? _noStatesAssociation] = newElement;
                //Element was switched. Make sure the correct Element is chosen.
                SelectCurrentElement();
            }
            else
                throw new Exception("CreateElementBaseForStateAssoc.CreateElementBaseForStateAssoc: Element collision for Binder: " +
                                    GetFullName());

            //MachinationsObjects (non-GameAware) will not use StatesAssociation and will only have a single Holder per Binder.
            if (statesAssociation == null)
                _currentElement = newElement;
        }

        /// <summary>
        /// Changes the CurrentElement's MInt Value by the given amount.
        /// </summary>
        /// <param name="amount"></param>
        public void ChangeValueWith (int amount)
        {
            CurrentElement.ChangeValueWith(amount);
        }

        /// <summary>
        /// Changes the CurrentElement's Int Value to the given one.
        /// </summary>
        /// <param name="value"></param>
        public void ChangeValueTo (int value)
        {
            CurrentElement.ChangeValueTo(value);
        }

        /// <summary>
        /// Returns the fully qualified name of this Binder.
        /// </summary>
        /// <returns></returns>
        private string GetFullName ()
        {
            return (ParentGameObject != null ? ParentGameObject.GameObjectName : "!NoParent!") + "." + DiagMapping.GameObjectPropertyName;
        }

        /// <summary>
        /// Outputs a string with the current state of this Binder, used for Debugging.
        /// </summary>
        /// <returns></returns>
        private string DebugContext ()
        {
            return "ElementBinder '" + GetFullName() + "' @ currentGameState: " + _currentGameState +
                   " and currentGameObjectState: " + _currentGameObjectState + ".";
        }

    }
}