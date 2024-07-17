using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

# region config
var authConfig = new
{
  TenantId = "Your Entra ID Tenant ID",
  ClientId = "Your Service Principal Client ID",
  ClientSecret = "Your Service Principal Client Secret",
  Authority = "https://login.microsoftonline.com/",
  scopes = new string[] { "https://api.fabric.microsoft.com/.default" },
};

var managementPlaneConfig = new
{
  workspaceId = "Your Fabric Workspace ID",
  eventhouseId = "Your Fabric Eventhouse item ID for creating a new KQL database in",
  baseUrl = "https://api.fabric.microsoft.com/v1/",
  databaseToBeCreated = "Name of the database you want to create",
};

var dataPlaneConfig = new
{
  databaseName = "Name of the existing Fabric KQL database",
  baseUrl = "Your Fabric KQL DB query URL",
  tableToBeCreated = "Name of the table you want to create",
  tableToQuery = "Name of an existing table you want to query",
};
# endregion

# region management_plane_operations
IConfidentialClientApplication msalClient = ConfidentialClientApplicationBuilder
  .Create(authConfig.ClientId)
  .WithClientSecret(authConfig.ClientSecret)
  .WithAuthority(authConfig.Authority+authConfig.TenantId)
  .Build();
AuthenticationResult msalAuthResult = await msalClient
  .AcquireTokenForClient(authConfig.scopes).ExecuteAsync();

// Call Fabric API with the access token
HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", msalAuthResult.AccessToken);
client.BaseAddress = new Uri(managementPlaneConfig.baseUrl);

// Get items
String getUrl = $"workspaces/{managementPlaneConfig.workspaceId}/items";
HttpResponseMessage response = await client.GetAsync(getUrl);
string responseBody = await response.Content.ReadAsStringAsync();
Console.WriteLine(responseBody);

// Create a KQL database
var postUrl = $"workspaces/{managementPlaneConfig.workspaceId}/kqlDatabases";
var payload = new
{
  displayName = managementPlaneConfig.databaseToBeCreated,
  creationPayload = new
  {
    databaseType = "ReadWrite",
    parentEventhouseItemId = managementPlaneConfig.eventhouseId,
  }
};
string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
StringContent body = new StringContent(payloadJson, Encoding.UTF8, "application/json");
var postResult = await client.PostAsync(postUrl, body);
var postResultContent = await postResult.Content.ReadAsStringAsync();
Console.WriteLine(postResult.IsSuccessStatusCode == true ?
  "Item created successfully!" :
  "Failed to create item: {0}, {1}", postResult.ReasonPhrase, postResultContent);
#endregion

# region data_plane_operations
// create tables inside an existing KQL database
// ensure the service principal is at least a user of the database, for example,
// .add database <db_name> users ('aadapp=<sp_app_id>;<tenant_id>')
IConfidentialClientApplication msalClientKusto = ConfidentialClientApplicationBuilder
  .Create(authConfig.ClientId)
  .WithClientSecret(authConfig.ClientSecret)
  .WithAuthority(authConfig.Authority+authConfig.TenantId)
  .Build();
AuthenticationResult msalAuthResultKusto = await msalClientKusto
  .AcquireTokenForClient(new[] {$"{dataPlaneConfig.baseUrl}/.default"}).ExecuteAsync();

HttpClient clientKusto = new HttpClient();
clientKusto.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", msalAuthResultKusto.AccessToken);
clientKusto.BaseAddress = new Uri(dataPlaneConfig.baseUrl);
var payloadDDL = new
{
  db = dataPlaneConfig.databaseName,
  csl = $".create table {dataPlaneConfig.tableToBeCreated} ( Id:int64, Name:string, Value:float, Timestamp: datetime)",
};
string payloadJsonDDL = System.Text.Json.JsonSerializer.Serialize(payloadDDL);
StringContent bodyDDL = new StringContent(payloadJsonDDL, Encoding.UTF8, "application/json");
var postDDLResult = await clientKusto.PostAsync("/v1/rest/mgmt", bodyDDL);
var postDDLResultContent = await postDDLResult.Content.ReadAsStringAsync();
Console.WriteLine(postDDLResult.IsSuccessStatusCode == true ?
  "table created successfully!" :
  "Failed to create table: {0}, {1}", postDDLResult.ReasonPhrase, postDDLResultContent);

// run a kql query
var payloadQuery = new
{
  db = dataPlaneConfig.databaseName,
  csl = $"{dataPlaneConfig.tableToQuery} | take 1",
};
string payloadJsonQuery = System.Text.Json.JsonSerializer.Serialize(payloadQuery);
StringContent bodyQuery = new StringContent(payloadJsonQuery, Encoding.UTF8, "application/json");
var postQueryResult = await clientKusto.PostAsync("/v1/rest/query", bodyQuery);
var postQueryResultContent = await postQueryResult.Content.ReadAsStringAsync();
if (postQueryResult.IsSuccessStatusCode == true) {
  var jobj = JObject.Parse(postQueryResultContent);
  var table = jobj["Tables"][0];
  Console.WriteLine($"Table: {table["TableName"]}");
  table["Rows"].ToList().ForEach(row =>
  {
    Console.WriteLine($"Row: {row}");
  });
} else {
  Console.WriteLine("Failed to run query: {0}, {1}",
    postQueryResult.ReasonPhrase, postQueryResultContent);
}
#endregion