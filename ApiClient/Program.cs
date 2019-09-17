using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using MQS.Security;
using MQS.FileManager;
using MQS.TM;
using MQS.ServerProject;

namespace ApiClient
{
    public class Program
    {
        /// <summary>
        /// URL of the memoQ server we're calling, ending in /memoqservices, without a trailing slash.
        /// </summary>
        const string baseUrl = "https://my-memoq-server.com/memoqservices";
        /// <summary>
        /// API key (from Server Admin / Web Service Interface)
        /// </summary>
        const string apiKey = "<my API key>";
        /// <summary>
        /// User name that we will create
        /// </summary>
        const string userName = "johndoe";
        /// <summary>
        /// Password we'll set up for new user
        /// </summary>
        const string userPass = "SamplePass123";
        /// <summary>
        /// Full name of user that we'll create
        /// </summary>
        const string userFullName = "John Doe";
        /// <summary>
        /// Source language of TM and project we'll create
        /// </summary>
        const string srcLang = "eng";
        /// <summary>
        /// Target language of TM and project we'll create
        /// </summary>
        const string trgLang = "ger";
        /// <summary>
        /// Name of TM we'll create
        /// </summary>
        const string tmName = "SampleTM";
        /// <summary>
        /// Name of project we'll create
        /// </summary>
        const string projectName = "SampleProject";
        /// <summary>
        /// Document we'll import into project from local file system.
        /// </summary>
        const string fileName = "sample.docx";
        /// <summary>
        /// Path to the file we'll import.
        /// </summary>
        const string filePath = "../Content";

        public static void Main(string[] args)
        {
            Service<ISecurityService> security = null;
            Service<IFileManagerService> fileManager = null;
            Service<ITMService> tm = null;
            Service<IServerProjectService> serverProject = null;
            try
            {
                security = new Service<ISecurityService>(baseUrl, apiKey);
                fileManager = new Service<IFileManagerService>(baseUrl, apiKey);
                tm = new Service<ITMService>(baseUrl, apiKey);
                serverProject = new Service<IServerProjectService>(baseUrl, apiKey);

                // Create user if it doesn't exist yet
                var users = security.Proxy.ListUsers();
                var userInfo = Array.Find(users, x => x.UserName.ToLower() == userName.ToLower());
                // We'll need user Guid later when we assign document in project
                Guid userGuid;
                if (userInfo != null) userGuid = userInfo.UserGuid;
                else
                {
                    // Password is SHA-1 hashed with hard-wired salt for this call
                    // A needless "security measure" that's simply a relic
                    const string salt = "fgad s d f sgds g  sdg gfdg";
                    var sha1 = SHA1.Create();
                    byte[] bytesToHash = Encoding.UTF8.GetBytes(userPass + salt);
                    HashAlgorithm algorithm = SHA1.Create();
                    byte[] hash = algorithm.ComputeHash(bytesToHash);
                    // Create user
                    var newUser = new MQS.Security.UserInfo
                    {
                        UserName = userName,
                        Password = bytesToHex(hash),
                        FullName = userFullName,
                        // If we also specified an email address, memoQ server would send messages
                        // when we assign documents to this user in projects.
                    };
                    userGuid = security.Proxy.CreateUser(newUser);
                }
                // Create TM if it doesn't exist yet
                // Sloppiness warning: For simplicity, this code doesn't handle the situation 
                // when a TM with the same name but a different language pair does exist.
                var tms = tm.Proxy.ListTMs(srcLang, trgLang);
                var tmInfo = Array.Find(tms, x => x.Name.ToLower() == tmName.ToLower());
                Guid tmGuid;
                if (tmInfo != null) tmGuid = tmInfo.Guid;
                else
                {
                    var newTM = new MQS.TM.TMInfo
                    {
                        Name = tmName,
                        SourceLanguageCode = srcLang,
                        TargetLanguageCode = trgLang,
                        AllowMultiple = false,
                        AllowReverseLookup = true,
                        StoreFormatting = true,
                        UseContext = true,
                        UseIceSpiceContext = false,
                    };
                    tmGuid = tm.Proxy.CreateAndPublish(newTM);
                    // For this script's use case, remove all default explicit permissions from TM
                    security.Proxy.SetObjectPermissions(tmGuid, new ObjectPermission[0]);
                }
                // Delete old project if it exists (same name)
                var projects = serverProject.Proxy.ListProjects(null);
                var prInfo = Array.Find(projects, x => x.Name.ToLower() == projectName.ToLower());
                if (prInfo != null) serverProject.Proxy.DeleteProject(prInfo.ServerProjectGuid);
                // Create new project
                var newProject = new ServerProjectDesktopDocsCreateInfo
                {
                    CreateOfflineTMTBCopies = false,
                    DownloadPreview = true,
                    DownloadSkeleton = true,
                    EnableSplitJoin = true,
                    EnableWebTrans = true,
                    EnableCommunication = false,
                    AllowOverlappingWorkflow = true,
                    AllowPackageCreation = false,
                    PreventDeliveryOnQAError = false,
                    RecordVersionHistory = true,
                    CreatorUser = userGuid, // This makes our user PM in project
                    Deadline = DateTime.Now, // Project-level deadline is display info only
                    Name = projectName,
                    SourceLanguageCode = srcLang,
                    // Projects can have multiple target languages. In this use case, we only have one.
                    TargetLanguageCodes = new string[] { trgLang },
                };
                var projectGuid = serverProject.Proxy.CreateProject(newProject);
                // Add TM to project
                // If our project had multiple target languages, we would pass several such objects.
                var tmForTarget = new ServerProjectTMAssignmentsForTargetLang
                {
                    MasterTMGuid = tmGuid,
                    PrimaryTMGuid = tmGuid,
                    TMGuids = new Guid[] { tmGuid },
                    TargetLangCode = trgLang,
                };
                serverProject.Proxy.SetProjectTMs2(projectGuid, new ServerProjectTMAssignmentsForTargetLang[] { tmForTarget });
                // Upload file to translate
                Guid fileGuid;
                using (FileStream fs = new FileStream(Path.Combine(filePath, fileName), FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fileGuid = fileManager.Proxy.BeginChunkedFileUpload(fileName, false);
                    while (true)
                    {
                        byte[] buf = br.ReadBytes(4096);
                        if (buf.Length == 0) break;
                        fileManager.Proxy.AddNextFileChunk(fileGuid, buf);
                        if (buf.Length < 4096) break;
                    }
                    fileManager.Proxy.EndChunkedFileUpload(fileGuid);
                }
                // Import file into project
                var tdoc = serverProject.Proxy.ImportTranslationDocument(
                    projectGuid, fileGuid,
                    // If project had multiple target lanuages, array below would decide which ones
                    // we want to translate the document into. Passing null means: import into all TLs.
                    null,
                    null);
                // Sloppiness warning: In production code, you must check import result here.
                // One or more files might have failed to import.
                // Remove uploaded file
                fileManager.Proxy.DeleteFile(fileGuid);
                // Pre-translate document in project
                var preTransOptions = new PretranslateOptions
                {
                    UseMT = true,
                    OnlyUnambiguousMatches = false,
                    PretranslateLookupBehavior = PretranslateLookupBehavior.AnyMatch,
                    FinalTranslationState = PretranslateExpectedFinalTranslationState.Confirmed,
                    ConfirmLockPretranslated = PretranslateStateToConfirmAndLock.ExactMatchWithContext,
                    ConfirmLockUnambiguousMatchesOnly = true,
                    LockPretranslated = false,
                };
                serverProject.Proxy.PretranslateProject(
                    projectGuid,
                    null, // Passing null for target language array means: everything
                    preTransOptions);
                // Run statistics
                var statsOpt = new StatisticsOptions
                {
                    Algorithm = StatisticsAlgorithm.MemoQ,
                    Analysis_Homogenity = false,
                    Analysis_ProjectTMs = true,
                    Analyzis_DetailsByTM = false,
                    DisableCrossFileRepetition = false,
                    IncludeLockedRows = false,
                    RepetitionPreferenceOver100 = true,
                    ShowCounts = false,
                    ShowResultsPerFile = false,
                };
                var statsRes = serverProject.Proxy.GetStatisticsOnProject(projectGuid, null, statsOpt, StatisticsResultFormat.CSV_MemoQ);
                // ResultsForTargetLangs has one item per project target language
                byte[] statResBytes = statsRes.ResultsForTargetLangs[0].ResultData;
                // Result is byte array in Unicode encoding
                string statResStr = Encoding.Unicode.GetString(statResBytes);
                // Setup details about document-user assignment
                var assmt = new ServerProjectTranslationDocumentUserAssignments
                {
                    // tdoc.DocumentGuids only has one element, b/c we imported our document into a single
                    // target language only.
                    DocumentGuid = tdoc.DocumentGuids[0],
                    // We are only assigning one role, TR = translator.
                    UserRoleAssignments = new TranslationDocumentUserRoleAssignment[1],
                };
                assmt.UserRoleAssignments[0] = new TranslationDocumentUserRoleAssignment
                {
                    UserGuid = userGuid,
                    // 0: Translator
                    // 1: Reviewer 1
                    // 2: Reviewer 2
                    DocumentAssignmentRole = 0,
                    // In real life, deadline would be specified by client
                    DeadLine = DateTime.UtcNow.AddDays(3),
                };
                // Assign document to user
                serverProject.Proxy.SetProjectTranslationDocumentUserAssignments(
                    projectGuid,
                    // 1 item in array, b/c we're only assigning 1 doc in 1 target language.
                    new ServerProjectTranslationDocumentUserAssignments[] { assmt });
                // We're done! What now?
                // User can open document in webTrans, or check out online project and translate in memoQ.
            }
            finally
            {
                if (security != null) security.Dispose();
                if (fileManager != null) fileManager.Dispose();
                if (tm != null) tm.Dispose();
                if (serverProject != null) serverProject.Dispose();
            }
        }

        private static string bytesToHex(byte[] arr)
        {
            StringBuilder sb = new StringBuilder(arr.Length * 2);
            for (int i = 0; i < arr.Length; i++)
            {
                int v = arr[i] & 0xff;
                sb.Append(v.ToString("X2"));
            }
            return sb.ToString().ToUpper();
        }
    }
}
