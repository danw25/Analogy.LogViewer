using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Analogy.Interfaces;

namespace Analogy
{
    public class FileProcessor
    {
        private UserSettingsManager Settings { get; } = UserSettingsManager.UserSettings;
        private string FileName { get; set; }
        public Stream DataStream { get; set; }
        private ILogMessageCreatedHandler DataWindow { get; set; }
        public UCLogs LogWindow { get; set; }

        public FileProcessor(ILogMessageCreatedHandler dataWindow)
        {
            DataWindow = dataWindow;
        }
        public async Task<IEnumerable<AnalogyLogMessage>> Process(IAnalogyOfflineDataProvider fileDataProvider, string filename, CancellationToken token)
        {
            FileName = filename;
            if (string.IsNullOrEmpty(FileName)) return new List<AnalogyLogMessage>();
            if (!DataWindow.ForceNoFileCaching && FileProcessingManager.Instance.AlreadyProcessed(FileName) && Settings.EnableFileCaching) //get it from the cache
            {
                var cachedMessages = FileProcessingManager.Instance.GetMessages(FileName);
                DataWindow.AppendMessages(cachedMessages, Utils.GetFileNameAsDataSource(FileName));
                return cachedMessages;

            }

            if (FileProcessingManager.Instance.IsFileCurrentlyBeingProcessed(FileName))
            {
                while (FileProcessingManager.Instance.IsFileCurrentlyBeingProcessed(FileName))
                {
                    await Task.Delay(1000);
                }
                var cachedMessages = FileProcessingManager.Instance.GetMessages(FileName);
                DataWindow.AppendMessages(cachedMessages, Utils.GetFileNameAsDataSource(FileName));
                
                return cachedMessages;

            }
            //otherwise read file:
            FileProcessingManager.Instance.AddProcessingFile(FileName);
            Settings.AddToRecentFiles(fileDataProvider.ID, FileName);
            var messages = (await fileDataProvider.Process(filename, token, DataWindow).ConfigureAwait(false)).ToList();
            FileProcessingManager.Instance.DoneProcessingFile(messages.ToList(), FileName);
            return messages;

        }
    }
}