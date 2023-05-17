using Pulumi;
using System.Text;
using System.Text.Json;

class AppLandingZone : Stack {
    public AppLandingZone()
    {
            // Grab some values from the Pulumi stack configuration (or use defaults)
            var projCfg = new Pulumi.Config();
            var configAzureNative = new Pulumi.Config("azure-native");
            var location = configAzureNative.Require("location");
            var commonArgs = new LandingZoneArgs(Pulumi.Deployment.Instance.StackName, location, "shd");

            // The next two configuration values are required (no default can be provided)
            var subnetArr = projCfg.RequireObject<JsonElement>("subnets");
            var vnetCidr = projCfg.Require("virtual-network-cidr");
            var mgmtGroupId = projCfg.Require("mgmtGroupId");


            // Generate Names
            var resourceGroupName = $"rg-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var vnetName = $"vnet-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var clusterName = $"aks-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var lawName = $"law-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var managedGrafanaName = $"grf-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var agwName = $"agw-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";
            var pipName = $"pip-{commonArgs.Application}-{commonArgs.LocationShort}-{commonArgs.EnvironmentShort}";

            // Instantiate LandingZone Class for Resource Group and Virtual Network
            var landingZone = new LandingZone(resourceGroupName, vnetCidr, vnetName, subnetArr);

            // Instantiate Monitoring Class for LAW Deployment
            var monitoring = new Monitoring(lawName, managedGrafanaName, mgmtGroupId, landingZone.ResourceGroupName);
            LogAnalyticsWorkspaceId = monitoring.LogAnalyticsWorkspaceId;

            // Looking for GatewaySubnet
            string gatewaySubnet = string.Empty;
            foreach (var subnet in subnetArr.EnumerateArray())
            {
                if (subnet.GetProperty("name").GetString().Contains("agw"))
                {
                    Pulumi.Log.Info($"Subnet {subnet.GetProperty("name").GetString()} will be used for AGW");
                    gatewaySubnet = subnet.GetProperty("name").GetString();
                    break;
                }
            }
            GatewaySubnetId = landingZone.SubnetDictionary.Apply(subnetId => subnetId[gatewaySubnet]);
        }

        [Output] public Output<string> LogAnalyticsWorkspaceId { get; set; }
        [Output] public Output<string> GatewaySubnetId { get; set; }
    }