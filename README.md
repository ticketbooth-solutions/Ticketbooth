# Ticketbooth

## Overview

Smart contract event ticketing, built on Stratis platform. Enables the selling of tickets with flexible pricing per event. Includes a refund policy, where ticket holders can release tickets back to the contract, for a fixed fee. Optional enforcement of requiring identity verification of ticket holders, to prevent ticket scalping.

Ticketbooth can be integrated with payment gateways, ticket sales platforms and social platforms.

## Projects

### Ticketbooth.Api

An extension to the Stratis full node that provides an API for interacting with Ticketbooth.

### Ticketbooth.Contract

[![Build Status](https://dev.azure.com/developmomentum/Ticketbooth/_apis/build/status/Contract?branchName=master)](https://dev.azure.com/developmomentum/Ticketbooth/_build/latest?definitionId=8&branchName=master) 
[![Nuget](https://img.shields.io/nuget/v/Ticketbooth)](https://www.nuget.org/packages/Ticketbooth/)

This project contains the smart contract(s) that Ticketbooth is based on.

### Ticketbooth.FullNode

A Stratis full node on the Cirrus network, with the Ticketbooth API extension enabled.

### Ticketbooth.Scanner

[![Build Status](https://dev.azure.com/developmomentum/Ticketbooth/_apis/build/status/Scanner%20App?branchName=master)](https://dev.azure.com/developmomentum/Ticketbooth/_build/latest?definitionId=9&branchName=master)

A web application allowing venues to scan tickets, built with Blazor.

## Documentation

 [![Build Status](https://dev.azure.com/developmomentum/Ticketbooth/_apis/build/status/Docs?branchName=master)](https://dev.azure.com/developmomentum/Ticketbooth/_build/latest?definitionId=12&branchName=master) ![Netlify](https://img.shields.io/netlify/c03dc389-d69c-4203-bd5f-540f145e2896)
 
You can view the live documentation at [https://developer.ticketbooth.solutions](https://developer.ticketbooth.solutions/).  Documentation is generated from the docs folder with [Wyam](https://wyam.io/).

## Resources

* [Open issues](https://github.com/drmathias/Ticketbooth/issues)
* [Contributing guide](https://github.com/drmathias/Ticketbooth/blob/master/CONTRIBUTING.md)
* [Code of conduct](https://github.com/drmathias/Ticketbooth/blob/master/CODE_OF_CONDUCT.md)
