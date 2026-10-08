namespace WinClean.Core.Monitoring.Parsers;

public readonly record struct CounterArrayItem(string Instance, uint Status, double Value);
