using System.Collections.Generic;
using System.Runtime.Serialization;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;

namespace MachinationsUP.GameEngineAPI.States
{
    /// <summary>
    /// Bundles a series of Game States with a series of Game Object States under a given Title.
    /// This will then be used to get the data from a given Machinations Diagram.
    /// </summary>
    [DataContract(Name = "MachinationsStatesAssociation", Namespace = "http://www.machinations.io")]
    public class StatesAssociation
    {

        private string _title;

        /// <summary>
        /// A name that represents this combination of Game and/or Game Object States.
        /// This will be used when requesting data from the Machinations Diagram, for a given States Association.
        /// </summary>
        [DataMember()]
        public string Title
        {
            get => _title;
            private set => _title = value;
        }

        /// <summary>
        /// Game States covered by this StatesAssociation.
        /// </summary>
        [DataMember()]
        readonly public List<GameStates> gameStates;

        /// <summary>
        /// Game Object States covered by this StatesAssociation.
        /// </summary>
        [DataMember()]
        readonly public List<GameObjectStates> gameObjectStates;

        //For Serialization only!
        private StatesAssociation ()
        {
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        /// <param name="title">A name that represents this combination of Game and/or Game Object States.
        /// This will be used when requesting data from the Machinations Diagram, for a given States Association.</param>
        /// <param name="gameStates">Game States covered by this StatesAssociation.</param>
        /// <param name="gameObjectStates">Game Object States covered by this StatesAssociation.</param>
        public StatesAssociation (string title,
            List<GameStates> gameStates = null,
            List<GameObjectStates> gameObjectStates = null)
        {
            this._title = title;
            this.gameStates = gameStates ?? new List<GameStates>();
            this.gameObjectStates = gameObjectStates ?? new List<GameObjectStates>();
        }

        override public string ToString ()
        {
            string ret = Title;
            if (gameStates != null)
                foreach (GameStates gs in gameStates)
                    ret += gs.ToString();
            if (gameObjectStates != null)
                foreach (GameObjectStates gs in gameObjectStates)
                    ret += gs.ToString();
            return ret == Title ? Title + "@N/A" : ret;
        }

    }
}
