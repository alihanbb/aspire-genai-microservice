using Aspire.Hosting.Azure;
using AspireApps.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureApplicationInsightsResource>? appInsights = null;
if (builder.ExecutionContext.IsPublishMode)
{
    var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("loganalytics");
    appInsights = builder.AddAzureApplicationInsights("appinsights", logAnalytics);
}
 
var postgresKeycloakUser = builder.AddParameter("POSTGRES-KEYCLOAK-USER");
var postgresKeycloakPassword = builder.AddParameter("POSTGRES-KEYCLOAK-PASSWORD", secret: true);

var postgresKeycloak = builder
    .AddPostgres("postgres-keycloak", postgresKeycloakUser, postgresKeycloakPassword, 5433)
    .WithImage("postgres", "16.2")
    .WithVolume("keycloak_postgres_volume", "/var/lib/postgresql/data")
    .WithLifetime(ContainerLifetime.Persistent);

var keycloakDb = postgresKeycloak.AddDatabase("keycloakdb");

var keycloak = builder
    .AddContainer("keycloak", "quay.io/keycloak/keycloak", "25.0")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "password")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL", "jdbc:postgresql://postgres-keycloak:5432/keycloakdb")
    .WithEnvironment("KC_DB_USERNAME", postgresKeycloakUser)
    .WithEnvironment("KC_DB_PASSWORD", postgresKeycloakPassword)
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithArgs("start-dev")
    .WaitFor(keycloakDb)
    .WithHttpEndpoint(port: 9090, targetPort: 8080, name: "keycloak-http")
    .WithLifetime(ContainerLifetime.Persistent);

var keycloakEndpoint = keycloak.GetEndpoint("keycloak-http");

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("postgres-primary-data")
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var postgresRead = builder
    .AddPostgres("postgres-read")
    .WithDataVolume("postgres-read-data")
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDbRead = postgresRead.AddDatabase("catalogdb-read");

var redisMaster = builder
    .AddRedis("redis-master")
    .WithRedisInsight()
    .WithDataVolume("redis-master-data")
    .WithLifetime(ContainerLifetime.Persistent);

var redisReplica1 = builder
    .AddContainer("redis-replica-1", "redis", "7.4-alpine")
    .WithArgs("redis-server", "--replicaof", "redis-master", "6379")
    .WithEndpoint(port: 6380, targetPort: 6379, name: "redis-replica-1")
    .WithVolume("redis-replica-1-data", "/data")
    .WaitFor(redisMaster);

var redisReplica2 = builder
    .AddContainer("redis-replica-2", "redis", "7.4-alpine")
    .WithArgs("redis-server", "--replicaof", "redis-master", "6379")
    .WithEndpoint(port: 6381, targetPort: 6379, name: "redis-replica-2")
    .WithVolume("redis-replica-2-data", "/data")
    .WaitFor(redisMaster);

var sentinel1 = builder
    .AddContainer("redis-sentinel-1", "redis", "7.4-alpine")
    .WithBindMount("./config/sentinel.conf", "/etc/redis/sentinel.conf")
    .WithArgs("redis-sentinel", "/etc/redis/sentinel.conf")
    .WithEndpoint(port: 26379, targetPort: 26379, name: "sentinel-1")
    .WaitFor(redisMaster)
    .WaitFor(redisReplica1)
    .WaitFor(redisReplica2);

var sentinel2 = builder
    .AddContainer("redis-sentinel-2", "redis", "7.4-alpine")
    .WithBindMount("./config/sentinel.conf", "/etc/redis/sentinel.conf")
    .WithArgs("redis-sentinel", "/etc/redis/sentinel.conf")
    .WithEndpoint(port: 26380, targetPort: 26379, name: "sentinel-2")
    .WaitFor(redisMaster)
    .WaitFor(redisReplica1)
    .WaitFor(redisReplica2);

var sentinel3 = builder
    .AddContainer("redis-sentinel-3", "redis", "7.4-alpine")
    .WithBindMount("./config/sentinel.conf", "/etc/redis/sentinel.conf")
    .WithArgs("redis-sentinel", "/etc/redis/sentinel.conf")
    .WithEndpoint(port: 26381, targetPort: 26379, name: "sentinel-3")
    .WaitFor(redisMaster)
    .WaitFor(redisReplica1)
    .WaitFor(redisReplica2);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);



var ollama = builder
    .AddOllama("ollama", 11434)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOpenWebUI();

var llma = ollama.AddModel("llama3-2", "llama3.2");

var embedding = ollama.AddModel("all-minilm");


var apiServiceBuilder = builder
    .AddProject<Projects.AspireApps_CatalogService>("aspireapps-catalogservice")
    .WithReference(catalogDb)
    .WithReference(catalogDbRead)
    .WithReference(rabbitmq)
    .WithReference(llma)
    .WithReference(embedding)   
    .WaitFor(catalogDb)
    .WaitFor(catalogDbRead)
    .WaitFor(rabbitmq)
    .WaitFor(llma)
    .WaitFor(embedding)
    .WithAppInsights(appInsights);

var basketServiceBuilder = builder
    .AddProject<Projects.AspireApps_BasketService>("aspireapps-basketservice")
    .WithReference(redisMaster)
    .WithReference(rabbitmq)
    .WithReference(apiServiceBuilder)
    .WithEnvironment("Keycloak__auth-server-url", keycloakEndpoint)
    .WithEnvironment("Keycloak__realm", "eshop")
    .WithEnvironment("Keycloak__resource", "basket-api")
    .WithEnvironment("Keycloak__ssl-required", "none")
    .WithEnvironment("Keycloak__verify-token-audience", "true")
    .WaitFor(redisMaster)
    .WaitFor(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(apiServiceBuilder)
    .WithAppInsights(appInsights);


var webFrontendBuilder = builder
    .AddProject<Projects.AspireApps_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(redisMaster)
    .WithReference(apiServiceBuilder)
    .WaitFor(apiServiceBuilder)
    .WithReference(basketServiceBuilder)
    .WaitFor(basketServiceBuilder)
    .WithAppInsights(appInsights);

builder.Build().Run();
