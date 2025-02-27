/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *
 *
 * The BHoM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License, or
 * (at your option) any later version.
 *
 * The BHoM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.
 */

using BH.UI.Excel.Templates;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BH.UI.Excel.Addin
{
    public partial class Ribbon : ExcelRibbon
    {
        /*******************************************/
        /**** Methods                           ****/
        /*******************************************/

        public void RunCondense(IRibbonControl control)
        {
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                AddIn.WriteFormula("=BHoM.Condense");
            });
        }

        /*******************************************/

        [ExcelFunction(Name = "BHoM.Condense", Description = "Take a group of cells and store their content as a list in a single cell.", Category = "UI")]
        public static object Condense(object[] items)
        {
            items = AddIn.FromExcel(items.Where(x => !(x is ExcelEmpty)).ToArray());
            return AddIn.ToExcel(items.ToList());
        }

        /*******************************************/
    }
}


