using System;
using Volo.Abp.DependencyInjection;

namespace SwapExchange
{
    public class HelloWorldService : ITransientDependency
    {
        public void SayHello()
        {
            Console.WriteLine("\tHello World!");
        }
    }
}
