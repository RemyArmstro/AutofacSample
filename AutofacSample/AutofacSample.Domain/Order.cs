using AutofacSample.Abstractions;

namespace AutofacSample.Domain
{
    public class Order : IOrder
    {
        public string Name { get; set; }
    }
}