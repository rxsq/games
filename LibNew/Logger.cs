using log4net;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

public class logger
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
    static logger()
    {
        // Basic configuration. log4net will search for configuration in the consuming application's config file.
        log4net.Config.XmlConfigurator.Configure();
    }

    public static void Log(string message)
    {
        log.Info(message);
    }

    public static void LogError(string message)
    {
        log.Error(message);
    }
}