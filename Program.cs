using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text;

var authConfig = new
{
  TenantId = "Your Entra ID Tenant ID",
  ClientId = "Your Service Principal Client ID",
  ClientSecret = "Your Service Principal Client Secret",
  Authority = "https://login.microsoftonline.com/",
  scopes = new string[] { "https://api.fabric.microsoft.com/.default" },
};

var fabricConfig = new
{
  workspaceId = "Your Fabric Workspace ID",
  eventhouseId = "Your Fabric Eventhouse item ID for creating a new KQL database in",
  baseUrl = "https://api.fabric.microsoft.com/v1/",
};

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
client.BaseAddress = new Uri(fabricConfig.baseUrl);

// Get items
String getUrl = $"workspaces/{fabricConfig.workspaceId}/items";
HttpResponseMessage response = await client.GetAsync(getUrl);
string responseBody = await response.Content.ReadAsStringAsync();
Console.WriteLine(responseBody);

// Create a KQL database
var postUrl = $"workspaces/{fabricConfig.workspaceId}/kqlDatabases";
var payload = new
{
  displayName = "kqldb1",
  creationPayload = new
  {
    databaseType = "ReadWrite",
    parentEventhouseItemId = fabricConfig.eventhouseId,
  }
};
string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
StringContent body = new StringContent(payloadJson, Encoding.UTF8, "application/json");
var postResult = await client.PostAsync(postUrl, body);
var postResultContent = await postResult.Content.ReadAsStringAsync();
Console.WriteLine(postResult.IsSuccessStatusCode == true ?
  "Item created successfully!" :
  "Failed to create item: {0}, {1}", postResult.ReasonPhrase, postResultContent);
