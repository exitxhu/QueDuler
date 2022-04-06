﻿// See https://aka.ms/new-console-template for more information
using System.Text.Json;

public abstract class BaseArgument
{
    public abstract string ToJson();
}
public class DispatchableJobArgument : BaseArgument
{
    public DispatchableJobArgument(string jobId, object argumentObject = null)
    {
        JobId = jobId;
        ArgumentObject = argumentObject;
    }
    public string JobId { get; }
    public object ArgumentObject { get; }
    public static bool TryParse(string json, out DispatchableJobArgument argument)
    {
        try
        {
            argument = JsonSerializer.Deserialize<DispatchableJobArgument>(json);
            return true;
        }
        catch (Exception)
        {
            argument = null;
            return false;
        }
    }

    public override string ToJson() => JsonSerializer.Serialize(this);
}
