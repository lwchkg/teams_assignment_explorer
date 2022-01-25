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
                                                        (_, value) => value && isWorkingFiles);
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

            // Glob [basePath]/Submitted files/[user]/[homework]/Version */*.*,
            // [basePath]/Submitted files/[user]/[homework]/*.*,
            // and [basePath]/Working files/[user]/[homework]/*.*
            //
            // Due to translations of folder names by Microsoft, we do not check for the phrases
            // "Submitted files", "Working files" and "Version". Otherwise we need to add code for
            // every translation.
            //
            // There is no failproof way to identify the second type of file as submitted files, but
            // we do it with best effort.
#if DEBUG
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
#endif
            // We opt not to persist the names of submitted file folders across searches, because
            // users can carelessly moving files to other folder. Note that this only affect this
            // app, but not MS Teams as they identify files by UUID.
            var submittedFilesDirNames = new ConcurrentDictionary<string, bool>();

            var submittedFiles = new ConcurrentBag<string>();
            var maybeWorkingFiles = new ConcurrentBag<string>();
            try
            {
                foreach (string submittedOrWorkingFilesDir in
                         Directory.EnumerateDirectories(basePath))
                {
                    Parallel.ForEach(Directory.EnumerateDirectories(submittedOrWorkingFilesDir),
                        userDir =>
                        {
                            bool isSubmittedFilesDir = false;

                            string hwPath = Path.Combine(userDir, homework);
                            if (!Directory.Exists(hwPath)) return;

                            foreach (string versionDir in Directory.EnumerateDirectories(hwPath))
                            {
                                foreach (string file in Directory.EnumerateFiles(versionDir))
                                {
                                    submittedFiles.Add(StripBasePath(file, basePath));
                                    isSubmittedFilesDir = true;
                                }
                            }

                            if (isSubmittedFilesDir)
                            {
                                submittedFilesDirNames.TryAdd(
                                    Path.GetFileName(submittedOrWorkingFilesDir), true);
                            }

                            foreach (string file in Directory.EnumerateFiles(hwPath))
                                maybeWorkingFiles.Add(StripBasePath(file, basePath));
                        });
                }
            }
            catch (Exception) { /* Do nothing */ }

            List<string> submittedFilesSorted = submittedFiles.ToList();
            List<string> workingFilesSorted = new List<string>();

            foreach (string file in maybeWorkingFiles)
            {
                string topDir = file.Substring(0, file.IndexOf(Path.DirectorySeparatorChar));
                if (submittedFilesDirNames.ContainsKey(topDir))
                    submittedFilesSorted.Add(file);
                else
                    workingFilesSorted.Add(file);
            }
            submittedFilesSorted.Sort();
            workingFilesSorted.Sort();

#if DEBUG
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine("Traversing homework \"{0}\" takes {1} ms.",
                                               homework, stopwatch.ElapsedMilliseconds);
#endif

            return new SubmittedAndWorkingFiles() {
                SubmittedFiles = submittedFilesSorted,
                WorkingFiles = workingFilesSorted
            };
        }

        private static string StripBasePath(string path, string basePath)
        {
            if (!path.StartsWith(basePath))
                return path;

            if (basePath.Last() == Path.DirectorySeparatorChar)
                return path.Substring(basePath.Length);
            return path.Substring(basePath.Length + 1);
        }
    }
}
