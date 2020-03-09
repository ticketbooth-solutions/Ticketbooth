namespace Ticketbooth.Api.Responses
{
    /// <summary>
    /// Ticket reservation details which are returned after a ticket is purchased
    /// </summary>
    public class HashedSecretTicketReservationResponse
    {
        /// <summary>
        /// The smart contract transaction hash
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// The plaintext ticket secret
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// The encryption values for the customer identifier, if applicable
        /// </summary>
        public CbcValues CustomerName { get; set; }
    }
}
