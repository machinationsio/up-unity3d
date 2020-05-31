using System;
using System.Collections.Generic;
using MachinationsUP.Engines.Unity;
using MachinationsUP.GameEngineAPI.Events;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.SyncAPI;
using UnityEngine;

namespace MachinationsUP.Integration.GameObject
{
    /// <summary>
    /// A MachinationsGameObject holds & manages Machinations-related information about
    /// a Game Object. Its main purpose is to hold a MachinationsGameObjectManifest that
    /// defines what Game Object Properties are to be retrieved from the Machinations Diagram.
    /// </summary>
    public class MachinationsGameObject
    {

        readonly protected string _gameObjectName;

        /// <summary>
        /// The Game Object Name. A Game Object can hold multiple Game Object Properties.
        /// When querrying the Machinations diagram, this name is used as the root of all Properties.
        /// <see cref="MachinationsUP.Integration.Binder.ElementBinder"/>
        /// </summary>
        public string GameObjectName
        {
            get { return _gameObjectName; }
        }

        readonly protected MachinationsGameObjectManifest _manifest;

        /// <summary>
        /// Stores data required to initialize a Machination Game Object.
        /// </summary>
        public MachinationsGameObjectManifest Manifest => _manifest;

        /// <summary>
        /// Event triggered when MachinationsGameLayer initialization is completed.
        /// </summary>
        public event EventHandler OnBindersUpdated;

        /// <summary>
        /// Dictionary of Game Object Property Name and <see cref="MachinationsUP.Integration.Binder.ElementBinder"/>.
        /// Defines how this Game Object maps to Machinations based on the Game State and on this
        /// own elements Game Element State. Each states combination MAY map to a different
        /// MachinationsElementBase.
        /// </summary>
        readonly protected Dictionary<string, ElementBinder> _binders = new Dictionary<string, ElementBinder>();

        public Dictionary<EventsAssociation, UpdateRules> commonEventMapping;
        public Dictionary<StatesAssociation, OverwriteRules> commonOverwriteMapping;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="manifest">The Manifest that will be used to initialized this MachinationsGameObject.</param>
        /// <param name="onBindersUpdated">When a <see cref="MachinationsGameObject"/> enrolls itself
        /// using <see cref="MachinationsGameLayer.EnrollGameObject"/>, this event WILL fire if the MachinationsGameLayer
        /// has been initialized. So, this is why it is allowed to send an EventHandler callback upon Construction.
        /// </param>
        public MachinationsGameObject (MachinationsGameObjectManifest manifest, EventHandler onBindersUpdated = null)
        {
            _gameObjectName = manifest.GameObjectName;
            _manifest = manifest;
            foreach (DiagramMapping diagramMapping in _manifest.PropertiesToSync)
                CreateBinder(diagramMapping);
            //Assign event, if any was provided. This has to be done before EnrollGameObject, because that function
            //may call MGLInitComplete, which may in turn call OnBindersUpdated.
            if (onBindersUpdated != null)
                OnBindersUpdated = onBindersUpdated;
            MachinationsGameLayer.EnrollGameObject(this);
        }

        /// <summary>
        /// Returns the MachinationsElementBase for the given gameObjectPropertyName
        /// </summary>
        /// <param name="gameObjectPropertyName"></param>
        public ElementBinder this [string gameObjectPropertyName] => _binders[gameObjectPropertyName];

        /// <summary>
        /// Creates an <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> for the required Game Object Property Name.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="MachinationsUP.Integration.Inventory.DiagramMapping"/> of this Game Object Property Name.</param>
        /// <returns>A new ElementBinder.</returns>
        private void CreateBinder (DiagramMapping diagramMapping)
        {
            _binders.Add(diagramMapping.GameObjectPropertyName, new ElementBinder(this, diagramMapping));
        }

        /// <summary>
        /// Called by <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> when initialization is complete.
        /// For all <see cref="MachinationsUP.Integration.Binder.ElementBinder"/>, retrieves their
        /// required <see cref="MachinationsUP.Integration.Elements.ElementBase"/> from MGL.
        /// <param name="isRunningOffline">TRUE: the <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> is running in offline mode.</param>
        /// </summary>
        virtual internal void MGLInitComplete (bool isRunningOffline = false)
        {
            Debug.Log("MGLInitComplete in " + this);
            //Go through all Binders and ask them to retrieve their ElementBase.
            foreach (string gameObjectPropertyName in _binders.Keys)
                _binders[gameObjectPropertyName].CreateElementBaseForStateAssoc(null, isRunningOffline, isRunningOffline);

            //Notify any listeners of base.OnBindersUpdated.
            NotifyBindersUpdated();
        }
        
        /// <summary>
        /// Called by <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> if it's ready for work.
        /// Usually called just after a MachinationsGameObject Enrolls, if the MGL is initialized.
        /// </summary>
        public void MGLReady ()
        {
            MGLInitComplete();
        }

        /// <summary>
        /// Asks this Game-Aware Object to update one of its <see cref="MachinationsUP.Integration.Binder.ElementBinder"/> usually because
        /// new values arrived from the Machinations Back-end.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="DiagramMapping"/> associated with this Binder.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> obtained by parsing the back-end update.</param>
        virtual internal void UpdateBinder (DiagramMapping diagramMapping, ElementBase elementBase)
        {
            //TODO: on update, shouldn't create new elements, but rather UPDATE the current element.
            //Ask the necessary Binder to go get its ElementBase.
            _binders[diagramMapping.GameObjectPropertyName].CreateElementBaseForStateAssoc(null, true);
            //Notify any listeners of base.OnBindersUpdated.
            NotifyBindersUpdated();
        }

        /// <summary>
        /// Notifies any listeners that the MGL Init Request has completed.
        /// </summary>
        protected void NotifyBindersUpdated ()
        {
            OnBindersUpdated?.Invoke(this, EventArgs.Empty);
        }

        virtual protected string DebugContext ()
        {
            return "MachinationsGameObject '" + _gameObjectName + "'";
        }

        override public string ToString ()
        {
            return DebugContext();
        }

    }
}