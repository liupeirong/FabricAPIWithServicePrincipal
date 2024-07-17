# Overview

This is a minimum dotnet console app that uses a Service Principal to make API calls to Microsoft Fabric.
It uses OAuth2 confidential client flow to obtain the access token.
It can be used to check if Service Principal is supported for authenticating against a specific Fabric API.
Use it only for testing from your dev machine.

There are management plane API calls such as creating a KQL database in Fabric,
 and data plane API calls such as creating a table in an existing KQL database or
 query a table.

## Management plane API

> To make these APIs, grant the Service Principal `Contributor` role to your Fabric
 workspace.

* A get call to fetch the items in a specified Fabric Workspace. - This call should succeed.
* A Post call to create a KQL database. - This should fail with a message
 `The operation is not supported for the principal type`, indicating Fabric doesn't yet support using
 Service Principal to create a KQL database.

## Data plane API

> To make these API calls, the Service Principal must be granted at least the user role
 of the existing database by running a KQL command such as
`.add database <db_name> users ('aadapp=<sp_app_id>;<tenant_id>')`.

* A call to create a table in an existing KQL database. - This call should succeed.
* A call to query an existing table. - This call should succeed.

## Summary

You can see that at the time of this writing, Service Principal is not yet supported by
 all management plane APIs in Fabric.
It does work with KQL database at the data plane level, same as how it works with
 Azure Data Explorer.
