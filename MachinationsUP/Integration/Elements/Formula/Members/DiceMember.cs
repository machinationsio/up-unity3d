using System;

namespace MachinationsUP.Integration.Elements.Formula.Members
{
    /// <summary>
    /// A simple, humble, Random Number Generator.
    /// <see cref="MachinationsFormula"/>
    /// </summary>
    public class DiceMember : MemberBase
    {

        /// <summary>
        /// Bottom bound of the DiceMember.
        /// </summary>
        public int Min {  get; private set; }

        /// <summary>
        /// Top bound of the DiceMember.
        /// </summary>
        public int Max {  get; private set; }

        readonly private Random _rnd = new Random();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DiceMember (int min, int max)
        {
            Min = min;
            Max = max;
            Throw();
        }

        /// <summary>
        /// Throws the DiceMember, what else? :)
        /// </summary>
        public void Throw ()
        {
            Value = _rnd.Next(Min, Max);
        }

    }
}
