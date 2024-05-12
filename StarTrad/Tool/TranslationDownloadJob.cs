using System.Collections.Generic;
using System.Net;

namespace StarTrad.Tool
{
    internal class TranslationDownloadJob
    {
        // The channel this file should be installed for.
        public readonly List<ChannelFolder> m_channelFolders = new List<ChannelFolder>();

        public readonly string m_remoteFileUrl;
        public readonly string m_localFilePath;

        private DownloadProgressChangedEventArgs? m_progressEvent = null;
        private bool m_completed = false;

        /*
		Constructor
		*/

        public TranslationDownloadJob(string remoteFileUrl, string localFilePath)
        {
            m_remoteFileUrl = remoteFileUrl;
            m_localFilePath = localFilePath;
        }

        /*
		Public
		*/

        public void StartDownload()
        {

        }

        /*
		Accessor
		*/

        public long BytesReceived
        {
            get { return m_progressEvent == null ? 0 : m_progressEvent.BytesReceived; }
        }

        public long TotalBytesToReceive
        {
            get { return m_progressEvent == null ? 0 : m_progressEvent.TotalBytesToReceive; }
        }

        public bool Completed
        {
            get { return m_completed; }
        }

        /*
		Event
		*/

        /// <summary>
        /// Called periodically as the download of the global.ini file progresses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WebClient_GlobalIniFileDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            m_progressEvent = e;
        }

        /// <summary>
        /// Called once the global.ini has been downloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WebClient_GlobalIniFileDownloadCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            m_completed = true;
        }
    }
}
