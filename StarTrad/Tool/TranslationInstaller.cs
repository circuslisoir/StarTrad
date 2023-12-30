using StarTrad.Helper;
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

        // The object representing the Star Citizen's installation directory for a channel, for example:
        // "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        private readonly ChannelFolder channelFolder;

        // A small window to display the installation progressing.
        private readonly View.Window.Progress? progressWindow = null;

        private TranslationInstaller(ChannelFolder channelFolder, bool silent = true)
        {
            this.channelFolder = channelFolder;

            if (!silent) {
                this.progressWindow = new View.Window.Progress();
            }
        }


        #region Static

        /// <summary>
        /// Runs the whole installation process, if possible.
        /// <param name="showDownloadWindow">
        /// If true, no UI elements will be displayed to show the progress of the installation.
        /// </param>
        /// </summary>
        public static void Run(bool silent = true)
        {
            ChannelFolder? channelFolder = ChannelFolder.Make();

            if (channelFolder == null) {
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(channelFolder, silent);
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
            if (installedVersion != null && !latestVersion.IsNewerThan(installedVersion))
            {
                LoggerFactory.LogInformation("Dernière version de traduction déjà installée");
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
            string? installedGlobalIniFilePath = this.channelFolder.GlobalIniInstallationFilePath;

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

            if (this.progressWindow != null) {
                this.progressWindow.Show();
            }

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
            string globalIniDestinationDirectoryPath = this.channelFolder.GlobalIniInstallationDirectoryPath;

            try
            {
                // Create destination directory if need
                if (!Directory.Exists(globalIniDestinationDirectoryPath))
                {
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                    LoggerFactory.LogInformation($"Création du dossier suivant : {globalIniDestinationDirectoryPath}");
                }

                // Move downloaded file
                File.Move(downloadedGlobalIniFilePath, this.channelFolder.GlobalIniInstallationFilePath, true);

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
            if (!File.Exists(this.channelFolder.UserCfgFilePath))
            {
                // Si le fichier n'existe pas, le créer avec la ligne g_language
                File.WriteAllText(this.channelFolder.UserCfgFilePath, g_language);
                LoggerFactory.LogWarning($"Création du fichier User.cfg, avec la clé : {g_language}");
                return;
            }

            // Lecture du fichier ligne par ligne
            string[] lines = File.ReadAllLines(this.channelFolder.UserCfgFilePath);

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
            File.WriteAllLines(this.channelFolder.UserCfgFilePath, lines);
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
            if (this.progressWindow == null) {
                return;
            }

            int percentage = (int)(((float)e.BytesReceived / (float)e.TotalBytesToReceive) * 100.0);

            this.progressWindow.ProgressBarPercentage = percentage;
            this.progressWindow.ProgressBarLabelText = percentage + " % (" + (e.BytesReceived / 1000000) + "Mo / " + (e.TotalBytesToReceive / 1000000) + "Mo)";
        }

        /// <summary>
        /// Called once the global.ini has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebClient_GlobalIniFileDownloadCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (this.progressWindow != null) {
                this.progressWindow.Close();
            }

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
