using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.SDK;
using log4net;

namespace AElf.Automation.ScenariosExecution
{
    public class OldEnvCheck
    {
        private static NodesInfo _config;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static readonly string AccountDir = CommonHelper.GetCurrentDataDir();

        private static readonly OldEnvCheck Instance = new OldEnvCheck();

        private OldEnvCheck()
        {
            _config = NodeInfoHelper.Config;
        }

        private static ContractServices Services { get; set; }

        public static OldEnvCheck GetDefaultEnvCheck()
        {
            return Instance;
        }
        
        public ContractServices GetContractServices()
        {
            if (Services != null)
                return Services;

            var specifyEndpoint = ConfigInfoHelper.Config.SpecifyEndpoint;
            var url = specifyEndpoint.Enable
                ? specifyEndpoint.ServiceUrl
                : _config.Nodes.First(o => o.Status).Endpoint;
            Logger.Info($"All request sent to endpoint: {url}");
            var nodeManager = new NodeManager(url, AccountDir);

            var bpAccount = _config.Nodes.First().Account;
            Services = new ContractServices(nodeManager, bpAccount);

            return Services;
        }
    }
}