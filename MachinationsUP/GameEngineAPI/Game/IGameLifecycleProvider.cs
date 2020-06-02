namespace MachinationsUP.GameEngineAPI.Game
{
    /// <summary>
    /// Defines a contract that allows a Game Engine to be queried/used by Machinations.
    /// See <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/>.
    /// </summary>
    public interface IGameLifecycleProvider
    {

        /// <summary>
        /// Return current GameState.
        /// </summary>
        GameStates GetGameState ();

        /// <summary>
        /// Notifies a Game Engine that Machinations is undergoing Initialization.
        /// Perhaps the Game wishes to pause itself during Machinations Initialization?
        /// </summary>
        void MachinationsInitStart ();
        
        /// <summary>
        /// Notifies a Game Engine that Machinations has completed Initialization.
        /// Perhaps the Game wishes to un-pause itself?
        /// </summary>
        void MachinationsInitComplete ();

    }
}