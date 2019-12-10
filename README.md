# SmartTicket

[![Build Status](https://dev.azure.com/developmomentum/SmartTicket/_apis/build/status/drmathias.SmartTicket?branchName=master)](https://dev.azure.com/developmomentum/SmartTicket/_build/latest?definitionId=8&branchName=master)

## Overview

A smart contract for event ticketing, built on Stratis Platform. Enables the selling of tickets with flexible pricing per event. Includes a refund policy, where ticket holders can release tickets back to the contract, for a fixed fee.

This could be integrated with a fiat to crypto payment gateway and managed by a ticket sales platform, or the venue itself.

## The Problem

Event venues use ticket distribution platforms such as TicketMaster and StubHub to sell their tickets. These platforms are well established and work with promoters, which bid against one another to get exclusive deals. Platforms like TicketMaster end up charging additional fees to the customer, commonly between 10-15% of the original ticket price. Some of this comes from 'service fees' which are used to pay the venue, for allowing them to sell tickets on their behalf, as they profit from other fees they charge the customer. Resale platforms charge fees to brokers, in some cases up to 50% of the ticket value, further increasing ticket prices to the end customer. 

Artists and event organisers generally do not get a choice in choosing the ticket seller as this is done by venues, many of the larger ones working exclusively with TicketMaster. Different artists have different contracts for selling tickets to their shows. Some artists and event organisers receive fixed payment from promoters, while some have more control over ticket prices to their events. Artists may choose to keep their base ticket prices low, while they are sold through platforms which allow secondary sellers to buy up tickets and sell them at a massively inflated price. This generally will involve artists getting paid extra by promoters or resellers.

All of this means that ticket pricing is not very transparent or fair. Ticketing platforms and marketplaces primary goal, of profiting from the customer, leads them to actively prevent competition in the market.
