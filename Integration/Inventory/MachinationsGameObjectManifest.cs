using System.Collections.Generic;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Elements;

namespace MachinationsUP.Integration.Inventory
{
    /// <summary>
    /// Stores data required to initialize a <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> from the Machinations Diagram.
    /// </summary>
    public class MachinationsGameObjectManifest
    {

        /// <summary>
        /// Game Object ID where all Game Object Property Names are located.
        /// <see cref="PropertiesToSync"/>
        /// </summary>
        public string GameObjectName { get; set; }

        /// <summary>
        /// The Game Object Property Names to retrieve from the Machinations Back-end.
        /// If there are StatesAssociations defined (via StatesAssociationsPerPropertyName or
        /// CommonStatesAssociations), they will be retrieved per each StateAssociation.
        /// <see cref="StatesAssociationsPerPropertyName"/>
        /// </summary>
        public List<DiagramMapping> PropertiesToSync { get; set; }

        /// <summary>
        /// Dictionary of gameObjectPropertyName and StatesAssociation.
        /// The Game Object Property will be retrieved for each of the StatesAssociations.
        /// <see cref="PropertiesToSync"/>
        /// </summary>
        public Dictionary<string, List<StatesAssociation>> StatesAssociationsPerPropertyName { get; set; }

        /// <summary>
        /// List of StatesAssociation to be requested for ALL the DiagramMappings.
        /// </summary>
        public List<StatesAssociation> CommonStatesAssociations { get; set; }

        /// <summary>
        /// What Events are emitted by the <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/> owning this Manifest.
        /// </summary>
        public List<string> EventsToEmit { get; set; }

        /// <summary>
        /// Creates a Dictionary of DiagramMapping and ElementBase.
        /// The dictionary represents all MachinationsElements that are required to satisfy
        /// this GameObjectManifest, including their Diagram whereabouts (via DiagramMapping).
        /// </summary>
        /// <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/>
        public Dictionary<DiagramMapping, ElementBase> GetMachinationsDiagramTargets ()
        {
            Dictionary<DiagramMapping, ElementBase> targets =
                new Dictionary<DiagramMapping, ElementBase>();

            foreach (DiagramMapping diagramMapping in PropertiesToSync)
            {
                diagramMapping.GameObjectName = GameObjectName;
                //If StatesAssociations were defined and this Property is found, create a separate
                //machinationsUniqueID *per* StateAssociation.
                if (StatesAssociationsPerPropertyName != null &&
                    StatesAssociationsPerPropertyName.ContainsKey(diagramMapping.GameObjectPropertyName))
                    foreach (StatesAssociation sa in StatesAssociationsPerPropertyName[diagramMapping.GameObjectPropertyName])
                    {
                        diagramMapping.StatesAssoc = sa;
                        targets.Add(diagramMapping, null);
                    }
                else
                {
                    //Only add "N/A" to Game Objects that don't have a CommonStatesAssociations.
                    if (CommonStatesAssociations == null)
                        targets.Add(diagramMapping, null);
                }

                //If any CommonStatesAssociations were defined, applying them to all elements.
                if (CommonStatesAssociations != null)
                    foreach (StatesAssociation sa in CommonStatesAssociations)
                    {
                        diagramMapping.StatesAssoc = sa;
                        targets.Add(diagramMapping, null);
                    }
            }

            return targets;
        }

        /// <summary>
        /// Returns all States Associations that a certain Game Object Property is involved in.
        /// </summary>
        /// <param name="gameObjectPropertyName"></param>
        /// <returns></returns>
        public List<StatesAssociation> GetStatesAssociationsForPropertyName (string gameObjectPropertyName)
        {
            var ret = new List<StatesAssociation>();
            //Add all CommonStatesAssociations, if defined.
            if (CommonStatesAssociations != null) ret.AddRange(CommonStatesAssociations);
            //Add only those StatesAssociations that match the gameObjectPropertyName from StatesAssociationsPerPropertyName.
            if (StatesAssociationsPerPropertyName != null && StatesAssociationsPerPropertyName.ContainsKey(gameObjectPropertyName))
                ret.AddRange(StatesAssociationsPerPropertyName[gameObjectPropertyName]);
            return ret;
        }

        override public string ToString ()
        {
            return "MachinationsGameObjectManifest for " + GameObjectName;
        }

        /// <summary>
        /// Returns the <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> that matches the requested Game Object Property Name.
        /// </summary>
        public DiagramMapping GetDiagramMapping (string gameObjectPropertyName)
        {
            foreach (DiagramMapping dm in PropertiesToSync)
                if (dm.GameObjectPropertyName == gameObjectPropertyName)
                    return dm;
            return null;
        }

    }
}
