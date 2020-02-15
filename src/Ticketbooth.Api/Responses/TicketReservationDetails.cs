using SmartContract.Essentials.Ciphering;

namespace Ticketbooth.Api.Responses
{
    /// <summary>
    /// Ticket reservation details which are returned after a ticket is purchased
    /// </summary>
    public class TicketReservationDetails
    {
        /// <summary>
        /// The smart contract transaction hash
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// The encryption result for the secret
        /// </summary>
        public CbcResult Secret { get; set; }

        /// <summary>
        /// The encryption result for the customer identifier, if applicable
        /// </summary>
        public CbcResult CustomerName { get; set; }
    }
}
