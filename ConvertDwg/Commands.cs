using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace BP.ConvertDwg
{
    public class Commands
    {
        [CommandMethod("BatchConvert", CommandFlags.Session)]
        public static void BatchConvert()
        {
            #region Avoid Snapping
            //avoid snapping when executing the "insert" command, remember the snaps, turn them off an on again after the script execution
            object snapmodesaved = Application.GetSystemVariable("SNAPMODE");
            object osmodesaved = Application.GetSystemVariable("OSMODE");
            object os3dmodesaved = Application.GetSystemVariable("3DOSMODE");

            Application.SetSystemVariable("SNAPMODE", 0);
            Application.SetSystemVariable("OSMODE", 0);
            Application.SetSystemVariable("3DOSMODE", 0);
            #endregion

            BatchCommands.BatchConvert(false);

            #region Restore Snapping
            Application.SetSystemVariable("SNAPMODE", snapmodesaved);
            Application.SetSystemVariable("OSMODE", osmodesaved);
            Application.SetSystemVariable("3DOSMODE", os3dmodesaved);
            #endregion

        }

        [CommandMethod("BatchConvertDebug", CommandFlags.Session)]
        public static void BatchConvertDebug()
        {
            #region Avoid Snapping
            //avoid snapping when executing the "insert" command, remember the snaps, turn them off an on again after the script execution
            object snapmodesaved = Application.GetSystemVariable("SNAPMODE");
            object osmodesaved = Application.GetSystemVariable("OSMODE");
            object os3dmodesaved = Application.GetSystemVariable("3DOSMODE");

            Application.SetSystemVariable("SNAPMODE", 0);
            Application.SetSystemVariable("OSMODE", 0);
            Application.SetSystemVariable("3DOSMODE", 0);
            #endregion

            BatchCommands.BatchConvert(true);

            #region Restore Snapping
            Application.SetSystemVariable("SNAPMODE", snapmodesaved);
            Application.SetSystemVariable("OSMODE", osmodesaved);
            Application.SetSystemVariable("3DOSMODE", os3dmodesaved);
            #endregion

        }
    }
}
