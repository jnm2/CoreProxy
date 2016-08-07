using System;
using System.IO;
using System.ServiceProcess;
using jnm2.CoreProxy.Config;

namespace CoreProxy.Windows.Service
{
    public partial class CoreProxyService : ServiceBase
    {
        private jnm2.CoreProxy.CoreProxyService service;
        private FileLogger logger;

        public CoreProxyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoreProxy");
            logger = new FileLogger(Path.Combine(baseDir, "service.log"));

            try
            {
                var config = new JsonConfigProvider(Path.Combine(baseDir, "service.config.json")).Load();
                service = new jnm2.CoreProxy.CoreProxyService(config, logger.Log);
                service.StartAll();
            }
            catch (Exception ex)
            {
                logger.Log(ex.ToString());
                logger.Dispose();
                throw;
            }
        }

        protected override void OnStop()
        {
            service.Dispose();
            logger.Dispose();
        }
    }
}
