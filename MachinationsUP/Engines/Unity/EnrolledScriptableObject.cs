using System.Collections.Generic;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    
    /// <summary>
    /// Used by <see cref="MachinationsGameLayer"/> to keep track of enrolled Scriptable Objects.
    /// </summary>
    public class EnrolledScriptableObject
    {

        /// <summary>
        /// The <see cref="IMachinationsScriptableObject"/> represented by this class.
        /// </summary>
        public IMachinationsScriptableObject MScriptableObject;
        
        /// <summary>
        /// The <see cref="MachinationsGameObjectManifest"/> defining what
        /// the <see cref="IMachinationsScriptableObject"/> needs.
        /// </summary>
        public MachinationsGameObjectManifest Manifest;
        
        /// <summary>
        /// The Binders used by the <see cref="IMachinationsScriptableObject"/>.
        /// They can be set only AFTER <see cref="MachinationsGameLayer"/> initialization. 
        /// </summary>
        public Dictionary<string, ElementBinder> Binders;

    }
}