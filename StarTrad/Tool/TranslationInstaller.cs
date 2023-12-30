using StarTrad.Helper;
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
        private const string DOWNLOAD_ERROR_MESSAGE = "Erreur de téléchargement du fichier de traduction.";

        // The object representing the Star Citizen's installation directory for a channel, for example:
        // "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        private readonly ChannelFolder channelFolder;

        // Should the installed run without using any UI element to report its progress.
        private readonly bool silent = true;

        // A small window to display the installation progressing.
        private View.Window.Progress? progressWindow = null;

        // Define an event to be called once the traslation has been installed.
        public delegate void TranslationInstalledHandler<ChannelFolder>(object sender, ChannelFolder channelFolder);
        public event TranslationInstalledHandler<ChannelFolder>? OnTranslationInstalled = null;

        private TranslationInstaller(ChannelFolder channelFolder, bool silent = true)
        {
            this.channelFolder = channelFolder;
            this.silent = silent;
        }

		#region Static

		/// <summary>
		/// Runs the whole installation process, if possible.
		/// <param name="silent">
		/// If true, no UI elements will be displayed to show the progress of the installation.
		/// </param>
        /// <param name="onTranslationInstalled">
		/// If not null, this event will be called after a successful installation of the translation.
		/// </param>
		/// </summary>
        public static void Install(bool silent = true, TranslationInstalledHandler<ChannelFolder>? onTranslationInstalled = null)
        {
            ChannelFolder? channelFolder = ChannelFolder.Make();

            if (channelFolder == null) {
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(channelFolder, silent);
            
            if (onTranslationInstalled != null) {
                installer.OnTranslationInstalled += onTranslationInstalled;
            }

            installer.InstallLatestTranslation();
        }

        /// <summary>
        /// Uninstalls the translation.
        /// </summary>
        /// <returns>
        /// Success.
        /// </returns>
        public static bool Uninstall()
        {
            ChannelFolder? channelFolder = ChannelFolder.Make();

            if (channelFolder == null) {
                return false;
            }

            string globalIniFilePath = channelFolder.GlobalIniInstallationFilePath;

            if (!File.Exists(globalIniFilePath)) {
                return false;
            }

            try {
                File.Delete(globalIniFilePath);
            } catch (Exception ex) {
                LoggerFactory.LogError(ex);

                return false;
            }

            return true;
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
            if (latestVersion == null) {
                this.Notify(ToolTipIcon.Warning, "Impossible de récuprérer la version de la dernière traduction.", true);

                return;
            }

            TranslationVersion? installedVersion = this.GetInstalledTranslationVersion();

            // We already have the latest version installed
            if (installedVersion != null && !latestVersion.IsNewerThan(installedVersion)) {
                this.Notify(ToolTipIcon.Info, "Dernière version de traduction déjà installée.", true);
                this.PostGlobalIniInstallation();

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

            if (!this.silent) {
                this.progressWindow = new View.Window.Progress(channelFolder.Name);
                this.progressWindow.Show();
            }

            client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);
            client.Dispose();
        }

        /// <summary>
        /// Given the location of a local global.ini file, installs it in the correct directory.
        /// </summary>
        /// <param name="downloadedGlobalIniFilePath">
        /// The absolute path to the downloaded global.ini file.
        /// </param>
        /// <returns>
        /// Success.
        /// </returns>
        private bool InstallGlobalIniFile(string downloadedGlobalIniFilePath)
        {
            LoggerFactory.LogInformation($"Installation du nouveau fichier Global.ini");
            string globalIniDestinationDirectoryPath = this.channelFolder.GlobalIniInstallationDirectoryPath;

            try
            {
                // Create destination directory if need
                if (!Directory.Exists(globalIniDestinationDirectoryPath)) {
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                    LoggerFactory.LogInformation($"Création du dossier suivant : {globalIniDestinationDirectoryPath}");
                }

                // Move downloaded file
                File.Move(downloadedGlobalIniFilePath, this.channelFolder.GlobalIniInstallationFilePath, true);
            }
            catch (Exception e)
            {
                LoggerFactory.LogError(e);

                return false;
            }

            return this.PostGlobalIniInstallation();
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

        /// <summary>
        /// To be called once we have the confirmation that the global.ini file is at its final destination.
        /// </summary>
        /// /// <returns>
        /// Success.
        /// </returns>
        private bool PostGlobalIniInstallation()
        {
            // Create or Update the user.cfg file
            try {
                this.CreateOrUpdateUserCfgFile();
            } catch (Exception e) {
                LoggerFactory.LogError(e);

                return false;
            }

            // Trigger installed event
            if (this.OnTranslationInstalled != null) {
                this.OnTranslationInstalled(this, this.channelFolder);
            }

            return true;
        }

        /// <summary>
        /// Displays a message from the notify icon.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="message"></param>
        /// <param name="log">
        /// If true, the message will also be written to the log file.
        /// </param>
        private void Notify(ToolTipIcon icon, string message, bool log = false)
        {
            if (this.silent) {
                return;
            }

            App.Notify(icon, message);

            if (!log) {
                return;
            }

            if (icon == ToolTipIcon.Info) {
                LoggerFactory.LogInformation(message);
            } else if (icon == ToolTipIcon.Warning) {
                LoggerFactory.LogWarning(message);
            }
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

            if (e.Error != null) {
                LoggerFactory.LogError(e.Error);
                this.Notify(ToolTipIcon.Error, DOWNLOAD_ERROR_MESSAGE);
                
                return;
            }

            string localGlobalIniFilePath = App.workingDirectoryPath + GLOBAL_INI_FILE_NAME;

            if (!File.Exists(localGlobalIniFilePath))  {
                LoggerFactory.LogWarning($"Fichier global.ini téléchargé mais non trouvé, chemin de recherche : {localGlobalIniFilePath}");
                this.Notify(ToolTipIcon.Warning, DOWNLOAD_ERROR_MESSAGE);

                return;
            }

            long length = new FileInfo(localGlobalIniFilePath).Length;

            // File is empty, download failed
            if (length <= 0) {
                LoggerFactory.LogWarning("Erreur de téléchargement, fichier vide");
                this.Notify(ToolTipIcon.Warning, DOWNLOAD_ERROR_MESSAGE);

                return;
            }

            bool success = this.InstallGlobalIniFile(localGlobalIniFilePath);

            if (success) {
                this.Notify(ToolTipIcon.Info, "Traduction installée avec succès !");
            } else {
                this.Notify(ToolTipIcon.Error, "Erreur d'installation de la traduction.");
            }
        }

        #endregion
    }
}
