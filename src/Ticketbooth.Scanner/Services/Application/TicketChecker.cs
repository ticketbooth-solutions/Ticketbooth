using Microsoft.Extensions.Logging;
using SmartContract.Essentials.Ciphering;
using System;
using System.Security.Cryptography;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Data;
using static TicketContract;

namespace Ticketbooth.Scanner.Services.Application
{
    public class TicketChecker : ITicketChecker
    {
        private readonly ICipherFactory _cipherFactory;
        private readonly ILogger<TicketChecker> _logger;

        public TicketChecker(ICipherFactory cipherFactory, ILogger<TicketChecker> logger)
        {
            _cipherFactory = cipherFactory;
            _logger = logger;
        }

        public TicketScanResult CheckTicket(DigitalTicket scannedTicket, Ticket actualTicket)
        {
            if (scannedTicket is null)
            {
                throw new ArgumentNullException(nameof(scannedTicket), "Cannot check null ticket");
            }

            if (!scannedTicket.Seat.Equals(actualTicket.Seat))
            {
                throw new ArgumentException(nameof(actualTicket), "Seats do not match");
            }

            string plainTextSecret;
            try
            {
                using var cbc = _cipherFactory.CreateCbcProvider();
                plainTextSecret = cbc.Decrypt(actualTicket.Secret, scannedTicket.SecretKey, scannedTicket.SecretIV);
            }
            catch (CryptographicException e)
            {
                _logger.LogDebug(e.Message);
                return new TicketScanResult(false, string.Empty);
            }
            catch (ArgumentException e)
            {
                _logger.LogWarning(e.Message);
                return null;
            }

            if (plainTextSecret is null || !plainTextSecret.Equals(scannedTicket.Secret))
            {
                return new TicketScanResult(false, string.Empty);
            }

            if (actualTicket.CustomerIdentifier is null)
            {
                return new TicketScanResult(true, string.Empty);
            }

            string plainTextCustomerIdentifier;
            try
            {
                using var aes = _cipherFactory.CreateCbcProvider();
                plainTextCustomerIdentifier = aes.Decrypt(actualTicket.CustomerIdentifier, scannedTicket.NameKey, scannedTicket.NameIV);
            }
            catch (CryptographicException e)
            {
                _logger.LogDebug(e.Message);
                return new TicketScanResult(true, string.Empty);
            }
            catch (ArgumentException e)
            {
                _logger.LogWarning(e.Message);
                return null;
            }

            return new TicketScanResult(true, plainTextCustomerIdentifier);
        }
    }
}
