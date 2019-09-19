using System;
using System.Collections.Generic;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class ElectionTests
    {
        protected static readonly ILog _logger = Log4NetHelper.GetLogger();
        protected static string RpcUrl { get; } = "http://18.162.41.20:8000";

        protected Behaviors Behaviors;

        //protected RpcApiHelper CH { get; set; }   
        protected INodeManager CH { get; set; }
        protected string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        protected List<string> BpNodeAddress { get; set; }
        protected List<string> FullNodeAddress { get; set; }
        protected static List<string> UserList { get; set; }
        protected List<string> NodesPublicKeys { get; set; }
        protected List<CandidateInfo> CandidateInfos { get; set; }
        protected Dictionary<Behaviors.ProfitType, Hash> ProfitItemsIds { get; set; }

        protected class CandidateInfo
        {
            public string Name { get; set; }
            public string Account { get; set; }
            public string PublicKey { get; set; }
        }

        protected void Initialize()
        {
            //Init Logger
            Log4NetHelper.LogInit("ElectionTest");

            #region Get services

            CH = new NodeManager(RpcUrl);
            var contractServices = new ContractServices(CH, InitAccount);
            Behaviors = new Behaviors(contractServices);

//            var schemeIds = Behaviors.GetCreatedProfitItems().SchemeIds;
//            ProfitItemsIds = new Dictionary<Behaviors.ProfitType, Hash>
//            {
//                {Behaviors.ProfitType.Treasury, schemeIds[0]},
//                {Behaviors.ProfitType.MinerReward, schemeIds[1]},
//                {Behaviors.ProfitType.BackSubsidy, schemeIds[2]},
//                {Behaviors.ProfitType.CitizenWelfare, schemeIds[3]},
//                {Behaviors.ProfitType.BasicMinerReward, schemeIds[4]},
//                {Behaviors.ProfitType.VotesWeightReward, schemeIds[5]},
//                {Behaviors.ProfitType.ReElectionReward, schemeIds[6]},
//            };

            #endregion

            #region Basic Preparation

            //Get FullNode Info
            FullNodeAddress = new List<string>();
            FullNodeAddress.Add("2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6");
            FullNodeAddress.Add("YF8o6ytMB7n5VF9d1RDioDXqyQ9EQjkFK3AwLPCH2b9LxdTEq");
            FullNodeAddress.Add("h6CRCFAhyozJPwdFRd7i8A5zVAqy171AVty3uMQUQp1MB9AKa");
            FullNodeAddress.Add("28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823");
            FullNodeAddress.Add("2Dyh4ASm6z7CaJ1J1WyvMPe2sJx5TMBW8CMTKeVoTMJ3ugQi3P");
            FullNodeAddress.Add("2G4L1S7KPfRscRP6zmd7AdVwtptVD3vR8YoF1ZHgPotDNbZnNY");
            FullNodeAddress.Add("W4xEKTZcvPKXRAmdu9xEpM69ArF7gUxDh9MDgtsKnu7JfePXo");
            FullNodeAddress.Add("2REajHMeW2DMrTdQWn89RQ26KQPRg91coCEtPP42EC9Cj7sZ61");
            FullNodeAddress.Add("2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV");
            FullNodeAddress.Add("2cv45MBBUHjZqHva2JMfrGWiByyScNbEBjgwKoudWQzp6vX8QX");


            //Get BpNode Info
            BpNodeAddress = new List<string>();
            BpNodeAddress.Add("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK");
            BpNodeAddress.Add("2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz");
            BpNodeAddress.Add("WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k");
            BpNodeAddress.Add("2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D");
            BpNodeAddress.Add("2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2");
            BpNodeAddress.Add("eFU9Quc8BsztYpEHKzbNtUpu9hGKgwGD2tyL13MqtFkbnAoCZ");
            BpNodeAddress.Add("2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws");
            BpNodeAddress.Add("EKRtNn3WGvFSTDewFH81S7TisUzs9wPyP4gCwTww32waYWtLB");
            BpNodeAddress.Add("2LA8PSHTw4uub71jmS52WjydrMez4fGvDmBriWuDmNpZquwkNx");

            //Get candidate infos
            NodesPublicKeys = new List<string>();
            CandidateInfos = new List<CandidateInfo>();
            for (var i = 0; i < BpNodeAddress.Count; i++)
            {
                var name = $"Bp-{i + 1}";
                var account = BpNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo() {Name = name, Account = account, PublicKey = pubKey});
            }

            for (var i = 0; i < FullNodeAddress.Count; i++)
            {
                var name = $"Full-{i + 1}";
                var account = FullNodeAddress[i];
                var pubKey = CH.GetPublicKeyFromAddress(account);
                NodesPublicKeys.Add(pubKey);
                _logger.Info($"{account}: {pubKey}");
                CandidateInfos.Add(new CandidateInfo() {Name = name, Account = account, PublicKey = pubKey});
            }

            //Transfer candidate 200_000
            var balance = Behaviors.GetBalance(FullNodeAddress[0]);
            if (balance.Balance == 0)
            {
                foreach (var account in FullNodeAddress)
                {
                    Behaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 200_000L,
                        To = AddressHelper.Base58StringToAddress(account),
                        Memo = "Transfer token for announcement."
                    });
                }

                Behaviors.TokenService.CheckTransactionResultList();
            }

           
            PrepareUserAccountAndBalance(30);

            #endregion
        }

        protected void TestCleanUp()
        {
            /*
            if (UserList.Count == 0) return;
            _logger.Info("Delete all account files created.");
            foreach (var item in UserList)
            {
                var file = Path.Combine(CommonHelper.GetCurrentDataDir(), $"{item}.json");
                File.Delete(file);
            }
            */
        }

        protected void PrepareUserAccountAndBalance(int userAccount)
        {
            
            //Account preparation
            UserList = NewAccount(Behaviors,userAccount);
            UnlockAccounts(Behaviors,userAccount,UserList);

            //分配资金给普通用户
            var balance = Behaviors.GetBalance(UserList[0]);
            if (balance.Balance >= 20_0000_00000000)
                return;

            Behaviors.TokenService.SetAccount(BpNodeAddress[0]);
            foreach (var acc in UserList)
            {
                Behaviors.TokenService.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = 20_0000_00000000,
                    Memo = "transfer for balance test",
                    Symbol = "ELF",
                    To = AddressHelper.Base58StringToAddress(acc)
                });
            }

            Behaviors.TokenService.CheckTransactionResultList();

            foreach (var userAcc in UserList)
            {
                var callResult = Behaviors.TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                    new GetBalanceInput
                    {
                        Symbol = "ELF",
                        Owner = AddressHelper.Base58StringToAddress(userAcc)
                    });
                Console.WriteLine($"User-{userAcc} balance: " + callResult.Balance);
            }

            _logger.Info("All accounts prepared and unlocked.");
        }
        
        protected List<string> NewAccount(Behaviors services, int count)
        {
            var accountList = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var account = services.NodeManager.NewAccount();
                accountList.Add(account);
            }

            return accountList;
        }
        
        protected void UnlockAccounts(Behaviors services, int count, List<string> accountList)
        {
            services.NodeManager.ListAccounts();
            for (var i = 0; i < count; i++)
            {
                var result = services.NodeManager.UnlockAccount(accountList[i]);
                Assert.IsTrue(result);
            }
        }
    }
}