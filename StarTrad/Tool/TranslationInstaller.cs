using StarTrad.Helper;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

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

        // UI elements used to display the installation progress
        private View.Window.Progress? progressWindow = null;
        private NotifyIcon? notifyIcon = null;

        // Define an event to be called once the translation installer has finished running.
        public delegate void InstallationEndedHandler<ChannelFolder>(object sender, ChannelFolder channelFolder);
        public event InstallationEndedHandler<ChannelFolder>? OnInstallationEnded = null;

        /*
        Constructor
        */

        public TranslationInstaller(ChannelFolder channelFolder)
        {
            this.channelFolder = channelFolder;
        }

        #region Public

        /// <summary>
        /// Installs, if needed, the latest version of the translation from the circuspes website.
        /// </summary>
        public void InstallLatest()
        {
            TranslationVersion? latestVersion = this.QueryLatestAvailableTranslationVersion();

            // Unable to obtain the remote version
            if (latestVersion == null)
            {
                this.Notify(ToolTipIcon.Warning, "Impossible de récuprérer la version de la dernière traduction.", true);
                if (this.OnInstallationEnded != null) this.OnInstallationEnded(this, this.channelFolder);

                return;
            }

            TranslationVersion? installedVersion = this.GetInstalledTranslationVersion();

            // We already have the latest version installed
            if (installedVersion != null && !latestVersion.IsNewerThan(installedVersion))
            {
                this.CreateOrUpdateUserCfgFile();
                this.Notify(ToolTipIcon.Info, "Dernière version de traduction déjà installée.", true);
                if (this.OnInstallationEnded != null) this.OnInstallationEnded(this, this.channelFolder);

                return;
            }

            this.StartGlobalIniFileDownload();
        }

        public static void InstallTranslationWithoutUI()
        {
            ChannelFolder? channelFolder = ChannelFolder.Make(true);

            if (channelFolder == null)
            {
                LoggerFactory.LogWarning("Channel Folder not found");
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(channelFolder);
            installer.InstallLatest();
        }

        /// <summary>
        /// Uninstalls the translation.
        /// </summary>
        /// <returns>
        /// Success.
        /// </returns>
        public bool Uninstall()
        {
            ChannelFolder? channelFolder = ChannelFolder.Make();

            if (channelFolder == null)
            {
                return false;
            }

            string globalIniFilePath = channelFolder.GlobalIniInstallationFilePath;

            if (!File.Exists(globalIniFilePath))
            {
                return false;
            }

            try
            {
                File.Delete(globalIniFilePath);
            }
            catch (Exception ex)
            {
                LoggerFactory.LogError(ex);

                return false;
            }

            return true;
        }

        #endregion

        #region Private

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
                            TranslationVersion version = TranslationVersion.Make(line.Replace(versionCommentToken, ""));
                            LoggerFactory.LogInformation($"Dernière version local installée : {version}");
                            return version;
                        }
                    }
                }
            }

            LoggerFactory.LogInformation("Aucune version local de la traduction n'a été trouvée");
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

            TranslationVersion version = TranslationVersion.Make(html);

            LoggerFactory.LogInformation($"Dernière version disponnible : {version}");
            return version;
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

            if (this.progressWindow != null)
            {
                this.progressWindow.Show();
            }

            client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);
            LoggerFactory.LogInformation("Téléchargement de la mise à jour de la traduction terminer");
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
                if (!Directory.Exists(globalIniDestinationDirectoryPath))
                {
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                    LoggerFactory.LogInformation($"Création du dossier suivant : {globalIniDestinationDirectoryPath}");
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                }

                // Move downloaded file
                File.Move(downloadedGlobalIniFilePath, this.channelFolder.GlobalIniInstallationFilePath, true);
            }
            catch (Exception e)
            {
                LoggerFactory.LogError(e);

                return false;
            }

            return this.CreateOrUpdateUserCfgFile();
        }

        private bool CreateOrUpdateUserCfgFile()
        {
            try
            {
                string g_language = "g_language = french_(france)";

                // Vérification si le fichier existe
                if (!File.Exists(this.channelFolder.UserCfgFilePath))
                {
                    // Si le fichier n'existe pas, le créer avec la ligne g_language
                    File.WriteAllText(this.channelFolder.UserCfgFilePath, g_language);
                    LoggerFactory.LogWarning($"Création du fichier User.cfg, avec la clé : {g_language}");

                    return true;
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
            catch (Exception e)
            {
                LoggerFactory.LogError(e);

                return false;
            }

            return true;
        }

        /// <summary>
        /// To be called once the global.ini file has been downloaded in order to install it.
        /// </summary>
        /// <param name="e"></param>
        private void GlobalIniFileDownloadCompleted(System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (this.progressWindow != null)
            {
                this.progressWindow.Close();
            }

            if (e.Error != null)
            {
                this.Notify(ToolTipIcon.Error, DOWNLOAD_ERROR_MESSAGE);
                LoggerFactory.LogError(e.Error);

                return;
            }

            string localGlobalIniFilePath = App.workingDirectoryPath + GLOBAL_INI_FILE_NAME;

            if (!File.Exists(localGlobalIniFilePath))
            {
                this.Notify(ToolTipIcon.Warning, DOWNLOAD_ERROR_MESSAGE);
                LoggerFactory.LogWarning($"Fichier global.ini téléchargé mais non trouvé, chemin de recherche : {localGlobalIniFilePath}");

                return;
            }

            long length = new FileInfo(localGlobalIniFilePath).Length;

            // File is empty, download failed
            if (length <= 0)
            {
                this.Notify(ToolTipIcon.Warning, DOWNLOAD_ERROR_MESSAGE);
                LoggerFactory.LogWarning("Erreur de téléchargement, fichier vide");

                return;
            }

            bool success = this.InstallGlobalIniFile(localGlobalIniFilePath);

            if (success)
            {
                Properties.Settings.Default.LastUpdateDate = DateTime.Now;
                Properties.Settings.Default.Save();
                this.Notify(ToolTipIcon.Info, "Traduction installée avec succès !");
            }
            else
            {
                this.Notify(ToolTipIcon.Error, "Erreur d'installation de la traduction.");
            }
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
            if (this.notifyIcon != null)
            {
                this.notifyIcon.ShowBalloonTip(2000, App.PROGRAM_NAME, message, icon);
            }

            if (log)
            {
                if (icon == ToolTipIcon.Info)
                {
                    LoggerFactory.LogInformation(message);
                }
                else if (icon == ToolTipIcon.Warning)
                {
                    LoggerFactory.LogWarning(message);
                }
            }
        }

        #endregion

        #region Accessor

        public View.Window.Progress? ProgressWindow
        {
            set { this.progressWindow = value; }
        }

        public NotifyIcon? NotifyIcon
        {
            set { this.notifyIcon = value; }
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
            if (this.progressWindow == null)
            {
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
            this.GlobalIniFileDownloadCompleted(e);

            if (this.OnInstallationEnded != null)
            {
                this.OnInstallationEnded(this, this.channelFolder);
            }
        }

        #endregion
    }
}
