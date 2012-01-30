using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using JetBrains.Annotations;

namespace SpACraft
{
    public static class Paths
    {

        static readonly string[] ProtectedFiles;

        internal static readonly string[] DataFilesToBackup;

        static Paths()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyDir != null)
            {
                WorkingPathDefault = Path.GetFullPath(assemblyDir);
            }
            else
            {
                WorkingPathDefault = Path.GetPathRoot(assemblyDir);
            }

            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
            ConfigFileName = ConfigFileNameDefault;

            ProtectedFiles = new[]{
                "ConfigGUI.exe",
                "ConfigCLI.exe",
                "fCraft.dll",
                "fCraftGUI.dll",
                "ServerCLI.exe",
                "ServerGUI.exe",
                "ServerWinService.exe",
                UpdaterFileName,
                ConfigFileNameDefault,
                PlayerDBFileName,
                IPBanListFileName,
                RulesFileName,
                AnnouncementsFileName,
                GreetingFileName,
                HeartbeatDataFileName,
                WorldListFileName,
                AutoRankFileName
            };

            DataFilesToBackup = new[]{
                PlayerDBFileName,
                IPBanListFileName,
                WorldListFileName,
                ConfigFileName
            };
        }


        #region Paths & Properties

        public static bool IgnoreMapPathConfigKey { get; internal set; }

        public const string MapPathDefault = "maps",
                            LogPathDefault = "logs",
                            ConfigFileNameDefault = "config.xml";

        public static readonly string WorkingPathDefault;

        public static string MapPath { get; set; }

        public static string WorkingPath { get; set; }

        public static string LogPath { get; set; }

        public static string ConfigFileName { get; set; }


        public const string PlayerDBFileName = "PlayerDB.txt";

        public const string IPBanListFileName = "ipbans.txt";

        public const string GreetingFileName = "greeting.txt";

        public const string AnnouncementsFileName = "announcements.txt";

        public const string RulesFileName = "rules.txt";

        public const string RulesDirectory = "rules";

        public const string HeartbeatDataFileName = "heartbeatdata.txt";

        public const string UpdaterFileName = "UpdateInstaller.exe";

        public const string WorldListFileName = "worlds.xml";

        public const string AutoRankFileName = "autorank.xml";

        public const string BlockDBDirectory = "blockdb";


        public static string BlockDBPath
        {
            get { return Path.Combine(WorkingPath, BlockDBDirectory); }
        }

        public static string RulesPath
        {
            get { return Path.Combine(WorkingPath, RulesDirectory); }
        }

        public static string BackupPath
        {
            get
            {
                return Path.Combine(MapPath, "backups");
            }
        }

        #endregion


        #region Utility Methods

        public static void MoveOrReplace([NotNull] string source, [NotNull] string destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (File.Exists(destination))
            {
                if (Path.GetPathRoot(Path.GetFullPath(source)) == Path.GetPathRoot(Path.GetFullPath(destination)))
                {
                    string backupFileName = destination + ".bak";
                    File.Replace(source, destination, backupFileName, true);
                    File.Delete(backupFileName);
                }
                else
                {
                    File.Copy(source, destination, true);
                }
            }
            else
            {
                File.Move(source, destination);
            }
        }


        public static bool TestDirectory([NotNull] string pathLabel, [NotNull] string path, bool checkForWriteAccess)
        {
            if (pathLabel == null) throw new ArgumentNullException("pathLabel");
            if (path == null) throw new ArgumentNullException("path");
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                DirectoryInfo info = new DirectoryInfo(path);
                if (checkForWriteAccess)
                {
                    string randomFileName = Path.Combine(info.FullName, "fCraft_write_test_" + Guid.NewGuid());
                    using (File.Create(randomFileName)) { }
                    File.Delete(randomFileName);
                }
                return true;

            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestDirectory: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is DirectoryNotFoundException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is IOException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestDirectory: Specified directory for {0} is not readable/writable ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message);
                }
                else
                {
                    throw;
                }
            }
            return false;
        }


        public static bool TestFile([NotNull] string fileLabel, [NotNull] string filename,
                                    bool createIfDoesNotExist, FileAccess neededAccess)
        {
            if (fileLabel == null) throw new ArgumentNullException("fileLabel");
            if (filename == null) throw new ArgumentNullException("filename");
            try
            {
                new FileInfo(filename);
                if (File.Exists(filename))
                {
                    if ((neededAccess & FileAccess.Read) == FileAccess.Read)
                    {
                        using (File.OpenRead(filename)) { }
                    }
                    if ((neededAccess & FileAccess.Write) == FileAccess.Write)
                    {
                        using (File.OpenWrite(filename)) { }
                    }
                }
                else if (createIfDoesNotExist)
                {
                    using (File.Create(filename)) { }
                }
                return true;

            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestFile: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestFile: Cannot create or write to {0}, please check permissions ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is DirectoryNotFoundException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestFile: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message);
                }
                else if (ex is IOException)
                {
                    Logger.Log(LogType.Error,
                                "Paths.TestFile: Specified file for {0} is not readable/writable ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message);
                }
                else
                {
                    throw;
                }
            }
            return false;
        }


        public static bool IsDefaultMapPath([CanBeNull] string path)
        {
            return String.IsNullOrEmpty(path) || Compare(MapPathDefault, path);
        }


        public static bool Compare([NotNull] string p1, [NotNull] string p2)
        {
            if (p1 == null) throw new ArgumentNullException("p1");
            if (p2 == null) throw new ArgumentNullException("p2");
            return Compare(p1, p2, MonoCompat.IsCaseSensitive);
        }


        public static bool Compare([NotNull] string p1, [NotNull] string p2, bool caseSensitive)
        {
            if (p1 == null) throw new ArgumentNullException("p1");
            if (p2 == null) throw new ArgumentNullException("p2");
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return String.Equals(Path.GetFullPath(p1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                  Path.GetFullPath(p2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                  sc);
        }


        public static bool IsValidPath(string path)
        {
            try
            {
                new FileInfo(path);
                return true;
            }
            catch (ArgumentException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (NotSupportedException)
            {
            }
            return false;
        }


        public static bool Contains([NotNull] string parentPath, [NotNull] string childPath)
        {
            if (parentPath == null) throw new ArgumentNullException("parentPath");
            if (childPath == null) throw new ArgumentNullException("childPath");
            return Contains(parentPath, childPath, MonoCompat.IsCaseSensitive);
        }


        public static bool Contains([NotNull] string parentPath, [NotNull] string childPath, bool caseSensitive)
        {
            if (parentPath == null) throw new ArgumentNullException("parentPath");
            if (childPath == null) throw new ArgumentNullException("childPath");
            string fullParentPath = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullChildPath = Path.GetFullPath(childPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return fullChildPath.StartsWith(fullParentPath, sc);
        }


        public static bool FileExists([NotNull] string fileName, bool caseSensitive)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (caseSensitive == MonoCompat.IsCaseSensitive)
            {
                return File.Exists(fileName);
            }
            else
            {
                return new FileInfo(fileName).Exists(caseSensitive);
            }
        }


        public static bool Exists([NotNull] this FileInfo fileInfo, bool caseSensitive)
        {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");
            if (caseSensitive == MonoCompat.IsCaseSensitive)
            {
                return fileInfo.Exists;
            }
            else
            {
                DirectoryInfo parentDir = fileInfo.Directory;
                StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                return parentDir.GetFiles("*", SearchOption.TopDirectoryOnly)
                                .Any(file => file.Name.Equals(fileInfo.Name, sc));
            }
        }


        public static void ForceRename([NotNull] string originalFullFileName, [NotNull] string newFileName)
        {
            if (originalFullFileName == null) throw new ArgumentNullException("originalFullFileName");
            if (newFileName == null) throw new ArgumentNullException("newFileName");
            FileInfo originalFile = new FileInfo(originalFullFileName);
            if (originalFile.Name == newFileName) return;
            FileInfo newFile = new FileInfo(Path.Combine(originalFile.DirectoryName, newFileName));
            string tempFileName = originalFile.FullName + Guid.NewGuid();
            MoveOrReplace(originalFile.FullName, tempFileName);
            MoveOrReplace(tempFileName, newFile.FullName);
        }


        public static FileInfo[] FindFiles([NotNull] string fullFileName)
        {
            if (fullFileName == null) throw new ArgumentNullException("fullFileName");
            FileInfo fi = new FileInfo(fullFileName);
            DirectoryInfo parentDir = fi.Directory;
            return parentDir.GetFiles("*", SearchOption.TopDirectoryOnly)
                            .Where(file => file.Name.Equals(fi.Name, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
        }


        public static bool IsProtectedFileName([NotNull] string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            return ProtectedFiles.Any(t => Compare(t, fileName));
        }

        #endregion
    }
}