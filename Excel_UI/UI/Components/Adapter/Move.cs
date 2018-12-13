﻿using System;

using BH.oM.Base;

using BH.UI.Excel.Templates;
using BH.UI.Templates;
using BH.UI.Components;

namespace BH.UI.Excel.Components
{
    public class MoveFormula : CallerFormula
    {
        /*******************************************/
        /**** Properties                        ****/
        /*******************************************/

        public override string Name => "Adapter." + Caller.Name;

        public override Caller Caller { get; } = new MoveCaller();

        /*******************************************/
        /**** Constructors                      ****/
        /*******************************************/

        public MoveFormula(FormulaDataAccessor accessor) : base(accessor) { }

        /*******************************************/
    }
}