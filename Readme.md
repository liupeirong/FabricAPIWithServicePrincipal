# Overview

This is a minimum dotnet console app that uses a Service Principal to make API calls to Microsoft Fabric.
It uses OAuth2 confidential client flow to obtain the access token.
It can be used to check if Service Principal is supported for authenticating against a specific Fabric API.

This sample makes two calls:

* A get call to fetch the items in a specified Fabric Workspace. - This call should succeed.
* A Post call to create a KQL database. - This should fail with a message
 `The operation is not supported for the principal type`, indicating Fabric doesn't yet support using
 Service Principal to create a KQL database.
