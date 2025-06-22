namespace Prototype.Enum;

public enum DataSourceTypeEnum
{
    // Database connections
    MicrosoftSqlServer,
    MySql,
    PostgreSql,
    MongoDb,
    Redis,
    Oracle,
    MariaDb,
    Sqlite,
    Cassandra,
    ElasticSearch,
    
    // API connections
    RestApi,
    GraphQL,
    SoapApi,
    ODataApi,
    WebSocket,
    
    // File-based connections
    CsvFile,
    JsonFile,
    XmlFile,
    ExcelFile,
    ParquetFile,
    YamlFile,
    TextFile,
    
    // Cloud storage
    AzureBlobStorage,
    AmazonS3,
    GoogleCloudStorage,
    
    // Message queues
    RabbitMQ,
    ApacheKafka,
    AzureServiceBus
}