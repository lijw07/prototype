namespace Prototype.Utility;

public static class DataSourceCredentialMatrix
{
    public static readonly Dictionary<DataSourceTypeEnum, CredentialTypeEnum[]> SupportedCredentials =
        new()
        {
            // Relational DBs
            { DataSourceTypeEnum.SqlServer, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.WindowsAuth, CredentialTypeEnum.ManagedIdentity, CredentialTypeEnum.Kerberos, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.MySql, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Anonymous } },
            { DataSourceTypeEnum.PostgreSql, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Kerberos } },
            { DataSourceTypeEnum.Oracle, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.Certificate, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Kerberos } },
            { DataSourceTypeEnum.Sqlite, new[] { CredentialTypeEnum.Anonymous } },

            // NoSQL/Cloud DBs
            { DataSourceTypeEnum.MongoDb, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Certificate, CredentialTypeEnum.Anonymous } },
            { DataSourceTypeEnum.CosmosDb, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.ApiKey, CredentialTypeEnum.ManagedIdentity, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.DynamoDb, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.ManagedIdentity, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.Cassandra, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Certificate, CredentialTypeEnum.Kerberos } },
            { DataSourceTypeEnum.Elasticsearch, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.ApiKey, CredentialTypeEnum.BearerToken, CredentialTypeEnum.Kerberos, CredentialTypeEnum.Saml } },

            // Big Data/Analytics
            { DataSourceTypeEnum.Snowflake, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Certificate, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.BigQuery, new[] { CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.Firestore, new[] { CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.Saml } },

            // File & Object Storage
            { DataSourceTypeEnum.AzureBlobStorage, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.ManagedIdentity, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.AmazonS3, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.GoogleCloudStorage, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.ServiceAccount, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.Sftp, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.SshKey } },
            { DataSourceTypeEnum.Ftp, new[] { CredentialTypeEnum.UsernamePassword } },

            // APIs
            { DataSourceTypeEnum.RestApi, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.BearerToken, CredentialTypeEnum.OAuth, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.GraphQl, new[] { CredentialTypeEnum.ApiKey, CredentialTypeEnum.BearerToken, CredentialTypeEnum.OAuth, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Saml } },

            // Generic connectors
            { DataSourceTypeEnum.Odbc, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Kerberos, CredentialTypeEnum.Saml } },
            { DataSourceTypeEnum.OleDb, new[] { CredentialTypeEnum.UsernamePassword, CredentialTypeEnum.Anonymous, CredentialTypeEnum.Kerberos, CredentialTypeEnum.Saml } },
        };
    
    public static CredentialTypeEnum[] GetSupportedCredentialTypes(DataSourceTypeEnum dataSourceTypeEnum) =>
        SupportedCredentials.TryGetValue(dataSourceTypeEnum, out var creds) ? creds : new CredentialTypeEnum[] { };
}