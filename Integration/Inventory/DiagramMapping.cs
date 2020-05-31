using System;
using System.Runtime.Serialization;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.SyncAPI;

namespace MachinationsUP.Integration.Inventory
{
    /// <summary>
    /// Summarizes information about how a Machinations Game Object Property is mapped to the Diagram.
    /// Can be seen as the "coordinates" of a Game Object Property.
    /// See <see cref="MachinationsUP.Integration.Binder"/>
    /// </summary>
    [DataContract(Name = "MachinationsDiagramMapping", Namespace = "http://www.machinations.io")]
    public class DiagramMapping
    {

        private string _gameObjectName;

        /// <summary>
        /// The name of the Machinations Game Object that owns this property.
        /// See <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/>
        /// </summary>
        [DataMember()]
        public string GameObjectName
        {
            get => _gameObjectName;
            set => _gameObjectName = value;
        }

        private string _gameObjectPropertyName;
        
        /// <summary>
        /// The <see cref="ElementBinder"/> manifesting this Diagram Mapping in the game.
        /// </summary>
        public ElementBinder Binder { get; set; }

        /// <summary>
        /// The name of this Property.
        /// </summary>
        [DataMember()]
        public string GameObjectPropertyName
        {
            get => _gameObjectPropertyName;
            set => _gameObjectPropertyName = value;
        }

        private StatesAssociation _statesAssociation;

        /// <summary>
        /// The States Association where this Property applies.
        /// </summary>
        [DataMember()]
        public StatesAssociation StatesAssoc
        {
            get => _statesAssociation;
            set => _statesAssociation = value;
        }

        private int _diagramElementID;

        /// <summary>
        /// Machinations Back-end Element ID.
        /// </summary>
        [DataMember()]
        public int DiagramElementID
        {
            get => _diagramElementID;
            set => _diagramElementID = value;
        }

        /// <summary>
        /// How to handle situations when different values come from the Diagram.
        /// //TODO: Not implemented yet.
        /// </summary>
        public OverwriteRules OvewriteRule { get; set; }

        /// <summary>
        /// A Default ElementBase, to be used only during OFFLINE mode.
        /// </summary>
        public ElementBase DefaultElementBase { get; set; }

        /// <summary>
        /// The last value received from the Machinations back-end.
        /// </summary>
        [DataMember()]
        public ElementBase CachedElementBase { get; set; }

        /// <summary>
        /// Verifies if this DiagramMapping matches the provided criteria.
        /// </summary>
        /// <param name="gameObjectName">Game Object name to match.</param>
        /// /// <param name="gameObjectPropertyName">Game Object Property name to match.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (string gameObjectName, string gameObjectPropertyName, StatesAssociation statesAssociation,
            bool stringifyStatesAssociation)
        {
            return GameObjectName == gameObjectName && GameObjectPropertyName == gameObjectPropertyName &&
                   (
                       (stringifyStatesAssociation && (StatesAssoc == null || StatesAssoc.ToString() == statesAssociation.ToString())) ||
                       (!stringifyStatesAssociation && StatesAssoc == statesAssociation)
                   );
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches the Game Object-specific properties of an ElementBinder and
        /// any given <see cref="StatesAssociation"/>.
        /// </summary>
        /// <param name="elementBinder">Element Binder to verify.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (ElementBinder elementBinder, StatesAssociation statesAssociation, bool stringifyStatesAssociation)
        {
            return Matches(elementBinder.ParentGameObject?.GameObjectName, elementBinder.GameObjectPropertyName, statesAssociation,
                stringifyStatesAssociation);
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches another DiagramMapping.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (DiagramMapping diagramMapping, bool stringifyStatesAssociation)
        {
            return Matches(diagramMapping.GameObjectName, diagramMapping.GameObjectPropertyName, diagramMapping.StatesAssoc, stringifyStatesAssociation);
        }

        override public string ToString ()
        {
            return "DiagramMapping for " + GameObjectName + "." + GameObjectPropertyName + "." +
                   (_statesAssociation != null ? _statesAssociation.Title : "N/A") +
                   " bound to DiagramID: " + DiagramElementID;
        }

    }
}