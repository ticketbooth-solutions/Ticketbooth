# Ticketbooth

[![Build Status](https://dev.azure.com/developmomentum/Ticketbooth/_apis/build/status/drmathias.Ticketbooth?branchName=master)](https://dev.azure.com/developmomentum/Ticketbooth/_build/latest?definitionId=8&branchName=master)

## Overview

Smart contract event ticketing, built on Stratis Platform. Enables the selling of tickets with flexible pricing per event. Includes a refund policy, where ticket holders can release tickets back to the contract, for a fixed fee. Optional enforcement of requiring identity verification of ticket holders, to prevent ticket scalping.

Ticketbooth can be integrated with payment gateways, ticket sales platforms and social platforms.

## Projects

### Ticketbooth.Contract

This project contains the smart contract(s) that Ticketbooth is based on.

### Ticketbooth.Scanner

The ticket scanner is a FOSS web application, that is built with Blazor and is designed to interact with the ticketing contract. It allows a venue to scan customer tickets, according to their policies. The goal is to have it eventually be an entirely client-side WASM application, which could be achieved after Blazor WASM is officially released.

## Documentation

Contract documentation is available on the [wiki](https://github.com/drmathias/Ticketbooth/wiki).
