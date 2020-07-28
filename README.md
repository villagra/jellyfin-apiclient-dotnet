Jellyfin.ApiClient
======================

 .Net client library for Jellyfin's API.

 # Getting Started
 ## Installing
 Using the [.NET Core command-line interface (CLI) tools][dotnet-core-cli-tools]:

```sh
dotnet add package Villagralabs.Jellyfin.ApiClient
```

Using the [NuGet Command Line Interface (CLI)][nuget-cli]:

```sh
nuget install Villagralabs.Jellyfin.ApiClient
```

Using the [Package Manager Console][package-manager-console]:

```powershell
Install-Package Villagralabs.Jellyfin.ApiClient
```

From within Visual Studio:

1. Open the Solution Explorer.
2. Right-click on a project within your solution.
3. Click on *Manage NuGet Packages...*
4. Click on the *Browse* tab and search for "Villagralabs.Jellyfin.ApiClient".
5. Click on the Villagralabs.Jellyfin.ApiClient package, select the appropriate version in the
   right-tab and click *Install*.

 ## Usage
 ### Authenticating
 ```
var client = new JellyfinClient(uri, new UserAuthentication(clientName, deviceName, deviceId, applicationVersion));
var authResponse = await Client.AuthenticateUserAsync(username, password);
 ```

 ### Querying the catalog
 
```
JellyfinModel.ItemFilters filters = JellyfinModel.ItemFilters.Create()
                                    .Recursive()
                                    .IncludeItemType(JellyfinModel.ItemTypes.Movie)
                                    .Order(JellyfinModel.SortOrder.Ascending)
                                    .Take(25);

 var itemsResponse = await Client.GetItemsAsync(authResponse.User.Id.ToString(), filters);
 ```