﻿using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ResourceContractsTest
    {
        public static ILogHelper Logger = LogHelper.GetLogHelper();
        public string TokenAbi { get; set; }
        public CliHelper CH { get; set; }
        public string RpcUrl { get; } = "http://192.168.197.34:8000/chain";
        public List<string> AccList { get; set; }

        public ResourceContract resourceService { get; set; }

        [TestInitialize]
        public void Initlize()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            CH = new CliHelper(RpcUrl, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo("connect_chain");
            CH.RpcConnectChain(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Get AElf.Contracts.Token ABI
            ci.GetJsonInfo();
            TokenAbi = ci.JsonInfo["AElf.Contracts.Token"].ToObject<string>();

            //Load default Contract Abi
            ci = new CommandInfo("load_contract_abi");
            CH.RpcLoadContractAbi(ci);
            Assert.IsTrue(ci.Result, "Load contract abi got exception.");

            //Account preparation
            AccList = new List<string>();
            ci = new CommandInfo("account new", "account");
            for (int i = 0; i < 5; i++)
            {
                ci.Parameter = "123";
                ci = CH.ExecuteCommand(ci);
                if (ci.Result)
                    AccList.Add(ci.InfoMsg?[0].Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo("account unlock", "account");
                uc.Parameter = String.Format("{0} {1} {2}", AccList[i], "123", "notimeout");
                uc = CH.ExecuteCommand(uc);
            }
            //Init resource service
            resourceService = new ResourceContract(CH, AccList[2]);

            #endregion
        }

        [TestMethod]
        public void IssueResourceTest()
        {
            PrepareResourceToken();
        }

        [TestMethod]
        public void BuyResource1()
        {
            resourceService.Account = AccList[3];
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "100");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Ram", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }

        public void BuyResource2()
        {
            resourceService.Account = AccList[4];
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Cpu", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Ram", "500");
            resourceService.CallContractMethod(ResourceMethod.BuyResource, "Net", "1000");
            QueryResourceInfo();
        }


        [TestMethod]
        public void SellResource()
        {

        }

        private void PrepareResourceToken()
        {
            resourceService = new ResourceContract(CH, AccList[2]);
            //Init
            resourceService.CallContractMethod(ResourceMethod.Initialize, TokenAbi, AccList[2], AccList[2]);

            //Issue
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Cpu", "1000000");
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Net", "1000000");
            resourceService.CallContractMethod(ResourceMethod.IssueResource, "Ram", "1000000");

            //Query address
            var tokenAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfTokenAddress);
            Logger.WriteInfo("Token address:", resourceService.ConvertQueryResult(tokenAddress));

            var feeAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetFeeAddress);
            Logger.WriteInfo("Fee address:", resourceService.ConvertQueryResult(feeAddress));

            var controllerAddress = resourceService.CallReadOnlyMethod(ResourceMethod.GetResourceControllerAddress);
            Logger.WriteInfo("Controller address:", resourceService.ConvertQueryResult(controllerAddress));
        }

        private void QueryResourceInfo()
        {
            //Converter message
            var cpuConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Cpu");
            var ramConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Ram");
            var netConverter = resourceService.CallReadOnlyMethod(ResourceMethod.GetConverter, "Net");
            Logger.WriteInfo("GetConverter info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertQueryResult(cpuConverter),
                resourceService.ConvertQueryResult(ramConverter),
                resourceService.ConvertQueryResult(netConverter));

            //User Balance
            var cpuBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Cpu");
            var ramBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Ram");
            var netBalance = resourceService.CallReadOnlyMethod(ResourceMethod.GetUserBalance, AccList[2], "Net");
            Logger.WriteInfo("GetUserBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertQueryResult(cpuBalance),
                resourceService.ConvertQueryResult(ramBalance),
                resourceService.ConvertQueryResult(netBalance));

            //Exchange Balance
            var cpuExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Cpu");
            var ramExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Ram");
            var netExchange = resourceService.CallReadOnlyMethod(ResourceMethod.GetExchangeBalance, "Net");
            Logger.WriteInfo("GetExchangeBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertQueryResult(cpuExchange),
                resourceService.ConvertQueryResult(ramExchange),
                resourceService.ConvertQueryResult(netExchange));

            //Elf Balance
            var cpuElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Cpu");
            var ramElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Ram");
            var netElf = resourceService.CallReadOnlyMethod(ResourceMethod.GetElfBalance, "Net");
            Logger.WriteInfo("GetElfBalance info: Cpu-{0}, Ram-{1}, Net-{2}",
                resourceService.ConvertQueryResult(cpuElf),
                resourceService.ConvertQueryResult(ramElf),
                resourceService.ConvertQueryResult(netElf));
        }
    }
}