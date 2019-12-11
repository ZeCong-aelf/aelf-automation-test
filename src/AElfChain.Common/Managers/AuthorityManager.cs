using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Acs0;
using Acs3;
using AElf;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Consensus.AEDPoS;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.Utils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace AElfChain.Common.Managers
{
    public class AuthorityManager
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly ConsensusContract _consensus;
        private readonly GenesisContract _genesis;
        private readonly ParliamentAuthContract _parliament;
        private readonly TokenContract _token;
        private NodesInfo _info;

        public INodeManager NodeManager { get; set; }

        public AuthorityManager(INodeManager nodeManager, string caller = "")
        {
            GetConfigNodeInfo();
            NodeManager = nodeManager;
            _genesis = GenesisContract.GetGenesisContract(nodeManager, caller);
            _consensus = _genesis.GetConsensusContract();
            _token = _genesis.GetTokenContract();
            _parliament = _genesis.GetParliamentAuthContract();
            
            CheckBpBalance();
        }

        public Address DeployContractWithAuthority(string caller, string contractName)
        {
            Logger.Info($"Deploy contract: {contractName}");
            var fileName = contractName.Contains(".dll") ? contractName : $"{contractName}.dll";
            var contractPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var code = File.ReadAllBytes(Path.Combine(contractPath, fileName));

            return DeployContractWithAuthority(caller, code);
        }

        public void UpdateContractWithAuthority(string caller, string address, string contractName)
        {
            Logger.Info($"Update contract: {contractName}");
            var fileName = contractName.Contains(".dll") ? contractName : $"{contractName}.dll";
            var contractPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aelf", "contracts");
            var code = File.ReadAllBytes(Path.Combine(contractPath, fileName));

            UpdateContractWithAuthority(caller, address, code);
        }

        private Address DeployContractWithAuthority(string caller, byte[] code)
        {
            var input = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(code),
                Category = KernelConstants.CodeCoverageRunnerCategory
            };
            var approveUsers = GetMinApproveMiners();

            var proposalNewContact = _genesis.ProposeNewContract(input,caller);
            var proposalId = ProposalCreated.Parser.ParseFrom(proposalNewContact.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(proposalNewContact.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            
            var releaseInput = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
            
            var transactionResult = ApproveAndRelease(releaseInput, approveUsers, caller);
            var deployProposalId = ProposalCreated.Parser.ParseFrom(transactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            Logger.Info($"Deploy contract proposal info: \n proposal id: {deployProposalId}\n proposal input hash: {proposalHash}");

            if (!CheckProposalStatue(deployProposalId))
            {
                Logger.Info("Contract code didn't pass the code check");
                return null;
            } 
            
            var checkCodeRelease = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = deployProposalId
            };

            var release = _genesis.ReleaseCodeCheckedContract(checkCodeRelease,caller);
            release.Status.ShouldBe("MINED");
            var byteString3 = ByteString.FromBase64(release.Logs.First(l =>l.Name.Contains(nameof(ContractDeployed))).NonIndexed);
            var deployAddress = ContractDeployed.Parser.ParseFrom(byteString3).Address;
            Logger.Info($"Contract deploy passed authority, contract address: {deployAddress}");
            
            return deployAddress ;
        }

        private Address UpdateContractWithAuthority(string caller, string address, byte[] code)
        {
            var input = new ContractUpdateInput
            {
                Address = address.ConvertAddress(),
                Code = ByteString.CopyFrom(code)
            };
            var approveUsers = GetMinApproveMiners();

            var proposalUpdateContact = _genesis.ProposeUpdateContract(input);
            var proposalId = ProposalCreated.Parser.ParseFrom(proposalUpdateContact.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            var proposalHash = ContractProposed.Parser
                .ParseFrom(proposalUpdateContact.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            
            var releaseInput = new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposalHash
            };
            
            var transactionResult = ApproveAndRelease(releaseInput, approveUsers, caller);
            var deployProposalId = ProposalCreated.Parser.ParseFrom(transactionResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            Logger.Info($"Update contract proposal info: \n proposal id: {deployProposalId}\n proposal input hash: {proposalHash}");

            if (!CheckProposalStatue(deployProposalId))
            {
                Logger.Info("Contract code didn't pass the code check");
                return null;
            } 
            
            var checkCodeRelease = new ReleaseContractInput()
            {
                ProposedContractInputHash = proposalHash,
                ProposalId = deployProposalId
            };

            var release = _genesis.ReleaseCodeCheckedContract(checkCodeRelease,caller);
            release.Status.ShouldBe("MINED");
            var byteString = ByteString.FromBase64(release.Logs.First(l =>l.Name.Contains(nameof(CodeUpdated))).NonIndexed);
            var updateAddress = CodeUpdated.Parser.ParseFrom(byteString).Address;
            Logger.Info($"Contract update passed authority, contract address: {updateAddress}");
            return updateAddress;
        }

        public List<string> GetCurrentMiners()
        {
            var currentMiners = _info.GetMinerNodes(_consensus).Select(o => o.Account).ToList();
            return currentMiners;
        }

        public List<string> GetMinApproveMiners()
        {
            var minersCount = _consensus.CallViewMethod<PubkeyList>(ConsensusMethod.GetCurrentMinerPubkeyList, new Empty())
                .Pubkeys.Count;
            var minNumber = minersCount * 2 / 3 + 1;
            var currentMiners = GetCurrentMiners();
            return currentMiners.Take(minNumber).ToList();
        }

        public Address GetGenesisOwnerAddress()
        {
            return _parliament.GetGenesisOwnerAddress();
        }

        public TransactionResult ExecuteTransactionWithAuthority(string contractAddress, string method, IMessage input,
            Address organizationAddress, IEnumerable<string> approveUsers, string callUser)
        {
            //create proposal
            var proposalId = _parliament.CreateProposal(contractAddress,
                method, input,
                organizationAddress, callUser);

            //approve
            foreach (var account in approveUsers) _parliament.ApproveProposal(proposalId, account);

            //release
            return _parliament.ReleaseProposal(proposalId, callUser);
        }

        private TransactionResult ApproveAndRelease(ReleaseContractInput input,IEnumerable<string> approveUsers, string callUser)
        {
            //approve
            foreach (var account in approveUsers) _parliament.ApproveProposal(input.ProposalId, account);

            //release
            return _genesis.ReleaseApprovedContract(input, callUser);
        }

        private void GetConfigNodeInfo()
        {
            var nodes = NodeInfoHelper.Config;
            nodes.CheckNodesAccount();

            _info = nodes;
        }

        private bool CheckProposalStatue(Hash proposalId)
        {
            var proposal = _parliament.CheckProposal(proposalId);
            var expired = false;
            while (!proposal.ToBeReleased && !expired)
            {
                Thread.Sleep(1000);
                var dateTime = TimestampHelper.GetUtcNow();
                proposal = _parliament.CheckProposal(proposalId);
                if (dateTime >= proposal.ExpiredTime) expired = true;
            }

            return proposal.ToBeReleased;
        }

        private void CheckBpBalance()
        {
            Logger.Info("Check bp balance and transfer for authority.");
            var bps = GetCurrentMiners();
            var primaryToken = NodeManager.GetPrimaryTokenSymbol();
            foreach (var bp in bps.Skip(1))
            {
                var balance = _token.GetUserBalance(bp,primaryToken);
                if (balance < 1000_00000000)
                    _token.TransferBalance(bps[0], bp, 10000_00000000 - balance, primaryToken);
            }
        }
    }
}