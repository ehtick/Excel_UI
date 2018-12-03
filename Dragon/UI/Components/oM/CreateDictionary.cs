﻿using System;
using BH.oM.Base;
using BH.UI.Dragon.Templates;
using BH.UI.Templates;
using BH.UI.Components;

namespace BH.UI.Dragon.Components
{
    public class CreateDictionaryFormula : CallerFormula
    {
        /*******************************************/
        /**** Properties                        ****/
        /*******************************************/

        public override Caller Caller { get; } = new CreateDictionaryCaller();

        /*******************************************/
        /**** Constructors                      ****/
        /*******************************************/


        public CreateDictionaryFormula(FormulaDataAccessor accessor) : base(accessor) { }
        /*******************************************/
    }
}
