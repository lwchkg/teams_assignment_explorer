using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TeamsAssignmentExplorer
{
    class DirAndFileScanner
    {
        public struct HomeworkItem
        {
            public string Homework;
            public bool WorkingFilesOnly;
        }

        public struct SubmittedAndWorkingFiles
        {
            public List<string> SubmittedFiles;
            public List<string> WorkingFiles;
        }

        public static List<string> GetFolderList()
        {
            var output = new List<string>();
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (string orgDir in Directory.EnumerateDirectories(basePath))
            {
                try
                {
                    foreach (string repoDir in Directory.EnumerateDirectories(orgDir))
                    {
                        if (StringConstants.StudentWorkSuffixes.Any(s => repoDir.EndsWith(s)))
                            output.Add(repoDir);
                    }
                }
                catch (Exception) { /* Do nothing */ }
            }

            return output;
        }

        public static List<HomeworkItem> GetHomeworkList(string basePath)
        {
#if DEBUG 
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
#endif

            // Glob [basePath]/Submitted files/[user]/[homework] and
            // [basePath]/Working files/[user]/[homework]
            var homeworkMap = new ConcurrentDictionary<string, bool>();

            try
            {
                foreach (string submittedOrWorkingFilesDir in
                         Directory.EnumerateDirectories(basePath))
                {
                    Parallel.ForEach(
                        Directory.EnumerateDirectories(submittedOrWorkingFilesDir),
                        userDir =>
                        {
                            foreach (string homeworkDir in Directory.EnumerateDirectories(userDir))
                            {
                                bool isWorkingFiles = !Directory.EnumerateDirectories(homeworkDir)
                                                                .GetEnumerator()
                                                                .MoveNext();

                                homeworkMap.AddOrUpdate(Path.GetFileName(homeworkDir),
                                                        isWorkingFiles,
                                                        (_, value) => value || isWorkingFiles);
                            }
                        });
                }
            }
            catch (Exception) { /* Do nothing. */ }

            var list = homeworkMap.Select(r => new HomeworkItem() {
                Homework = r.Key, WorkingFilesOnly = r.Value
            }).ToList();
            list.Sort((a, b) => a.Homework.CompareTo(b.Homework));

#if DEBUG
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine("Traversing \"{0}\" takes {1} ms.", basePath,
                                               stopwatch.ElapsedMilliseconds);
#endif

            return list;
        }

        public static SubmittedAndWorkingFiles GetSubmittedAndWorkingFiles(string basePath, 
                                                                           string homework)
        {
            System.Diagnostics.Debug.Assert(homework.Trim() != "");

            // Glob [basePath]/Submitted files/[user]/[homework]/Version */*.*
            // and [basePath]/Working files/[user]/[homework]/Version */*.*
            //
            // Due to translations of folder names by Microsoft, we do not check for the phrases
            // "Submitted files", "Working files" and "Version". Otherwise we need to add code for
            // every translation.)
            var submittedFiles = new List<string>();
            var workingFiles = new List<string>();
            try
            {
                foreach (string submittedOrWorkingFilesDir in
                         Directory.EnumerateDirectories(basePath))
                {
                    foreach (string userDir in
                             Directory.EnumerateDirectories(submittedOrWorkingFilesDir))
                    {
                        string hwPath = Path.Combine(userDir, homework);
                        if (!Directory.Exists(hwPath))
                            continue;
                        foreach (string versionDir in Directory.EnumerateDirectories(hwPath))
                        {
                            var files = Directory.GetFiles(versionDir);
                            Array.Sort(files);
                            foreach (string file in files)
                            {
                                // Strip base path from the filename.
                                submittedFiles.Add(file.Substring(basePath.Length + 1));
                            }
                        }

                        { // Scope
                            var files = Directory.GetFiles(hwPath);
                            Array.Sort(files);
                            foreach (string file in files)
                            {
                                // Strip base path from the filename.
                                workingFiles.Add(file.Substring(basePath.Length + 1));
                            }
                        }
                    }
                }
            }
            catch (Exception) { /* Do nothing */ }

            return new SubmittedAndWorkingFiles() {
                SubmittedFiles = submittedFiles,
                WorkingFiles = workingFiles
            };
        }
    }
}
