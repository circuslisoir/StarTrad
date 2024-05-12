using StarTrad.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace StarTrad.Tool
{
    internal class BatchTranslationDownloader
    {
        // Event
        public delegate void DownloadEndedHandler<ActionResult>(object sender, ActionResult result);
        public event DownloadEndedHandler<ActionResult>? m_onDownloadEnded = null;

        // UI elements used to display the installation progress
        private View.Window.Progress? m_progressWindow = null;

        private readonly List<TranslationDownloadJob> m_jobs = new List<TranslationDownloadJob>();
        private readonly bool m_silent;

        /*
		Constructor
		*/

        public BatchTranslationDownloader(bool silent)
        {
            m_silent = silent;
        }

        /*
		Public
		*/

        public void AddJob(ChannelFolder channelFolder, string remoteFileUrl)
        {
            TranslationDownloadJob? job = GetJobWithUrl(remoteFileUrl);

            // The same translation file could be installed on more than on channel folder
            if (job != null) {
                job.m_channelFolders.Add(channelFolder);

                return;
            }

            string localFilePath = App.workingDirectoryPath + Guid.NewGuid().ToString() + ".ini";

            job = new TranslationDownloadJob(remoteFileUrl, localFilePath);
            job.m_channelFolders.Add(channelFolder);
            
            m_jobs.Add(job);
        }

        public void StartDownload()
        {
            foreach (TranslationDownloadJob job in m_jobs) {
                // Delete any previously existing file before downloading a new one
                if (File.Exists(job.m_localFilePath)) {
                    try {
                        File.Delete(job.m_localFilePath);
                    } catch (Exception e) {
                        Logger.LogError(e);
                    }
                }

                WebClient client = new WebClient();
                CircuspesClient.AddUserAgentHeader(client.Headers);

                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(job.WebClient_GlobalIniFileDownloadProgress);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClient_GlobalIniFileDownloadProgress);

                // The job event MUST be bound first!
                client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(job.WebClient_GlobalIniFileDownloadCompleted);
                client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(WebClient_GlobalIniFileDownloadCompleted);

                client.DownloadFileAsync(new Uri(job.m_remoteFileUrl), job.m_localFilePath);
                client.Dispose();
            }

            if (!m_silent) {
                m_progressWindow = new View.Window.Progress();
                m_progressWindow.Show();
            }
        }

        /*
		Private
		*/

        public TranslationDownloadJob? GetJobWithUrl(string remoteFileUrl)
        {
            foreach (TranslationDownloadJob job in m_jobs) {
                if (job.m_remoteFileUrl == remoteFileUrl) {
                    return job;
                }
            }

            return null;
        }

        /*
		Accessor
		*/

        public TranslationDownloadJob[] Jobs
        {
            get { return m_jobs.ToArray(); }
        }

        /*
		Event
		*/

        /// <summary>
        /// Called periodically as the download of the global.ini file progresses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebClient_GlobalIniFileDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if (m_progressWindow == null) {
                return;
            }

            long bytesReceived = 0;
            long totalBytesToReceive = 0;

            foreach (TranslationDownloadJob job in m_jobs) {
                bytesReceived += job.BytesReceived;
                totalBytesToReceive += job.TotalBytesToReceive;
            }

            int percentage = (int)(bytesReceived / (float)totalBytesToReceive * 100.0);

            m_progressWindow.ProgressBarPercentage = percentage;
            m_progressWindow.ProgressBarLabelText = percentage + " % (" + bytesReceived / 1000000 + "Mo / " + totalBytesToReceive / 1000000 + "Mo)";
        }

        /// <summary>
        /// Called once the global.ini has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebClient_GlobalIniFileDownloadCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            foreach (TranslationDownloadJob job in m_jobs) {
                if (!job.Completed) {
                    return;
                }
            }

            if (m_progressWindow != null) {
                m_progressWindow.Close();
            }

            if (m_onDownloadEnded == null) {
                return;
            }

            ActionResult result = ActionResult.Successful;

            if (e.Error != null) {
                result = ActionResult.Failure;
                Logger.LogError(e.Error);
            }

            m_onDownloadEnded(this, result);
        }
    }
}
