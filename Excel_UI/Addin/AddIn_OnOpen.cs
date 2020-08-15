/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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

using System;
using System.IO;
using System.Reflection;
using System.Linq;
using ExcelDna.Integration;
using System.Collections.Generic;
using System.Collections;
using BH.Engine.Reflection;
using BH.oM.Base;
using System.Linq.Expressions;
using BH.UI.Base;
using BH.UI.Excel.Templates;
using BH.UI.Excel.Components;
using BH.UI.Excel.Global;
using BH.UI.Base.Global;
using BH.UI.Base.Components;
using System.Runtime.InteropServices;
using NetOffice.ExcelApi;
using NetOffice.OfficeApi;
using NetOffice.OfficeApi.Enums;
using NetOffice.ExcelApi.Enums;
using System.Drawing;
using System.Xml;
using BH.oM.UI;
using BH.Engine.Base;

namespace BH.UI.Excel
{
    public partial class AddIn : IExcelAddIn
    {


        /*******************************************/
        /**** Methods                           ****/
        /*******************************************/

        public void AutoOpen()
        {
            m_Instance = this;

            ExcelDna.IntelliSense.IntelliSenseServer.Install();
            ExcelAsyncUtil.QueueAsMacro(() => InitBHoMAddin());

            m_Application = Application.GetActiveInstance();
            m_Application.WorkbookOpenEvent += App_WorkbookOpen;
        }


        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        private bool InitBHoMAddin()
        {
            if (m_Initialised)
                return true;
            if (m_GlobalSearch == null)
            {
                try
                {
                    m_GlobalSearch = new SearchMenu_WinForm();
                    m_GlobalSearch.ItemSelected += GlobalSearch_ItemSelected;
                }
                catch (Exception e)
                {
                    Engine.Reflection.Compute.RecordError(e.Message);
                }
            }
            ComponentManager.ComponentRestored += ComponentManager_ComponentRestored;
            m_Application.WorkbookBeforeCloseEvent += App_WorkbookClosed;

            ExcelDna.Registration.ExcelRegistration.RegisterCommands(ExcelDna.Registration.ExcelRegistration.GetExcelCommands());
            ExcelDna.IntelliSense.IntelliSenseServer.Refresh();
            m_Initialised = true;
            ExcelDna.Logging.LogDisplay.Clear();
            return true;
        }

        /*******************************************/

        private void ComponentManager_ComponentRestored(object sender, KeyValuePair<string, Tuple<string, string>> restored)
        {
            string key = restored.Key;
            string json = restored.Value.Item2;
            string callerType = restored.Value.Item1;
            if (Callers.ContainsKey(callerType))
            {
                var formula = Callers[callerType];
                if (formula.Caller.Read(json))
                {
                    if (formula.Function != key)
                    {
                        if (formula.Caller.SelectedItem != null)
                            new UI.Global.ComponentUpgrader(key, formula); // TODO: Look into this, seems weird
                        else
                            return;
                    }
                    formula.Register();
                }
            }
        }

        /*******************************************/

        private void App_WorkbookOpen(Workbook workbook)
        {
            // Try to recover the sheet where data was saved if it exists
            Sheets sheets = workbook.Sheets;
            Worksheet dataSheet = null;
            if (sheets.Contains("BHoM_DataHidden"))
                dataSheet = sheets["BHoM_DataHidden"] as Worksheet;
            else if (sheets.Contains("BHoM_DataHidden"))
                dataSheet = sheets["BHoM_Data"] as Worksheet; // Backwards compatibility
            else
                return;

            // Initialise the BHoM Addin and run first calculation

            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                bool hasComponents = sheets.OfType<Worksheet>().FirstOrDefault(s => s.Name == "BHoM_ComponetRequests") != null;
                if (!hasComponents)
                {
                    var searcher = new FormulaSearchMenu(Callers); //TODO: Look into this, seems weird
                    searcher.SetParent(null);
                }
                else
                    ComponentManager.GetManager(workbook).Restore();

                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    foreach (Worksheet sheet in sheets.OfType<Worksheet>())
                    {
                        bool before = sheet.EnableCalculation;
                        sheet.EnableCalculation = false;
                        sheet.Calculate();
                        sheet.EnableCalculation = before;
                    }
                });
            });

            // Collect all the saved json strings
            List<string> json = new List<string>();
            foreach (Range row in dataSheet.UsedRange.Rows)
            {
                string str = "";
                try
                {
                    Range cell = row.Cells[1, 1];
                    while (cell.Value != null && cell.Value is string && (cell.Value as string).Length > 0)
                    {
                        str += cell.Value;
                        cell = cell.Next;
                    }
                }
                catch { }

                if (str.Length > 0)
                    json.Add(str);
            }

            // Restore project state
            Project.ActiveProject.Deserialize(json);
        }

        /*******************************************/
        /**** Private Fields                    ****/
        /*******************************************/

        private bool m_Initialised = false;

        /*******************************************/
    }
}
