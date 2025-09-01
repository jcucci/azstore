namespace AzStore.Core.Models.Authentication;

/// <summary>
/// Contains information about an Azure Storage account.
/// </summary>
/// <param name="AccountName">The name of the storage account.</param>
/// <param name="AccountKind">The kind of storage account (StorageV2, BlobStorage, etc.).</param>
/// <param name="SubscriptionId">The Azure subscription ID containing this storage account.</param>
/// <param name="ResourceGroupName">The name of the resource group containing this storage account.</param>
/// <param name="PrimaryEndpoint">The primary blob service endpoint URI.</param>
public record StorageAccountInfo(
    string AccountName,
    string? AccountKind,
    Guid? SubscriptionId,
    string? ResourceGroupName,
    Uri PrimaryEndpoint);