using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SHA3.Net;
using SmartContract.Essentials.Ciphering;
using SmartContract.Essentials.Hashing;
using Stratis.Bitcoin.Features.SmartContracts.Models;
using Stratis.Bitcoin.Features.SmartContracts.ReflectionExecutor.Consensus.Rules;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR;
using Swashbuckle.AspNetCore.Examples;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ticketbooth.Api.Requests;
using Ticketbooth.Api.Requests.Examples;
using Ticketbooth.Api.Responses;
using Ticketbooth.Api.Responses.Examples;
using Ticketbooth.Api.Tools;
using Ticketbooth.Api.Validation;
using static TicketContract;

namespace Ticketbooth.Api.Controllers
{
    /// <summary>
    /// Public API controller for ticketbooth v1.1 modifications
    /// </summary>
    public partial class PublicController
    {
        /// <summary>
        /// Requests to purchase a ticket from the ticket contract. Tickets can only be purchased while a sale is active.
        /// </summary>
        /// <param name="address">The ticket contract address</param>
        /// <param name="reserveTicketRequest">The reserve ticket request</param>
        /// <returns>HTTP response</returns>
        /// <response code="201">Returns ticket reservation details</response>
        /// <response code="400">Invalid request</response>
        /// <response code="403">Node has no connections</response>
        /// <response code="404">Contract does not exist</response>
        /// <response code="409">Sale is not currently open</response>
        /// <response code="500">Unexpected error occured</response>
        [ApiVersion("1.1")]
        [HttpPost("{address}/ReserveTicket")]
        [SwaggerRequestExample(typeof(ReserveTicketRequest), typeof(ReserveTicketRequestExample))]
        [SwaggerResponseExample(StatusCodes.Status201Created, typeof(HashedSecretTicketReservationResponseExample))]
        [ProducesResponseType(typeof(HashedSecretTicketReservationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReserveTicketHashSecret(string address, ReserveTicketRequest reserveTicketRequest)
        {
            // validate address
            if (!AddressParser.IsValidAddress(address, _network))
            {
                return StatusCode(StatusCodes.Status400BadRequest, $"Invalid address {address}");
            }

            // check contract exists
            var numericAddress = address.ToUint160(_network);
            if (!_stateRepositoryRoot.IsExist(numericAddress))
            {
                return StatusCode(StatusCodes.Status404NotFound, $"No smart contract found at address {address}");
            }

            // check for state of ticket
            var ticket = FindTicket(numericAddress, reserveTicketRequest.Seat);
            if (ticket.Equals(default(Ticket)))
            {
                return StatusCode(StatusCodes.Status400BadRequest, $"Invalid seat {reserveTicketRequest.Seat.ToDisplayString()}");
            }

            // check contract state
            if (!HasOpenSale(numericAddress))
            {
                return StatusCode(StatusCodes.Status409Conflict, "Sale is not currently open");
            }

            if (ticket.Address != Address.Zero)
            {
                return StatusCode(StatusCodes.Status400BadRequest, $"Ticket for seat {reserveTicketRequest.Seat.ToDisplayString()} not available to purchase");
            }

            var requiresIdentityVerification = RetrieveIdentityVerificationPolicy(numericAddress);
            if (requiresIdentityVerification && string.IsNullOrWhiteSpace(reserveTicketRequest.CustomerName))
            {
                return StatusCode(StatusCodes.Status400BadRequest, $"Customer name is required");
            }

            // check connections
            if (!_connectionManager.ConnectedPeers.Any())
            {
                _logger.LogTrace("No connected peers");
                return StatusCode(StatusCodes.Status403Forbidden, "Can't send transaction as the node requires at least one connection.");
            }

            var seatBytes = _serializer.Serialize(reserveTicketRequest.Seat);
            var seatParameter = $"{Serialization.TypeIdentifiers[typeof(byte[])]}#{Serialization.ByteArrayToHex(seatBytes)}";

            var secret = _stringGenerator.CreateUniqueString(15);
            byte[] hashedSecret;
            using (var hasher = Sha3.Sha3224())
            {
                hashedSecret = hasher.ComputeHash(secret);
            }

            var secretParameter = $"{Serialization.TypeIdentifiers[typeof(byte[])]}#{Serialization.ByteArrayToHex(hashedSecret)}";

            CbcResult customerNameCipherResult = null;
            string customerNameParameter = null;
            if (requiresIdentityVerification)
            {
                using (var cipherProvider = _cipherFactory.CreateCbcProvider())
                {
                    customerNameCipherResult = cipherProvider.Encrypt(reserveTicketRequest.CustomerName);
                }

                customerNameParameter = $"{Serialization.TypeIdentifiers[typeof(byte[])]}#{Serialization.ByteArrayToHex(customerNameCipherResult.Cipher)}";
            }

            // build transaction
            var parameterList = new List<string> { seatParameter, secretParameter };
            if (customerNameParameter != null)
            {
                parameterList.Add(customerNameParameter);
            }

            var callTxResponse = _smartContractTransactionService.BuildCallTx(new BuildCallContractTransactionRequest
            {
                AccountName = reserveTicketRequest.AccountName,
                Amount = StratoshisToStrats(ticket.Price),
                ContractAddress = address,
                FeeAmount = "0",
                GasLimit = SmartContractFormatLogic.GasLimitMaximum,
                GasPrice = reserveTicketRequest.GasPrice,
                MethodName = nameof(TicketContract.Reserve),
                Outpoints = reserveTicketRequest.Outpoints,
                Parameters = parameterList.ToArray(),
                Password = reserveTicketRequest.Password,
                Sender = reserveTicketRequest.Sender,
                WalletName = reserveTicketRequest.WalletName,
            });

            if (!callTxResponse.Success)
            {
                return StatusCode(StatusCodes.Status400BadRequest, callTxResponse.Message);
            }

            // broadcast transaction
            var transaction = _network.CreateTransaction(callTxResponse.Hex);
            await _broadcasterManager.BroadcastTransactionAsync(transaction);
            var transactionBroadCastEntry = _broadcasterManager.GetTransaction(transaction.GetHash()); // check if transaction was added to mempool

            if (transactionBroadCastEntry?.State == Stratis.Bitcoin.Features.Wallet.Broadcasting.State.CantBroadcast)
            {
                _logger.LogError("Exception occurred: {0}", transactionBroadCastEntry.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, transactionBroadCastEntry.ErrorMessage);
            }

            var transactionHash = transaction.GetHash().ToString();
            var cbcCustomerValues = requiresIdentityVerification
                ? new CbcValues
                {
                    Key = customerNameCipherResult.Key,
                    IV = customerNameCipherResult.IV
                }
                : null;

            return Created(
                $"/api/smartContracts/receipt?txHash={transactionHash}",
                new HashedSecretTicketReservationResponse
                {
                    TransactionHash = transactionHash,
                    Secret = secret,
                    CustomerName = cbcCustomerValues
                });
        }
    }
}
