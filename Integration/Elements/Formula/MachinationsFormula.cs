using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MachinationsUP.Integration.Elements.Formula.Members;

namespace MachinationsUP.Integration.Elements.Formula
{
    /// <summary>
    /// Machination Formulas are used by FormulaELements to calculate values.
    /// <see cref="FormulaElement"/>
    /// UNDER HEAVY CONSTRUCTION.
    /// </summary>
    [DataContract(Name = "MachinationsFormula", Namespace = "http://www.machinations.io")]
    public class MachinationsFormula
    {

        /// <summary>
        /// Formula passed in.
        /// </summary>
        [DataMember()]
        private string FormulaString { get; set; }

        /// <summary>
        /// Types of operators that Formulas can use.
        /// UNDER HEAVY CONSTRUCTION.
        /// </summary>
        enum Operators
        {

            Plus,
            Minus

        }

        /// <summary>
        /// List of Members that this Formula has. Running the Formula will execute all Members, in order of the List,
        /// applying the associated Operator.
        /// </summary>
        private List<KeyValuePair<MemberBase, Operators?>> _members = new List<KeyValuePair<MemberBase, Operators?>>();

        //For Serialization only!
        private MachinationsFormula ()
        {
        }

        /// <summary>
        /// Default constructor. Parses the provided string to build the Formula Members.
        /// Examples of Formulas: 20+D24, -20 - D15.
        /// </summary>
        /// <see cref="MemberBase"/>
        public MachinationsFormula (string formulaString)
        {
            FormulaString = formulaString;
            string number = ""; //Currently processed number.
            bool isDice = false; //Has a Dice function been activated?
            Operators? op = Operators.Plus; //Default Operator.

            //Process Formula char by char.
            foreach (char c in formulaString)
            {
                if (char.IsWhiteSpace(c)) continue;

                //Append all digits.
                if (char.IsDigit(c)) number += c;
                //Detect Dice.
                else if (c == 'D') isDice = true;
                //Most likely an Operator.
                else
                {
                    //Was there any Operator before?
                    if (op != null)
                    {
                        _members.Add(new KeyValuePair<MemberBase, Operators?>(new MemberBase(int.Parse(number)), op));
                        number = "";
                    }

                    op = ExtractOperator(c);
                }
            }

            //Apply any previous Dice
            if (!isDice)
                _members.Add(new KeyValuePair<MemberBase, Operators?>(new MemberBase(int.Parse(number)), op));
            else
                _members.Add(new KeyValuePair<MemberBase, Operators?>(new DiceMember(1, int.Parse(number)), op));
        }

        /// <summary>
        /// Gets an Operator from a Char.
        /// </summary>
        private Operators? ExtractOperator (char c)
        {
            switch (c)
            {
                case '-':
                    return Operators.Minus;
                case '+':
                    return Operators.Plus;
            }

            return null;
        }

        /// <summary>
        /// Runs the Formula, returning its value.
        /// </summary>
        public int Run ()
        {
            int ret = 0;
            //Go through each Member and extract its value.
            foreach (KeyValuePair<MemberBase, Operators?> memberPair in _members)
            {
                switch (memberPair.Value)
                {
                    case Operators.Minus:
                        ret -= memberPair.Key.Value;
                        break;
                    case Operators.Plus:
                        ret += memberPair.Key.Value;
                        break;
                    case null:
                        throw new Exception("Operator cannot be NULL.");
                }
            }

            return ret;
        }

        override public string ToString ()
        {
            return FormulaString;
        }

    }
}
