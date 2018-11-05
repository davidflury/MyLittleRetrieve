using System;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MyLittleRetrieve.Helpers
{
    public static class FileHelper
    {
        public static string ReadContent(this FileInfo file)
        {
            try
            {
                return File.ReadAllText(file.FullName);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string SelectFolder(string path = "", string title = "")
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = title;
                if (!string.IsNullOrEmpty(path))
                {
                    dialog.InitialDirectory = path;
                }
                var result = dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok || string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    return string.Empty;
                }
                return dialog.FileName;
            }
        }

        public static string SelectFile(string path = "", string title = "")
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false;
                dialog.Multiselect = false;
                dialog.Title = title;
                if (!string.IsNullOrEmpty(path))
                {
                    dialog.InitialDirectory = path;
                }
                var result = dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok || string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    return string.Empty;
                }
                return dialog.FileName;
            }
        }
    }
}
