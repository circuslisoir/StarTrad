using StarTrad.Helper;
using StarTrad.Helper.ComboxList;
using StarTrad.Properties;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace StarTrad.Tool
{
    /// <summary>
    /// Downloads then installs a Star Citizen translation.
    /// </summary>
    internal class TranslationInstaller
    {
        private const string GLOBAL_INI_FILE_NAME = "global.ini";

        // The absolute path to the Star Citizen's installation directory for the configured channel, for example:
        // "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        private string currentChannelDirectoryPath;


        private TranslationInstaller(string currentChannelDirectoryPath)
        {
            this.currentChannelDirectoryPath = currentChannelDirectoryPath;
        }


        #region Static
        /// <summary>
        /// Runs the whole installation process, if possible.
        /// </summary>
        public static void Run()
        {
            string? channel = EnumHelper.GetDescription((ChanelVersionEnum)Settings.Default.RsiLauncherChannel);

            if (channel == null) {
                return;
            }

            string? currentChannelDirectoryPath = LibraryFolderFinder.GetStarCitizenInstallDirectoryPath(channel);

            if (currentChannelDirectoryPath == null)
            {
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(currentChannelDirectoryPath);
            installer.InstallLatestTranslation();
        }
        #endregion


        #region Private

        /// <summary>
        /// Installs, if needed, the latest version of the translation from the circuspes website.
        /// </summary>
        private void InstallLatestTranslation()
        {
            TranslationVersion? latestVersion = this.QueryLatestAvailableTranslationVersion();

            // Unable to obtain the remote version
            if (latestVersion == null)
            {
                LoggerFactory.LogWarning("Impossible de récuprérer version de la dernière traduction");
                return;
            }

            TranslationVersion? installedVersion = this.GetInstalledTranslationVersion();

            // We already have the latest version installed
            if (installedVersion != null && latestVersion.IsNewerThan(installedVersion))
            {
                LoggerFactory.LogInformation("Dernière version de traduction déjà installer");
                return;
            }

            this.StartGlobalIniFileDownload();
        }

        /// <summary>
        /// Reads the installed translation file, if any, in order to obtain its version.
        /// </summary>
        /// <returns>
        /// The installed version as an object, or null if the file cannot be found.
        /// </returns>
        private TranslationVersion? GetInstalledTranslationVersion()
        {
            LoggerFactory.LogInformation("Récupération de la version local de la traduction");
            string? installedGlobalIniFilePath = this.GlobalIniInstallationFilePath;

            if (!File.Exists(installedGlobalIniFilePath))
            {
                return null;
            }

            string versionCommentToken = "; Version :";

            using (FileStream fileStream = File.OpenRead(installedGlobalIniFilePath))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
                {
                    string? line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.StartsWith(versionCommentToken))
                        {
                            return TranslationVersion.Make(line.Replace(versionCommentToken, ""));
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Obtains an object representing the latest version of the translation.
        /// </summary>
        /// <returns></returns>
        private TranslationVersion? QueryLatestAvailableTranslationVersion()
        {
            LoggerFactory.LogInformation("Récupération de la dernière version de la traduction");
            string? html = CircuspesClient.GetRequest("/download/version.html");

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            return TranslationVersion.Make(html);
        }

        /// <summary>
        /// Starts downloading the global.ini translation file from the circuspes website.
        /// </summary>
        private void StartGlobalIniFileDownload()
        {
            LoggerFactory.LogInformation("Lancement du téléchargement de la dernière version de la traduction");
            // Define where to store the to-be-downloaded file
            string localGlobalIniFilePath = App.workingDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME;

            if (File.Exists(localGlobalIniFilePath))
            {
                File.Delete(localGlobalIniFilePath);
            }

            WebClient client = new WebClient();

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.WebClient_GlobalIniFileDownloadProgress);
            client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(this.WebClient_GlobalIniFileDownloadCompleted);
            client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);

            client.Dispose();
        }

        /// <summary>
        /// Given the location of a local global.ini file, installs it in the correct directory.
        /// </summary>
        /// <param name="downloadedGlobalIniFilePath"></param>
        private void InstallGlobalIniFile(string downloadedGlobalIniFilePath)
        {
            LoggerFactory.LogInformation($"Installation du nouveau fichier Global.ini");
            string globalIniDestinationDirectoryPath = this.GlobalIniInstallationDirectoryPath;

            try
            {
                // Create destination directory if need
                if (!Directory.Exists(globalIniDestinationDirectoryPath))
                {
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                    LoggerFactory.LogInformation($"Création du dossier suivant : {globalIniDestinationDirectoryPath}");
                }

                // Move downloaded file
                File.Move(downloadedGlobalIniFilePath, this.GlobalIniInstallationFilePath, true);

                // Create or Update the user.cfg file
                CreateOrUpdateUserCfgFile();
            }
            catch (Exception e)
            {
                LoggerFactory.LogError(e);
            }
        }

        private void CreateOrUpdateUserCfgFile()
        {
            string g_language = "g_language = french_(france)";

            // Vérification si le fichier existe
            if (!File.Exists(UserCfgFilePath))
            {
                // Si le fichier n'existe pas, le créer avec la ligne g_language
                File.WriteAllText(UserCfgFilePath, g_language);
                LoggerFactory.LogWarning($"Création du fichier User.cfg, avec la clé : {g_language}");
                return;
            }

            // Lecture du fichier ligne par ligne
            string[] lines = File.ReadAllLines(UserCfgFilePath);

            bool gLanguageFound = false;

            for (int i = 0; i < lines.Length; i++)
            {
                // Vérification si la ligne commence par g_language
                if (lines[i].TrimStart().StartsWith("g_language"))
                {
                    // Vérification que toute la ligne est égale à g_language
                    if (lines[i].Trim() != g_language)
                    {
                        // Modification de la ligne
                        lines[i] = g_language;
                        LoggerFactory.LogInformation($"Mise à jour de la ligne : {lines[i]}. Dans le fichier User.cfg");
                    }

                    gLanguageFound = true;
                    break; // La ligne g_language a été trouvée, pas besoin de continuer la recherche
                }
            }

            // Si la ligne g_language n'a pas été trouvée, l'ajouter
            if (!gLanguageFound)
            {
                // Ajout de la nouvelle ligne à la fin du fichier
                Array.Resize(ref lines, lines.Length + 1);
                lines[lines.Length - 1] = g_language;
                LoggerFactory.LogInformation($"Nouvelle ligne ajouté, au fichier User.cfg : {lines[lines.Length - 1]}");
            }

            // Écriture des modifications dans le fichier
            File.WriteAllLines(UserCfgFilePath, lines);
        }

        #endregion


        #region Accessor

        /// <summary>
        /// Returns the absolute path to the final destination of the global.ini file.
        /// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)\global.ini".
        /// </summary>
        private string UserCfgFilePath
        {
            get { return this.currentChannelDirectoryPath + '\\' + "user.cfg"; }
        }

        /// <summary>
        /// Gets the absolute path to the directory where the global.ini file should be installed.
        /// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)".
        /// </summary>
        private string GlobalIniInstallationDirectoryPath
        {
            get { return this.currentChannelDirectoryPath + @"\data\Localization\french_(france)"; }
        }

        /// <summary>
        /// Returns the absolute path to the final destination of the global.ini file.
        /// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)\global.ini".
        /// </summary>
        private string GlobalIniInstallationFilePath
        {
            get { return this.GlobalIniInstallationDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME; }
        }

        #endregion


        #region Event

        /// <summary>
        /// Called periodically as the download of the global.ini file progresses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebClient_GlobalIniFileDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            int percentage = (int)(((float)e.BytesReceived / (float)e.TotalBytesToReceive) * 100.0);

            Debug.WriteLine("Downloading " + GLOBAL_INI_FILE_NAME + ": " + percentage + " % (" + (e.BytesReceived / 1000000) + "Mo / " + (e.TotalBytesToReceive / 1000000) + "Mo)");
        }

        /// <summary>
        /// Called once the global.ini has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebClient_GlobalIniFileDownloadCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LoggerFactory.LogError(e.Error);
                return;
            }

            string localGlobalIniFilePath = App.workingDirectoryPath + GLOBAL_INI_FILE_NAME;

            if (!File.Exists(localGlobalIniFilePath))
            {
                LoggerFactory.LogWarning($"Fichier global.ini télécharger mais non trouver, chemin de recherche : {localGlobalIniFilePath}");
                return;
            }

            long length = new FileInfo(localGlobalIniFilePath).Length;

            // File is empty, download failed
            if (length <= 0)
            {
                LoggerFactory.LogWarning("Erreur de téléchargement, fichier vide");
                return;
            }

            this.InstallGlobalIniFile(localGlobalIniFilePath);
        }

        #endregion
    }
}
