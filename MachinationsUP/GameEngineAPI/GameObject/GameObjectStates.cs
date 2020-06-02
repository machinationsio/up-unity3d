using System.Runtime.Serialization;

namespace MachinationsUP.GameEngineAPI.GameObject
{

    /// <summary>
    /// Example Enum for what cound be a Game's States.
    /// Machinations can use such States in order to switch between different
    /// designs, contained in different diagrams.
    /// <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/>
    /// </summary>
    [DataContract(Name = "RubyGameObjectStates", Namespace = "http://www.rubygame.com")]
    public enum GameObjectStates
    {

        Undefined,
        Idle,
        Walking,
        Running,
        Fighting,
        Talking,
        Sleeping,

    }
}
