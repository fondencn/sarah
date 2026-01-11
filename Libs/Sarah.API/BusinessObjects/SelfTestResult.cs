using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.API.BusinessObjects
{
    public class SelfTestResult
    {
        public bool IsError { get; private set; }
        public string Message { get; private set; }
        public string Name { get; private set; }

        public SelfTestResult(bool isError, string name, string message)
        {
            this.IsError = isError;
            this.Message = message;
            this.Name = name;
        }
    }
}
