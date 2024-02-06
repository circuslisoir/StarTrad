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
        private const string MESSAGE_DOWNLOAD_ERROR = "Erreur de téléchargement du fichier de traduction.";
        private const string MESSAGE_GAME_FOLDER_NOT_FOUND = "Impossible de trouver le dossier d'installation du jeu.";

        // A flag which will turn `true` while installing the translation.
        private static bool installing = false;

        // Define an event to be called once the translation installer has finished running.
        public delegate void InstallationEndedHandler<Boolean>(object sender, Boolean channelFolder);
        public event InstallationEndedHandler<bool>? OnInstallationEnded = null;

        // The object representing the Star Citizen's installation directory for a channel, for example:
        // "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        private readonly ChannelFolder channelFolder;

        // If true, no UI elements will be used to report progress
        private readonly bool silent = true;

        // UI elements used to display the installation progress
        private View.Window.Progress? progressWindow = null;

        /*
        Constructor
        */

        public TranslationInstaller(ChannelFolder channelFolder, bool silent)
        {
            this.channelFolder = channelFolder;
            this.silent = silent;
        }

        #region Static

        /// <summary>
        /// Shortcut for the non-static Install() method.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="installationEnded"></param>
        public static void Install(bool silent, InstallationEndedHandler<bool>? installationEnded = null)
        {
            ChannelFolder? channelFolder = ChannelFolder.Make(!silent);

            if (channelFolder == null)
            {
                if (!silent) App.Notify(ToolTipIcon.Warning, MESSAGE_GAME_FOLDER_NOT_FOUND);
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(channelFolder, silent);
            installer.OnInstallationEnded += installationEnded;
            installer.Install();
        }

        /// <summary>
        /// Shortcut for the non-static Uninstall() method.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="installationEnded"></param>
        public static void Uninstall(bool silent)
        {
            ChannelFolder? channelFolder = ChannelFolder.Make(!silent);

            if (channelFolder == null)
            {
                if (!silent) App.Notify(ToolTipIcon.Warning, MESSAGE_GAME_FOLDER_NOT_FOUND);
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(channelFolder, silent);
            bool success = installer.Uninstall();

            if (silent)
            {
                return;
            }

            if (success)
            {
                App.Notify(ToolTipIcon.Info, "Traduction désinstallée avec succès !");
            }
            else
            {
                App.Notify(ToolTipIcon.Warning, "La traduction n'a pas pu être désinstallée.");
            }
        }

        #endregion Static

        #region Public

        /// <summary>
        /// Installs, if needed, the latest version of the translation from the circuspes website.
        /// </summary>
        public void Install()
        {
            // Another installation is already in progress
            if (TranslationInstaller.installing) return;
            TranslationInstaller.installing = true;

            TranslationVersion? latestVersion = this.QueryLatestAvailableTranslationVersion();

            // Unable to obtain the remote version
            if (latestVersion == null)
            {
                this.Notify(ToolTipIcon.Warning, "Impossible de récuprérer la version de la dernière traduction.", true);
                this.End(false);

                return;
            }

            TranslationVersion? installedVersion = this.GetInstalledTranslationVersion();

            // We already have the latest version installed
            if (installedVersion != null && !latestVersion.IsNewerThan(installedVersion))
            {
                this.CreateOrUpdateUserCfgFile();
                this.Notify(ToolTipIcon.Info, "Dernière version de traduction déjà installée.", true);
                this.End(true);

                return;
            }

            this.StartGlobalIniFileDownload();
        }

        /// <summary>
        /// Uninstalls the translation.
        /// </summary>
        /// <returns>
        /// Success.
        /// </returns>
        public bool Uninstall()
        {
            // Don't try to uninstall while another instance is trying to install
            if (TranslationInstaller.installing)
            {
                return false;
            }

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
                        if (!line.StartsWith(versionCommentToken))
                        {
                            continue;
                        }

                        TranslationVersion? version = TranslationVersion.Make(line.Replace(versionCommentToken, ""));

                        if (version == null)
                        {
                            continue;
                        }

                        LoggerFactory.LogInformation($"Dernière version local installée : {version.FullVersionNumber}");

                        return version;
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

            TranslationVersion? version = TranslationVersion.Make(html);

            if (version == null) {
                return null;
            }

            LoggerFactory.LogInformation($"Dernière version disponnible : {version.FullVersionNumber}");

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
                try
                {
                    File.Delete(localGlobalIniFilePath);
                }
                catch (Exception e)
                {
                    LoggerFactory.LogError(e);
                }
            }

            WebClient client = new WebClient();
            CircuspesClient.AddUserAgentHeader(client.Headers);

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.WebClient_GlobalIniFileDownloadProgress);
            client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(this.WebClient_GlobalIniFileDownloadCompleted);

            if (!this.silent)
            {
                this.progressWindow = new View.Window.Progress(this.channelFolder.Name);
                this.progressWindow.Show();
            }

            client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);
            LoggerFactory.LogInformation("Téléchargement de la traduction terminée");
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
        private bool GlobalIniFileDownloadCompleted(System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (this.progressWindow != null)
            {
                this.progressWindow.Close();
            }

            if (e.Error != null)
            {
                this.Notify(ToolTipIcon.Error, MESSAGE_DOWNLOAD_ERROR);
                LoggerFactory.LogError(e.Error);

                return false;
            }

            string localGlobalIniFilePath = App.workingDirectoryPath + GLOBAL_INI_FILE_NAME;

            if (!File.Exists(localGlobalIniFilePath))
            {
                this.Notify(ToolTipIcon.Warning, MESSAGE_DOWNLOAD_ERROR);
                LoggerFactory.LogWarning($"Fichier global.ini téléchargé mais non trouvé, chemin de recherche : {localGlobalIniFilePath}");

                return false;
            }

            long length = new FileInfo(localGlobalIniFilePath).Length;

            // File is empty, download failed
            if (length <= 0)
            {
                this.Notify(ToolTipIcon.Warning, MESSAGE_DOWNLOAD_ERROR);
                LoggerFactory.LogWarning("Erreur de téléchargement, fichier vide");

                return false;
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

            return success;
        }

        /// <summary>
        /// To be called at the end of the installation process.
        /// </summary>
        private void End(bool success)
        {
            if (this.OnInstallationEnded != null)
            {
                this.OnInstallationEnded(this, success);
            }

            TranslationInstaller.installing = false;
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
            if (!this.silent)
            {
                App.Notify(icon, message);
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
            bool success = this.GlobalIniFileDownloadCompleted(e);
            this.End(success);
        }

        #endregion
    }
}
