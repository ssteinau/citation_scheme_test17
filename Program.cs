using Recite.Core.Citations;

var doc = ExampleData.SampleDocument();

foreach (var template in ExampleData.Templates)
    Console.WriteLine($"{template,-24} -> {new CitationKeyScheme(template).BaseKey(doc)}");
