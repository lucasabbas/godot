using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using GodotTools.Internals;

namespace GodotTools.Ides.Consulo
{
    public static class ConsuloPathManager
    {
        public static readonly string EditorPathSettingName = "dotnet/editor/editor_path_optional";

        private static string GetConsuloPathFromSettings()
        {
            var editorSettings = GodotSharpEditor.Instance.GetEditorInterface().GetEditorSettings();
            if (editorSettings.HasSetting(EditorPathSettingName))
                return (string)editorSettings.GetSetting(EditorPathSettingName);
            return null;
        }

        public static void Initialize()
        {
            var editorSettings = GodotSharpEditor.Instance.GetEditorInterface().GetEditorSettings();
            var editor = editorSettings.GetSetting(GodotSharpEditor.Settings.ExternalEditor).As<ExternalEditorId>();
            if (editor == ExternalEditorId.Consulo)
            {
                if (!editorSettings.HasSetting(EditorPathSettingName))
                {
                    Globals.EditorDef(EditorPathSettingName, "Optional");
                    editorSettings.AddPropertyInfo(new Godot.Collections.Dictionary
                    {
                        ["type"] = (int)Variant.Type.String,
                        ["name"] = EditorPathSettingName,
                        ["hint"] = (int)PropertyHint.File,
                        ["hint_string"] = ""
                    });
                }

                var ConsuloPath = (string)editorSettings.GetSetting(EditorPathSettingName);
                if (IsConsuloAndExists(ConsuloPath))
                {
                    Globals.EditorDef(EditorPathSettingName, ConsuloPath);
                    return;
                }

                var paths = ConsuloPathLocator.GetAllConsuloPaths();

                if (!paths.Any())
                    return;

                string newPath = paths.Last().Path;
                Globals.EditorDef(EditorPathSettingName, newPath);
                editorSettings.SetSetting(EditorPathSettingName, newPath);
            }
        }

        public static bool IsExternalEditorSetToConsulo(EditorSettings editorSettings)
        {
            return editorSettings.HasSetting(EditorPathSettingName) &&
                IsConsulo((string)editorSettings.GetSetting(EditorPathSettingName));
        }

        public static bool IsConsulo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                return false;

            var fileInfo = new FileInfo(path);
            string filename = fileInfo.Name.ToLowerInvariant();
            return filename.StartsWith("Consulo", StringComparison.Ordinal);
        }

        private static string CheckAndUpdatePath(string ConsuloPath)
        {
            if (IsConsuloAndExists(ConsuloPath))
            {
                return ConsuloPath;
            }

            var editorSettings = GodotSharpEditor.Instance.GetEditorInterface().GetEditorSettings();
            var paths = ConsuloPathLocator.GetAllConsuloPaths();

            if (!paths.Any())
                return null;

            string newPath = paths.Last().Path;
            editorSettings.SetSetting(EditorPathSettingName, newPath);
            Globals.EditorDef(EditorPathSettingName, newPath);
            return newPath;
        }

        private static bool IsConsuloAndExists(string ConsuloPath)
        {
            return !string.IsNullOrEmpty(ConsuloPath) && IsConsulo(ConsuloPath) && new FileInfo(ConsuloPath).Exists;
        }

        public static void OpenFile(string slnPath, string scriptPath, int line)
        {
            string pathFromSettings = GetConsuloPathFromSettings();
            string path = CheckAndUpdatePath(pathFromSettings);

            var args = new List<string>();
            args.Add(slnPath);
            if (line >= 0)
            {
                args.Add("--line");
                args.Add((line + 1).ToString()); // https://github.com/JetBrains/godot-support/issues/61
            }
            args.Add(scriptPath);
            try
            {
                Utils.OS.RunProcess(path, args);
            }
            catch (Exception e)
            {
                GD.PushError($"Error when trying to run code editor: Consulo. Exception message: '{e.Message}'");
            }
        }
    }
}
