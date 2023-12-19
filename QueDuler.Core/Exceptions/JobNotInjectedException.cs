using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueDuler.Core.Exceptions;

public class JobNotInjectedException : Exception
{
    public JobNotInjectedException(string jobName) : base($"job FullName: {jobName} is not injected! be sure that you used the method \"AddQueduler()\" to inject the job assembly into di container, and also chosen the right assemblies")
    {

    }
}
