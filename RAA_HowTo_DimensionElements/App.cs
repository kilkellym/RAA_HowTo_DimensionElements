#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows.Markup;

#endregion

namespace RAA_HowTo_DimensionElements
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            //// 1. Create ribbon tab
            //string tabName = "My First Revit Add-in";
            //try
            //{
            //    app.CreateRibbonTab(tabName);
            //}
            //catch (Exception)
            //{
            //    Debug.Print("Tab already exists.");
            //}

            //// 2. Create ribbon panel 
            //RibbonPanel panel = Utils.CreateRibbonPanel(app, tabName, "Revit Tools");

            //// 3. Create button data instances
            //PushButtonData btnData1 = Command1.GetButtonData();
            //PushButtonData btnData2 = Command2.GetButtonData();

            //// 4. Create buttons
            //PushButton myButton1 = panel.AddItem(btnData1) as PushButton;
            //PushButton myButton2 = panel.AddItem(btnData2) as PushButton;

            // NOTE:
            // To create a new tool, copy lines 35 and 39 and rename the variables to "btnData3" and "myButton3". 
            // Change the name of the tool in the arguments of line 

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
