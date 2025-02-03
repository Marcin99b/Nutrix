using Nutrix.Commons.FileSystem;
using Nutrix.Downloading;
using Nutrix.Importing;
using Nutrix.Logging;
using Serilog.Context;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter, ETLStorage storage, EventLogger eventLogger)
{
    public async Task RunDownloader(string downloader)
    {
        using (LogContext.PushProperty("RunDownloader_CorrelationId", Guid.NewGuid().ToString()))
        {
            await ileWazyDownloader.Download();
        }
    }

    public async Task RunImporter(string importer)
    {
        using (LogContext.PushProperty("RunImporter_CorrelationId", Guid.NewGuid().ToString()))
        {
            var filesToImport = storage.GetFilesToImport(nameof(IleWazyDownloader)).ToArray();
            eventLogger.Importer_Started(nameof(IleWazyDownloader), filesToImport.Length);

            var filesImported = 0;
            foreach (var path in filesToImport)
            {
                bool exception = false;
                try
                {
                    var fileName = Path.GetFileName(path);
                    var content = File.ReadAllText(path);
                    await ileWazyImporter.Import(fileName, content);
                }
                catch(Exception ex)
                {
                    exception = true;
                    eventLogger.Importer_Exception(nameof(IleWazyDownloader), path, ex);
                }
                finally
                {
                    if (!exception) 
                    {
                        File.Delete(path);
                        filesImported++;
                    }
                }
            }

            eventLogger.Importer_Finished(nameof(IleWazyDownloader), filesToImport.Length, filesImported);
        }
    }
}