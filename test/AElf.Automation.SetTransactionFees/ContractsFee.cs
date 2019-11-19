using System;
using System.Linq;
using Acs1;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.ContractSerializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.SetTransactionFees
{
    public class ContractsFee
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public ContractsFee(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            ContractHandler = new ContractHandler();
            Caller = NodeOption.AllNodes.First().Account;
            Genesis = nodeManager.GetGenesisContract(Caller);
        }

        private INodeManager NodeManager { get; }
        private ContractHandler ContractHandler { get; }

        private GenesisContract Genesis { get; }

        private string Caller { get; }

        public void SetAllContractsMethodFee(long amount)
        {
            var authority = new AuthorityManager(NodeManager);
            var genesisOwner = authority.GetGenesisOwnerAddress();
            var miners = authority.GetCurrentMiners();
            var systemContracts = Genesis.GetAllSystemContracts();

            foreach (var provider in GenesisContract.NameProviderInfos.Keys)
            {
                Logger.Info($"Begin set contract: {provider}");
                var contractInfo = ContractHandler.GetContractInfo(provider);
                var contractAddress = systemContracts[provider];
                if (contractAddress == new Address())
                {
                    Logger.Warn($"Contract {provider} not deployed.");
                    continue;
                }

                var contractFee =
                    new ContractMethodFee(NodeManager, authority, contractInfo, contractAddress.GetFormatted());
                var primaryToken = NodeManager.GetPrimaryTokenSymbol();
                contractFee.SetContractFees(primaryToken, amount, genesisOwner, miners, Caller);
            }
        }

        public void QueryAllContractsMethodFee()
        {
            var systemContracts = Genesis.GetAllSystemContracts();
            foreach (var provider in GenesisContract.NameProviderInfos.Keys)
            {
                Logger.Info($"Query contract fees: {provider}");
                var contractInfo = ContractHandler.GetContractInfo(provider);
                var contractAddress = systemContracts[provider];
                if (contractAddress == new Address())
                {
                    Logger.Warn($"Contract {provider} not deployed.");
                    Console.WriteLine();
                    continue;
                }

                foreach (var method in contractInfo.ActionMethodNames)
                {
                    var feeResult = NodeManager.QueryView<MethodFees>(Caller, contractAddress.GetFormatted(),
                        "GetMethodFee", new StringValue
                        {
                            Value = method
                        });
                    if (feeResult.Fees.Count > 0)
                    {
                        var amountInfo = feeResult.Fees.First();
                        Logger.Info(
                            $"Method: {method.PadRight(48)} Symbol: {amountInfo.Symbol}   Amount: {amountInfo.BasicFee}");
                    }
                    else
                    {
                        var primaryToken = NodeManager.GetPrimaryTokenSymbol();
                        Logger.Warn($"Method: {method.PadRight(48)} Symbol: {primaryToken}   Amount: 0");
                    }
                }

                Console.WriteLine();
            }
        }
    }
}