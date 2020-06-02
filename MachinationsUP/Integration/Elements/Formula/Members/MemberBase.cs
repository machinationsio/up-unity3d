namespace MachinationsUP.Integration.Elements.Formula.Members
{
    /// <summary>
    /// Base class for all Machination Formula Members.
    /// <see cref="MachinationsFormula"/>
    /// </summary>
    public class MemberBase
    {

        /// <summary>
        /// Value of this Formula Member.
        /// </summary>
        public int Value { get; protected set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MemberBase (int value = -1)
        {
            Value = value;
        }

    }
}
