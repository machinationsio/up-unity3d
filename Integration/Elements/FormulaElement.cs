using System.Runtime.Serialization;
using System.Xml.Serialization;
using MachinationsUP.Integration.Elements.Formula;
using UnityEngine;

namespace MachinationsUP.Integration.Elements
{
    /// <summary>
    /// Wraps a Machinations Formula, allowing Element-related access to it.
    /// <see cref="MachinationsFormula"/>
    /// </summary>
    [DataContract(Name = "MachinationsFormula", Namespace = "http://www.machinations.io")]
    public class FormulaElement : ElementBase
    {

        private MachinationsFormula _mFormula;

        /// <summary>
        /// Machinations Formula that is used to determine this Element's value.
        /// </summary>
        [DataMember()]
        public MachinationsFormula MFormula
        {
            get => _mFormula;
            private set => _mFormula = value;
        }

        /// <summary>
        /// If to recalculate the BaseValue from the Formula at reset.
        /// </summary>
        [DataMember()]
        public bool RerunFormulaAtReset { get; private set; }

        /// <summary>
        /// If to recalculate the BaseValue from the Formula each time the CurrentValue is queried.
        /// </summary>
        [DataMember()]
        public bool RerunFormulaAtEveryAccess { get; private set; }

        /// <summary>
        /// Formula that this Element uses.
        /// </summary>
        [DataMember()]
        public string FormulaString { get; private set; }

        //For Serialization only!
        private FormulaElement ()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="formulaString">Formula to use.</param>
        /// <param name="rerunFormulaAtReset">If to recalculate the BaseValue from the Formula at reset.</param>
        /// <param name="rerunFormulaAtEveryAccess">If to recalculate the BaseValue from the Formula each time the CurrentValue is queried.</param>
        public FormulaElement (string formulaString, bool rerunFormulaAtReset = true, bool rerunFormulaAtEveryAccess = true)
        {
            FormulaString = formulaString;
            MFormula = new MachinationsFormula(formulaString);
            RerunFormulaAtReset = rerunFormulaAtReset;
            RerunFormulaAtEveryAccess = rerunFormulaAtEveryAccess;
            BaseValue = MFormula.Run();
            base.Reset();
        }

        /// <summary>
        /// The Current Value of this Element. May change depending on <see cref="RerunFormulaAtReset"/> and
        /// <see cref="RerunFormulaAtEveryAccess"/>.
        /// </summary>
        [XmlIgnore]
        override public int CurrentValue
        {
            get
            {
                if (RerunFormulaAtEveryAccess)
                {
                    BaseValue = MFormula.Run();
                    base.Reset();
                }

                return _currentValue;
            }
            protected set => _currentValue = value;
        }

        /// <summary>
        /// Resets the Current Value to the Base Value, potentially re-running the Formula.
        /// </summary>
        override protected void Reset ()
        {
            if (RerunFormulaAtReset)
                BaseValue = MFormula.Run();
            base.Reset();
        }

        /// <summary>
        /// Returns a duplicate of this Element Base. Required in <see cref="MachinationsUP.Engines.Unity.MachinationsGameLayer"/> in CreateElement.
        /// </summary>
        /// <returns></returns>
        override public ElementBase Clone ()
        {
            return new FormulaElement(FormulaString, RerunFormulaAtReset, RerunFormulaAtEveryAccess);
        }

        override public string ToString ()
        {
            return "F: " + MFormula;
        }

    }
}
