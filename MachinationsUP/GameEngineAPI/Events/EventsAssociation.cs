using System.Collections.Generic;

namespace MachinationsUP.GameEngineAPI.Events
{
	/// <summary>
	/// Represents an association of Game / Game Object Events, upon which something may have to happen.
	/// </summary>
	public class EventsAssociation
	{

		public List<string> gameEvents;
		public List<string> gameObjectEvents;

	}

}