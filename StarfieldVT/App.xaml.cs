using System.IO;
using System.Windows;

using Serilog;

using StarfieldVT.Core.Filesystem;

namespace StarfieldVT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(AppDataFolder.GetLogDir(),
                        $"starfieldvt.log"
                        ),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 524288000
                    )
                .CreateLogger();

            base.OnStartup(e);
        }
    }

}
