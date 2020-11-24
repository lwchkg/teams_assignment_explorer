using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeamsAssignmentExplorer
{
    class DirAndFileScanner
    {
        public struct HomeworkItem
        {
            public string Homework;
            public bool WorkingFilesOnly;
        }

        public static List<string> GetFolderList()
        {
            var output = new List<string>();
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (string orgDir in Directory.GetDirectories(basePath))
            {
                try
                {
                    foreach (string repoDir in Directory.GetDirectories(orgDir))
                    {
                        if (repoDir.EndsWith(" - Student Work"))
                            output.Add(repoDir);
                    }
                }
                catch (Exception) { /* Do nothing */ }
            }

            return output;
        }

        public static List<HomeworkItem> GetHomeworkList(string basePath)
        {
            // Glob [basePath]/Submitted files/[user]/[homework] and
            // [basePath]/Working files/[user]/[homework]
            var homeworkMap = new SortedDictionary<string, bool>();

            try
            {
                foreach (string userDir in Directory.GetDirectories(Path.Combine(
                    basePath, StringConstants.submittedFiles)))
                {
                    foreach (string homeworkDir in Directory.GetDirectories(userDir))
                    {
                        if (!homeworkMap.ContainsKey(Path.GetFileName(homeworkDir)))
                            homeworkMap.Add(Path.GetFileName(homeworkDir), false);
                    }
                }
            }
            catch (Exception) { /* Do nothing. */ }

            try
            {
                foreach (string userDir in Directory.GetDirectories(Path.Combine(
                    basePath, StringConstants.workingFiles)))
                {
                    foreach (string homeworkDir in Directory.GetDirectories(userDir))
                    {
                        if (!homeworkMap.ContainsKey(Path.GetFileName(homeworkDir)))
                            homeworkMap.Add(Path.GetFileName(homeworkDir), true);
                    }
                }
            }
            catch (Exception) { /* Do nothing. */ }

            return homeworkMap.Select(r => new HomeworkItem() {
                Homework = r.Key, WorkingFilesOnly = r.Value
            }).ToList();
        }

        public static List<string> GetSubmittedFiles(string basePath, string homework)
        {
            // Glob [basePath]/Submitted files/[user]/[homework]/Version */*.*
            // (Do not check for the word "Version".)
            var output = new List<string>();
            try
            {
                foreach (string userDir in Directory.GetDirectories(Path.Combine(
                    basePath, StringConstants.submittedFiles)))
                {
                    string hwPath = Path.Combine(userDir, homework);
                    if (!Directory.Exists(hwPath))
                        continue;
                    foreach (string versionDir in Directory.GetDirectories(hwPath))
                    {
                        var files = Directory.GetFiles(versionDir);
                        Array.Sort(files);
                        foreach (string file in files)
                        {
                            // Strip base path from the filename.
                            output.Add(file.Substring(basePath.Length + 1));
                        }
                    }
                }
            }
            catch (Exception) { /* Do nothing */ }

            return output;
        }

        public static List<string> GetWorkingFiles(string basePath, string homework)
        {
            // Glob [basePath]/Working files/[user]/[homework]/Version */*.*
            var output = new List<string>();
            try
            {
                foreach (string userDir in Directory.GetDirectories(Path.Combine(
                    basePath, StringConstants.workingFiles)))
                {
                    string hwPath = Path.Combine(userDir, homework);
                    if (!Directory.Exists(hwPath))
                        continue;
                    var files = Directory.GetFiles(hwPath);
                    Array.Sort(files);
                    foreach (string file in files)
                    {
                        // Strip base path from the filename.
                        output.Add(file.Substring(basePath.Length + 1));
                    }
                }
            }
            catch (Exception) { /* Do nothing */ }

            return output;
        }
    }
}
