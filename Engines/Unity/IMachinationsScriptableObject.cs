using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    /// <summary>
    /// Defines a contract that allows a Scriptable Object to be notified by Machinations.
    /// See <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/>.
    /// </summary>
    public interface IMachinationsScriptableObject
    {

        /// <summary>
        /// Called when Machinations initialization has been completed.
        /// </summary>
        void MGLInitCompleteSO ();

        /// <summary>
        /// Called by the <see cref="MachinationsGameLayer"/> when an element has been updated in the Machinations back-end.
        /// </summary>
        /// <param name="diagramMapping">The <see cref="DiagramMapping"/> of the modified element.</param>
        /// <param name="elementBase">The <see cref="ElementBase"/> that was sent from the backend.</param>
        void MGLUpdateSO (DiagramMapping diagramMapping = null, ElementBase elementBase = null);

    }
}