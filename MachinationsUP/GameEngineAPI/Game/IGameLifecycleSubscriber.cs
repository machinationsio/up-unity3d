using MachinationsUP.SyncAPI;

namespace MachinationsUP.GameEngineAPI.Game
{
    /// <summary>
    /// Defines a contract that the Machinations Game Objects will follow in order to
    /// be notified by the Game Engine about State Changes.
    /// </summary>
    public interface IGameLifecycleSubscriber
    {

        /// <summary>
        /// Returns the current Game State.
        /// </summary>
        GameStates CurrentGameState { get; }

        /// <summary>
        /// Informs an IGameLifecycleSubscriber about a new Game State.
        /// </summary>
        /// <param name="newGameState"></param>
        void OnGameStateChanged (GameStates newGameState);

        /// <summary>
        /// Informs an IGameLifecycleSubscriber that a Game Event occured.
        /// </summary>
        /// <param name="evnt"></param>
        void OnGameEvent (string evnt);

        /// <summary>
        /// Machinations -> Game commands. Intended for Future use.
        /// </summary>
        /// <param name="command"></param>
        void GameCommand (MachinationsCommands command);

    }
}
