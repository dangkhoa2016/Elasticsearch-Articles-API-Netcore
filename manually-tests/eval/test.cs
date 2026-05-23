#r "nuget: Newtonsoft.Json, 13.0.4"
using Newtonsoft.Json;


Console.WriteLine("Hello World");

var product = new { Name = "Apple", ExpiryDate = new DateTime(2008, 12, 28) };
string json = JsonConvert.SerializeObject(product);
Console.WriteLine(json);

