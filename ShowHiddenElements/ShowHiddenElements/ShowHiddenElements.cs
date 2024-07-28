using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAddin
{
    [Transaction(TransactionMode.Manual)]
    public class ShowHiddenElementsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Collect all hidden elements in the active view
                var hiddenElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.IsHidden(doc.ActiveView))
                    .ToList();

                // If no hidden elements found, show a message and return
                if (hiddenElements.Count == 0)
                {
                    TaskDialog.Show("Hidden Elements", "No hidden elements found in the active view.");
                    return Result.Cancelled;
                }

                // Create a string to show hidden elements
                string hiddenElementsInfo = "Hidden Elements:\n";
                foreach (var element in hiddenElements)
                {
                    hiddenElementsInfo += $"Category: {element.Category?.Name ?? "Uncategorized"}, Id: {element.Id}\n";
                }

                // Show a Windows Forms dialog for confirmation
                DialogResult dialogResult = MessageBox.Show(
                    $"There are {hiddenElements.Count} hidden elements in the active view. Do you want to unhide them?",
                    "Unhide Elements",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    // Start a transaction to modify elements
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Unhide Elements");

                        foreach (var element in hiddenElements)
                        {
                            // Unhide each element
                            doc.ActiveView.UnhideElements(new List<ElementId> { element.Id });
                        }

                        // Commit the transaction
                        tx.Commit();

                        // Show a message box with the count of unhidden elements
                        MessageBox.Show($"Successfully unhidden {hiddenElements.Count} elements.", "Unhide Elements", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return Result.Succeeded;
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"Failed to show or unhide elements. Error: {ex.Message}";
                return Result.Failed;
            }
        }
    }

    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Define the tab and panel names
            string tabName = "KAITECH-BD-R06";
            string panelName = "QC";

            // Ensure the tab exists (if not, create it)
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                // Tab already exists
            }

            // Get the existing panel, or create a new one if it doesn't exist
            RibbonPanel ribbonPanel = null;
            List<RibbonPanel> panels = application.GetRibbonPanels(tabName);
            foreach (RibbonPanel panel in panels)
            {
                if (panel.Name == panelName)
                {
                    ribbonPanel = panel;
                    break;
                }
            }

            if (ribbonPanel == null)
            {
                ribbonPanel = application.CreateRibbonPanel(tabName, panelName);
            }

            // Create a push button in the ribbon panel
            string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("cmdShowHiddenElements", "Show Hidden Elements", thisAssemblyPath, "RevitAddin.ShowHiddenElementsCommand");

            // Set the icon for the button
            string iconPath = @"C:\Users\adghedy\OneDrive\Desktop\Heiden.ico"; // Replace with your actual icon path
            buttonData.LargeImage = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute));

            PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}