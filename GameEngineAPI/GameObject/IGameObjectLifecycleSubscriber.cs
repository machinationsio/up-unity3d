namespace MachinationsUP.GameEngineAPI.GameObject
{
	public interface IGameObjectLifecycleSubscriber
	{

		/// <summary>
		/// Returns the current Game Object State.
		/// </summary>
		GameObjectStates CurrentGameObjectState { get; }
		
		/// <summary>
		/// Informs an IGameObjectLifecycleSubscriber about a new Game Object State.
		/// </summary>
		/// <param name="newGameObjectState"></param>
		void OnGameObjectStateChanged(GameObjectStates newGameObjectState);

		/// <summary>
		/// Informs an IGameObjectLifecycleSubscriber that a Game Object Event occured.
		/// </summary>
		/// <param name="evnt"></param>
		void OnGameObjectEvent(string evnt);
	}

}