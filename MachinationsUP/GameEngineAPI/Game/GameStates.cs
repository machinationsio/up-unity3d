using System.Runtime.Serialization;

namespace MachinationsUP.GameEngineAPI.Game
{

    /// <summary>
    /// Example Enum for what cound be a Game's States.
    /// Machinations can use such States in order to switch between different
    /// designs, contained in different diagrams.
    /// See <see cref="MachinationsUP.GameEngineAPI.States.StatesAssociation"/>.
    /// </summary>
    [DataContract(Name = "RubyGameStates", Namespace = "http://www.rubygame.com")]
    public enum GameStates
    {

        [EnumMemberAttribute]
        Undefined,
        Idle,
        [EnumMemberAttribute]
        Exploring,
        Fighting,
        Tactical,
        Strategical,
        LevelBoss,
        SomeOtherBoss,
        Trading,
        Diplomacy,
        Unused

    }
}
