using System;
using AElfChain.Common.Managers;
using AElf.Contracts.TestContract.Performance;
using AElf.Types;
using AElfChain.Common.Utils;
using AElfChain.SDK.Models;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum PerformanceMethod
    {
        //Action
        InitialPerformanceContract,
        Write1KContentByte,
        Write2KContentByte,
        Write5KContentByte,
        Write10KContentByte,
        ComputeLevel1,
        ComputeLevel2,
        ComputeLevel3,
        ComputeLevel4,

        //View
        QueryReadInfo,
        QueryFibonacci
    }

    public class PerformanceContract : BaseContract<PerformanceMethod>
    {
        public PerformanceContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public PerformanceContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.Performance";

        public void InitializePerformance()
        {
            var initializeResult = ExecuteMethodWithResult(PerformanceMethod.InitialPerformanceContract,
                new InitialPerformanceInput
                {
                    ContractName = $"Performance_{Guid.NewGuid().ToString()}",
                    Manager = CallAddress.ConvertAddress()
                });
            initializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}