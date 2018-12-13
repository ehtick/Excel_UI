﻿using System;
using BH.oM.Base;
using BH.UI.Excel.Templates;
using BH.UI.Templates;
using BH.UI.Components;

namespace BH.UI.Excel.Components
{
    public class SetPropertyFormula : CallerFormula
    {
        /*******************************************/
        /**** Properties                        ****/
        /*******************************************/

        public override Caller Caller { get; } = new SetPropertyCaller();

        /*******************************************/
        /**** Constructors                      ****/
        /*******************************************/

        public SetPropertyFormula(FormulaDataAccessor accessor) : base(accessor) { }

        /*******************************************/
    }
}