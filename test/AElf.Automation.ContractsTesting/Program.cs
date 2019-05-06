﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ContractsTesting
{
    class Program
    {
        #region Private Properties
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private string TokenContract { get; set; }
        private List<string> Users { get; set; }
        #endregion

        #region Parameter Option

        [Option("-ba|--bp.accoount", Description = "Bp account info")]
        public string BpAccount { get; set; } = "ELF_3SMq6XUt2ogboq3fTXwKF6bs3zt9f3EBqsMfDpVzvaX4U4K";

        [Option("-bp|--bp.password", Description = "Bp account password info")]
        public string BpPassword { get; set; } = "123";

        [Option("-e|--endpoint", Description = "Node service endpoint info")]
        public string Endpoint { get; set; } = "http://192.168.197.13:8100/chain";

        #endregion

        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (AssertFailedException ex)
            {
                Logger.WriteError($"Execute failed: {ex.Message}");
            }

            return 0;
        }

        private void OnExecute()
        {
            #region Basic Preparation
            //Init Logger
            string logName = "ContractTest_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            var ch = new RpcApiHelper(Endpoint, AccountManager.GetDefaultDataDir());

            //Connect Chain
            var ci = new CommandInfo(ApiMethods.GetChainInformation);
            ch.ExecuteCommand(ci);
            Assert.IsTrue(ci.Result, "Connect chain got exception.");

            //Account preparation
            Users = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                ci = new CommandInfo(ApiMethods.AccountNew)
                {
                    Parameter = "123"
                };
                ci = ch.NewAccount(ci);
                if(ci.Result)
                    Users.Add(ci.InfoMsg?[0].ToString().Replace("Account address:", "").Trim());

                //unlock
                var uc = new CommandInfo(ApiMethods.AccountUnlock)
                {
                    Parameter = $"{Users[i]} 123 notimeout"
                };
                ch.UnlockAccount(uc);
            }
            #endregion

            #region Block verify testing

            var blockHeight = 1;
            while (true)
            {
                var heightCommand = new CommandInfo(ApiMethods.GetBlockHeight);
                ch.RpcGetBlockHeight(heightCommand);
                heightCommand.GetJsonInfo();
                var height = int.Parse(heightCommand.JsonInfo["result"].ToString());
                if (blockHeight == height)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    for (var i = blockHeight; i <= height; i++)
                    {
                        var blockCommand = new CommandInfo(ApiMethods.GetBlockInfo)
                        {
                            Parameter = $"{i} false"
                        };
                        ch.RpcGetBlockInfo(blockCommand);
                        blockCommand.GetJsonInfo();
                        Logger.WriteInfo("Height={0}, Block Hash={1}, TxCount={2}, Time: {3}", 
                            i,
                            blockCommand.JsonInfo["result"]["BlockHash"].ToString(),
                            blockCommand.JsonInfo["result"]["Body"]["TransactionsCount"].ToString(),
                            blockCommand.JsonInfo["result"]["Header"]["Time"].ToString()); 
                    }

                    blockHeight = height;
                }
            }
            
            #endregion
        }
    }
}
