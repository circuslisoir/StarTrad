using StarTrad.Properties;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using StarTrad.Enum;

namespace StarTrad.Tool
{
    /// <summary>
    /// Downloads then installs a Star Citizen translation.
    /// </summary>
    internal class TranslationInstaller
    {
        private const string GLOBAL_INI_FILE_NAME = "global.ini";
        private const string MESSAGE_GAME_FOLDER_NOT_FOUND = "Impossible de trouver le dossier d'installation du jeu.";

        // A flag which will turn `true` while installing the translation.
        private static bool installing = false;

        // Define an event to be called once the translation installer has finished running.
        public delegate void InstallationEndedHandler<Boolean>(object sender, Boolean channelFolder);
        public event InstallationEndedHandler<ActionResult>? OnInstallationEnded = null;

        // The object representing the Star Citizen's installation directory for a channel, for example:
        // "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        private readonly LibraryFolder libraryFolder;

        // If true, no UI elements will be used to report progress
        private readonly bool silent = true;

        /*
        Constructor
        */

        public TranslationInstaller(LibraryFolder libraryFolder, bool silent)
        {
            this.libraryFolder = libraryFolder;
            this.silent = silent;
        }

        #region Static

        /// <summary>
        /// Shortcut for the non-static Install() method.
        /// </summary>
        /// <param name="silent"></param>
        /// <param name="installationEnded"></param>
        public static void Install(bool silent, InstallationEndedHandler<ActionResult>? installationEnded = null)
        {
            LibraryFolder? libraryFolder = LibraryFolder.Make(!silent);

            if (libraryFolder == null)
            {
                if (!silent) App.Notify(ToolTipIcon.Warning, MESSAGE_GAME_FOLDER_NOT_FOUND);
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(libraryFolder, silent);
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
            LibraryFolder? libraryFolder = LibraryFolder.Make(!silent);

            if (libraryFolder == null) {
                if (!silent) App.Notify(ToolTipIcon.Warning, MESSAGE_GAME_FOLDER_NOT_FOUND);
                return;
            }

            TranslationInstaller installer = new TranslationInstaller(libraryFolder, silent);
            ActionResult result = installer.Uninstall();

            if (silent) {
                return;
            }

            switch (result) {
                case ActionResult.Successful: App.Notify(ToolTipIcon.Info, "Traduction désinstallée avec succès !"); break;
                case ActionResult.Failure: App.Notify(ToolTipIcon.Warning, "La traduction n'a pas pu être désinstallée."); break;
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
                this.End(ActionResult.Failure);

                return;
            }

            List<ChannelFolder> channelsWithoutUpTodateTranslation = new List<ChannelFolder>();

            foreach (ChannelFolder channelFolder in this.libraryFolder.EnumerateChannelFolders(true)) {
                TranslationVersion? installedVersion = channelFolder.GetInstalledTranslationVersion();

                // We don't have a way to check the version number for the non-LIVE remote translations, so we'll always include them
                if (installedVersion == null || !channelFolder.IsLiveChannel || latestVersion.IsNewerThan(installedVersion)) {
                    channelsWithoutUpTodateTranslation.Add(channelFolder);

                    continue;
                }

                // We already have the latest version installed for this channel, we'll just setup the user.cfg file just in case
                this.CreateOrUpdateUserCfgFile(channelFolder);
            }

            if (channelsWithoutUpTodateTranslation.Count < 1) {
                this.Notify(ToolTipIcon.Info, "Dernière version de traduction déjà installée.", true);
                this.End(ActionResult.Successful);

                return;
            }

            ChannelFolder[] channelsForInstallation = channelsWithoutUpTodateTranslation.ToArray();

            // We'll let the user select the channels the translation should be installed on
            if (!this.silent && channelsWithoutUpTodateTranslation.Count > 1) {
                View.Window.ChannelSelector channelSelectorWindow = new(channelsForInstallation, "Installer");
                channelSelectorWindow.ShowDialog();

                channelsForInstallation = channelSelectorWindow.SelectedChannelFolders;

                if (channelsForInstallation.Length < 1) {
                    this.End(ActionResult.UserCanceled);
                 
                    return;
                }
            }

            this.StartGlobalIniFileDownload(channelsForInstallation.ToArray());
        }

        /// <summary>
        /// Uninstalls the translation.
        /// </summary>
        /// <returns>
        /// Success.
        /// </returns>
        public ActionResult Uninstall()
        {
            // Don't try to uninstall while another instance is trying to install
            if (TranslationInstaller.installing) {
                return ActionResult.Aborted;
            }

            LibraryFolder? libraryFolder = LibraryFolder.Make();

            if (libraryFolder == null) {
                return ActionResult.Failure;
            }

            ChannelFolder[] channelFolders = libraryFolder.EnumerateChannelFolders(true, true).ToArray();

            // We'll let the user select the channels the translation should be uninstalled from
            if (!this.silent && channelFolders.Length > 1) {
                View.Window.ChannelSelector channelSelectorWindow = new(channelFolders, "Désinstaller");
                channelSelectorWindow.ShowDialog();

                channelFolders = channelSelectorWindow.SelectedChannelFolders;

                if (channelFolders.Length < 1) {
                    return ActionResult.UserCanceled;
                }
            }

            foreach (ChannelFolder channelFolder in channelFolders) {
                string globalIniFilePath = channelFolder.GlobalIniInstallationFilePath;
                string userCfgFilePath = channelFolder.UserCfgFilePath;

                if (File.Exists(globalIniFilePath)) {
                    try {
                        File.Delete(globalIniFilePath);
                    } catch (Exception ex) {
                        Logger.LogError(ex);

                        return ActionResult.Failure;
                    }
                }

                if (File.Exists(userCfgFilePath)) {
                    try {
                        File.Delete(userCfgFilePath);
                    } catch (Exception ex) {
                        Logger.LogError(ex);

                        return ActionResult.Failure;
                    }
                }
            }

            return ActionResult.Successful;
        }

        #endregion

        #region Private

        /// <summary>
        /// Obtains an object representing the latest version of the translation.
        /// </summary>
        /// <returns></returns>
        private TranslationVersion? QueryLatestAvailableTranslationVersion()
        {
            Logger.LogInformation("Récupération de la dernière version de la traduction");
            string? html = CircuspesClient.GetRequest("/download/version.html");

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            TranslationVersion? version = TranslationVersion.Make(html);

            if (version == null) {
                return null;
            }

            Logger.LogInformation($"Dernière version disponnible : {version.FullVersionNumber}");

            return version;
        }

        /// <summary>
        /// Starts downloading the global.ini translation file from the circuspes website.
        /// </summary>
        private void StartGlobalIniFileDownload(ChannelFolder[] channels)
        {
            Logger.LogInformation("Lancement du téléchargement de la dernière version de la traduction");

            BatchTranslationDownloader downloader = new(this.silent);
            downloader.m_onDownloadEnded += BatchFileDownload_DownloadCompleted;

            foreach (ChannelFolder channel in channels) {
                string part = "download";

                if (!channel.IsLiveChannel) {
                    part = "download_ptu";
                }

                downloader.AddJob(channel, CircuspesClient.HOST + "/" + part + "/" + GLOBAL_INI_FILE_NAME);
            }

            downloader.StartDownload();
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
        private bool InstallGlobalIniFile(ChannelFolder channelFolder, string downloadedGlobalIniFilePath)
        {
            Logger.LogInformation($"Installation du nouveau fichier Global.ini");
            string globalIniDestinationDirectoryPath = channelFolder.GlobalIniInstallationDirectoryPath;

            try
            {
                // Create destination directory if need
                if (!Directory.Exists(globalIniDestinationDirectoryPath)) {
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                    Logger.LogInformation($"Création du dossier suivant : {globalIniDestinationDirectoryPath}");
                    Directory.CreateDirectory(globalIniDestinationDirectoryPath);
                }

                // Move downloaded file
                File.Copy(downloadedGlobalIniFilePath, channelFolder.GlobalIniInstallationFilePath, true);
            } catch (Exception e) {
                Logger.LogError(e);

                return false;
            }

            return this.CreateOrUpdateUserCfgFile(channelFolder);
        }

        private bool CreateOrUpdateUserCfgFile(ChannelFolder channelFolder)
        {
            try
            {
                string g_language = "g_language = french_(france)";

                // Vérification si le fichier existe
                if (!File.Exists(channelFolder.UserCfgFilePath))
                {
                    // Si le fichier n'existe pas, le créer avec la ligne g_language
                    File.WriteAllText(channelFolder.UserCfgFilePath, g_language);
                    Logger.LogWarning($"Création du fichier User.cfg, avec la clé : {g_language}");

                    return true;
                }

                // Lecture du fichier ligne par ligne
                string[] lines = File.ReadAllLines(channelFolder.UserCfgFilePath);

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
                            Logger.LogInformation($"Mise à jour de la ligne : {lines[i]}. Dans le fichier User.cfg");
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
                    Logger.LogInformation($"Nouvelle ligne ajouté, au fichier User.cfg : {lines[lines.Length - 1]}");
                }

                // Écriture des modifications dans le fichier
                File.WriteAllLines(channelFolder.UserCfgFilePath, lines);
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                return false;
            }

            return true;
        }

        /// <summary>
        /// To be called once the global.ini file has been downloaded in order to install it.
        /// </summary>
        /// <param name="e"></param>
        private ActionResult InstallDownloadedGlobalIniFile(TranslationDownloadJob job)
        {
            if (!File.Exists(job.m_localFilePath)) {
                Logger.LogWarning($"Fichier global.ini téléchargé mais non trouvé, chemin de recherche : {job.m_localFilePath}");

                return ActionResult.Failure;
            }

            long length = new FileInfo(job.m_localFilePath).Length;

            // File is empty, download failed
            if (length <= 0) {
                Logger.LogWarning("Erreur de téléchargement, fichier vide");

                return ActionResult.Failure;
            }

            bool success = true;
            
            foreach (ChannelFolder channel in job.m_channelFolders) {
                success = success && this.InstallGlobalIniFile(channel, job.m_localFilePath);
            }

            try {
                File.Delete(job.m_localFilePath);
            } catch (Exception ex) {
                Logger.LogError(ex);
            }

            if (!success) {
                return ActionResult.Failure;
            }

            return ActionResult.Successful;
        }

        /// <summary>
        /// Checks how went the files download and installs them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="downloadResult"></param>
        /// <returns></returns>
        private ActionResult InstallCompletedDownloads(object? sender, ActionResult downloadResult)
        {
            if (sender == null) {
                return ActionResult.Failure;
            }

            if (downloadResult != ActionResult.Successful) {
                return downloadResult;
            }

            BatchTranslationDownloader downloader = (BatchTranslationDownloader)sender;
            TranslationDownloadJob[] jobs = downloader.Jobs;

            if (jobs.Length == 0) {
                return ActionResult.Failure;
            }

            // Install each downloaded file for the right channel folder
            foreach (TranslationDownloadJob job in jobs) {
                ActionResult installResult = InstallDownloadedGlobalIniFile(job);

                if (installResult != ActionResult.Successful) {
                    return installResult;
                }
            }

            Settings.Default.LastUpdateDate = DateTime.Now;
            Settings.Default.Save();
            
            this.Notify(ToolTipIcon.Info, "Traduction installée avec succès !");
            
            return ActionResult.Successful;
        }

        /// <summary>
        /// To be called at the end of the installation process.
        /// </summary>
        private void End(ActionResult result)
        {
            if (this.OnInstallationEnded != null) {
                this.OnInstallationEnded(this, result);
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
            if (!this.silent) {
                App.Notify(icon, message);
            }

            if (log) {
                if (icon == ToolTipIcon.Info) {
                    Logger.LogInformation(message);
                } else if (icon == ToolTipIcon.Warning) {
                    Logger.LogWarning(message);
                }
            }
        }

        #endregion

        #region Event

        /// <summary>
        /// Called once the global.ini has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatchFileDownload_DownloadCompleted(object? sender, ActionResult downloadResult)
        {
            ActionResult installResult = InstallCompletedDownloads(sender, downloadResult);

            if (installResult == ActionResult.Failure) {
                this.Notify(ToolTipIcon.Error, "Erreur d'installation de la traduction.");
            }

            this.End(installResult);
        }

        #endregion
    }
}
