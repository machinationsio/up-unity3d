using System.Runtime.Serialization;

namespace MachinationsUP.Integration.Elements
{
    /// <summary>
    /// Wraps a Machinations Value.
    /// </summary>
    [DataContract(Name = "MachinationsElementBase", Namespace = "http://www.machinations.io")]
    [KnownType(typeof(FormulaElement))]
    public class ElementBase
    {

        private int _baseValue;

        /// <summary>
        /// The value received from Machinations.
        /// </summary>
        [DataMember()]
        public int BaseValue
        {
            get => _baseValue;
            protected set => _baseValue = value;
        }

        protected int _currentValue;

        /// <summary>
        /// INT value of this ElementBase.
        /// May be overrided in Child Classes.
        /// </summary>
        [DataMember()]
        virtual public int CurrentValue
        {
            get => _currentValue;
            protected set => _currentValue = value;
        }

        private int _maxValue;

        /// <summary>
        /// Top cap.
        /// </summary>
        [DataMember()]
        public int MaxValue { get; set; }

        private int _minValue;

        /// <summary>
        /// Bottom cap. NOT USED YET.
        /// </summary>
        [DataMember()]
        public int MinValue { get; set; }

        //For Serialization only!
        private ElementBase ()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="baseValue">Value to start with.</param>
        public ElementBase (int baseValue = -1)
        {
            BaseValue = baseValue;
            MaxValue = -1;
            Reset();
        }

        /// <summary>
        /// Resets the Current Value to the Base Value.
        /// </summary>
        virtual protected void Reset ()
        {
            CurrentValue = BaseValue;
        }

        /// <summary>
        /// Makes sure the value doesn't exceed Min or Max caps.
        /// </summary>
        private void Clamp ()
        {
            if (MaxValue != -1 && CurrentValue > MaxValue) CurrentValue = MaxValue;
        }

        /// <summary>
        /// Changes the Int Value with the given amount.
        /// TODO: think how to support other than MInt.
        /// </summary>
        /// <param name="amount"></param>
        public void ChangeValueWith (int amount)
        {
            CurrentValue += amount;
            Clamp();
        }

        /// <summary>
        /// Changes the MInt Value to the given one.
        /// </summary>
        /// <param name="value"></param>
        public void ChangeValueTo (int value)
        {
            CurrentValue = value;
            Clamp();
        }

        /// <summary>
        /// Returns a duplicate of this Element Base. Required in <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> in CreateElement.
        /// </summary>
        /// <returns></returns>
        virtual public ElementBase Clone ()
        {
            return new ElementBase(BaseValue);
        }

        override public string ToString ()
        {
            return BaseValue.ToString();
        }

    }
}
