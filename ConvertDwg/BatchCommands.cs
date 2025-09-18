
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Microsoft.Extensions.Logging;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using OpenFileDialog = Autodesk.AutoCAD.Windows.OpenFileDialog;

namespace BP.ConvertDwg
{
    public static class BatchCommands
    {
        public static void BatchConvert(bool debug)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddDebug());
            var configFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logger = factory.CreateLogger(nameof(PidConverter));
            var count = 0;
            var failed = 0;
            try
            {
                var config = Config.Load(configFolder);
                var sourceFolder = config?.Source;
                if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
                {
                    var srcDialog = new OpenFileDialog("Select source folder", "defaultName", "extension", "dialogName", OpenFileDialog.OpenFileDialogFlags.AllowFoldersOnly);
                    srcDialog.ShowDialog();
                    sourceFolder = srcDialog.Filename;
                }

                if (string.IsNullOrEmpty(sourceFolder))
                {
                    return;
                }

                config = config ?? Config.Load(sourceFolder);
                if (config == null || config.Tags.Count == 0)
                {
                    MessageBox.Show("No configurations loaded. Exiting!");
                    return;
                }

                var destFolder = config?.Destination;
                if (string.IsNullOrEmpty(destFolder) || !Directory.Exists(destFolder))
                {
                    var destDialog = new OpenFileDialog("Select destination folder", "defaultName", "extension", "dialogName", OpenFileDialog.OpenFileDialogFlags.AllowFoldersOnly);
                    destDialog.ShowDialog();
                    destFolder = destDialog.Filename;
                }

                if (string.IsNullOrEmpty(destFolder))
                {
                    return;
                }

                var sourceFiles = Directory.GetFiles(sourceFolder, "*.dwg");
                var destFiles = Directory.GetFiles(destFolder);
                if (destFiles.Length > 0)
                {
                    MessageBox.Show("Destination folder has to be empty!");
                    return;
                }

                foreach (var dwg in sourceFiles)
                {
                    Document docToWorkOn = null;
                    try
                    {
                        docToWorkOn = Application.DocumentManager.Open(dwg, true);


                        Application.DocumentManager.MdiActiveDocument = docToWorkOn;
                        using (docToWorkOn.LockDocument())
                        {
                            var fileName = Path.GetFileName(dwg);
                            new PidConverter().Convert(docToWorkOn, debug, config.Tags);
                            docToWorkOn.Database.SaveAs(Path.Combine(destFolder, fileName), Autodesk.AutoCAD.DatabaseServices.DwgVersion.Current);
                        }
                        docToWorkOn.CloseAndDiscard();
                        docToWorkOn = null;
                        count++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Error converting file {Path.GetFileName(dwg)}, error: {ex.Message}\n");
                        logger.LogError(ex, ex.Message);
                    }
                    finally
                    {
                        if (docToWorkOn != null)
                        {
                            docToWorkOn.CloseAndDiscard();
                        }
                    }

                }

                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Batch Export to AutoCAD finished. {count} drawings converted. {failed} failed conversion.\n");
            }
            catch (Exception e)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Error running command, error: {e.Message}\n");
                logger.LogError(e, e.Message);
            }
            return;
        }
    }
}
