namespace Instrument.Execution.Grpc;

using System;
using System.Collections.Generic;
using Instrument.Execution.Common.Parameter;
using Instrument.Execution.Grpc.Parameters;
using Instrument.Execution.Parameter;

public static class DataContractExtensions
{
    public static ICollection<ISequenceParameterValue> ToSequenceParameterCollection(this ParameterValueContract[] contract)
    {
        var collection = new List<ISequenceParameterValue>();
        //  although nullable aware the gRPC will not set the param value and it will be null.
        if (contract == null)
        {
            return Array.Empty<ISequenceParameterValue>();
        }

        foreach (var parameter in contract)
        {
            collection.Add(parameter.ToSequenceParameter());
        }
        return collection;
    }
    public static ISequenceParameterValue ToSequenceParameter(this ParameterValueContract contract)
    {

        return contract switch
        {
            StringParameterValueContract pvc => new StringSequenceParameterValue(contract.Name, pvc.Value),
            DecimalParameterValueContract pvc => new DecimalSequenceParameterValue(contract.Name, pvc.Value),
            IntegerParameterValueContract pvc => new IntegerSequenceParameterValue(contract.Name, pvc.Value),
            BooleanParameterValueContract pvc => new BooleanSequenceParameterValue(contract.Name, pvc.Value),
            DecimalArrayParameterValueContract pvc => new ArraySequenceParameterValue<decimal>(contract.Name, pvc.Value),
            StringArrayParameterValueContract pvc => new ArraySequenceParameterValue<string>(contract.Name, pvc.Value),
            IntArrayParameterValueContract pvc => new ArraySequenceParameterValue<int>(contract.Name, pvc.Value),
            BooleanArrayParameterValueContract pvc => new ArraySequenceParameterValue<bool>(contract.Name, pvc.Value),

            _ => throw new Exception("Unsupported contract type")
        };

    }
}