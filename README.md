# Computer Rename with a Azure Function App

A lot of organizations have different naming conventions for things and it seems like it varies so much that none of them are the same. Now, the question is, why? It may be because of some very minute detail in a larger process. With things like Autopilot, by default, only has a serial number or a random number.

This Azure Function App solution takes in a CSV in Azure Blob Storage do a lookup on a value and return the desired hostname listed in the CSV. In this example, its the serial number as it is unique and simple. 

## Prerequisites

1. Azure Function App with a System Managed Identity
2. Azure Storage Account
