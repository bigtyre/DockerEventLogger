
Console.WriteLine("Hello World");

/*
using Microsoft.Extensions.Configuration;
var configBuilder = new ConfigurationBuilder();
configBuilder.AddUserSecrets<Program>();
var config = configBuilder.Build();

var values = config.AsEnumerable();
var numConfigValues = values.Count();
Console.WriteLine($"Configured values: {numConfigValues}");

foreach (var configValue in values)
{
    var key = configValue.Key;
    var value = configValue.Value;
    Console.WriteLine($"{key} = {value}");
}

*/